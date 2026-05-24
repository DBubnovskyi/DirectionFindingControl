using Newtonsoft.Json;

namespace SpectrozirRotatorImitator.Models
{
    public class RotatorState
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("angle")]
        public double CurrentAngle { get; set; }

        [JsonProperty("req_angle")]
        public double RequestedAngle { get; set; }

        [JsonProperty("az_angle")]
        public double AzimuthAngle { get; set; }

        [JsonProperty("dac")]
        public int DAC { get; set; }
    }
}
