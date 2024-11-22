using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using APR.SimhubPlugins.Models;

using static iRacingSDK.SessionData._DriverInfo;
using System.Globalization;
using System.Runtime;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;

namespace APR.SimhubPlugins.Data {
    internal class Session : IDisposable  {
        private OpponentsSettings Settings;
        public double SessionTime;
        public SessionState CurrentSessionState;
        public SessionState PreviousSessionState;
        public long CurrentSessionID;
        public long PreviousSessionID;
        public double CurrentSessionTick;
        public double PreviousSessionTick;

        public string EventType;
        public string SessionType { get { return EventType; } }

        private TimeDelta _timeDelta;
        private Track _track;


        /* iRacing Session states are
        public enum SessionState {
            Invalid,
            GetInCar,
            Warmup,
            ParadeLaps,
            Racing,
            Checkered,
            CoolDown
        }
        */
        public bool IsCheckered {
            get {
                return CurrentSessionState == SessionState.Checkered;
            }
        }

        DataSampleEx iRacingData;
        GameData data;

        _Drivers iRCameraCar = null;
        Driver Leader = null;
        Driver CameraCar = null;
        int LeaderLap = 0;

        _Drivers[] iRCompetitors;
        _Drivers[] iRDrivers;

        public List<Driver> Drivers = new List<Driver>();
        private List<Driver> _driversAhead = new List<Driver>();
        private List<Driver> _driversBehind = new List<Driver>();

        public List<Driver> DriversAhead {
            get {
                // if the distance is negative they are ahead
                _driversAhead.Clear();
                if (Settings.RelativeShowCarsInPits) {

                    _driversAhead = Drivers.FindAll(a => a.LapDistSpectatedCar < 0 &&
                        !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.IsConnected)
                        .OrderByDescending(a => a.LapDistSpectatedCar).ToList();

//                    _driversAhead = value.FindAll(a => (!a.IsInPitLane || !a.IsInPitStall));
                }
                else {
                    _driversAhead = Drivers.FindAll( a => a.LapDistSpectatedCar < 0 &&
                        (!a.IsInPitLane || !a.IsInPitStall) &&
                        !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.IsConnected)
                        .OrderByDescending(a => a.LapDistSpectatedCar).ToList();
                }
                return _driversAhead;

            }
        }

        public List<Driver> DriversBehind {
            get {
                // if the distance is positive they are behind
                _driversBehind.Clear();
                if (Settings.RelativeShowCarsInPits) {

                    _driversBehind = Drivers.FindAll(
                        a => a.LapDistSpectatedCar < 0 &&
                        a.IsConnected)
                        .OrderBy(a => a.LapDistSpectatedCar).ToList();
                }
                else {
                    _driversBehind = Drivers.FindAll(a => a.LapDistSpectatedCar > 0 &&
                        (!a.IsInPitLane || !a.IsInPitStall) &&
                        !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.IsConnected)
                        .OrderBy(a => a.LapDistSpectatedCar).ToList();
                }
                return _driversBehind;
            }
        }
        public List<CarClass> CarClasses = new List<CarClass>();
        public Relatives Relative = new Relatives();

        internal void CheckAndAddCarClass(long CarClassID, string CarClassShortName, string CarClassColor, string CarClassTextColor) {
            bool has = this.CarClasses.Any(a => a.CarClassID == CarClassID);

            if (has == false && CarClassID != 0) {
                this.CarClasses.Add(new CarClass() {
                    CarClassID = CarClassID,
                    carClassShortName = CarClassShortName,
                    carClassColor = CarClassColor,
                    carClassTextColor = CarClassTextColor
                });
            }
        }

        public string Description;

        public Session(ref OpponentsSettings settings, ref GameData shData, ref DataSampleEx irData) {
            this.Settings = settings;
            this.iRacingData = irData;
        }

        public void GetSessionData() {

            // Get the iRacing Session Details
            SessionTime = iRacingData.Telemetry.SessionTime;
            EventType = iRacingData.SessionData.WeekendInfo.EventType;
            CurrentSessionState = iRacingData.Telemetry.SessionState;
            CurrentSessionID = iRacingData.SessionData.WeekendInfo.SessionID;
            _track = Track.FromSessionInfo(iRacingData.SessionData.WeekendInfo,iRacingData.SessionData.SplitTimeInfo);
            _timeDelta = new TimeDelta((float)_track.Length * 1000f, 20, 64);

        }

        public void GetGameData() {

            // Get the current camera car. This will be the player or car being observed
            iRCameraCar = iRacingData.SessionData.DriverInfo.Drivers.SingleOrDefault(x => x.CarIdx == iRacingData.Telemetry.CamCarIdx);
            CameraCar = new Driver(ref data, ref iRacingData, iRCameraCar);

            iRCompetitors = iRacingData.SessionData.DriverInfo.CompetingDrivers;
            iRDrivers = iRacingData.SessionData.DriverInfo.Drivers;
           
            // Update the car classes
            foreach (_Drivers competitor in iRCompetitors) {
                var newDriver = new Driver(ref data, ref iRacingData, competitor);
                Drivers.Add(newDriver);
                CheckAndAddCarClass(newDriver.CarClassID, newDriver.CarClass, newDriver.CarClassColor, newDriver.CarClassTextColor);
            }

            // Update the reference lap time for each class
            foreach (var item in CarClasses)
            {
                item.UpdateReferenceClassLaptime(Drivers);
            }

            var bob = Leader;

            // Find the overall leader for each class and update their total time
            foreach (var item in CarClasses) {
                List<Driver> classbyPosition = Drivers.FindAll(a => a.CarClassID == item.CarClassID && !a.IsSpectator).OrderBy(a => a.Position).ToList();

                Driver classLeader = classbyPosition[0];
                item.LeaderTotalTime = (classLeader.CurrentLap * item.ReferenceLapTime) + (item.ReferenceLapTime * classLeader.TrackPositionPercent);
            }

            CalculateLivePositions();
            
            UpdateTimeDelta();
            Relative.Update(ref Drivers, CameraCar);

        }

        private void CalculateLivePositions() {
            // In a race that is not yet in checkered flag mode,
            // Live positions are determined from track position (total lap distance)
            // Any other conditions (race finished, P, Q, etc), positions are ordered as result positions

            if (EventType == "Race" && !IsCheckered) {
                // Determine live position from lapdistance
                int pos = 1;
                foreach (var driver in Drivers.OrderByDescending(d => d.TotalLapDistance)) {
                    if (!driver.IsPaceCar) {
                        if (pos == 1)
                            Leader = driver;
                        driver.LivePosition = pos;
                        pos++;
                    } 
                }
            }
            else {
                // In P or Q, set live position from result position (== best lap according to iRacing)
                foreach (var driver in Drivers.OrderBy(d => d.Position)) {
                    if (!driver.IsPaceCar) {
                        if (this.Leader == null)
                            Leader = driver;
                        driver.LivePosition = driver.Position;
                    }
                }
            }

            // Determine live class position from live positions and class
            // Group drivers in dictionary with key = classid and value = list of all drivers in that class
            var dict = (from driver in Drivers
                        group driver by driver.CarClassID)
                .ToDictionary(d => d.Key, d => d.ToList());

            // Set class position
            foreach (var drivers in dict.Values) {
                var pos = 1;
                foreach (var driver in drivers.OrderBy(d => d.LivePosition)) {
                    if (!driver.IsPaceCar) {
                        driver.ClassLivePosition = pos;
                        pos++;
                    }
                }
            }

            //if (this.Leader != null && this.Leader.CurrentResults != null)
            if (this.Leader != null )
                LeaderLap = Leader.LapsComplete + 1;
        }

        // Matches simhub getplayerleaderboardposition()
        // But we return cameracar Idx not the player :-)
        public int GetPlayerLeaderboardPosition() {
            return  iRacingData.Telemetry.CarIdxPosition[iRacingData.Telemetry.CamCarIdx];
        }

        // Matches simhub getplayerleaderboardposition()
        // But we return cameracar Idx not the player :-)
        public int GetPlayerClassLeaderboardPosition() {
            return iRacingData.Telemetry.CarIdxClassPosition[iRacingData.Telemetry.CamCarIdx];
        }

        private void UpdateTimeDelta() {


        }

        private void UpdateTimeDelta_old() {
            if (_timeDelta == null) return;

            // Update the positions of all cars
            _timeDelta.Update(SessionTime, iRacingData.Telemetry.CarIdxLapDistPct);

            // Order drivers by live position
            var drivers = Drivers
                .Where(x => (
                    !String.IsNullOrEmpty(x.Name) &&
                    x.TotalLapDistance > 0
                ))
                .OrderBy(d => d.TotalLapDistance).ToList();


            if (drivers.Count > 0) {

                // Get leader
                var leader = drivers[0];
                Leader.GapToLeader = "-";
                Leader.GapToNext = "-";

                // Loop through drivers
                for (int i = 1; i < drivers.Count; i++) {
                    var behind = drivers[i];
                    var ahead = drivers[i - 1];

                    // Lapped?
                    var leaderLapDiff = Math.Abs(this.Leader.TotalLapDistance - behind.TotalLapDistance);
                    var nextLapDiff = Math.Abs(ahead.TotalLapDistance - behind.TotalLapDistance);

                    if (leaderLapDiff < 1) {
                        var leaderDelta = _timeDelta.GetDelta(behind.Id, Leader.Id);
                        behind.GapToLeader = TimeDelta.DeltaToString(leaderDelta);
                    }
                    else {
                        behind.GapToLeader = Math.Floor(leaderLapDiff) + " L";
                    }

                    if (nextLapDiff < 1) {
                        var nextDelta = _timeDelta.GetDelta(behind.Id, ahead.Id);
                        behind.GapToNext = TimeDelta.DeltaToString(nextDelta);
                    }
                    else {
                        behind.GapToNext = Math.Floor(nextLapDiff) + " L";
                    }
                }
            }
        }
  

        protected virtual void Dispose(bool disposing) {
        }
        #region Interface: IDisposable

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }

}
