using System;
using Newtonsoft.Json;

namespace VATSIM.Network.Dataserver.Models
{
    public class FsdClient
    {
        public string Callsign;
        public string Cid;
        public string Realname;
        public string Clienttype;
        public string Frequency;
        public double Latitude;
        public double Longitude;
        public int Altitude;
        public int Groundspeed;
        public string PlannedAircraft;
        public string PlannedTascruise;
        public string PlannedDepairport;
        public string PlannedAltitude;
        public string PlannedDestairport;
        public string Server;
        public int Protrevision;
        public int Rating;
        public int Transponder;
        public int Facilitytype;
        public int Visualrange;
        public string PlannedRevision;
        public string PlannedFlighttype;
        public string PlannedDeptime;
        public string PlannedActdeptime;
        public string PlannedHrsenroute;
        public string PlannedMinenroute;
        public string PlannedHrsfuel;
        public string PlannedMinfuel;
        public string PlannedAltairport;
        public string PlannedRemarks;
        public string PlannedRoute;
        public double PlannedDepairportLat;
        public double PlannedDepairportLon;
        public double PlannedDestairportLat;
        public double PlannedDestairportLon;
        public string AtisMessage;
        public DateTime TimeLastAtisReceived;
        public DateTime TimeLogon;
        public int Heading;
        public double QnhIHg;
        public int QnhMb;
        [JsonIgnore]
        public bool AppendAtis = false;
        [JsonIgnore]
        public DateTime LastUpdated;
    }
}