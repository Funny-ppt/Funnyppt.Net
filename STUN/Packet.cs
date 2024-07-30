using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Funnyppt.Net.STUN;

internal static class Packet {
    static readonly byte[] MagicCookie = [0x21, 0x12, 0xA4, 0x42];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteMethodAndClass(byte[] array, int index, Method method, MessageClass messageClass) {
        // 开头两个bit恒定为0, 第一个字节的最后一个字符和第二个字节的最后一个字符为STUN_CLASS对应的值
        // 其余部分为STUN_METHOD对应的值, 分别为 5bitM 1bitC 3bitM 1bitC 4bitM
        int mv = (int)method, cv = (int)messageClass;
        int res = ((mv & 0xf80) << 2) | ((cv & 0b10) << 7) | ((mv & 0x70) << 1) | ((cv & 0b1) << 4) | (mv & 0xf);
        Debug.Assert(res < 0x4000);
        array[index] = (byte)((mv & 0xf80) >> 6 | (cv & 0b10) >> 1);
        array[index + 1] = (byte)((mv & 0x70) << 1 | (cv & 0b1) << 4 | mv & 0xf);
    }

    public static byte[] GetBytes(Method method, MessageClass messageClass) {
        var bytes = new byte[20];

        WriteMethodAndClass(bytes, 0, method, messageClass);
        Array.Copy(MagicCookie, 0, bytes, 4, 4); // 展开是否更高效？
        RandomNumberGenerator.Fill(new Span<byte>(bytes, 8, 12));
        return bytes;
    }
    public static byte[] GetBytes(Method method, MessageClass messageClass, Attribute[] attributes) {
        throw new NotImplementedException();
    }
    public static byte[] GetBytes(Method method, MessageClass messageClass, IEnumerable<Attribute> attributes) {
        throw new NotImplementedException();
    }

}
