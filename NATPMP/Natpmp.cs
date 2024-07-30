using System.Buffers;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
namespace Funnyppt.Net.NATPMP;

public enum NatpmpProtocol : byte {
    UDP = 1,
    TCP = 2,
}

public enum NatpmpResultCode : short {
    UnknownError = -1,
    Success = 0,
    UnsupportedVersion = 1,
    NotAuthorized = 2,
    NetworkFailure = 3,
    OutOfResources = 4,
    UnsupportedOpCode = 5,
}

public enum NatpmpResponseType : short
{
    Unknown = -1,
    PublicAddress = 0,
    UDPPortMapping = 1,
    TCPPortMapping = 2,
}

// TODO: 该结构可能需要重构以更恰当符合C#编程最佳实践
public struct NatpmpResponse {
    public NatpmpResponseType Type { get; set; }
    public NatpmpResultCode ResultCode { get; set; }
    public uint Epoch { get; set; }
    public IPAddress? PublicAddress { get; set; }
    public ushort PrivatePort { get; set; }
    public ushort MappedPublicPort { get; set; }
    public uint Lifetime { get; set; }
}

/// <summary>
/// 当前构建为仅支持同时发送单条请求（不具备本地路由响应消息包到对应请求的功能）
/// </summary>
public sealed class Natpmp : IDisposable {
    const int NATPMP_PORT = 5351;
    const int NATPMP_TIMEOUT_MS = 250;
    const int NATPMP_MAX_RETRIES = 9;

    Socket socket;
    bool disposedValue;

    public Natpmp(IPAddress? gateway = null) {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
            Blocking = false
        };
        gateway ??= GetGatewayAddress() ?? throw new InvalidOperationException("No gateway address found");
        socket.Connect(gateway, NATPMP_PORT);
    }

    private static ushort htons(ushort value) => (ushort)IPAddress.HostToNetworkOrder((short)value);
    private static int htonl(int value) => IPAddress.HostToNetworkOrder(value);
    private static short ntohs(ReadOnlySpan<byte> span) => IPAddress.NetworkToHostOrder(BitConverter.ToInt16(span));
    private static int ntohl(ReadOnlySpan<byte> span) => IPAddress.NetworkToHostOrder(BitConverter.ToInt32(span));

    public Task<NatpmpResponse> RequestPortMappingAsync(
        NatpmpProtocol protocol, ushort privatePort, ushort publicPort, int lifetime = 3600)
    {
        byte[] bytes = ArrayPool<byte>.Shared.Rent(12);
        Span<byte> span = bytes;
        bytes[0] = 0x0;
        bytes[1] = (byte)protocol;
        bytes[2] = 0x0;
        bytes[3] = 0x0;
        BitConverter.TryWriteBytes(span[4..6], htons(privatePort));
        BitConverter.TryWriteBytes(span[6..8], htons(publicPort));
        BitConverter.TryWriteBytes(span[8..12], htonl(lifetime));
        return SendRequestAsync(bytes, 0, 12, true,
            resp => resp.Type == (NatpmpResponseType)protocol && resp.PrivatePort == privatePort);
    }

    static readonly byte[] __RequestPublicAddressData = [0, 0];
    public Task<NatpmpResponse> RequestPublicAddressAsync() {
        return SendRequestAsync(__RequestPublicAddressData, 0, 2, false,
            resp => resp.Type == NatpmpResponseType.PublicAddress);
    }

    private async Task<NatpmpResponse> SendRequestAsync(
        byte[] data, int offset, int length, bool dataIsRented, Func<NatpmpResponse, bool> validator)
    {
        var retries = NATPMP_MAX_RETRIES;
        var timeout = NATPMP_TIMEOUT_MS;
        var buf = ArrayPool<byte>.Shared.Rent(16);
        try {
            while (retries > 0) {
                await socket.SendAsync(data[offset..(offset+length)]);
                using var cts = new CancellationTokenSource(timeout);
                try
                {
                    var bytesRead = await socket.ReceiveAsync(buf, SocketFlags.None, cts.Token);
                    // TODO: 需要检查数据包源是否确实来自网关
                    var resp = Parse(buf);
                    if (validator(resp)) { return resp; }
                }
                catch (OperationCanceledException) // timeout
                {
                    // TODO: logging?
                }
                timeout <<= 1;
                retries--;
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(buf);
            if (dataIsRented) ArrayPool<byte>.Shared.Return(data);
        }
        // TODO: 更多细节信息
        throw new InvalidOperationException("RequestFailed");
    }

    public void Dispose() {
        if (!disposedValue) {
            socket.Dispose();
            disposedValue = true;
        }
    }


    private static NatpmpResponse Parse(ReadOnlySpan<byte> buf) {
        NatpmpResponse resp = new() {
            ResultCode = (NatpmpResultCode)ntohs(buf[2..4]),
            Epoch = (uint)ntohl(buf[4..8]),
        };
        if (buf[0] != 0) {
            resp.ResultCode = NatpmpResultCode.UnsupportedVersion;
            return resp;
        }
        if (buf[1] < 128 || buf[1] > 130) {
            resp.ResultCode = NatpmpResultCode.UnsupportedOpCode;
            return resp;
        }
        if (resp.ResultCode == NatpmpResultCode.Success) {
            resp.Type = (NatpmpResponseType)(buf[1] & 0x7f);
            if (buf[1] == 128) { // Request Public Address
                resp.PublicAddress = new IPAddress(buf[8..12]);
            } else { // Request Port Mapping
                resp.PrivatePort = (ushort)ntohs(buf[8..10]);
                resp.MappedPublicPort = (ushort)ntohs(buf[10..12]);
                resp.Lifetime = (uint)ntohl(buf[12..16]);
            }
        }
        return resp;
    }
    public static IPAddress? GetGatewayAddress() => GetGatewayAddressesImpl().FirstOrDefault().addr;
    public static (string name, IPAddress addr)[] GetGatewayAddresses() => GetGatewayAddressesImpl().ToArray();
    static IEnumerable<(string name, IPAddress addr)> GetGatewayAddressesImpl() {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (networkInterface.OperationalStatus == OperationalStatus.Up) {
                IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                foreach (GatewayIPAddressInformation gateway in ipProperties.GatewayAddresses) {
                    yield return (networkInterface.Name, gateway.Address);
                }
            }
        }
    }
}