namespace Funnyppt.Net.STUN;
using static AttributeSetters;

public sealed class STUNClient : IDisposable {
    // see https://datatracker.ietf.org/doc/html/rfc5389
    const int STUN_RTO_MS = 500;
    const int STUN_RETRIES = 7;
    const int MAX_BODY_LENGTH_IPV4 = 548;

    STUNSocket socket;
    Func<Socket>? socketFactory;
    public STUNOptions Options { get; }
    public STUNContext Context { get; }
    public IPEndPoint? LocalEndPoint => socket.Socket.LocalEndPoint as IPEndPoint;
    public IPEndPoint? RemoteEndPoint => socket.Socket.RemoteEndPoint as IPEndPoint;

    /// <param name="hostOrAddress">可以是主机名,地址,主机名:端口,地址:端口中的任意一种.如果给出端口号,则覆盖port参数</param>
    /// <param name="port">如果hostOrAddress参数包含端口部分,则该参数无效</param>
    public static STUNClient Create(STUNOptions options, string hostOrAddress, int port = 3478, STUNContext? context = null) {
        var addrFamily = options.IPFamily.ToAddressFamily();
        var proto = options.ProtocolType.ToProtocolType();
        var socktype = proto switch {
            ProtocolType.Tcp => SocketType.Stream,
            ProtocolType.Udp => SocketType.Dgram,
            _ => throw new UnreachableException(),
        };

        if (hostOrAddress.Contains(':')) {
            var splits = hostOrAddress.Split(':');
            hostOrAddress = splits[0];
            port = int.Parse(splits[1]);
        }
        Socket sockFactory() {
            var sock = new Socket(addrFamily, socktype, proto);
            sock.Connect(hostOrAddress, port);
            return sock;
        }
        context ??= new();
        return new STUNClient(sockFactory, options, context);
    }

    internal STUNClient(Socket socket, STUNOptions options, STUNContext context) {
        this.socket = new(socket, false);
        Options = options;
        Context = context;
    }
    internal STUNClient(Func<Socket> sockFactory, STUNOptions options, STUNContext context) {
        socketFactory = sockFactory;
        socket = new(socketFactory(), true);
        Options = options;
        Context = context;
    }

    public async Task<ParsedSTUNResponse> SendBindRequestAsync() {
        ParsedSTUNResponse? resp = null;
        bool ValidateResponse(STUNResponse rawresp) {
            resp = new(rawresp, Context);
            // TODO: Error Handle is incomplete
            return resp.Success && resp.Valid && resp.Address != null;
        }
        IAttributeSetter[] attrs = Options.UseFingerprint ? [Fingerprint] : [];
        await SendAsync(STUNMethod.Binding, MessageClass.Request, attrs, ValidateResponse);
        return resp!;
    }
    public async Task<IPEndPoint> GetPublicEPAsync() {
        return (await SendBindRequestAsync()).Address
            ?? throw new ParseException("Failed to get mapped address");
    }

    public Task<STUNResponse> SendAsync(STUNMethod method, MessageClass msgClass, params IAttributeSetter[] attrs) {
        return SendAsync(method, msgClass, attrs, null);
    }

    public Task<STUNResponse> SendAsync(STUNMethod method, MessageClass msgClass, IAttributeSetter[] attrs, Func<STUNResponse, bool>? validator) {
        EnsureSocketAvaliable();
        return socket.SendAsync(Options, Context, method, msgClass, attrs, validator);
    }

    private void EnsureSocketAvaliable() {
        if (Options.ProtocolType == STUNProtocolType.TCP && socketFactory != null) {
            socket.Socket.Send(Array.Empty<byte>(), SocketFlags.None, out var errorCode);
            if (errorCode != SocketError.Success && errorCode != SocketError.WouldBlock) {
                socket.Dispose();
                socket = new(socketFactory(), true);
            }
        }
    }

    public void Dispose() {
        socket?.Dispose();
    }


    /// <param name="stunServers">可以是主机名,地址,主机名:端口,地址:端口任意组合的数组.
    /// 没有指定端口则默认为3478</param>
    public static async Task<NATMappingType> TestMappingBehavior(string[] stunServers, IPFamily family = IPFamily.IPv4) {
        var addrFamily = family.ToAddressFamily();
        using var socket = new Socket(addrFamily, SocketType.Dgram, ProtocolType.Udp);
        using var socket1 = new Socket(addrFamily, SocketType.Dgram, ProtocolType.Udp);
        var option = new STUNOptions() {
            IPFamily = family,
            UseFingerprint = false,
        };
        var context = new STUNContext();
        var client = new STUNClient(socket, option, context);
        var client1 = new STUNClient(socket1, option, context);
        NATMappingType mappingType = default;

        // Determining NAT Mapping Behavior
        // 1. test I - Client cP0, Server sA0,sP0
        bool success = false;
        ParsedSTUNResponse? resp0 = null, resp1, resp2;
        for (int i = 0; i < stunServers.Length; i++) {
            var ep = Util.ResolveFrom(stunServers[i], 3478, addrFamily);
            socket.Connect(ep);
            if (resp0 != null) {
                // the first stun server not support RFC5780, use two stun server
                goto Test2;
            }
            resp0 = await client.SendBindRequestAsync();
            if (resp0.Address!.Equals(client.LocalEndPoint)) {
                mappingType = NATMappingType.EndpointIndependentMapping;
                success = true;
                break;
            }
            if (resp0.ServerOtherAddress == null
                || resp0.ServerOtherAddress.Address.Equals(client.RemoteEndPoint)) {
                // Server not support RFC5780 or server doesnt have secondary ip address
                continue;
            }
            // 2. test II - Client cP0, Server sA1, sP1
            socket.Connect(resp0.ServerOtherAddress);
        Test2:
            resp1 = await client.SendBindRequestAsync();
            if (!resp1.Address!.Address.Equals(resp0.Address!.Address)) {
                // incoinsist public ip from different server, discard it
                continue;
            }
            if (resp1.Address.Equals(resp0.Address)) {
                mappingType = NATMappingType.EndpointIndependentMapping;
                success = true;
                break;
            }
            // 3. test III - Client cP1, Server sA1, sP1
            socket1.Connect(ep);
            resp2 = await client1.SendBindRequestAsync();
            if (!resp2.Address!.Address.Equals(resp1.Address!.Address)) {
                // incoinsist public ip from different server, discard it
                continue;
            }
            if (resp2.Address.Equals(resp1.Address)) {
                mappingType = NATMappingType.AddressDependentMapping;
                success = true;
                break;
            } else {
                mappingType = NATMappingType.AddressAndPortDependentMapping;
                success = true;
                break;
            }
        }
        if (!success) {
            // TODO: Custom Exception
            throw new Exception("Tried all server and fails to test");
        }
        return mappingType;
    }
}
