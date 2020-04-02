using System.Collections.Generic;
using VATSIM.Network.Dataserver.Models;

namespace VATSIM.Network.Dataserver.Resources
{
    public class JsonFileResource
    {
        public List<FsdClient> Clients { get; }
        public List<FsdServer> Servers { get; }
        public List<FsdClient> Prefiles { get; }

        public JsonFileResource(List<FsdClient> clients, List<FsdServer> servers, List<FsdClient> prefiles)
        {
            Clients = clients;
            Servers = servers;
            Prefiles = prefiles;
        }
    }
}