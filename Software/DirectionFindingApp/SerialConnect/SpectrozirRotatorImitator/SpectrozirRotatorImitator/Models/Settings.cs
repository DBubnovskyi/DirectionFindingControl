using Newtonsoft.Json;

namespace SpectrozirRotatorImitator.Models
{
    public class Settings
    {
        [JsonProperty("sweep")]
        public int Sweep { get; set; } = 240;

        [JsonProperty("max_sweep")]
        public double MaxSweep { get; set; } = 240.0;

        [JsonProperty("azimuth")]
        public int Azimuth { get; set; } = 0;

        [JsonProperty("lat")]
        public double Latitude { get; set; } = 48.490677;

        [JsonProperty("lng")]
        public double Longitude { get; set; } = 35.368084;

        [JsonProperty("ip")]
        public string IP { get; set; } = "127.0.0.1";

        [JsonProperty("port")]
        public int Port { get; set; } = 80;

        [JsonProperty("sn")]
        public string SerialNumber { get; set; } = "SZR-4CDCD0D8CBB0";

        [JsonProperty("version")]
        public string Version { get; set; } = "ROTATOR-SZ-1M; HW:1.0; FW:1.11";

        [JsonProperty("reverse")]
        public int Reverse { get; set; } = 1;

        [JsonProperty("c_n_180")]
        public int CalibrationNeg180 { get; set; } = 4150;

        [JsonProperty("c_n_90")]
        public int CalibrationNeg90 { get; set; } = 3135;

        [JsonProperty("c_0")]
        public int Calibration0 { get; set; } = 2107;

        [JsonProperty("c_90")]
        public int Calibration90 { get; set; } = 1066;

        [JsonProperty("c_180")]
        public int Calibration180 { get; set; } = 0;
    }
}
