using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.SimhubPlugins.Models {

    public class CarClass {
        public long carClassID;
        public string carClassShortName;
        public string carClassColor;
        public string carClassTextColor;
        public int carClassSOF;
        public long LeaderCarIdx;
        public double LeaderTotalTime;
        public double ReferenceLapTime;
        public TimeSpan BestLapTime;
    }



}
