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
using System.Drawing.Text;
using GameReaderCommon.Replays;
using System.Diagnostics.Eventing.Reader;

namespace APR.SimhubPlugins.Data {
    internal class Session : IDisposable  {
        private PluginSettings Settings;
        public double SessionTime;
        public SessionState CurrentSessionState;
        public SessionState PreviousSessionState;
        public long CurrentSessionID;
        public long PreviousSessionID;
        public double CurrentSessionTick;
        public double PreviousSessionTick;

        public string EventType;
        public string SessionType;

        // This is used for sending messages
        public string DriverAheadId = string.Empty;
        public string DriverAheadName = string.Empty;
        public string DriverBehindId = string.Empty;
        public string DriverBehindName = string.Empty;


        // private TimeDelta _timeDelta;
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

        public bool IsV8VetsSession {
            get {
                return Settings.V8VetsLeagueIDs.Contains(_track.LeagueID);
            }
        }

        DataSampleEx iRacingData;
        GameData data;

        _Drivers iRCameraCar = null;
        Driver Leader = null;
        public Driver CameraCar = null;
        int LeaderLap = 0;

        _Drivers[] iRCompetitors;

        public List<Driver> Drivers = new List<Driver>();
        private List<Driver> _driversAhead = new List<Driver>();
        private List<Driver> _driversBehind = new List<Driver>();

        public List<Driver> DriversAhead {
            get {
                // if the distance is negative they are ahead
                _driversAhead.Clear();
                if (Settings.RelativeShowCarsInPits) {
                    _driversAhead = Drivers.FindAll(a => a.LapDistToCameraCar < 0 &&
                        !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.IsConnected)
                        .OrderByDescending(a => a.LapDistToCameraCar).ToList();
                }
                else {
                    _driversAhead = Drivers.FindAll( a => a.LapDistToCameraCar < 0 &&
                        !a.IsInPitLane &&
                        !a.IsInPitStall &&
                        !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.IsConnected)
                        .OrderByDescending(a => a.LapDistToCameraCar).ToList();
                }

                // this is used for sending messages
                if (_driversAhead.Count > 0) {
                    if (_driversAhead[0].GapToCameraCarRaw < 1.0) {
                        this.DriverAheadId = _driversAhead[0].CarNumber;
                        this.DriverAheadName = _driversAhead[0].Name;
                    }
                    else {
                        this.DriverAheadId = string.Empty;
                        this.DriverAheadName = string.Empty;
                    }
                }
                else {
                    this.DriverAheadId = string.Empty;
                    this.DriverAheadName = string.Empty;
                }
                return _driversAhead;
            }
        }

        public void Reset() {
            iRCameraCar = null;
            Leader = null;
            CameraCar = null;
            LeaderLap = 0;

            Drivers = new List<Driver>();
            _driversAhead = new List<Driver>();
            _driversBehind = new List<Driver>();

        }

        public List<Driver> DriversBehind {
            get {
                // if the distance is positive they are behind
                _driversBehind.Clear();
                if (Settings.RelativeShowCarsInPits) {

                    _driversBehind = Drivers.FindAll(a => a.LapDistToCameraCar > 0 &&
                        !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.IsConnected)
                        .OrderBy(a => a.LapDistToCameraCar).ToList();
                }
                else {
                    _driversBehind = Drivers.FindAll(a => a.LapDistToCameraCar > 0 &&
                        !a.IsInPitLane &&
                        !a.IsInPitStall &&
                        !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.IsConnected)
                        .OrderBy(a => a.LapDistToCameraCar).ToList();
                }

                if (_driversBehind.Count > 0) {
                    if (_driversBehind[0].GapToCameraCarRaw < 1.0) {
                        this.DriverBehindId = _driversBehind[0].CarNumber;
                        this.DriverBehindName = _driversBehind[0].Name;
                    }
                    else {
                        this.DriverBehindId = string.Empty;
                        this.DriverBehindName = string.Empty;
                    }
                }
                else {
                    this.DriverBehindId = string.Empty;
                    this.DriverBehindName = string.Empty;
                }

                return _driversBehind;
            }
        }

        public List<CarClass> CarClasses = new List<CarClass>();
    
        // Saftey car
        public bool IsUnderSC { get; set; }
        public bool IsSafetyCarMovingInPitane { get; set; }
        
        public long SafetyCarIdx;
        public double SafetyCarTrackDistancePercent;
        public double LapDistSafetyCar {
            get {
                var trackLengthM = _track.Length * 1000;
                // calculate the difference between the two cars
                var distance = (SafetyCarTrackDistancePercent * trackLengthM) - (CameraCar.TrackPositionPercent * trackLengthM);
                if (distance > trackLengthM / 2) {
                    distance -= trackLengthM;
                }
                else if (distance < -trackLengthM / 2) {
                    distance += trackLengthM;
                }
                return distance;
            }
        }

        public string LapDistSafetyCarString {
            get {
                if (LapDistSafetyCar > 0) {
                    return LapDistSafetyCar.ToString("0") + "m AHEAD";
                }
                return Math.Abs(LapDistSafetyCar).ToString("0") + "m BEHIND";
            }
        }

        public bool _FirstSCPeriodBreaksEarlySCRule;
        public bool SafetyCarCountLock;

       // public int _safetyCarPeriodCount;
      //  public int SafetyCarPeriodCount() { return _safetyCarPeriodCount; }

       

        internal void CheckAndAddCarClass(long CarClassID, string CarClassShortName, string CarClassColor, string CarClassTextColor) {
            bool has = this.CarClasses.Any(a => a.CarClassID == CarClassID);

            if (has == false) {
                this.CarClasses.Add(new CarClass() {
                    CarClassID = CarClassID,
                    carClassShortName = CarClassShortName,
                    carClassColor = CarClassColor,
                    carClassTextColor = CarClassTextColor
                });
            }
        }

        public string Description;

        public Session(ref PluginSettings settings, ref GameData shData, ref DataSampleEx irData) {
            this.Settings = settings;
            this.iRacingData = irData;
            this.data = shData;
        }

        public void GetSessionData() {

            // Get the iRacing Session Details
            SessionTime = iRacingData.Telemetry.SessionTime;
            EventType = iRacingData.SessionData.WeekendInfo.EventType;
            var sessionNumber = iRacingData.Telemetry.SessionNum;
            SessionType = iRacingData.SessionData.SessionInfo.Sessions[sessionNumber].SessionType;

            CurrentSessionState = iRacingData.Telemetry.SessionState;
            CurrentSessionID = iRacingData.SessionData.WeekendInfo.SessionID;
            _track = Track.FromSessionInfo(iRacingData.SessionData.WeekendInfo,iRacingData.SessionData.SplitTimeInfo);

        }

        public void GetGameData() {

        }

        public void GetGameDataEverySecond() {
            // Get the current camera car. This will be the player or car being observed
            iRCameraCar = iRacingData.SessionData.DriverInfo.Drivers.SingleOrDefault(x => x.CarIdx == iRacingData.Telemetry.CamCarIdx);
            CameraCar = new Driver(ref data, ref iRacingData, iRCameraCar);

            iRCompetitors = iRacingData.SessionData.DriverInfo.CompetingDrivers;

            // Update the car classes
            foreach (_Drivers competitor in iRCompetitors) {
                var newDriver = new Driver(ref data, ref iRacingData, competitor);
                Drivers.Add(newDriver);
                if (!String.IsNullOrEmpty(newDriver.Name)) {
                    CheckAndAddCarClass(newDriver.CarClassID, newDriver.CarClass, newDriver.CarClassColor, newDriver.CarClassTextColor);
                }
            }

            // Update the reference lap time for each class
            foreach (var item in CarClasses) {
                item.UpdateReferenceClassLaptime(Drivers);
            }

            // Find the overall leader for each class and update their total time
            foreach (var item in CarClasses) {
                List<Driver> classbyPosition = Drivers.FindAll(a => a.CarClassID == item.CarClassID && !a.IsSpectator).OrderBy(a => a.Position).ToList();

                Driver classLeader = classbyPosition[0];
                item.LeaderTotalTime = (classLeader.CurrentLap * item.ReferenceLapTime) + (item.ReferenceLapTime * classLeader.TrackPositionPercent);
            }
            CalculateLivePositions();
            CalculateSimhubPositions();

            // Need to update the CameraCar
            if (CameraCar.IsIRPaceCar) {
                CameraCar.SimhubPosition = 0;
            }
            else {
                CameraCar.SimhubPosition = Drivers.Find(x => x.CarIdx == iRacingData.Telemetry.CamCarIdx).SimhubPosition;
            }
            UpdateLeaderTimeDelta(ref Drivers, ref CarClasses, ref Leader);
            UpdateCarAheadTimeDelta(ref Drivers, ref CarClasses);


        }

        private void CalculateSimhubPositions() {
  
            List <Driver> simhubSortList = new List<Driver>();
            // Simhub sorts by Position, if position is zero then CarIdx with zero position cars at the end
            
            // first we add the drivers with positions
            foreach (var driver in Drivers.Where(x => x.Position > 0).OrderBy(d => d.Position).ThenBy(x => x.CarIdx)) {
                simhubSortList.Add(driver);
            }

            // then we add drivers with position = 0
            foreach (var driver in Drivers.Where(x => x.Position == 0).OrderBy(d => d.Position).ThenBy(x => x.CarIdx)) {
                simhubSortList.Add(driver);
            }

            // now update the driver value
            for (int i = 0; i < simhubSortList.Count; i++) {
                Drivers.Find(x => x.CarIdx == simhubSortList[i].CarIdx).SimhubPosition = i+1;
            }

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
                foreach (var driver in Drivers.OrderBy(d => d.Position).ThenBy(x => x.CarIdx)) {
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

        public void CheckIfUnderSafetyCar() {
           
            if (SessionType == "Race") {
                if (IsV8VetsSession) { 
                    foreach (var item in Drivers) {
                        if (item.IsVetsPaceCar && item.Speed > 0.01f ) {
                            SafetyCarIdx = item.CarIdx;
                            SafetyCarTrackDistancePercent = item.TrackPositionPercent;
                            if (!item.IsInPitLane ) { // SC is on track
                                IsUnderSC = true;
                                IsSafetyCarMovingInPitane = false;
                            }
                            else { // We are in pit lane and moving 
                                IsUnderSC = true;
                                IsSafetyCarMovingInPitane = true;
                            }
                        }
                        else {
                            IsUnderSC = false;
                            IsSafetyCarMovingInPitane = false;
                        }
                    }
                }
                else {
                    IsUnderSC = false;
                    //IsUnderSC = iRacingData.Telemetry.UnderPaceCar;
                    //if (IsUnderSC && !CameraCar.IsPlayer && !(CameraCar.CarIdx == 0)) {
                    //    //The iracing SC is CarIdx 0
                    //    SafetyCarIdx = 0;
                    //    SafetyCarTrackDistancePercent = (float)iRacingData.Telemetry.CarIdxLapDistPct[0];
                    //}
                    //IsSafetyCarMovingInPitane = false;
                }
            }
            else {
                IsUnderSC = false;
                IsSafetyCarMovingInPitane = false;
            }
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

        private void UpdateLeaderTimeDelta(ref List<Driver> drivers, ref List<CarClass> carClasses, ref Driver leader) {

            double leaderTotalTime = leader.TotalLapTime;
            foreach (var driver in drivers) {
                if (driver.CurrentLap > 0) {
                    double gapToLeader = leaderTotalTime - driver.TotalLapTime;
                    driver.SetGapToLeader = gapToLeader;
                    driver.LapsToLeader = Leader.LapsComplete - driver.LapsComplete;
                }
            }
        }

        private void UpdateCarAheadTimeDelta(ref List<Driver> drivers, ref List<CarClass> carClasses) {

            // Sorted drivers to do the gaps to the car ahead
            var sortedDrivers = drivers.FindAll(a => !String.IsNullOrEmpty(a.Name) &&
                        a.TotalLapDistance > 0 &&
                        a.Position > 0 &&
                        a.IsConnected)
                        .OrderBy(a => a.GapToLeaderRaw)
                        .ThenBy(x => x.CarIdx)
                        .ToList();

            for (var i = 0; i < sortedDrivers.Count; i++) {
                if (i == 0) {
                    sortedDrivers[i].SetGapToPositionAhead = 0.0;
                }
                else {
                    double value = sortedDrivers[i].GapToLeaderRaw - sortedDrivers[i - 1].GapToLeaderRaw;
                    sortedDrivers[i].SetGapToPositionAhead = value;
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
