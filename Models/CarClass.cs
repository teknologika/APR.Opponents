using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.SimhubPlugins.Models {

    public class CarClass {
        public long CarClassID;
        public string carClassShortName;
        public string carClassColor;
        public string carClassTextColor;
        public int carClassSOF;
        public long LeaderCarIdx;
        public double LeaderTotalTime;
        public double ReferenceLapTime;
        public TimeSpan BestLapTime;

        internal void UpdateReferenceClassLaptime(List<Driver> drivers) {
            List<Driver> driversInClass = drivers.FindAll(a => a.CarClassID == CarClassID);

            double averageLapTime = 0;
            int count = 0;
            foreach (var item in driversInClass) {
                double LastLapTimeSeconds = item.LastLapTime.TotalSeconds;
                double BestLapTimeSeconds = item.BestLapTime.TotalSeconds;

                // use the  last lap time
                if (LastLapTimeSeconds > 0 &&
                        (LastLapTimeSeconds < (LastLapTimeSeconds * 1.05)) &&
                        (LastLapTimeSeconds > (LastLapTimeSeconds * 0.95))) {
                    averageLapTime += LastLapTimeSeconds;
                    count++;
                }
                // if the last lap time is empty, try and use the best
                else if (BestLapTimeSeconds > 0 &&
                        (BestLapTimeSeconds < (BestLapTimeSeconds * 1.05)) &&
                        (BestLapTimeSeconds > (BestLapTimeSeconds * 0.95))) {
                    averageLapTime += BestLapTimeSeconds;
                    count++;
                }
            }
            if (count > 0) {
                averageLapTime = averageLapTime / count;
            }
            // if no time, just use 2 mins
            if (averageLapTime == 0) {
                averageLapTime = 120.0;
            }

            // Push this time back into every driver in class
            foreach (var item in driversInClass)
            {
                item.ClassReferenceLapTime = TimeSpan.FromSeconds(averageLapTime);
            }

            ReferenceLapTime = averageLapTime;

        }
    }
}
