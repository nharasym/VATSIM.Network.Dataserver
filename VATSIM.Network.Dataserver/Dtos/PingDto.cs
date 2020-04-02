using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class PingDto : FsdDto
    {
        public string Data { get; }
        
        public PingDto(string destination, string source, int packetNumber, int hopCount, string data) : base(destination, source, packetNumber, hopCount)
        {
            Data = data;
        }

        public static PingDto Deserialize(string[] fields)
        {
            if (fields.Length < 6)
            {
                throw new FormatException("Failed to parse PING packet.");
            }
            try
            {
                return new PingDto(fields[1], fields[2], Convert.ToInt32(fields[3].Substring(1)),
                    Convert.ToInt32(fields[4]), fields[5]);
            }
            catch (Exception e)
            {
                throw new FormatException("Failed to parse PING packet.", e);
            }
        }
    }
}