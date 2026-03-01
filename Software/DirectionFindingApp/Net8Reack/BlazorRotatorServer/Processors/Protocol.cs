using BlazorRotatorServer.Models;
using System.Globalization;
using System.Reflection.Emit;

namespace BlazorRotatorServer.Processors
{
    public class Protocol
    {
        public void ProcessRx(string message, Rotator rotator)
        {
            if (string.IsNullOrWhiteSpace(message) || rotator == null)
                return;

            string cleanMessage = message.Trim().TrimEnd(';');
            string[] parts = cleanMessage.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();

                if (trimmedPart.StartsWith("AZ,"))
                {
                    string angleStr = trimmedPart.Substring(3).Trim();
                    if (float.TryParse(angleStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float azimuthFloat))
                    {
                        rotator.Parameters.Azimuth = azimuthFloat;
                    }
                }
                else if (trimmedPart.StartsWith("AN,"))
                {
                    string angleStr = trimmedPart.Substring(3).Trim();
                    if (float.TryParse(angleStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float angleFloat))
                    {
                        rotator.Parameters.Angle = angleFloat;
                        rotator.Parameters.AngleNormalized = angleFloat % 360;
                    }
                }
            }
        }
    }
}
