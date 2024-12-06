using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using iRacingSDK;
using IRacingReader;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using APR.SimhubPlugins.Data;
using static iRacingSDK.SessionData._SessionInfo;
using SimHub.Plugins.OutputPlugins.BeltTensionner;
using APR.SimhubPlugins.Models;
using System.Linq;
using System.Text;
using static iRacingSDK.SessionData._DriverInfo;

namespace APR.SimhubPlugins {
    [PluginDescription("Extended iRacing Data")]
    [PluginAuthor("Bruce McLeod")]
    [PluginName("APR iRacing Plugin")]
    public class APRiRacing : IPlugin, IDataPlugin, IWPFSettingsV2 {
        
        public PluginSettings Settings;

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Gets the left menu icon. Icon must be 24x24 and compatible with black and white display.
        /// </summary>
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);

        /// <summary>
        /// Gets a short plugin title to show in left menu. Return null if you want to use the title as defined in PluginName attribute.
        /// </summary>
        public string LeftMenuTitle => "APR iRacing";

        /// <summary>
        /// Called one ClockTime per game data update, contains all normalized game data,
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>


        DataSampleEx irData;


        /// <summary>
        /// Setup all the session timers to allow us to optomiste how things are called         
        /// </summary>

        public int frameCounter = 0;


        DateTime now;
        private long ClockTime;

        private long endTime1Sec;
        private readonly int every1sec = 10000000;
        private bool runEvery1Sec;

        private long endTime5Sec;
        private readonly int every5sec = 50000000;
        private bool runEvery5Sec;

        // Used to track when we cross the start / finsh line
        public double trackPosition = 0.0;
        public bool lineCrossed = false;

        // Session Variables
        private Session _session;
        private Session Session {
            get => _session;
            set {
                if (_session != value) {
                    _session?.Dispose();
                    _session = value;
                }
            }
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data) {
            // Use a frame counter to not update everything every frame
            // Simhub  runs this loop runs 60x per second
            frameCounter++;

            // reset the counter every 60hz
            // not sure what happens if you are on the free version ???
            if (frameCounter > 59) {
                frameCounter = 0;
            }

            // This plugin only Supports iRacing
            if (data.GameName != "IRacing") {
                SetProp("GameIsSupported", false);
                return;
            }

            // Define the value of our property (declared in init)
            if (data.GameRunning) {
                if (data.OldData != null && data.NewData != null) {
                    //Gaining access to raw data
                    if (data?.NewData?.GetRawDataObject() is DataSampleEx) { irData = data.NewData.GetRawDataObject() as DataSampleEx; }

                    Session = new Session(ref Settings, ref data, ref irData);
                    Session.GetSessionData();

                    // Setup timers
                    ClockTime = DateTime.Now.Ticks;

                    runEvery1Sec = ClockTime - endTime1Sec >= (long)every1sec;
                    runEvery5Sec = ClockTime - endTime5Sec >= (long)every5sec;

                    if (frameCounter == 1) {

                        // Timers
                        if (runEvery1Sec) {
                            endTime1Sec = DateTime.Now.Ticks;
                        }

                        if (runEvery5Sec) {
                            endTime5Sec = DateTime.Now.Ticks;
                        }

                        trackPosition = irData.Telemetry.LapDistPct;
                        // if we crossed the line, set line cross to true
                        if (trackPosition < 0.02) {
                            lineCrossed = true;
                        }
                        // if the threshold is greater set to false
                        if (trackPosition > 0.02) {
                            lineCrossed = false;
                        }
                    }

                    if (frameCounter == 9) {
                        try {
                            //if (runEvery1Sec) {
                                if (data.NewData.SessionTimeLeft.TotalSeconds > 0) {
                                    Session.GetGameDataEverySecond();
                                    Session.CheckIfUnderSafetyCar();
                                    SetProperties(Session);

                                    if (Settings.ShowDebug) {
                                        AddSetProp("RelativeDebug", RelativeDebug());

                                    }
                                }
                          //  }
                        }
                        catch (Exception) {
                        }
                    }


                    // update data every 10 cycles
                    if ((frameCounter % 10) == 0 ) {

                       // Session.GetGameData();
                       // SetProperties(Session);

                    }

                    // Trigger Low Fuel Warning
                    if (data.OldData.Fuel < Settings.LowFuelWarningLevel && data.OldData.Fuel >= Settings.LowFuelWarningLevel) {
                        // Trigger an event
                        this.TriggerEvent("LowFuelWarning");
                    }
                }
            }
        }
        public string RelativeDebug() {
            StringBuilder sb = new StringBuilder();
            foreach (var item in Session.DriversAhead) {
                sb.AppendLine(item.ToString());
            }
            foreach (var item in Session.DriversBehind) {
                sb.AppendLine(item.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager) {
            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) {
            return new SettingsControl(this) { DataContext = Settings };
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {
            SimHub.Logging.Current.Info("Starting APR iRacing plugin");

            // Load settings
            Settings = this.ReadCommonSettings<PluginSettings>("GeneralSettings", () => new PluginSettings());

            this.OnSessionChange(pluginManager);


            // Attatch delegates to the controls
            this.AttachDelegate("OverrideJavaScriptFunctions", () => Settings.OverrideJavaScriptFunctions);


            // Declare a property available in the property list, this gets evaluated "on demand" (when shown or used in formulas)
            this.AttachDelegate("CurrentDateTime", () => DateTime.Now);
           
            // Declare an event
            this.AddEvent("LowFuelWarning");

            // Declare an action which can be called
            this.AddAction("IncrementLowFuelWarning", (a, b) => {
                Settings.LowFuelWarningLevel++;
                SimHub.Logging.Current.Info("Low Fuel warning changed");
            });

            // Declare an action which can be called
            this.AddAction("LowFuelWarning", (a, b) => {
                Settings.LowFuelWarningLevel--;
            });

            AddProperties();
        }


        private void OnSessionChange(PluginManager pluginManager) {
            if (Session != null) {
                Session.Reset();
            }
        }

       
        private void AddProperties() {
            AddProp("GameIsSupported", true);

            AddProp($"Relative_00_LeaderboardPosition", "");
            AddProp($"Relative_00_GapToPlayer", "");
            AddProp($"Relative_00_NameColor", "");
            AddProp($"Relative_00_Name", "");
            AddProp($"Relative_00_SimhubPosition", "");

            for (int j = 0; j < Settings.RelativeMaxCarsAheadBehind; j++) {

                AddProp($"Relative_{j + 1:D2}_GapToPlayer", "");
                AddProp($"Relative_{j + 1:D2}_NameColor", "");
                AddProp($"Relative_{j + 1:D2}_Name", "");
                AddProp($"Relative_{j + 1:D2}_LeaderboardPosition", "");
                AddProp($"Relative_{j + 1:D2}_SimhubPosition", "");
 
                AddProp($"Relative_-{j + 1:D2}_GapToPlayer", "");
                AddProp($"Relative_-{j + 1:D2}_NameColor", "");
                AddProp($"Relative_-{j + 1:D2}_Name", "");
                AddProp($"Relative_-{j + 1:D2}_LeaderboardPosition", "");
                AddProp($"Relative_-{j + 1:D2}_SimhubPosition", "");
            }
        }

        public void ClearRelativeProperties() {

            SetProp($"Relative_00_GapToPlayer", "");
            SetProp($"Relative_00_NameColor", "");
            SetProp($"Relative_00_Name", "");
            SetProp($"Relative_00_LeaderboardPosition", "");
            SetProp($"Relative_00_SimhubPosition", "");
            
            for (int j = 0; j < Settings.RelativeMaxCarsAheadBehind; j++) {

                SetProp($"Relative_{j + 1:D2}_GapToPlayer", "");
                SetProp($"Relative_{j + 1:D2}_NameColor", "");
                SetProp($"Relative_{j + 1:D2}_Name", "");
                SetProp($"Relative_{j + 1:D2}_SimhubPosition", "");
                SetProp($"Relative_{j + 1:D2}_LeaderboardPosition", "");

                SetProp($"Relative_-{j + 1:D2}_GapToPlayer", "");
                SetProp($"Relative_-{j + 1:D2}_NameColor", "");
                SetProp($"Relative_-{j + 1:D2}_LeaderboardPosition", "");
                SetProp($"Relative_-{j + 1:D2}_SimhubPosition", "");
                SetProp($"Relative_-{j + 1:D2}_Name", "");
            }
        }

        private void SetProperties(Session session) {

            this.AttachDelegate("GetPlayerLeaderboardPosition", () => Session.GetPlayerLeaderboardPosition());
            this.AttachDelegate("GetPlayerClassLeaderboardPosition", () => Session.GetPlayerClassLeaderboardPosition());

            AddSetProp("Session.UnderSC", Session.IsUnderSC);
            AddSetProp("Session.SCMovingInPitlane", Session.IsSafetyCarMovingInPitane);
            AddSetProp("Session.DistanceToSC", Session.LapDistSafetyCarString);
            //AddSetProp("Session.SCPeriodCount", Session.SafetyCarPeriodCount);

            ClearRelativeProperties();

            // Properties sorted by Position
            var DriversByPosition = session.Drivers.Where(
                a => a.CarName != "" &&
                ( a.IsOnTrack || a.IsInPitLane || a.IsInPitStall ) &&
                a.IsConnected &&
                !a.IsPaceCar &&
                !a.IsSpectator)

                .OrderBy(x => x.SimhubPosition)
                //.OrderBy(x => x.Position)
                //.ThenBy(x => x.CarIdx)
                .ToList();

            int i = 1;
            foreach (var item in DriversByPosition) {
                this.AttachDelegate($"Driver_{i:D2}_LeaderboardPosition", () => item.Position);
                this.AttachDelegate($"Driver_{i:D2}_LeaderboardName", () => item.Name);
                this.AttachDelegate($"Driver_{i:D2}_GapToLeader", () => item.GapToLeader);
                this.AttachDelegate($"Driver_{i:D2}_GapToNext", () => item.GapToPositionAhead);
                this.AttachDelegate($"Driver_{i:D2}_GapToPlayer", () => item.GapToCameraCar);
                this.AttachDelegate($"Driver_{i:D2}_FlagRepair", () => item.FlagRepair);
                this.AttachDelegate($"Driver_{i:D2}_FlagBlackFurled", () => item.FlagBlackFurled);
                this.AttachDelegate($"Driver_{i:D2}_FlagBlack", () => item.FlagBlack);

                i++;
            }

            i = 1;
            foreach (var driver in Session.DriversAhead) {

                if (i < Settings.RelativeMaxCarsAheadBehind && Settings.RelativeShowCarsInPits || (!Settings.RelativeShowCarsInPits && !driver.IsInPitStall)) {
                    SetProp($"Relative_-{i:D2}_GapToPlayer", driver.GapToCameraCar);
                    SetProp($"Relative_-{i:D2}_Name", driver.Name);
                    SetProp($"Relative_-{i:D2}_LeaderboardPosition", driver.Position);
                    SetProp($"Relative_-{i:D2}_SimhubPosition", driver.SimhubPosition);
                    SetProp($"Relative_-{i:D2}_NameColor", driver.NameRelativeColour);
                    i++;
                }
            }

            i = 1;
            foreach (var driver in Session.DriversBehind) {

                if (i < Settings.RelativeMaxCarsAheadBehind && Settings.RelativeShowCarsInPits || (!Settings.RelativeShowCarsInPits && !driver.IsInPitStall)) {

                    SetProp($"Relative_{i:D2}_GapToPlayer", driver.GapToCameraCar);
                    SetProp($"Relative_{i:D2}_Name", driver.Name);
                    SetProp($"Relative_{i:D2}_LeaderboardPosition", driver.Position);
                    SetProp($"Relative_{i:D2}_SimhubPosition", driver.SimhubPosition);
                    SetProp($"Relative_{i:D2}_NameColor", driver.NameRelativeColour);

                    i++;
                }
            }

            SetProp($"Relative_00_GapToPlayer", "0.0");
            SetProp($"Relative_00_NameColor", Session.CameraCar.NameRelativeColour );
            SetProp($"Relative_00_Name", Session.CameraCar.Name);
            SetProp($"Relative_00_LeaderboardPosition", Session.CameraCar.Position);
            SetProp($"Relative_00_SimhubPosition", Session.CameraCar.SimhubPosition);

        }

        // Helper functions to deal with SimhubProperties
        public void AddSetProp(string PropertyName, dynamic value) {
            if (!HasProp(PropertyName)) {
                if (String.IsNullOrEmpty(Convert.ToString(value))) {
                    AddProp(PropertyName, null);
                }
                else {
                    AddProp(PropertyName, value);
                }
            }
            else {
                if (String.IsNullOrEmpty(Convert.ToString(value))) {
                    SetProp(PropertyName, null);
                }
                else {
                    SetProp(PropertyName, value);
                }
            }
        }

        public void ClearProp(string PropertyName) {
            if (HasProp(PropertyName)) {
                SetProp(PropertyName, null);
            }
        }

        public void AddProp(string PropertyName, dynamic defaultValue) => PluginManager.AddProperty(PropertyName, GetType(), defaultValue);
        public void AddProp(string PropertyName) => PluginManager.AddProperty(PropertyName, GetType(), "");

        public void SetProp(string PropertyName, dynamic value) => PluginManager.SetPropertyValue(PropertyName, GetType(), value);
        public dynamic GetProp(string PropertyName) => PluginManager.GetPropertyValue(PropertyName);
        public bool HasProp(string PropertyName) => PluginManager.GetAllPropertiesNames().Contains(PropertyName);
        public void AddEvent(string EventName) => PluginManager.AddEvent(EventName, GetType());
        public void TriggerEvent(string EventName) => PluginManager.TriggerEvent(EventName, GetType());
        public void AddAction(string ActionName, Action<PluginManager, string> ActionBody)
            => PluginManager.AddAction(ActionName, GetType(), ActionBody);

        public void TriggerAction(string ActionName) => PluginManager.TriggerAction(ActionName);


        // Function for easy debuggung
        public static void DebugMessage(string s) => SimHub.Logging.Current.Info((object)s);
    }
}