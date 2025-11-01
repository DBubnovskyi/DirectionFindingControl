using System.IO.Ports;

namespace SerialMonitor
{
    public class SerialPortInfo
    {
        public string PortName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }

        public override string ToString()
        {
            var info = PortName;
            
            if (!string.IsNullOrEmpty(Description) && Description != "Serial Port")
                info += $" - {Description}";
            
            return info;
        }
    }

    public static class SerialPortDetector
    {
        public static List<SerialPortInfo> GetDetailedPortInfo()
        {
            var portInfoList = new List<SerialPortInfo>();
            var availablePorts = SerialPort.GetPortNames();

            foreach (var portName in availablePorts)
            {
                var description = GetPortDescription(portName);
                
                portInfoList.Add(new SerialPortInfo
                {
                    PortName = portName,
                    Description = description,
                    IsAvailable = true
                });
            }

            return portInfoList.OrderBy(p => p.PortName).ToList();
        }

        private static string GetPortDescription(string portName)
        {
            // Simple description based on common port patterns
            if (portName.StartsWith("COM"))
            {
                return $"Serial Port ({portName})";
            }
            
            return "Serial Port";
        }
    }
}