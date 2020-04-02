namespace VATSIM.Network.Dataserver.Dtos
{
    public class FsdDto
    {
        public string Destination { get; }
        public string Source { get; }
        public int PacketNumber { get; }
        public int HopCount { get; }

        public FsdDto(string destination, string source, int packetNumber, int hopCount)
        {
            Destination = destination;
            Source = source;
            PacketNumber = packetNumber;
            HopCount = hopCount;
        }
    }
    
}