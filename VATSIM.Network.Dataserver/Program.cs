using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Prometheus;
using VATSIM.Network.Dataserver.Dtos;
using VATSIM.Network.Dataserver.Models;
using VATSIM.Network.Dataserver.Resources;
using Metrics = Prometheus.Metrics;
using Timer = System.Timers.Timer;

namespace VATSIM.Network.Dataserver
{
    internal static class Program
    {
        private static readonly FsdConsumer FsdConsumer = new FsdConsumer("canada.server.vatsim.net", 4113);
        private static List<FsdClient> _fsdClients = new List<FsdClient>();
        private static List<FsdClient> _fsdPrefiles = new List<FsdClient>();
        private static List<FsdServer> _fsdServers = new List<FsdServer>();
        private static readonly Timer TimeoutTimer = new Timer(60000);
        private static readonly Timer FileTimer = new Timer(15000);
        private static readonly Timer PrometheusMetricsTimer = new Timer(5000);

        private static readonly AmazonS3Config AmazonS3Config = new AmazonS3Config
        {
            ServiceURL = "https://sfo2.digitaloceanspaces.com"
        };

        private static readonly AmazonS3Client AmazonS3Client = new AmazonS3Client(AmazonS3Config);

        private static readonly Gauge TotalConnections = Metrics.CreateGauge("fsd_total_connections",
            "Total number of connections to the FSD network.", new GaugeConfiguration
            {
                LabelNames = new[] {"server"}
            });

        private static readonly Gauge UniqueConnections = Metrics.CreateGauge("fsd_unique_connections",
            "Unique number of connections to the FSD network.");

        private static async Task Main()
        {
            FsdConsumer.AddClientDtoReceived += fsdConsumer_AddClientDtoReceived;
            FsdConsumer.RemoveClientDtoReceived += fsdConsumer_RemoveClientDtoReceived;
            FsdConsumer.PilotDataDtoReceived += fsdConsumer_PilotDataDtoReceived;
            FsdConsumer.AtcDataDtoReceived += fsdConsumer_AtcDataDtoReceived;
            FsdConsumer.FlightPlanDtoReceived += fsdConsumer_FlightPlanDtoReceived;
            FsdConsumer.AtisDataDtoReceived += fsdConsumer_AtisDataDtoReceived;
            FsdConsumer.NotifyDtoReceived += fsdConsumer_NotifyDtoReceived;
            FsdConsumer.AtisTimer.Elapsed += fsdConsumer_AtisTimerElapsed;
            TimeoutTimer.Elapsed += RemoveTimedOutConnections;
            FileTimer.Elapsed += WriteDataFiles;
            PrometheusMetricsTimer.Elapsed += SetPrometheusConnectionCounts;

            FsdConsumer.Start();
            TimeoutTimer.Start();
            FsdConsumer.AtisTimer.Start();
            FileTimer.Start();
            PrometheusMetricsTimer.Start();
            MetricServer metricServer = new MetricServer(port: 8501);
            metricServer.Start();
            await Task.Run(() => Thread.Sleep(Timeout.Infinite));
        }

        private static void fsdConsumer_AddClientDtoReceived(object sender, DtoReceivedEventArgs<AddClientDto> p)
        {
            if (_fsdClients.Any(c => c.Callsign == p.Dto.Callsign) || p.Dto.Callsign == "AFVDATA" || p.Dto.Callsign == "SUP" || p.Dto.Callsign == "DATA" || p.Dto.Callsign == "DATASVR" || p.Dto.Callsign == "DCLIENT")
            {
                return;
            }
            FsdClient fsdClient = new FsdClient
            {
                Callsign = p.Dto.Callsign,
                Cid = p.Dto.Cid,
                Protrevision = p.Dto.ProtocolRevision,
                Rating = p.Dto.Rating,
                Realname = p.Dto.RealName,
                Server = p.Dto.Server,
                Clienttype = p.Dto.Type == 1 ? "PILOT" : "ATC",
                TimeLogon = DateTime.UtcNow,
                TimeLastAtisReceived = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            _fsdClients.Add(fsdClient);
            Console.WriteLine($"{p.Dto.Callsign} connected to the network.");
            Console.WriteLine(_fsdClients.Count);
        }

        private static void fsdConsumer_RemoveClientDtoReceived(object sender, DtoReceivedEventArgs<RemoveClientDto> p)
        {
            _fsdClients.RemoveAll(c => c.Callsign == p.Dto.Callsign);
            Console.WriteLine($"{p.Dto.Callsign} disconnected from the network.");
            Console.WriteLine(_fsdClients.Count);
        }

        private static void fsdConsumer_PilotDataDtoReceived(object sender, DtoReceivedEventArgs<PilotDataDto> p)
        {
            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.Callsign);
            fsdClient.Transponder = p.Dto.Transponder;
            fsdClient.Latitude = p.Dto.Latitude;
            fsdClient.Longitude = p.Dto.Longitude;
            fsdClient.Altitude = p.Dto.Altitude;
            fsdClient.Groundspeed = p.Dto.GroundSpeed;
            fsdClient.Heading = p.Dto.Heading;
            fsdClient.QnhIHg = Math.Round(29.92 - (p.Dto.PressureDifference / 1000.0), 2);
            fsdClient.QnhMb = (int) Math.Round(fsdClient.QnhIHg * 33.864);
            fsdClient.LastUpdated = DateTime.UtcNow;
        }

        private static void fsdConsumer_AtcDataDtoReceived(object sender, DtoReceivedEventArgs<AtcDataDto> p)
        {
            if (p.Dto.Callsign == "AFVDATA" || p.Dto.Callsign == "SUP" || p.Dto.Callsign == "DATA" || p.Dto.Callsign == "DATASVR" || p.Dto.Callsign == "DCLIENT")
            {
                return;
            }
            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.Callsign);
            fsdClient.Frequency = p.Dto.Frequency.Insert(2, ".").Insert(0, "1");
            fsdClient.Latitude = p.Dto.Latitude;
            fsdClient.Longitude = p.Dto.Longitude;
            fsdClient.Facilitytype = p.Dto.FacilityType;
            fsdClient.Visualrange = p.Dto.VisualRange;
            fsdClient.LastUpdated = DateTime.UtcNow;
        }

        private static async void fsdConsumer_FlightPlanDtoReceived(object sender,
            DtoReceivedEventArgs<FlightPlanDto> p)
        {
            try
            {
                FsdClient fsdClient;
                bool prefile;
                if (_fsdClients.All(c => c.Callsign != p.Dto.Callsign))
                {
                    string xml =
                        await new WebClient().DownloadStringTaskAsync(
                            $"https://cert.vatsim.net/cert/vatsimnet/idstatus.php?cid={p.Dto.Cid}");
                    XmlDocument certRecord = new XmlDocument();
                    certRecord.LoadXml(xml);
                    string lastName = certRecord.SelectSingleNode("/root/user/name_last").InnerText;
                    string firstName = certRecord.SelectSingleNode("/root/user/name_first").InnerText;
                    Console.WriteLine($"Prefile received for {firstName} {lastName}.");
                    fsdClient = new FsdClient
                    {
                        Cid = p.Dto.Cid,
                        Realname = $"{firstName} {lastName}",
                        Callsign = p.Dto.Callsign,
                        LastUpdated = DateTime.UtcNow,
                    };
                    prefile = true;
                }
                else
                {
                    fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.Callsign);
                    prefile = false;
                }

                fsdClient.PlannedAircraft = p.Dto.Aircraft;
                fsdClient.PlannedTascruise = p.Dto.CruiseSpeed;
                fsdClient.PlannedDepairport = p.Dto.DepartureAirport;
                fsdClient.PlannedAltitude = p.Dto.Altitude;
                fsdClient.PlannedDestairport = p.Dto.DestinationAirport;
                fsdClient.PlannedRevision = p.Dto.Revision;
                fsdClient.PlannedFlighttype = p.Dto.Type;
                fsdClient.PlannedDeptime = p.Dto.EstimatedDepartureTime;
                fsdClient.PlannedActdeptime = p.Dto.ActualDepartureTime;
                fsdClient.PlannedHrsenroute = p.Dto.HoursEnroute;
                fsdClient.PlannedMinenroute = p.Dto.MinutesEnroute;
                fsdClient.PlannedHrsfuel = p.Dto.HoursFuel;
                fsdClient.PlannedMinfuel = p.Dto.MinutesFuel;
                fsdClient.PlannedAltairport = p.Dto.AlternateAirport;
                fsdClient.PlannedRemarks = p.Dto.Remarks;
                fsdClient.PlannedRoute = p.Dto.Route;
                if (prefile)
                {
                    _fsdPrefiles.Add(fsdClient);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void fsdConsumer_AtisDataDtoReceived(object sender, DtoReceivedEventArgs<AtisDataDto> p)
        {
            Console.WriteLine($"ATIS data for {p.Dto.From}: {p.Dto.Data}");
            FsdClient fsdClient = _fsdClients.Find(c => c.Callsign == p.Dto.From);
            if (p.Dto.Type == "T")
            {
                if (fsdClient.AppendAtis)
                {
                     fsdClient.AtisMessage += $"^§{p.Dto.Data}";
                }
                else
                {
                    fsdClient.AtisMessage = p.Dto.Data;
                    fsdClient.AppendAtis = true;
                }
            }
            else if (p.Dto.Type == "E")
            {
                fsdClient.AppendAtis = false;
            }
        }

        private static void fsdConsumer_NotifyDtoReceived(object sender, DtoReceivedEventArgs<NotifyDto> p)
        {
            if (p.Dto.Hostname == "127.0.0.1" || p.Dto.Name.ToLower().Contains("data") ||
                p.Dto.Name.ToLower().Contains("afv") || _fsdServers.Any(s => s.Name == p.Dto.Name))
            {
                return;
            }
            FsdServer fsdServer = new FsdServer
            {
                Ident = p.Dto.Ident,
                HostnameOrIp = p.Dto.Hostname,
                Location = p.Dto.Location,
                Name = p.Dto.Name,
                ClientsConnectionAllowed = 1
            };
            _fsdServers.Add(fsdServer);
        }

        private static void fsdConsumer_AtisTimerElapsed(object source, ElapsedEventArgs e)
        {
            foreach (AtisRequestDto atisRequestDto in _fsdClients.Select(fsdClient =>
                new AtisRequestDto(fsdClient.Callsign, "DSERVER", FsdConsumer.DtoCount, 1, "DCLIENT")))
            {
                FsdConsumer.Client.Write(atisRequestDto + "\r\n");
                FsdConsumer.DtoCount++;
            }
        }

        private static void RemoveTimedOutConnections(object source, ElapsedEventArgs e)
        {
            _fsdClients.RemoveAll(c => (DateTime.UtcNow - c.LastUpdated).Minutes > 4);
            _fsdPrefiles.RemoveAll(p => (DateTime.UtcNow - p.LastUpdated).Hours > 2);
        }

        private static void WriteDataFiles(object source, ElapsedEventArgs e)
        {
            try
            {
                string contents = GenerateDatafileText();

                PutObjectRequest txtPutRequest = new PutObjectRequest
                {
                    BucketName = "vatsim-data-us",
                    Key = "vatsim-data.txt",
                    ContentBody = contents,
                    CannedACL = S3CannedACL.PublicRead
                };
               AmazonS3Client.PutObjectAsync(txtPutRequest);
                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                };
                JsonFileResource jsonFileResource = new JsonFileResource(_fsdClients, _fsdServers, _fsdPrefiles);
                string json = JsonConvert.SerializeObject(jsonFileResource, new JsonSerializerSettings
                {
                    ContractResolver = contractResolver,
                });
                PutObjectRequest jsonPutRequest = new PutObjectRequest
                {
                    BucketName = "vatsim-data-us",
                    Key = "vatsim-data-v2.json",
                    ContentBody = json,
                    CannedACL = S3CannedACL.PublicRead
                };
                AmazonS3Client.PutObjectAsync(jsonPutRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string GenerateDatafileText()
        {
            StringBuilder fileContents = new StringBuilder();
            fileContents.AppendLine(
                "; !CLIENTS section -         callsign:cid:realname:clienttype:frequency:latitude:longitude:altitude:groundspeed:planned_aircraft:planned_tascruise:planned_depairport:planned_altitude:planned_destairport:server:protrevision:rating:transponder:facilitytype:visualrange:planned_revision:planned_flighttype:planned_deptime:planned_actdeptime:planned_hrsenroute:planned_minenroute:planned_hrsfuel:planned_minfuel:planned_altairport:planned_remarks:planned_route:planned_depairport_lat:planned_depairport_lon:planned_destairport_lat:planned_destairport_lon:atis_message:time_last_atis_received:time_logon:heading:QNH_iHg:QNH_Mb:");
            fileContents.AppendLine("!GENERAL:");
            fileContents.AppendLine("VERSION = 9");
            fileContents.AppendLine("RELOAD = 1");
            fileContents.AppendLine($"UPDATE = {DateTime.UtcNow:yyyyMMddHHmmss}");
            fileContents.AppendLine($"CONNECTED CLIENTS = {_fsdClients.Count}");
            fileContents.AppendLine(
                $"UNIQUE USERS = {_fsdClients.GroupBy(c => c.Cid).Select(g => g.FirstOrDefault()).Count()}");
            fileContents.AppendLine("!CLIENTS:");
            foreach (FsdClient fsdClient in _fsdClients)
            {
                fileContents.AppendLine(
                    $"{fsdClient.Callsign}:{fsdClient.Cid}:{fsdClient.Realname}:{fsdClient.Clienttype}:{fsdClient.Frequency}:{fsdClient.Latitude}:{fsdClient.Longitude}:{fsdClient.Altitude}:{fsdClient.Groundspeed}:{fsdClient.PlannedAircraft}:{fsdClient.PlannedTascruise}:{fsdClient.PlannedDepairport}:{fsdClient.PlannedAltitude}:{fsdClient.PlannedDestairport}:{fsdClient.Server}:{fsdClient.Protrevision}:{fsdClient.Rating}:{fsdClient.Transponder}:{fsdClient.Facilitytype}:{fsdClient.Visualrange}:{fsdClient.PlannedRevision}:{fsdClient.PlannedFlighttype}:{fsdClient.PlannedDeptime}:{fsdClient.PlannedActdeptime}:{fsdClient.PlannedHrsenroute}:{fsdClient.PlannedMinenroute}:{fsdClient.PlannedHrsfuel}:{fsdClient.PlannedMinfuel}:{fsdClient.PlannedAltairport}:{fsdClient.PlannedRemarks}:{fsdClient.PlannedRoute}:{fsdClient.PlannedDepairportLat}:{fsdClient.PlannedDepairportLon}:{fsdClient.PlannedDestairportLat}:{fsdClient.PlannedDestairportLon}:{fsdClient.AtisMessage}:{fsdClient.TimeLastAtisReceived:yyyyMMddHHmmss}:{fsdClient.TimeLogon:yyyyMMddHHmmss}:{fsdClient.Heading}:{fsdClient.QnhIHg}:{fsdClient.QnhMb}:");
            }

            fileContents.AppendLine("!SERVERS:");
            foreach (FsdServer fsdServer in _fsdServers)
            {
                fileContents.AppendLine(
                    $"{fsdServer.Ident}:{fsdServer.HostnameOrIp}:{fsdServer.Location}:{fsdServer.Name}:{fsdServer.ClientsConnectionAllowed}:");
            }

            fileContents.AppendLine(";");
            fileContents.AppendLine(";");
            fileContents.AppendLine("!PREFILE:");
            foreach (FsdClient fsdClient in _fsdPrefiles)
            {
                fileContents.AppendLine(
                    $"{fsdClient.Callsign}:{fsdClient.Cid}:{fsdClient.Realname}:{fsdClient.Clienttype}:{fsdClient.Frequency}:{fsdClient.Latitude}:{fsdClient.Longitude}:{fsdClient.Altitude}:{fsdClient.Groundspeed}:{fsdClient.PlannedAircraft}:{fsdClient.PlannedTascruise}:{fsdClient.PlannedDepairport}:{fsdClient.PlannedAltitude}:{fsdClient.PlannedDestairport}:{fsdClient.Server}:{fsdClient.Protrevision}:{fsdClient.Rating}:{fsdClient.Transponder}:{fsdClient.Facilitytype}:{fsdClient.Visualrange}:{fsdClient.PlannedRevision}:{fsdClient.PlannedFlighttype}:{fsdClient.PlannedDeptime}:{fsdClient.PlannedActdeptime}:{fsdClient.PlannedHrsenroute}:{fsdClient.PlannedMinenroute}:{fsdClient.PlannedHrsfuel}:{fsdClient.PlannedMinfuel}:{fsdClient.PlannedAltairport}:{fsdClient.PlannedRemarks}:{fsdClient.PlannedRoute}:{fsdClient.PlannedDepairportLat}:{fsdClient.PlannedDepairportLon}:{fsdClient.PlannedDestairportLat}:{fsdClient.PlannedDestairportLon}:{fsdClient.AtisMessage}:{fsdClient.TimeLastAtisReceived:yyyyMMddHHmmss}:{fsdClient.TimeLogon:yyyyMMddHHmmss}:{fsdClient.Heading}:{fsdClient.QnhIHg}:{fsdClient.QnhMb}:");
            }

            return fileContents.ToString();
        }

        private static void SetPrometheusConnectionCounts(object source, ElapsedEventArgs e)
        {
            foreach (FsdServer fsdServer in _fsdServers)
            {
                TotalConnections.WithLabels(fsdServer.Name).Set(_fsdClients.Count(c => c.Server == fsdServer.Name));
            }

            UniqueConnections.Set(_fsdClients.GroupBy(c => c.Cid).Select(g => g.FirstOrDefault()).Count());
        }
    }
}
