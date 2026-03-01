namespace BlazorRotatorServer.Models
{
    public class Parameters
    {
        public bool IsEnabled { get; set; }
        public float Angle { get; set; }
        public float AngleSet { get; set; }
        public float AngleNormalized { get; set; }
        public float Azimuth { get; set; }
        public float AzimuthSet { get; set; }
        public int[]? Errors { get; set; }
    }
}