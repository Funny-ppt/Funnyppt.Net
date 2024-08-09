using System.Buffers.Binary;

namespace Funnyppt.Net;
internal static class Util {
#pragma warning disable IDE1006 // 命名样式
    public static short htons(short value)
        => IPAddress.HostToNetworkOrder(value);
    public static ushort htons(ushort value)
        => (ushort)IPAddress.HostToNetworkOrder((short)value);
    public static int htonl(int value)
        => IPAddress.HostToNetworkOrder(value);
    public static uint htonl(uint value)
        => (uint)IPAddress.HostToNetworkOrder((int)value);
    public static bool htons(ushort value, Span<byte> dest)
        => BinaryPrimitives.TryWriteUInt16BigEndian(dest, value);
    public static bool htons(short value, Span<byte> dest)
        => BinaryPrimitives.TryWriteInt16BigEndian(dest, value);
    public static bool htonl(int value, Span<byte> dest)
        => BinaryPrimitives.TryWriteInt32BigEndian(dest, value);
    public static bool htonl(uint value, Span<byte> dest)
        => BinaryPrimitives.TryWriteUInt32BigEndian(dest, value);
    public static short ntohs(ReadOnlySpan<byte> span)
        => BinaryPrimitives.ReadInt16BigEndian(span);
    public static int ntohl(ReadOnlySpan<byte> span)
        => BinaryPrimitives.ReadInt32BigEndian(span);

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

    public static IPEndPoint ResolveFrom(string hostOrAddress, int defaultPort, AddressFamily addressFamily) {
        if (hostOrAddress.Contains(':')) {
            var splits = hostOrAddress.Split(':');
            hostOrAddress = splits[0];
            defaultPort = int.Parse(splits[1]);
        }

        IPAddress addr = Dns.GetHostAddresses(hostOrAddress, addressFamily).First();
        return new(addr, defaultPort);
    }
}

internal ref struct LocalArrayBuilder<T> {
    int pos;
    Span<T> buf;
    T[]? rented;

    public readonly int Capacity => buf.Length;

    public LocalArrayBuilder(Span<T> buf) {
        pos = 0;
        this.buf = buf;
    }
    private void Grow(int length) {
        var newBuf = ArrayPool<T>.Shared.Rent(length);
        buf[..pos].CopyTo(newBuf);
        buf = newBuf;
        if (rented != null) {
            ArrayPool<T>.Shared.Return(rented);
        }
        rented = newBuf;
    }
    public void Append(in T value) {
        if (pos >= buf.Length) {
            Grow(Capacity * 2);
        }
        buf[pos++] = value;
    }
    public readonly T[] ToArray() {
        return buf[..pos].ToArray();
    }
}