using FMOD;
using GameReaderCommon;
using GameReaderCommon.Replays;
using IRacingReader;
using iRacingSDK;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Animation;
using static iRacingSDK.SessionData._DriverInfo;


namespace APR.SimhubPlugins.Models {

    internal class Driver {

        public override string ToString() {
            return $"P: {Position} LP:{LivePosition.ToString("0.0")} TLD:{TotalLapDistance} {Name} GP: {GapToCameraCar} GL: {GapToLeader} GN: {GapToPositionAhead}"; 
        }

        public string[] V8VetsSafetyCarNames = { "BMW M4 GT4", "Mercedes AMG GT3", "McLaren 720S GT3 EVO" };
        public int[] V8VetsLeagueIDs = {6455,10129,6788};
        
        /* Stuff here is not used 
         * 
        public bool? PitRequested { get; set; }
        public bool LapValid { get; set; } = true;
        public double[] Coordinates { get; set; }

        public double? DeltaToBest { get; set; }
        public double? GapToCameraCar { get; set; }
        public double? CurrentLapHighPrecision { get; set; }
        public double? GaptoLeader { get; set; }
        public double? GaptoClassLeader { get; set; }
        public double? GaptoPlayer { get; set; }

        public string GapToLeaderCombined {
            get => Driver.GetCombinedGapAsString(this.LapsToLeader, this.GaptoLeader);
        }

        public string GapToPlayerCombined {
            get => Driver.GetCombinedGapAsString(this.LapsToPlayer, this.GaptoPlayer);
        }

        public string GapToClassLeaderCombined {
            get => Driver.GetCombinedGapAsString(this.LapsToClassLeader, this.GaptoClassLeader);
        }

        public double? TrackPositionPercentToPlayer { get; set; }

        public double? RelativeGapToPlayer { get; set; }

        public double? RelativeDistanceToPlayer { get; set; }

        public PointF? RelativeCoordinatesToPlayer { get; internal set; }

        public double RelativeVectorAngleToPlayer { get; internal set; }

        public double RelativeVectorLengthToPlayer { get; internal set; }

        public double DraftEstimate { get; internal set; }

        public string FrontTyreCompoundGameCode { get; set; }

        public string RearTyreCompoundGameCode { get; set; }

        public string FrontTyreCompound { get; set; }

        public string RearTyreCompound { get; set; }

                public DateTime? PitOutAtTime { get; set; }

        public TimeSpan? PitOutSince { get; set; }

        public double? PitOutLapsDoneSince { get; set; }

        public bool IsOutLap { get; set; }

        public int? PitCount { get; set; }

        public double? PitOutAtLap { get; internal set; }

        public DateTime? PitEnterAtTime { get; internal set; }

        public double? PitEnterAtLap { get; internal set; }

        public TimeSpan? PitLastDuration { get; internal set; }

        public DateTime? GuessedLapStartTime { get; internal set; }

        public TimeSpan? CurrentLapTime { get; set; }

        public DateTime? StandingStillInPitLaneSince { get; internal set; }

        public bool StandingStillInPitLane { get; internal set; }

        public int? CurrentSector { get; set; }

        public int? StartPosition { get; set; }

        public int? StartPositionClass { get; set; }

        public double? SplitDeltaSelf { get; internal set; }

        public double? SplitDeltaBestLapOpponent { get; internal set; }

        public int? RacePositionGain { get; internal set; }

        public int? RacePositionClassGain { get; internal set; }

        public bool? DidNotFinish { get; set; }

        public bool? DidNotQualify { get; set; }

        public int? P2PCount { get; set; }

        public bool? P2PStatus { get; set; }


        */
        internal DataSampleEx _irData;
        internal Track _track;
        internal GameData _data;
        public long CustId { get; set; }
        public long CarIdx { get; set; }
        
        public int Id { get { return (int)CarIdx; } }

        public string Name { get; set; }
        public string NameRelativeColour {
            get {
                if (IsCameraCar) {
                    return "#FFFFD700";
                }
                string RelativeTextRed = "#FFD05E55";
                string RelativeTextBlue = "#FF09D0D4";
                string RelativeTextWhite = "#FFECEDF4";
                string RelativeTextLightGrey = "#FF3F3C40";
                //int cameraCarCurrentLap = _irData.Telemetry.CarIdxLap[_irData.Telemetry.CamCarIdx];
                var gap = TotalLapDistance - CameraCarTotalLapDistance+ _CameraCarTrackDistancePercent;
                
                if (IsInPitLane || IsInPitStall || IsInGarage || !IsConnected) {
                    return RelativeTextLightGrey;
                }

                if (_eventType == "Race") {
                    if (gap > 1 ) {
                        return IsInPitLane ? RelativeTextLightGrey : RelativeTextRed; // Lapping you
                    }
                    else if (gap < 0) {
                        return IsInPitLane ? RelativeTextLightGrey : RelativeTextBlue; // Being lapped by you
                    }
                    else {
                        return IsInPitLane ? RelativeTextLightGrey : RelativeTextWhite; // Same lap as you
                    }

                }
                else {
                    return RelativeTextWhite;
                }
            }
        }

        public string TeamName { get; set; }
        public long TeamId { get; set; }
        
        public string ShortName { get; set; }

        public string Initials { get; set; }

        public string ClubName { get; set; }

        public string LicenceString { get; set; }
        public string LicenceColor { get; set; }
        public float LicenseLevel { get; set; }
        public float LicenceSubLevel { get; set; }

        public string CarClassColor { get; set; }

        public string CarClassTextColor {
            get {
                if (!string.Equals(CarClassColor.ToLower(), "#ffffffff", StringComparison.Ordinal)) {
                    return "#00000000";
                }
                else {
                    return "#ffffffff";
                }
            }
        }

        private int _trackSurface;
        private string _eventType;

        public bool IsConnected { get { return (_trackSurface > -1); } }
        public bool IsOffTrack { get { return (_trackSurface == 0); } }
        public bool IsInPitStall { get { return (_trackSurface == 1); } }
        public bool IsInPitLane { get { return IsApproachingPits; } }
        public bool IsApproachingPits { get { return (_trackSurface == 2); } }
        public bool IsOnTrack { get { return (_trackSurface == 3); } }
        public bool IsInGarage { get; set; } = false;
        public bool IsPlayer { get { return IsCameraCar; } }
        public bool IsSpectator { get; set; }
        public bool IsVetsPaceCar {
            get {
                if (V8VetsLeagueIDs.Contains(_track.LeagueID)) { 
                    if (V8VetsSafetyCarNames.Contains(this.CarName)) {
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else {
                    return false;
                }
            }
        }
        private void SetIsIRPaceCar(long value) {
            if (value == 1) {
                IsIRPaceCar = true;
            }
            else
                IsIRPaceCar = false;
        }

        public bool IsIRPaceCar { get; set; }

        public bool IsPaceCar {
            get {
                if (IsIRPaceCar || IsVetsPaceCar) {
                    return true;
                }
                else {
                    return false;
                }
            }
        }

        public bool IsCameraCar { get; set; }

        public float EstTime {  get; set; }
        public float F2Time { get; set; }
        public int Gear { get; set; }
        public double Speed { get; set; }
        public float RRM { get; set; }
        private SessionFlag _sessionFlags { get; set; }
      

        public int Position { get; set; }
        public int SimhubPosition { get; set; }

        public int LivePosition { get; set; }

        public int ClassPosition { get; set; }
        public int ClassLivePosition { get; set; }
        
     
        public string CarName { get; set; }

        public string CarClass { get; set; }
        public long CarClassID { get; set; }

        public TimeSpan BestLapTime { get; set; }
        public TimeSpan LastLapTime { get; set; }
        private TimeSpan _classReferenceLapTime;
        public TimeSpan ClassReferenceLapTime {
            get {
                // if we have set it to 2 mins, instead base it on track length
                if (_classReferenceLapTime.TotalSeconds == 120.0) {

                    // 180 kmh reference speed
                    return TimeSpan.FromSeconds((_track.Length / 180) * 3600);
                }
                else {
                    return _classReferenceLapTime;
                }
            }
            set { _classReferenceLapTime = value; }
        }



        public float IRating { get; set; }

        public float TrackPositionPercent { get; set; }
        //internal double LapDistSpectatedCar { get; set; }


        public int CurrentLap { get; set; }
        public int LapsComplete { get; set; }

        internal double TotalLapDistance {          
            get {
                var value = CurrentLap + TrackPositionPercent;
                return value;
            }
        }

        internal double CameraCarTotalLapDistance {
            get {
                var value = _cameraCarLap + _CameraCarTrackDistancePercent;
                return value;
            }
        }

        internal double TotalLapTime {
            get {
                var value = TotalLapDistance * ClassReferenceLapTime.TotalSeconds;
                return value;
            }
        }

        public string CarNumber { get; set; } = "";

        private double _gapToLeader = 0;
        public double SetGapToLeader {
            set {
                _gapToLeader = value;
            }
        }

        

        public double GapToLeaderRaw { get { return Math.Round(_gapToLeader,1);} }
        public string GapToLeader {
            get {
                if (LapsToLeader == 0) {
                    if (Position == 1) {
                        return "Leader";
                    }
                    else {
                        return "-.--";
                    }
                }
                else {
                    if (LapsToLeader > 0) {
                        if (LapsToLeader > 1) {
                            return LapsToLeader + " Laps";
                        }
                        else {
                            return "1 Lap";
                        }
                    }
                    else {
                        return _gapToLeader.ToString("0.0");
                    }
                }
            }
        }
        private double _cameraCarLap = 0;
        public double SetCameraCarLap {
            set {
                _cameraCarLap = value;
            }
        }

        private double _SafetyCarTrackDistancePercent = 0;
        public double SetSafetyCarTrackDistancePercent {
            set {
                _SafetyCarTrackDistancePercent = value;
            }
        }

        private double _CameraCarTrackDistancePercent = 0;
        public double SetCameraCarTrackDistancePercent {
            set {
                _CameraCarTrackDistancePercent = value;
            }
        }

        public double LapDistPctToCameraCar {
            get {
                // calculate the difference between the two cars
                var pctGap = _CameraCarTrackDistancePercent - TrackPositionPercent;
                if (pctGap > 50.0) {
                    pctGap -= 50.0;
                }
                else if (pctGap < -50.0) {
                    pctGap += 50;
                }

                return pctGap;
            }
        }

        public double LapDistToCameraCar {
            get {
              
                var distance = (_CameraCarTrackDistancePercent * _track.Length) - (TrackPositionPercent * _track.Length);
                if (distance > _track.Length / 2) {
                    distance -= _track.Length;
                }
                else if (distance < -_track.Length / 2) {
                    distance += _track.Length;
                }

                return distance;
            }
        }

        public double LapDistToSafetyCar {
            get {

                var distance = (_SafetyCarTrackDistancePercent * _track.Length) - (TrackPositionPercent * _track.Length);
                if (distance > _track.Length / 2) {
                    distance -= _track.Length;
                }
                else if (distance < -_track.Length / 2) {
                    distance += _track.Length;
                }

                return distance;
            }
        }

        public double GapToCameraCarRaw {
            get {
                return Math.Round(ClassReferenceLapTime.TotalSeconds / _track.Length * LapDistToCameraCar,2);
            }
        }

        public string GapToCameraCar {
            get {
                if (GapToCameraCarRaw == 0) {
                    return "0.0";
                }
                else if (GapToCameraCarRaw > 0.0) {
                    return "+" + GapToCameraCarRaw.ToString("0.0");
                }
                else {
                    return GapToCameraCarRaw.ToString("0.0");
                }
            }
        }
        

        private double _aheadGapToLeader = 0;
        public double SetAheadGapToLeader {
            set {
                _aheadGapToLeader = value;
            }
        }

        // used when sorting the relative
        public double SortingRelativeGapToSpectator { get; set; } 

        private double _gapToPositionAhead = 0;
        public double SetGapToPositionAhead {
            set {
                _gapToPositionAhead = value;
            }
        }
       
        public string GapToPositionAhead {
            get {
                // We are the leader or have not set a lap
                if (Position <= 0) {
                    return "-.--";
                }
                else if (Position == 1) {
                    return "Leader";
                }
                else {

                    if (_gapToPositionAhead == 0) {
                        return "-.--";
                    }
                    else {
                        return _gapToPositionAhead.ToString("0.0");
                    }
                }
            }
        }
        public double GapToPositionAheadRaw { get { return Math.Round(_gapToPositionAhead, 1); } }

        public int LapsToLeader { get; set; }
        public int LapsToPositionAhead { get; set; }
        //public int? LapsToClassLeader { get; set; }
        public int LapsToCameraCar { get; set; }

        // Flags
        public bool FlagRepair {
            get {
                if (_sessionFlags.Contains(SessionFlags.Repair)) {
                    return true;
                }
                return false;
            }
        }

        // I don't think this is actually exposed
        public bool FlagBlue {
            get {
                if (_sessionFlags.Contains(SessionFlags.Blue)) {
                    return true;
                }
                return false;
            }
        }

        public bool FlagBlack {
            get {
                if (_sessionFlags.Contains(SessionFlags.Black)) {
                    return true;
                }
                return false;
            }
        }

        // This is normally a slowdown but also if a penalty
        public bool FlagBlackFurled {
            get {
                if (_sessionFlags.Contains(SessionFlags.Furled)) {
                    return true;
                }
                return false;
            }
        }

        /*
         public SectorTimes CurrentLapSectorTimes { get; set; }
         public SectorTimes LastLapSectorTimes { get; set; }
         public SectorTimes BestLapSectorTimes { get; set; }
         public SectorSplits BestSectorSplits { get; set; } = new SectorSplits();
        */
        private static string GetCombinedGapAsString(int? lapsGame, double? timeGap) {
            if (!timeGap.HasValue && !lapsGame.HasValue)
                return "";
            if (lapsGame.HasValue && (lapsGame.Value > 0 || !timeGap.HasValue)) {
                if (lapsGame.Value == 0)
                    return "";
                string str = lapsGame.Value.ToString() + " lap";
                return (lapsGame.Value > 0 ? "+" : "") + str + (Math.Abs(lapsGame.Value) > 1 ? "s" : "");
            }
            string str1 = timeGap.Value.ToString("0.00");
            return (timeGap.Value > 0.0 ? "+" : "") + str1;
        }

        private static TimeSpan? ToTimespan(double? sectortime) {
            return !sectortime.HasValue || sectortime.Value <= 0.0 ? new TimeSpan?() : new TimeSpan?(TimeSpan.FromMilliseconds(Math.Round(sectortime.Value, 3)));
        }

        public Driver() {
            Console.WriteLine("DANGER - DRIVER() Called !!");
        }

        public Driver(ref GameData data, ref DataSampleEx irData, _Drivers irDriver) {
            _irData = irData;
            _data = data;

            // calculate the difference between the two cars
            _track = Track.FromSessionInfo(_irData.SessionData.WeekendInfo, _irData.SessionData.SplitTimeInfo);

           
            // if we are the camera car push in extra fun stuff
            if (irData.Telemetry.CamCarIdx == irDriver.CarIdx) {
                this.IsCameraCar = true;
            }
            else {
                this.IsCameraCar = false;
            }

            this.IsSpectator = Convert.ToBoolean(irDriver.IsSpectator);
            if (this.IsPlayer) {
                this.IsInGarage = irData.Telemetry.IsInGarage;
            }
            
            this.CarClassID = irDriver.CarClassID;
            if (irDriver.IsPaceCar) {
                this.CarClass = "Pace Cars";
            }
            else {
                this.CarClass = irDriver.CarClassShortName;
            }


            this.CarClassColor = ParseColor(irDriver.CarClassColor);
            this.CarIdx = irDriver.CarIdx;
            this.CarName = irDriver.CarScreenName;
            this.CarNumber = irDriver.CarNumber;
            this.ClubName = irDriver.ClubName;
            this.Name = irDriver.UserName;
            this.TeamName = irDriver.TeamName;

            this._trackSurface = (int)irData.Telemetry.CarIdxTrackSurface[irDriver.CarIdx];
            this._eventType = irData.SessionData.WeekendInfo.EventType;
            this.ClassPosition = (int)irData.Telemetry.CarIdxClassPosition[irDriver.CarIdx];
            this.Position = (int)irData.Telemetry.CarIdxPosition[irDriver.CarIdx];
            this.CurrentLap = (int)irData.Telemetry.CarIdxLap[irDriver.CarIdx];
            int _lapsComplete = (int)irData.Telemetry.CarIdxLapCompleted[irDriver.CarIdx];
            if (_lapsComplete < 0) {
                LapsComplete = 0;
            }
            else {
                this.LapsComplete = _lapsComplete;
            }

            // float[] bestLapTimes = (float[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxBestLapTime").Value;
            //float[] lastLapTimes = (float[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxLastLapTime").Value;


            //int[] gear = (int[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxGear").Value;
            //float[] rpm = (float[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxRPM").Value;

            // Get the session flags for the driver and share in properties
            int[] flags = (int[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxSessionFlags").Value;
            this._sessionFlags = new SessionFlag(flags[CarIdx]);
            

            this.TrackPositionPercent = (float)irData.Telemetry.CarIdxLapDistPct[irDriver.CarIdx];
            
            // Calculate the distance between the CameraCar and the Driver
            // this is used for working out ahead / behind
            double cameraCarLapDistPct = (float)irData.Telemetry.CarIdxLapDistPct[irData.Telemetry.CamCarIdx];
            double cameraCarCurrentLap = (long)irData.Telemetry.CarIdxLap[irData.Telemetry.CamCarIdx];
            Track track = Track.FromSessionInfo(irData.SessionData.WeekendInfo, irData.SessionData.SplitTimeInfo);
       
        
            this.SetCameraCarTrackDistancePercent = cameraCarLapDistPct;
            this.SetCameraCarLap = cameraCarCurrentLap;

            this.IRating = irDriver.IRating;
            this.LicenceString = irDriver.LicString;
            this.LicenceColor = ParseColor(irDriver.LicColor);
            this.LicenseLevel = irDriver.LicLevel;
            this.LicenceSubLevel = irDriver.LicSubLevel;
            this.CustId = irDriver.UserID;

            this.EstTime = (float)irData.Telemetry.CarIdxEstTime[irDriver.CarIdx];
            this.F2Time = (float)irData.Telemetry.CarIdxF2Time[irDriver.CarIdx];
            this.Gear = (int)irData.Telemetry.CarIdxGear[irDriver.CarIdx];
            this.SetIsIRPaceCar(irDriver.CarIsPaceCar);
            CalculateSpeed();
        }

        private double _prevSpeedUpdateTime;
        private double _prevSpeedUpdateDist;
        private const float SPEED_CALC_INTERVAL = 0.5f;

        private void CalculateSpeed() {

            if (_irData == null) return;
            if (_track == null) return;

            try {
                var t1 = _irData.Telemetry.SessionTime;
                var t0 = _prevSpeedUpdateTime;
                var time = t1 - t0;
                double speedMS = 0;

                if (time < SPEED_CALC_INTERVAL) {
                    // Ignore
                    return;
                }

                var p1 = TrackPositionPercent;
                var p0 = _prevSpeedUpdateDist;

                if (p1 < -0.5 || !IsConnected) {
                    // Not in world?
                    return;
                }

                if (p0 - p1 > 0.5) {
                    // Lap crossing
                    p1 += 1;
                }
                var distancePct = p1 - p0;

                var distance = distancePct * _track.Length * 1000; //meters


                if (time >= Double.Epsilon) {
                    speedMS = distance / (time); // m/s
                }
                else {
                    if (distance < 0)
                        speedMS = Double.NegativeInfinity;
                    else
                        speedMS = Double.PositiveInfinity;
                }
                this.Speed = speedMS * 3.6;

                _prevSpeedUpdateTime = t1;
                _prevSpeedUpdateDist = p1;
            }
            catch (Exception ex) {
                //Log.Instance.LogError("Calculating speed of car " + this.Driver.Id, ex);
                this.Speed = 0;
            }
        }

        public string ParseColor(string value) {
            if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("0x")) {
                try {
                    return value.Replace("0x", "#");
                }
                catch (Exception) {
                }
            }
            return "#FFFFFFFF";
        }
    }
}