using System;
using System.Text;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class AtcDataDto : FsdDto
    {
        public string Callsign;
        public string Frequency;
        public int FacilityType;
        public int VisualRange;
        public int Rating;
        public double Latitude;
        public double Longitude;

        public AtcDataDto(string destination, string source, int packetNumber, int hopCount, string callsign, string frequency, int facilityType, int visualRange, int rating, double latitude, double longitude) : base(destination, source, packetNumber, hopCount)
        {
            Callsign = callsign;
            Frequency = frequency;
            FacilityType = facilityType;
            VisualRange = visualRange;
            Rating = rating;
            Latitude = latitude;
            Longitude = longitude;
        }
        
        public override string ToString()
        {
            StringBuilder message = new StringBuilder("AD");
            message.Append(":");
            message.Append(Destination);
            message.Append(":");
            message.Append(Source);
            message.Append(":");
            message.Append("B");
            message.Append(PacketNumber);
            message.Append(":");
            message.Append(HopCount);
            message.Append(":");
            message.Append(Callsign);
            message.Append(":");
            message.Append(Frequency);
            message.Append(":");
            message.Append(FacilityType);
            message.Append(":");
            message.Append(VisualRange);
            message.Append(":");
            message.Append(Rating);
            message.Append(":");
            message.Append(Latitude);
            message.Append(":");
            message.Append(Longitude);
            message.Append(":");
            message.Append("0"); // Transceiver altitude
            return message.ToString();
        }

        public static AtcDataDto Deserialize(string[] fields)
        {
            if (fields.Length < 13) throw new Exception("Failed to parse PD packet.");
            try
            {
                return new AtcDataDto(fields[1], fields[2], Convert.ToInt32(fields[3].Substring(1)),
                    Convert.ToInt32(fields[4]), fields[5], fields[6], Convert.ToInt32(fields[7]), Convert.ToInt32(fields[8]),
                    Convert.ToInt32(fields[9]), Convert.ToDouble(fields[10]), Convert.ToDouble(fields[11]));
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse PD packet.", e);
            }
        }
    }
}