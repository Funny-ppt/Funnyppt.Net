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
}
