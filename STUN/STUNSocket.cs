namespace Funnyppt.Net.STUN;
using static AttributeSetters;

public sealed class STUNSocket : IDisposable {
    // see https://datatracker.ietf.org/doc/html/rfc5389
    const int STUN_RTO_MS = 500;
    const int STUN_RETRIES = 7;
    const int MAX_BODY_LENGTH_IPV4 = 548;

    Socket socket;
    bool ownSocket;
    public Socket Socket => socket;

    public STUNSocket(Socket socket, bool ownSocket) {
        this.socket = socket;
        this.ownSocket = ownSocket;
    }

    public Task<STUNResponse> SendAsync(STUNOptions options, STUNContext context, STUNMethod method, MessageClass msgClass, IAttributeSetter[] attrs, Func<STUNResponse, bool>? validator) {
        STUNWriter writer = new() {
            Options = options,
            Context = context,
        };

        int len = writer.GetLength(attrs);
        byte[] buf = ArrayPool<byte>.Shared.Rent(len);
        writer.WriteAll(buf, len, method, msgClass, attrs);
        // 缓冲区的所有权必须让渡给异步函数,将其放在同步函数的finally块会因为异步函数提前返回并回收,导致访问已被回收的区域
        return SendAsyncImpl(buf, len, true, validator);
    }

    private async Task<STUNResponse> SendAsyncImpl(byte[] bytesToSend, int length, bool isRented, Func<STUNResponse, bool>? validator) {
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
                    // validator 会显著拖慢该函数吗?
                    // TODO: validator返回一个值指示后续如何响应(立即退出?等待到下次超时?立刻重发请求?)
                    if (validator == null || validator(resp)) return resp;
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
        throw new NetworkUnreachableException($"Retry {STUN_RETRIES} times and no response is received or receive bad response");
    }

    // TODO: do benchmarking and distinguish which one is better?
    private async Task<STUNResponse> SendAsyncImpl1(byte[] bytesToSend, int length, bool isRented) {
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
        if (ownSocket) socket.Dispose();
    }
}
