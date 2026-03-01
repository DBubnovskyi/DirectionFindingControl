namespace BlazorRotatorServer.Models
{
    public class Rotator
    {
        public BaseSettings BaseSettings { get; set; } = new BaseSettings();
        public ControlSettings ControlSettings { get; set; } = new ControlSettings();
        public Parameters Parameters { get; set; } = new Parameters();
        public Info Info { get; set; } = new Info();
    }
}