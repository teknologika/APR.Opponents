using iRacingSDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static iRacingSDK.SessionData;

namespace APR.SimhubPlugins.Models {
    internal class Track {
        private readonly List<Sector> _sectors;

        public Track() {
            _sectors = new List<Sector>();
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public string CodeName { get; set; }
        public string ConfigName { get; set; }
        public double Length { get; set; }
        public bool NightMode { get; set; }

        public List<Sector> Sectors {
            get { return _sectors; }
        }

        public static Track FromSessionInfo(_WeekendInfo info, _SplitTimeInfo sectors) {
            var track = new Track();

            //var query = info["WeekendInfo"];
            track.Id = info.TrackID;
            track.Name = info.TrackDisplayName;
            track.CodeName = info.TrackName;
            track.ConfigName = info.TrackConfigName;
            track.Length = Track.ParseTrackLength(info.TrackLength);
            track.NightMode = info.WeekendOptions.NightMode == "1";

            // Parse sectors
            track.Sectors.Clear();


            int nr = 0;
            foreach (var item in sectors.Sectors) {
                var sec = new Sector();
                sec.Number = nr;
                sec.StartPercentage = (float)item.SectorStartPct;
                track.Sectors.Add(sec);
                nr++;
            }
            return track;
        }

        private static double ParseTrackLength(string value) {
            // value = "6.93 km"
            double length = 0;

            var indexOfKm = value.IndexOf("km");
            if (indexOfKm > 0) value = value.Substring(0, indexOfKm);

            if (double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out length)) {
                return length;
            }
            return 0;
        }
    }
}
