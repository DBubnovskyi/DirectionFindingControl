namespace BlazorRotatorServer.Models
{
    public class ControlSettings
    {
        public int SensorError { get; set; }
        public int DiffAzAn { get; set; }
        public Position? Location{ get; set; }
        public Position? MagneticNorth { get; set; }
    }
}
