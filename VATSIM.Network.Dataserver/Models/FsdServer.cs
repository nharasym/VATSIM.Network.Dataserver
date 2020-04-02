using System;

namespace VATSIM.Network.Dataserver.Models
{
    public class FsdServer
    {
        public string Ident;
        public string HostnameOrIp;
        public string Location;
        public string Name;
        public int ClientsConnectionAllowed = 1;
    }
}