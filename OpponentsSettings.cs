using GameReaderCommon.Replays;

namespace APR.SimhubPlugins
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class OpponentsSettings
    {
        public double LowFuelWarningLevel = 5.0;
        public int MAX_CARS = 64;
        public bool OverrideJavaScriptFunctions = true;
    }
}