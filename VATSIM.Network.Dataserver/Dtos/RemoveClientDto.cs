using System;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class RemoveClientDto : FsdDto
    {
        public string Callsign { get; }
        
        public RemoveClientDto(string destination, string source, int packetNumber, int hopCount, string callsign) :
            base(destination, source, packetNumber, hopCount)
        {
            Callsign = callsign;
        }

        public static RemoveClientDto Deserialize(string[] fields)
        {
            if (fields.Length < 6)
            {
                throw new FormatException("Failed to parse RMCLIENT packet.");
            }
            try
            {
                return new RemoveClientDto(fields[1], fields[2], Convert.ToInt32(fields[3].Substring(1)),
                    Convert.ToInt32(fields[4]), fields[5]);
            }
            catch (Exception e)
            {
                throw new FormatException("Failed to parse RMCLIENT packet.", e);
            }
        }
    }
}