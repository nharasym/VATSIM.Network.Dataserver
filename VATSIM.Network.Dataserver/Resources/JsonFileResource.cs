using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models;

namespace VATSIM.Network.Dataserver.Resources
{
    public class JsonFileResource
    {
        public List<FsdClient> Clients;
        public List<FsdServer> Servers;
        public List<FsdClient> Prefiles;

        public JsonFileResource(List<FsdClient> clients, List<FsdServer> servers, List<FsdClient> prefiles)
        {
            Clients = clients;
            Servers = servers;
            Prefiles = prefiles;
        }
    }
}