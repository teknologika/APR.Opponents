using APR.SimhubPlugins.Models;
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


        public void Update(List<Driver> drivers, Driver CameraCar) {
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
                var car = drivers.Find(a => a.CarIdx == item.carIdx);

                double refLapTime;
                if (car.EstTime > 0) {
                    refLapTime = car.EstTime;
                }
                else {
                    refLapTime = GetReferenceClassLaptime(drivers, car.CarClassID);
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
        }

        private double GetReferenceClassLaptime(List<Driver> drivers, long CarClassID) {
            List<Driver> driversInClass = drivers.FindAll(a => a.CarClassID == CarClassID);

            double averageLapTime = 0;
            int count = 0;
            foreach (var item in driversInClass) {
                double LastLapTimeSeconds = item.LastLapTime.TotalSeconds;
                double BestLapTimeSeconds = item.BestLapTime.TotalSeconds;

                // use the  last lap time
                if (LastLapTimeSeconds > 0 &&
                        (LastLapTimeSeconds < (LastLapTimeSeconds * 1.05)) &&
                        (LastLapTimeSeconds > (LastLapTimeSeconds * 0.95))) {
                    averageLapTime += LastLapTimeSeconds;
                    count++;
                }
                // if the last lap time is empty, try and use the best
                else if (BestLapTimeSeconds > 0 &&
                        (BestLapTimeSeconds < (BestLapTimeSeconds * 1.05)) &&
                        (BestLapTimeSeconds > (BestLapTimeSeconds * 0.95))) {
                    averageLapTime += BestLapTimeSeconds;
                    count++;

                }
            }
            if (count > 0) {
                averageLapTime = averageLapTime / count;
            }
            // if no time, just use 2 mins
            if (averageLapTime == 0) {
                return 120.0;
            }
            return averageLapTime;
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
                return "#FFB923"; // Player car color
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
        private static List<RelativePosition> _relativePositions = new List<RelativePosition>();

        public void Clear() {
            _relativePositions.Clear();
        }

        public List<RelativePosition> Get() {
            return _relativePositions;
        }


        public void Add(long carIdx, int racePos, string carNumberString, string nameStr, double simpleRelativeGapToSpectator, int aheadBehind) {

            _relativePositions.Add(new RelativePosition() {
                carIdx = carIdx,
                racePosition = racePos,
                carNumberString = carNumberString,
                nameStr = nameStr,
                simpleRelativeGapToSpectator = simpleRelativeGapToSpectator,
                aheadBehind = aheadBehind
            });
        }


    }

    public class RelativePosition {
        public long carIdx;
        public int racePosition;
        public string carNumberString;
        public double sortingRelativeGapToSpectator;
        public double simpleRelativeGapToSpectator;
        public string simpleRelativeGapToSpectatorString {
            get {
                return simpleRelativeGapToSpectator.ToString($"0.0");
            }
        }

        public string nameStr;
        public string relGapStr;
        public string color;
        public int aheadBehind; // 1 = ahead, 0 = ignore, -1 = behind

        public override string ToString() {
            return $"{nameStr} p: {racePosition} {relGapStr}";
        }
    }
}
