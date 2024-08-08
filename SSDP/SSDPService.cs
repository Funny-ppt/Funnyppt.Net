namespace Funnyppt.Net.SSDP;

// see https://en.wikipedia.org/wiki/Simple_Service_Discovery_Protocol
internal class SSDPService : IDisposable {
    private const int Port = 1900;
    static readonly byte[] MSearchAll_Body = BuildHttpBody(
        "M-SEARCH * HTTP/1.1",
        "HOST: 239.255.255.250:1900",
        "MAN: \"ssdp:discover\"",
        "MX: 3",
        "ST: ssdp:all"
    );

    static readonly IPAddress MulticastAddress = IPAddress.Parse("239.255.255.250");
    static readonly IPAddress MulticastAddress_ipv6_link_local = IPAddress.Parse("ff02::c");
    static readonly IPAddress MulticastAddress_ipv6_site_local = IPAddress.Parse("ff05::c");

    private bool disposedValue;
    private UdpClient udpClient;
    private bool ownUdpClient;

    protected UdpClient UdpClient => udpClient;

    private static byte[] BuildHttpBody(params ReadOnlySpan<string> lines) {
        var nbytes = (lines.Length + 1) * 2;
        for (int i = 0; i < lines.Length; i++) {
            nbytes += lines[i].Length;
        }

        var bytes = new byte[nbytes];
        var pos = 0;
        for (int i = 0; i < lines.Length; i++) {
            var line = lines[i];
            pos += Encoding.ASCII.GetBytes(line, 0, line.Length, bytes, pos);
            bytes[pos++] = (byte)'\r';
            bytes[pos++] = (byte)'\n';
        }
        bytes[pos++] = (byte)'\r';
        bytes[pos++] = (byte)'\n';
        return bytes;
    }
    private static UdpClient CreateUdpClient() {
        var udpClient = new UdpClient {
            EnableBroadcast = true,
            ExclusiveAddressUse = false
        };
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
        udpClient.JoinMulticastGroup(MulticastAddress);
        return udpClient;
    }

    public SSDPService(UdpClient? udpClient = null, bool ownUdpClient = false) {
        if (udpClient == null) {
            this.udpClient = CreateUdpClient();
            this.ownUdpClient = true;
        } else {
            // if (udpClient.Client.LocalEndPoint is IPEndPoint localEndPoint && localEndPoint.Port != Port) {
            //     throw new ArgumentException($"Port of the provided UdpClient is {localEndPoint.Port}, not {Port}", nameof(udpClient));
            // }
            // if (udpClient.EnableBroadcast == false) {
            //     throw new ArgumentException("UdpClient.EnableBroadcast must be true", nameof(udpClient));
            // }
            this.udpClient = udpClient;
            this.ownUdpClient = ownUdpClient;
        }
    }

    /// <summary>
    /// Starts listening for SSDP M-Search requests. Should only called once.
    /// </summary>
    public async Task StartAsync(CancellationToken token = default) {
        while (true) {
            if (token.IsCancellationRequested) {
                throw new TaskCanceledException("SSDP Server is stopped");
            }

            UdpReceiveResult received = await udpClient.ReceiveAsync(token);
            string request = Encoding.UTF8.GetString(received.Buffer);
            Debug.Print($"Received: {request}");

            if (request.Contains("M-SEARCH")) {
                string response = "HTTP/1.1 200 OK\r\n" +
                                  "CACHE-CONTROL: max-age=1800\r\n" +
                                  "DATE: " + DateTime.Now.ToString("R") + "\r\n" +
                                  "EXT:\r\n" +
                                  "LOCATION: http://yourdevice/location\r\n" +
                                  "SERVER: YourServer UPnP/1.0 YourProduct/1.0\r\n" +
                                  "ST: urn:schemas-upnp-org:device:YourDevice:1\r\n" +
                                  "USN: uuid:YourUniqueID::urn:schemas-upnp-org:device:YourDevice:1\r\n";

                byte[] responseData = Encoding.UTF8.GetBytes(response);
                await udpClient.SendAsync(responseData, responseData.Length, received.RemoteEndPoint);
            }
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (ownUdpClient && !disposedValue) {
            if (disposing) {
                udpClient.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
    }
}