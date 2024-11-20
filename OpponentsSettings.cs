namespace APR.OpponentsPlugin
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class OpponentsSettings
    {
        public int SpeedWarningLevel = 100;
        public int MAX_CARS = 64;
    }
}