using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using iRacingSDK;
using IRacingReader;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using APR.OpponentsPlugin.Data;
using static iRacingSDK.SessionData._SessionInfo;
using SimHub.Plugins.OutputPlugins.BeltTensionner;

namespace APR.OpponentsPlugin
{
    [PluginDescription("Extended iRacing Opponents")]
    [PluginAuthor("Bruce McLeod")]
    [PluginName("APR Opponents Plugin")]
    public class OpponentsPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public OpponentsSettings Settings;

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
        public string LeftMenuTitle => "APR Opponents Plugin";

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



        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
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
                return;
            }

            // Define the value of our property (declared in init)
            if (data.GameRunning)
            {
                if (data.OldData != null && data.NewData != null)
                {
                    //Gaining access to raw data
                    if (data?.NewData?.GetRawDataObject() is DataSampleEx) { irData = data.NewData.GetRawDataObject() as DataSampleEx; }

                    Session = new Session(ref data, ref irData);
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

                        Session.GetGameData();
                    }

                    







                    // Trigger Speed Warning example
                    if (data.OldData.SpeedKmh < Settings.SpeedWarningLevel && data.OldData.SpeedKmh >= Settings.SpeedWarningLevel)
                    {
                        // Trigger an event
                        this.TriggerEvent("SpeedWarning");
                    }
                }
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new OpponentsSettingsControl(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            Settings = this.ReadCommonSettings<OpponentsSettings>("GeneralSettings", () => new OpponentsSettings());

            // Declare a property available in the property list, this gets evaluated "on demand" (when shown or used in formulas)
            this.AttachDelegate("CurrentDateTime", () => DateTime.Now);

            // Declare an event
            this.AddEvent("SpeedWarning");

            // Declare an action which can be called
            this.AddAction("IncrementSpeedWarning",(a, b) =>
            {
                Settings.SpeedWarningLevel++;
                SimHub.Logging.Current.Info("Speed warning changed");
            });

            // Declare an action which can be called
            this.AddAction("DecrementSpeedWarning", (a, b) =>
            {
                Settings.SpeedWarningLevel--;
            });
        }
    }
}