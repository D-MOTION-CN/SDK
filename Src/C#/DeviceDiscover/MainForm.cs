using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DeviceDiscover;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

        Task.Run(ListenForResponses);
    }

    private void ButtonDiscover_Click(object sender, EventArgs e)
    {
        SendBroadcast();
    }


    private void SendBroadcast()
    {
        if (_listenPort is null)
        {
            return;
        }

        //Find all network interfaces
        var interfaces = NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(v => v.OperationalStatus == OperationalStatus.Up)
                        .ToArray();


        //Get the local IP addresses of all interfaces
        var ipAddressInformationArray =
            interfaces.Select(v => v.GetIPProperties()
                                    .UnicastAddresses // IPv4 only
                                    .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                             )
                      .ToArray();

        //Send broadcast messages on each interface.
        foreach (var ipAddressInformation in ipAddressInformationArray)
        {
            using var broadcaster = new UdpClient(new IPEndPoint(ipAddressInformation.Address, _listenPort.Value));
            broadcaster.EnableBroadcast = true;

            //This 4-byte data represents the 'SEARCH' command, which instructs motion controller to respond with its information.
            byte[] data = [255, 0, 0, 0];

            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, 10255); //Some motion controllers use port 10255.
            broadcaster.Send(data, data.Length, broadcastEndpoint);


            broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, 10000); //Some motion controllers use port 10000.
            broadcaster.Send(data, data.Length, broadcastEndpoint);
        }
    }


    private int? _listenPort;

    private readonly Regex _regex =
        new("Base\\((\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}):(\\d{1,5})\\)<-{1,2}>Host\\((\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}):(\\d{1,5})\\)-?(.*)?$");

    private static readonly JsonSerializerOptions JsonOptions = new()
                                                                 {
                                                                     PropertyNameCaseInsensitive = true
                                                                 };

    private void ListenForResponses()
    {
        using var listener = new UdpClient(0);
        _listenPort              = (listener.Client.LocalEndPoint as IPEndPoint)?.Port;
        listener.EnableBroadcast = true;

        while (true)
        {
            var remoteEndpoint  = new IPEndPoint(IPAddress.Any, 0);
            var receivedBytes   = listener.Receive(ref remoteEndpoint);
            var receivedMessage = Encoding.ASCII.GetString(receivedBytes);

            // Legacy Motion Controller Data Format Example:
            // Base(192.168.1.150:10000)<-->Host(192.168.1.200:10010)
            // These devices default to port 69 for TFTP.
            //
            // Modern Controllers Return JSON Data Format:
            // The newer JSON specification explicitly declares the TFTP port.
            //
            // TFTP Protocol Usage:
            // - Read/Write configuration files
            // - Firmware updates and exports
            // See reference implementation: "DeviceConfiguratorDemo"


            if (receivedMessage[0] == '{') //Treat messages starting with '{' as JSON format
            {
                var controllerInfo = JsonSerializer.Deserialize<ControllerInfo>(receivedMessage, JsonOptions);

                listBoxFound.Invoke(() => { listBoxFound.Items.Add($"{controllerInfo.HostIP}:{controllerInfo.HostPort}<-->{controllerInfo.BaseIP}:{controllerInfo.BasePort} ControllerType:{controllerInfo.ControllerType} TFTPPort:{controllerInfo.TFTPPort}"); });
            }
            else
            {
                var match = _regex.Match(receivedMessage);
                if (match.Success)
                {
                    var hostIp               = match.Groups[3].Value;
                    var hostPort             = int.Parse(match.Groups[4].Value);
                    var motionControllerIp   = match.Groups[1].Value;
                    var motionControllerPort = int.Parse(match.Groups[2].Value);

                    var motionControllerType = match.Groups[5].Value switch
                                               {
                                                   "TwinCAT" => "TwinCAT",
                                                   ""        => "STM32",
                                                   _         => throw new ArgumentOutOfRangeException()
                                               };

                    listBoxFound.Invoke(() => { listBoxFound.Items.Add($"{hostIp}:{hostPort}<-->{motionControllerIp}:{motionControllerPort} ControllerType:{motionControllerType} TFTPPort:69"); });
                }
            }
        }
    }
}

public class ControllerInfo
{
    public string BaseIP         { get; set; }
    public int    BasePort       { get; set; }
    public string HostIP         { get; set; }
    public int    HostPort       { get; set; }
    public int    TFTPPort       { get; set; }
    public string ControllerType { get; set; }
}