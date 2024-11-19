using FMOD;
using GameReaderCommon;
using GameReaderCommon.Replays;
using IRacingReader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static iRacingSDK.SessionData._DriverInfo;


namespace APR.OpponentsPlugin.Models {

    internal class Driver {

        public bool? PitRequested { get; set; }

        public long CarIdx { get; set; }   

        public string Id { get { return CarIdx.ToString(); } }

        public string Name { get; set; }

        public string TeamName { get; set; }

        public string ShortName { get; set; }

        public bool LapValid { get; set; } = true;

        public string Initials { get; set; }

        public string ClubName { get; set; }

        public string LicenceString { get; set; }

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
        
        public bool IsConnected { get { return (_trackSurface > -1); } }
        public bool IsOffTrack { get { return (_trackSurface == 0); } }
        public bool IsInPitStall { get { return (_trackSurface == 1); } }
        public bool IsInPitLane { get { return IsApproachingPits; } }
        public bool IsApproachingPits { get { return (_trackSurface == 2); } }
        public bool IsOnTrack { get { return (_trackSurface == 3); } }
        public bool IsInGarage { get; set; } = false;
        public bool IsPlayer { get { return IsCameraCar; } }
        public bool IsSpectator { get; set; }
        public bool IsPaceCar { get; set; }
        public bool IsCameraCar { get; set; }

        public float EstTime {  get; set; }
        public float F2Time { get; set; }
        public int Gear { get; set; }
        public float Speed { get; set; }
        public float RRM { get; set; }

        public int Position { get; set; }

        public int LivePosition { get; set; }

        public int PositionInClass { get; set; }

        public double[] Coordinates { get; set; }

        public string CarName { get; set; }

        public string CarClass { get; set; }



        public TimeSpan BestLapTime { get; set; }

        public TimeSpan LastLapTime { get; set; }

        [Computed]
        public double? DeltaToBest { get; set; }

        [Computed]
        public double? DeltaToPlayer { get; set; }

        public double? IRacing_IRating { get; set; }

        public double? TrackPositionPercent { get; set; }

        public double? TrackPositionPercentRaw { get; set; }

        public int? CurrentLap { get; set; }

        public double? CurrentLapHighPrecision { get; set; }

        public string CarNumber { get; set; } = "";

        public double? GaptoLeader { get; set; }

        public double? GaptoClassLeader { get; set; }

        public double? GaptoLeaderSimHub { get; internal set; }

        public int? LapsToLeader { get; set; }

        public int? LapsToClassLeader { get; set; }

        public int? LapsToPlayer { get; set; }

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

        public SectorTimes CurrentLapSectorTimes { get; set; }

        public SectorTimes LastLapSectorTimes { get; set; }

        public SectorTimes BestLapSectorTimes { get; set; }

        public SectorSplits BestSectorSplits { get; set; } = new SectorSplits();

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

        public int? P2PCount { get; set; }

        public bool? P2PStatus { get; set; }


        public Driver(ref GameData data, ref DataSampleEx irData, _Drivers irDriver) {

            // these values come straight from the driver

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

            this.IsPaceCar = Convert.ToBoolean(irDriver.IsPaceCar);
            this.CarClass = irDriver.CarClassShortName;
            this.CarClassColor = irDriver.CarClassColor;
            this.CarIdx = irDriver.CarIdx;
            this.CarName = irDriver.CarScreenName;
            this.CarNumber = irDriver.CarNumber;
            this.ClubName = irDriver.ClubName;
            this.Name = irDriver.UserName;
            this.TeamName = irDriver.TeamName;

            this._trackSurface = (int)irData.Telemetry.CarIdxTrackSurface[irDriver.CarIdx];
            this.PositionInClass = (int)irData.Telemetry.CarIdxClassPosition[irDriver.CarIdx];
            this.Position = (int)irData.Telemetry.CarIdxPosition[irDriver.CarIdx];
            this.CurrentLap = (int)irData.Telemetry.CarIdxLap[irDriver.CarIdx];
            this.TrackPositionPercent = (float)irData.Telemetry.CarIdxLapDistPct[irDriver.CarIdx];

            this.EstTime = (float)irData.Telemetry.CarIdxEstTime[irDriver.CarIdx];
            this.F2Time = (float)irData.Telemetry.CarIdxF2Time[irDriver.CarIdx];
            this.Gear = (int)irData.Telemetry.CarIdxGear[irDriver.CarIdx];
        }
    }
}