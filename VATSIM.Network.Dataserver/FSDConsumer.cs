using System;
using System.Timers;
using SimpleTCP;
using VATSIM.Network.Dataserver.Dtos;
using Timer = System.Timers.Timer;

namespace VATSIM.Network.Dataserver
{
    public class FsdConsumer
    {
        public readonly SimpleTcpClient Client = new SimpleTcpClient();
        public int DtoCount = 1;
        private readonly string _host;
        private readonly int _port;
        public event EventHandler<DtoReceivedEventArgs<AddClientDto>> AddClientDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<RemoveClientDto>> RemoveClientDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<PilotDataDto>> PilotDataDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<AtcDataDto>> AtcDataDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<FlightPlanDto>> FlightPlanDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<PingDto>> PingDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<AtisDataDto>> AtisDataDtoReceived;
        public event EventHandler<DtoReceivedEventArgs<NotifyDto>> NotifyDtoReceived;
        private readonly Timer _serverTimer = new Timer(60000);
        private readonly Timer _clientTimer = new Timer(5000);
        public readonly Timer AtisTimer = new Timer(30000);

        public FsdConsumer(string host, int port)
        {
            _host = host;
            _port = port;
            Client.Delimiter = 10;
            Client.DelimiterDataReceived += client_DelimiterDataReceived;
            PingDtoReceived += fsdConsumer_PingDtoReceived;
            _serverTimer.Elapsed += fsdConsumer_ServerTimerElapsed;
            _clientTimer.Elapsed += fsdConsumer_ClientTimerElapsed;
        }

        public void Start()
        {
            Client.Connect(_host, _port);
            Client.Write($"SYNC:*:DSERVER:B{DtoCount}:1:\r\n");
            _serverTimer.Start();
            _clientTimer.Start();
        }

        private void client_DelimiterDataReceived(object sender, Message packet)
        {
            try
            {
                string[] fields = packet.MessageString.Replace("\r", "").Split(":");
                switch (fields[0])
                {
                    case "ADDCLIENT":
                        AddClientDto addClientDto = AddClientDto.Deserialize(fields);
                        OnAddClientDtoReceived(new DtoReceivedEventArgs<AddClientDto>(addClientDto));
                        break;
                    case "RMCLIENT":
                        RemoveClientDto removeClientDto = RemoveClientDto.Deserialize(fields);
                        OnRemoveClientDtoReceived(new DtoReceivedEventArgs<RemoveClientDto>(removeClientDto));
                        break;
                    case "PD":
                        PilotDataDto pilotDataDto = PilotDataDto.Deserialize(fields);
                        OnPilotDataDtoReceived(new DtoReceivedEventArgs<PilotDataDto>(pilotDataDto));
                        break;
                    case "AD":
                        AtcDataDto atcDataDto = AtcDataDto.Deserialize(fields);
                        OnAtcDataDtoReceived(new DtoReceivedEventArgs<AtcDataDto>(atcDataDto));
                        break;
                    case "PLAN":
                        FlightPlanDto flightPlanDto = FlightPlanDto.Deserialize(fields);
                        OnFlightPlanDtoReceived(new DtoReceivedEventArgs<FlightPlanDto>(flightPlanDto));
                        break;
                    case "PING":
                        PingDto pingDto = PingDto.Deserialize(fields);
                        OnPingDtoReceived(new DtoReceivedEventArgs<PingDto>(pingDto));
                        break;
                    case "MC":
                        if (fields[5] == "25")
                        {
                            AtisDataDto atisDataDto = AtisDataDto.Deserialize(fields);
                            OnAtisDataDtoReceived(new DtoReceivedEventArgs<AtisDataDto>(atisDataDto));
                        }

                        break;
                    case "NOTIFY":
                        NotifyDto notifyDto = NotifyDto.Deserialize(fields);
                        OnNotifyDtoReceived(new DtoReceivedEventArgs<NotifyDto>(notifyDto));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected virtual void OnAddClientDtoReceived(DtoReceivedEventArgs<AddClientDto> e)
        {
            EventHandler<DtoReceivedEventArgs<AddClientDto>> handler = AddClientDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnRemoveClientDtoReceived(DtoReceivedEventArgs<RemoveClientDto> e)
        {
            EventHandler<DtoReceivedEventArgs<RemoveClientDto>> handler = RemoveClientDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnPilotDataDtoReceived(DtoReceivedEventArgs<PilotDataDto> e)
        {
            EventHandler<DtoReceivedEventArgs<PilotDataDto>> handler = PilotDataDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnAtcDataDtoReceived(DtoReceivedEventArgs<AtcDataDto> e)
        {
            EventHandler<DtoReceivedEventArgs<AtcDataDto>> handler = AtcDataDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnFlightPlanDtoReceived(DtoReceivedEventArgs<FlightPlanDto> e)
        {
            EventHandler<DtoReceivedEventArgs<FlightPlanDto>> handler = FlightPlanDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnPingDtoReceived(DtoReceivedEventArgs<PingDto> e)
        {
            EventHandler<DtoReceivedEventArgs<PingDto>> handler = PingDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnAtisDataDtoReceived(DtoReceivedEventArgs<AtisDataDto> e)
        {
            EventHandler<DtoReceivedEventArgs<AtisDataDto>> handler = AtisDataDtoReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void OnNotifyDtoReceived(DtoReceivedEventArgs<NotifyDto> e)
        {
            EventHandler<DtoReceivedEventArgs<NotifyDto>> handler = NotifyDtoReceived;
            handler?.Invoke(this, e);
        }

        private void fsdConsumer_PingDtoReceived(object sender, DtoReceivedEventArgs<PingDto> e)
        {
            PongDto pongDto = new PongDto(e.Dto.Source, "DSERVER", DtoCount, 1, e.Dto.Data);
            Client.Write(pongDto + "\r\n");
            DtoCount++;
        }

        private void fsdConsumer_ServerTimerElapsed(object source, ElapsedEventArgs e)
        {
            Client.Write($"SYNC:*:DSERVER:B{DtoCount}:1:\r\n");
            DtoCount++;
            NotifyDto notifyDto = new NotifyDto("*", "DSERVER", DtoCount, 1, 0, "DSERVER", "DSERVER",
                "vpdev@vatasim.net", "127.0.0.1",
                "v1.0", 0, "Toronto, Ontario");
            Client.Write(notifyDto + "\r\n");
            DtoCount++;
        }

        private void fsdConsumer_ClientTimerElapsed(object source, ElapsedEventArgs e)
        {
            AddClientDto addClientDto =
                new AddClientDto("*", "DSERVER", DtoCount, 1, "0", "DSERVER", "DCLIENT", 2, 1, 100, "DCLIENT", -1, 1);
            Client.Write(addClientDto + "\r\n");
            DtoCount++;
            AtcDataDto atcDataDto = new AtcDataDto("*", "DSERVER", DtoCount, 1, "DCLIENT", "99999", 1, 100, 1, 0.00000,
                0.00000);
            Client.Write(atcDataDto + "\r\n");
            DtoCount++;
        }
    }
}