using APR.SimhubPlugins.Models;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.SimhubPlugins.Data {

    internal class Relatives {
        private RelativeTable _relativeTable = new RelativeTable();
        private List<Driver> _relativePositions = new List<Driver>();
        public List<Driver> Ahead = new List<Driver>();
        public List<Driver> Behind = new List<Driver>();

        public void Update(ref List<Driver> drivers, Driver CameraCar) {
            Clear();

            List<Driver> sortedDrivers = drivers
                .Where(x => (
                    !String.IsNullOrEmpty(x.Name) &&
                    x.TotalLapDistance > 0 &&
                    x.IsConnected
                ))
                .OrderBy(x => x.TotalLapDistance).ToList();

            for (int i = 0; i < sortedDrivers.Count; i++) {
                double simpleGapTime = (sortedDrivers[i].EstTime - CameraCar.EstTime);
                //string simpleGapTimeString = TimeToStr_ms(simpleGapTime, 1);
                int aheadBehind = DetermineIfLapAheadBedhind(sortedDrivers[i], CameraCar);

                _relativeTable.Add(
                    sortedDrivers[i].CarIdx,
                    sortedDrivers[i].Position,
                    sortedDrivers[i].CarNumber,
                    sortedDrivers[i].Name,
                    simpleGapTime,
                    aheadBehind
                );
            }

            // we loop the time around based on if the driver is 'in front' or not
            // technically we are on a loop so no one is in front or behind, so we take half the cars
            // and mark them in front, and half marked as behind.
            // we do this by adding or subtracting g_sessionObj.DriverInfo.DriverCarEstLapTime from time
            foreach (var item in _relativeTable.Get()) {
                var car = drivers.Find(a => a.CarIdx == item.CarIdx);

                double refLapTime;
                // If we have the iRacing EstTime, use it
                if (car.EstTime > 0) {
                    refLapTime = car.EstTime;
                }
                else {
                    // Need to check this calc of our own EstTime
                    refLapTime = car.ClassReferenceLapTime.TotalSeconds * car.TrackPositionPercent;
                    //refLapTime = car.car GetReferenceClassLaptime(drivers, car.CarClassID);
                }

                // if the gap is more than 50 of a lap ahead, they are actually behind
                if (item.simpleRelativeGapToSpectator > refLapTime / 2) {

                    // this just changes the sign from + to -
                    item.sortingRelativeGapToSpectator = -(item.simpleRelativeGapToSpectator * 2);
                }

                // if the gap is more than 50 of a lap behind, they are actually ahead
                else if (item.simpleRelativeGapToSpectator < refLapTime / 2) {
                    // this just changes the sign from - to +
                    item.sortingRelativeGapToSpectator = +(item.simpleRelativeGapToSpectator * 2);
                }
                else {
                    item.sortingRelativeGapToSpectator = item.simpleRelativeGapToSpectator;
                }
            }

            foreach (var rel in _relativeTable.Get()) {
                Driver dvr = drivers.Find(x => x.CarIdx == rel.CarIdx);
                dvr.GapToPlayer = rel.RelativeGapString;
            }


           
        }

        public void Clear() {
            _relativeTable.Clear();
            _relativePositions.Clear();
            Ahead.Clear();
            Behind.Clear();
        }

        public void Add(Driver item) {
            _relativePositions.Add(item);
        }

        public RelativeTable Get() {
            return _relativeTable;
        }

        private string TimeToStr_ms(double time, int precision) {
            // Convert time to a formatted string
            return time.ToString($"F{precision}");
        }

        private int DetermineIfLapAheadBedhind(Driver target, Driver spectator) {
            if (target.CarIdx == spectator.CarIdx) {
                return 0; // car is spectator / player
            }
            else if (target.CurrentLap > spectator.CurrentLap) {
                return 1; // Lapping you
            }
            else if (target.CurrentLap == spectator.CurrentLap) {
                return 0; // Same lap as you
            }
            else {
                return -1; // Being lapped by you
            }
        }

        private string DetermineLapColor(int AheadOrBehind, bool pitRoad) {

            if (AheadOrBehind == 1) {
                return pitRoad ? "#7F1818" : "#FE3030"; // Lapping you
            }
            else if (AheadOrBehind == -1) {
                return pitRoad ? "#00607F" : "#00C0FF"; // Being lapped by you
            }
            else {
                return pitRoad ? "#7F7F7F" : "#FFFFFF"; // Same lap as you
            }
        }

        private string DetermineColor(int i, int playerCarIdx, int lap, int playerLap, bool pitRoad) {
            if (i == playerCarIdx) {
                return "#FFB923"; // Player car DriverNameColor
            }
            else if (lap > playerLap) {
                return pitRoad ? "#7F1818" : "#FE3030"; // Lapping you
            }
            else if (lap == playerLap) {
                return pitRoad ? "#7F7F7F" : "#FFFFFF"; // Same lap as you
            }
            else {
                return pitRoad ? "#00607F" : "#00C0FF"; // Being lapped by you
            }
        }

    }
    public class RelativeTable {
        public static List<RelativePosition> _relativePositions = new List<RelativePosition>();

        public void Clear() {
            _relativePositions.Clear();
        }

        public List<RelativePosition> Get() {
            return _relativePositions;
        }

        public RelativePosition Get(long carIdx) {
            return _relativePositions.Find(x => x.CarIdx == carIdx) ?? new RelativePosition();
        }


        public void Add(long carIdx, int racePos, string carNumberString, string nameStr, double simpleRelativeGapToSpectator, int aheadBehind) {

            _relativePositions.Add(new RelativePosition() {
                CarIdx = carIdx,
                RacePosition = racePos,
                CarNumberString = carNumberString,
                DriverName = nameStr,
                simpleRelativeGapToSpectator = simpleRelativeGapToSpectator,
                AheadOrBehind = aheadBehind
            });
        }


    }

    public class RelativePosition {
        public long CarIdx;
        public int RacePosition;
        public string CarNumberString;
        public double sortingRelativeGapToSpectator;
        public double simpleRelativeGapToSpectator;
        public string RelativeGapString {
            get {
                return simpleRelativeGapToSpectator.ToString($"0.0");
            }
        }

        public string DriverName;
        public string DriverNameColor;
        //public string RelativeGapString;
        
        public int AheadOrBehind; // 1 = ahead, 0 = ignore, -1 = behind

        public override string ToString() {
            return $"{DriverName} p: {RacePosition} {RelativeGapString}";
        }
    }
}
