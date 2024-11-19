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

namespace APR.OpponentsPlugin.Data {
    internal class Session : IDisposable {

        DataSampleEx iRacingData;
        GameData data;

        _Drivers CameraCar = null;

        _Drivers[] iRCompetitors;
        _Drivers[] iRDrivers;
        List<Driver> Drivers = new List<Driver>();

        public string Description;

        public Session(ref GameData shData, ref DataSampleEx irData) {
            this.iRacingData = irData;
        }

        public void GetGameData() {
           // Description = iRacingData.Telemetry.Session.SessionType;
            CameraCar = iRacingData.SessionData.DriverInfo.Drivers.SingleOrDefault(x => x.CarIdx == iRacingData.Telemetry.CamCarIdx);
            //PlayerCar = iRacingData.SessionData.DriverInfo.Drivers.SingleOrDefault(x => x.CarIdx == iRacingData.Telemetry.pl);

            iRCompetitors = iRacingData.SessionData.DriverInfo.CompetingDrivers;
            iRDrivers = iRacingData.SessionData.DriverInfo.Drivers;

            foreach (_Drivers competitor in iRCompetitors) {
                var newDriver = new Driver(ref data, ref iRacingData, competitor);
                Drivers.Add( newDriver);
            }

            // Get the competitors and create the Drivers

            // Get the opponents and update the Drivers



            //SessionTime = iRacingData.Telemetry.SessionTime;
            //SessionType = iRacingData.Telemetry.Session.SessionType;
            //CurrentSessionState = iRacingData.Telemetry.SessionState;
            //CurrentSessionID = iRacingData.SessionData.WeekendInfo.SessionID;
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
