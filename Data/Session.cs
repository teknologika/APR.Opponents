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
using APR.OpponentsPlugin.Models;

using static iRacingSDK.SessionData._DriverInfo;
using System.Globalization;

namespace APR.OpponentsPlugin.Data {
    internal class Session : IDisposable {

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



        public bool IsCheckered {
            get {
                return CurrentSessionState == SessionState.Checkered;
            }
        }


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



        DataSampleEx iRacingData;
        GameData data;

        _Drivers CameraCar = null;
        Driver Leader = null;
        int LeaderLap = 0;

        _Drivers[] iRCompetitors;
        _Drivers[] iRDrivers;
        List<Driver> Drivers = new List<Driver>();

        public string Description;

        public Session(ref GameData shData, ref DataSampleEx irData) {
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



           // Description = iRacingData.Telemetry.Session.SessionType;
            CameraCar = iRacingData.SessionData.DriverInfo.Drivers.SingleOrDefault(x => x.CarIdx == iRacingData.Telemetry.CamCarIdx);
            //PlayerCar = iRacingData.SessionData.DriverInfo.Drivers.SingleOrDefault(x => x.CarIdx == iRacingData.Telemetry.pl);

            iRCompetitors = iRacingData.SessionData.DriverInfo.CompetingDrivers;
            iRDrivers = iRacingData.SessionData.DriverInfo.Drivers;

            foreach (_Drivers competitor in iRCompetitors) {
                var newDriver = new Driver(ref data, ref iRacingData, competitor);
                Drivers.Add(newDriver);
            }

            CalculateLivePositions();
            UpdateTimeDelta();

            // Get the competitors and create the Drivers

            // Get the opponents and update the Drivers



            //SessionTime = iRacingData.Telemetry.SessionTime;
            //SessionType = iRacingData.Telemetry.Session.SessionType;
            //CurrentSessionState = iRacingData.Telemetry.SessionState;
            //CurrentSessionID = iRacingData.SessionData.WeekendInfo.SessionID;
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

        private void UpdateTimeDelta() {
            if (_timeDelta == null) return;

            // Update the positions of all cars
            _timeDelta.Update(SessionTime, iRacingData.Telemetry.CarIdxLapDistPct);

            // Order drivers by live position
            var drivers = Drivers.OrderBy(d => d.LivePosition).ToList();
            if (drivers.Count > 0) {
                
                // Get leader
                var leader = drivers[0];
                Leader.DeltaToLeader = "-";
                Leader.DeltaToNext = "-";
                
                // Loop through drivers
                for (int i = 1; i < drivers.Count; i++) {
                    var behind = drivers[i];
                    var ahead = drivers[i - 1];

                    // Lapped?
                    var leaderLapDiff = Math.Abs(this.Leader.TotalLapDistance - behind.TotalLapDistance);
                    var nextLapDiff = Math.Abs(ahead.TotalLapDistance - behind.TotalLapDistance);

                    if (leaderLapDiff < 1) {
                        var leaderDelta = _timeDelta.GetDelta(behind.Id, Leader.Id);
                        behind.DeltaToLeader = TimeDelta.DeltaToString(leaderDelta);
                    }
                    else {
                        behind.DeltaToLeader = Math.Floor(leaderLapDiff) + " L";
                    }

                    if (nextLapDiff < 1) {
                        var nextDelta = _timeDelta.GetDelta(behind.Id, ahead.Id);
                        behind.DeltaToNext = TimeDelta.DeltaToString(nextDelta);
                    }
                    else {
                        behind.DeltaToNext = Math.Floor(nextLapDiff) + " L";
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
