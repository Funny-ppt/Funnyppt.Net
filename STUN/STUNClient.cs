namespace Funnyppt.Net.STUN;


public sealed class STUNClient : IDisposable {
    // see https://datatracker.ietf.org/doc/html/rfc5389
    const int STUN_RTO_MS = 500;
    const int STUN_RETRIES = 7;
    const int MAX_BODY_LENGTH_IPV4 = 548;

    Socket? socket;
    Func<Socket> socketFactory;
    public STUNOptions Options { get; }
    public STUNContext Context { get; }


    /// <param name="hostOrAddress">可以是主机名,地址,主机名:端口,地址:端口中的任意一种.如果给出端口号,则覆盖port参数</param>
    /// <param name="port">如果hostOrAddress参数包含端口部分,则该参数无效</param>
    public static STUNClient Create(STUNOptions options, string hostOrAddress, int port = 3478, string username = "", string passwd = "") {
        var mappedAddressFamily = options.IPFamily switch {
            IPFamily.IPv4 => AddressFamily.InterNetwork,
            IPFamily.IPv6 => AddressFamily.InterNetworkV6,
            _ => throw new ArgumentException("unknown IP family", nameof(options)),
        };

        var mappedProtocol = options.ProtocolType switch {
            STUNProtocolType.TCP => ProtocolType.Tcp,
            STUNProtocolType.UDP => ProtocolType.Udp,
            _ => throw new ArgumentException("unknown protocol type", nameof(options)),
        };
        var socketType = mappedProtocol switch {
            ProtocolType.Tcp => SocketType.Stream,
            ProtocolType.Udp => SocketType.Dgram,
            _ => throw new UnreachableException(),
        };
        if (hostOrAddress.Contains(':')) {
            var splits = hostOrAddress.Split(':');
            hostOrAddress = splits[0];
            port = int.Parse(splits[1]);
        }
        var resolved = Dns.GetHostAddresses(hostOrAddress)
            .Where(addr => addr.AddressFamily == mappedAddressFamily)
            .First();
        Socket sockFactory() {
            var sock = new Socket(mappedAddressFamily, socketType, mappedProtocol);
            sock.Connect(resolved, port);
            return sock;
        }
        var ctx = new STUNContext() {
            Username = username,
            Password = passwd
        };
        return new STUNClient(sockFactory, options, ctx);
    }

    internal STUNClient(Func<Socket> sockFactory, STUNOptions options, STUNContext context) {
        socketFactory = sockFactory;
        Options = options;
        Context = context;
    }

    public Task<STUNResponse> SendAsync(STUNMethod method, MessageClass msgClass, params IAttributeSetter[] attrs) {
        STUNWriter writer = new() {
            Options = Options,
            Context = Context,
        };

        int len = writer.GetLength(attrs);
        byte[] buf = ArrayPool<byte>.Shared.Rent(len);
        writer.WriteAll(buf, len, method, msgClass, attrs);
        // 缓冲区的所有权必须让渡给异步函数,将其放在同步函数的finally块会因为异步函数提前返回并回收,导致访问已被回收的区域
        return SendAsyncImpl(buf, len, true);
    }

    private void EnsureSocketAvaliable() {
        socket ??= socketFactory();

        if (Options.ProtocolType == STUNProtocolType.TCP) {
            socket.Send(Array.Empty<byte>(), SocketFlags.None, out var errorCode);
            if (errorCode != SocketError.Success && errorCode != SocketError.WouldBlock) {
                socket = socketFactory();
            }
        }
    }

    private async Task<STUNResponse> SendAsyncImpl(byte[] bytesToSend, int length, bool isRented) {
        EnsureSocketAvaliable();
        Debug.Assert(socket != null);

        var timeout = STUN_RTO_MS;
        var retries = STUN_RETRIES;
        // STUNResponse直接使用原始缓冲区作为数据后端, 因此无需向ArrayPool借用
        var buf = new byte[1024];
        try {
            while (retries > 0) {
                await socket.SendAsync(bytesToSend[..length]);
                using var cts = new CancellationTokenSource(timeout);
                try {
                    var bytesRead = await socket.ReceiveAsync(buf, SocketFlags.None, cts.Token);
                    var resp = STUNResponse.FromBytes(buf.AsMemory(0, bytesRead));
                    return resp;
                } catch (OperationCanceledException) {
                    // TODO: logging?
                }
                // TODO: Note in RFC5389, the RTO SHOULD computed as described in RFC 2988
                // and recommend to use KARN87 algorithm
                timeout = (timeout << 1) + STUN_RTO_MS;
                retries--;
            }
        } finally {
            if (isRented) ArrayPool<byte>.Shared.Return(bytesToSend);
        }
        throw new NetworkUnreachableException($"Retry {STUN_RETRIES} times and no response is received");
    }

    // TODO: do benchmarking and distinguish which one is better?
    private async Task<STUNResponse> SendAsyncImpl1(byte[] bytesToSend, int length, bool isRented) {
        EnsureSocketAvaliable();
        Debug.Assert(socket != null);

        var timeout = STUN_RTO_MS;
        var retries = STUN_RETRIES;
        var buf = new byte[1024];
        try {
            while (retries > 0) {
                await socket.SendAsync(bytesToSend[..length]);
                using var cts = new CancellationTokenSource();
                var recvTask = socket.ReceiveAsync(buf, SocketFlags.None, cts.Token).AsTask();
                var taskThatDones = await Task.WhenAny(recvTask, Task.Delay(timeout));
                if (taskThatDones == recvTask) {
                    var resp = STUNResponse.FromBytes(buf.AsMemory(0, recvTask.Result));
                    return resp;
                } else {
                    cts.Cancel();
                }

                // TODO: Note in RFC5389, the RTO SHOULD computed as described in RFC 2988
                // and recommend to use KARN87 algorithm
                timeout = (timeout << 1) + STUN_RTO_MS;
                retries--;
            }
        } finally {
            if (isRented) ArrayPool<byte>.Shared.Return(bytesToSend);
        }
        throw new NetworkUnreachableException($"Retry {STUN_RETRIES} times and no response is received");
    }

    public void Dispose() {
        socket?.Dispose();
    }
}
