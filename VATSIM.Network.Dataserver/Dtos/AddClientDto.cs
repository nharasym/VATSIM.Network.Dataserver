using System;
using System.Text;

namespace VATSIM.Network.Dataserver.Dtos
{
    public class AddClientDto : FsdDto
    {
        public string Callsign;
        public string Cid;
        public int Hidden;
        public int ProtocolRevision;
        public int Rating;
        public string RealName;
        public string Server;
        public int SimType;
        public int Type;

        public AddClientDto(string destination, string source, int packetNumber, int hopCount, string cid,
            string server, string callsign, int type, int rating, int protocolRevision, string realName, int simType,
            int hidden) : base(destination, source, packetNumber, hopCount)
        {
            Cid = cid;
            Server = server;
            Callsign = callsign;
            Type = type;
            Rating = rating;
            ProtocolRevision = protocolRevision;
            RealName = realName;
            SimType = simType;
            Hidden = hidden;
        }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder("ADDCLIENT");
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
            message.Append(Cid);
            message.Append(":");
            message.Append(Server);
            message.Append(":");
            message.Append(Callsign);
            message.Append(":");
            message.Append(Type);
            message.Append(":");
            message.Append(Rating);
            message.Append(":");
            message.Append(ProtocolRevision);
            message.Append(":");
            message.Append(RealName);
            message.Append(":");
            message.Append(SimType);
            message.Append(":");
            message.Append(Hidden);
            return message.ToString();
        }

        public static AddClientDto Deserialize(string[] fields)
        {
            if (fields.Length < 12) throw new Exception("Failed to parse ADDCLIENT packet.");
            try
            {
                return new AddClientDto(fields[1], fields[2], Convert.ToInt32(fields[3].Substring(1)),
                    Convert.ToInt32(fields[4]), fields[5], fields[6], fields[7], Convert.ToInt32(fields[8]),
                    Convert.ToInt32(fields[9]), Convert.ToInt32(fields[10]), fields[11], Convert.ToInt32(fields[12]),
                    Convert.ToInt32(fields[13]));
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse ADDCLIENT packet.", e);
            }
        }
    }
}