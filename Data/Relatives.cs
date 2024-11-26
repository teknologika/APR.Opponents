using APR.SimhubPlugins.Models;
using IRacingReader;
using MahApps.Metro.Controls;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static iRacingSDK.SessionData._DriverInfo;

namespace APR.SimhubPlugins.Data {

    internal class Relatives {
        private RelativeTable _relativeTable = new RelativeTable();
        private List<Driver> _relativePositions = new List<Driver>();
        public List<Driver> Ahead = new List<Driver>();
        public List<Driver> Behind = new List<Driver>();

        public void Update(ref DataSampleEx irData, ref List<CarClass> carClasses, ref List<Driver> drivers, Driver CameraCar) {
            Clear();

            float[] bestLapTimes = (float[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxBestLapTime").Value;
            float[] lastLapTimes = (float[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxLastLapTime").Value;


            // update the driver data
            /*
            foreach (var driver in drivers)
            {
                driver.EstTime = irData.Telemetry.CarIdxEstTime[driver.CarIdx];
                driver.BestLapTime = TimeSpan.FromSeconds(bestLapTimes[driver.CarIdx]);
                driver.LastLapTime = TimeSpan.FromSeconds(lastLapTimes[driver.CarIdx]);
            }
            */

            // update car reference lap time
            foreach (var driver in drivers) {
                if (driver.Name != "") {
                    TimeSpan carClassTime = TimeSpan.FromSeconds(carClasses.Find(c => c.CarClassID == driver.CarClassID).ReferenceLapTime);
                    driver.ClassReferenceLapTime = carClassTime;
                }
            }

            // Get the opponents in pitlane
            //_opponentsInPitlane = OpponentsExtended.FindAll(a => a.IsCarInPitLane == true);
            //_opponentsInPitBox = OpponentsExtended.FindAll(a => a.IsCarInPitBox == true);

            List<Driver> sortedDrivers = SortInWorldDriversByTrackPct(drivers);

            long cameraIdx = CameraCar.CarIdx;
            double cameraTime = cameraIdx >= 0 ? CameraCar.EstTime : 0;
            double cameraLap = cameraIdx >= 0 ? CameraCar.CurrentLap : 0;


            for (int i = 0; i < sortedDrivers.Count; i++) {

                double remoteTime = sortedDrivers[i].EstTime;
                double simpleGapTime = (remoteTime - cameraTime);
                int aheadBehind = DetermineIfLapAheadBedhind(sortedDrivers[i], CameraCar);
                if (sortedDrivers[i].Name != "") {
                    _relativeTable.Add(
                        sortedDrivers[i].CarIdx,
                        sortedDrivers[i].Position,
                        sortedDrivers[i].CarNumber,
                        sortedDrivers[i].Name,
                        simpleGapTime,
                        aheadBehind
                    );
                }
            }

            

            // need to loop the time around based on if the driver is 'in front' or not
            // technically we are on a loop so no one is in front or behind, so we take half the cars
            // and mark them in front, and half marked as behind.
            // we do this by adding or subtracting g_sessionObj.DriverInfo.DriverCarEstLapTime from time

            foreach (var item in _relativeTable.Get()) {
                var car = drivers.Find(a => a.CarIdx == item.CarIdx);

                double refLapTime;
                if (car.EstTime > 0) {
                    refLapTime = car.EstTime;
                }
                else {
                    refLapTime = car.ClassReferenceLapTime.TotalSeconds;
                }
                // if the gap is more than 50 of a lap ahead, they are actually behind
                if (item.simpleRelativeGapToCameraCar > refLapTime / 2) {

                    // this just changes the sign from + to -
                    item.sortingRelativeGapToSpectator = -(item.simpleRelativeGapToCameraCar * 2);
                }

                // if the gap is more than 50 of a lap behind, they are actually ahead
                else if (item.simpleRelativeGapToCameraCar < refLapTime / 2) {
                    // this just changes the sign from - to +
                    item.sortingRelativeGapToSpectator = +(item.simpleRelativeGapToCameraCar * 2);
                }
                else {
                    item.sortingRelativeGapToSpectator = item.simpleRelativeGapToCameraCar;
                }
            }
        
            // Now we have the relative table loop through and get the cars ahead and behind
            var tmpAhead = new List<Driver>();
            var tmpBehind = new List<Driver>();
            foreach (var item in _relativeTable.Get()) {
                // Add the cars ahead
                
                if (item.simpleRelativeGapToCameraCar > 0) {
                    var aheadCar = drivers.Find(a => a.CarIdx == item.CarIdx);
                    //aheadCar.SetGapToCameraCar = item.simpleRelativeGapToCameraCar;
                    //aheadCar.SimpleRelativeGapTimeString = item.simpleRelativeGapToSpectatorString;
                    //aheadCar.SortingRelativeGapToSpectator = item.sortingRelativeGapToSpectator;
                    //aheadCar.AheadBehind = DetermineIfLapAheadBedhind(aheadCar, cameraCar);
                   // aheadCar.AheadBehind = 1;
                    tmpAhead.Add(aheadCar);
                    // to do need to sort in reverse
                }

                // add the cars behind
                else if (item.simpleRelativeGapToCameraCar < 0) {
                    var behindCar = drivers.Find(a => a.CarIdx == item.CarIdx);
                    //behindCar.SetGapToCameraCar = item.simpleRelativeGapToCameraCar;
                    // behindCar.SimpleRelativeGapTimeString = item.simpleRelativeGapToSpectatorString;
                    // behindCar.SortingRelativeGapToSpectator = item.sortingRelativeGapToSpectator;
                    //behindCar.AheadBehind = DetermineIfLapAheadBedhind(behindCar, cameraCar);
                    //aheadCar.AheadBehind = -1;
                    tmpBehind.Add(behindCar);
                }
            }
            Ahead.Clear();
            Behind.Clear();
            Ahead = tmpAhead.OrderBy(x => x.LapDistToCameraCar).ToList();
            Behind = tmpBehind.OrderByDescending(x => x.LapDistToCameraCar).ToList();

            foreach (var item in drivers) {
                //item.SetGapToCameraCar = 0;
            }

            // Push the gaps to camera car into the drivers
            //foreach (var rel in _relativeTable.Get()) {
               // drivers.Find(x => x.CarIdx == rel.CarIdx).SetGapToCameraCar = rel.simpleRelativeGapToCameraCar;
             //   drivers.Find(x => x.CarIdx == rel.CarIdx).SetCameraCarTrackDistancePercent = CameraCar.TrackPositionPercent;
//            }

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

        public List<Driver> SortInWorldDriversByTrackPct(List<Driver> opponents) {


            // loop the time around based on if the driver is 'in front' or not
            // technically we are on a loop so no one is in front or behind, so we take half the cars
            // and mark them in front, and half marked as behind.
            // we do this by adding or subtracting g_sessionObj.DriverInfo.DriverCarEstLapTime from time

            foreach (var car in opponents) {


                double refLapTime;
                if (car.EstTime > 0) {
                    refLapTime = car.EstTime;
                }
                else {
                    refLapTime = car.ClassReferenceLapTime.TotalSeconds;
                }

                // if the gap is more than 50 of a lap ahead, they are actually behind
                if (car.GapToCameraCarRaw > refLapTime / 2) {

                    // this just changes the sign from + to -
                    car.SortingRelativeGapToSpectator = -(car.GapToCameraCarRaw * 2);
                }

                // if the gap is more than 50 of a lap behind, they are actually ahead
                else if (car.GapToCameraCarRaw < -(refLapTime / 2)) {
                    // this just changes the sign from - to +
                    car.SortingRelativeGapToSpectator = +(car.GapToCameraCarRaw * 2);
                }
                else {
                    car.SortingRelativeGapToSpectator = car.GapToCameraCarRaw;
                }
            }

            // Ensure the car is in world aka connected
            // Sort by position around track in descending order
            List<Driver> SortedOpponentsInWorld = opponents.FindAll(a => !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.Position > 0 &&
                        a.IsConnected)
                        .OrderByDescending(a => a.TrackPositionPercent)
                        .ToList();

            return SortedOpponentsInWorld;
        }

        private int DetermineIfLapAheadBedhind(Driver target, Driver cameraCar) {
            if (target.CarIdx == cameraCar.CarIdx) {
                return 0; // car is cameraCar / player
            }
            else if (target.CurrentLap > cameraCar.CurrentLap) {
                return 1; // Lapping you
            }
            else if (target.CurrentLap == cameraCar.CurrentLap) {
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
                simpleRelativeGapToCameraCar = simpleRelativeGapToSpectator,
                AheadOrBehind = aheadBehind
            });
        }


    }

    public class RelativePosition {
        public long CarIdx;
        public int RacePosition;
        public string CarNumberString;
        public double sortingRelativeGapToSpectator;
        public double simpleRelativeGapToCameraCar;
        public string RelativeGapString {
            get {
                return simpleRelativeGapToCameraCar.ToString($"0.0");
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
