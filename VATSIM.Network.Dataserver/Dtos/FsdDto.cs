namespace VATSIM.Network.Dataserver.Dtos
{
    public class FsdDto
    {
        public string Destination;
        public string Source;
        public int PacketNumber;
        public int HopCount;

        public FsdDto(string destination, string source, int packetNumber, int hopCount)
        {
            Destination = destination;
            Source = source;
            PacketNumber = packetNumber;
            HopCount = hopCount;
        }
    }
    
}