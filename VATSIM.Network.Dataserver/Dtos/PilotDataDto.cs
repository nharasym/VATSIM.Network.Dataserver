using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class PilotDataDto : FsdDto
    {
        public string IdentFlag;
        public string Callsign;
        public int Transponder;
        public int Rating;
        public double Latitude;
        public double Longitude;
        public int Altitude;
        public int GroundSpeed;
        public int Heading;
        public int PressureDifference;

        public PilotDataDto(string destination, string source, int packetNumber, int hopCount, string identFlag, string callsign, int transponder, int rating, double latitude, double longitude, int altitude, int groundSpeed, int heading, int pressureDifference) : base(destination, source, packetNumber, hopCount)
        {
            IdentFlag = identFlag;
            Callsign = callsign;
            Transponder = transponder;
            Rating = rating;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            GroundSpeed = groundSpeed;
            Heading = heading;
            PressureDifference = pressureDifference;
        }
        
        public static PilotDataDto Deserialize(string[] fields)
        {
            if (fields.Length < 14) throw new Exception("Failed to parse PD packet.");
            try
            {
                double hdgDbl = ParsePbh(fields[13]);
                return new PilotDataDto(fields[1], fields[2], Convert.ToInt32(fields[3].Substring(1)),
                    Convert.ToInt32(fields[4]), fields[5], fields[6], Convert.ToInt32(fields[7]), Convert.ToInt32(fields[8]),
                    Convert.ToDouble(fields[9]), Convert.ToDouble(fields[10]), Convert.ToInt32(fields[11]), Convert.ToInt32(fields[12]),
                    Convert.ToInt32(hdgDbl), Convert.ToInt32(fields[14]));
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse PD packet.", e);
            }
        }

        private static double ParsePbh(string pbhField)
        {
            uint pbh = uint.Parse(pbhField);
            uint hdg = (pbh >> 2) & 0x3FF;
            double hdgDbl = (double) hdg / 1024.0 * 360.0;
            if (hdgDbl < 0.0)
            {
                hdgDbl += 360.0;
            }
            else if (hdgDbl >= 360.0)
            {
                hdgDbl -= 360.0;
            }

            return hdgDbl;
        }
    }
}