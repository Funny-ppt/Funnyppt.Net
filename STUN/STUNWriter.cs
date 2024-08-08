using static Funnyppt.Net.Util;

namespace Funnyppt.Net.STUN;

public ref struct STUNWriter {
    public STUNOptions Options { get; init; }
    public STUNContext Context { get; init; }
    public ReadOnlySpan<IAttributeSetter> WrittenAttributes { get; private set; }
    public ReadOnlySpan<byte> WrittenData { get; private set; }
    public Span<byte> Buffer { get; private set; }
    internal Span<byte> RawBuffer { get; private set; }
    internal int Pos { get; private set; }


    private static int Align4(int n) => (n + 3) / 4 * 4;
    public readonly int GetLength(IAttributeSetter[] attrs) {
        var len = 20 + 4 * attrs.Length;
        for (int i = 0; i < attrs.Length; i++) {
            len += Align4(attrs[i].GetLength(in this));
        }
        return len;
    }
    public void WriteAll(Span<byte> buf, int len, STUNMethod method, MessageClass msgClass, IAttributeSetter[] attrs) {
        WriteSTUNHeader(buf, len, method, msgClass);
        RawBuffer = buf;
        Pos = 20;
        for (int i = 0; i < attrs.Length; i++) {
            Pos += WriteAttrImpl(buf, Pos, attrs[i]);
            WrittenAttributes = attrs.AsSpan(0, i + 1);
        }
    }
    private int WriteAttrImpl(Span<byte> buf, int pos, IAttributeSetter setter) {
        WrittenData = buf[..pos];
        var len = setter.GetLength(in this);
        htons((ushort)setter.Type, buf.Slice(pos, 2));
        htons(len, buf.Slice(pos + 2, 2));
        Buffer = buf.Slice(pos + 4, len);
        setter.WriteData(ref this);
        return Align4(len) + 4;
    }

    static void WriteMethodAndClass(Span<byte> buf, STUNMethod method, MessageClass messageClass) {
        // 开头两个bit恒定为0, 第一个字节的最后一个字符和第二个字节的最后一个字符为STUN_CLASS对应的值
        // 其余部分为STUN_METHOD对应的值, 分别为 5bitM 1bitC 3bitM 1bitC 4bitM
        int mv = (int)method, cv = (int)messageClass;
        int res = ((mv & 0xf80) << 2) | ((cv & 0b10) << 7) | ((mv & 0x70) << 1) | ((cv & 0b1) << 4) | (mv & 0xf);
        Debug.Assert((res & 0xc000_0000) == 0);
        buf[0] = (byte)((mv & 0xf80) >> 6 | (cv & 0b10) >> 1);
        buf[1] = (byte)((mv & 0x70) << 1 | (cv & 0b1) << 4 | mv & 0xf);
    }
    static void WriteSTUNHeader(Span<byte> buf, int len, STUNMethod method, MessageClass messageClass) {
        WriteMethodAndClass(buf, method, messageClass);
        htons((ushort)(len - 20), buf[2..4]);
        STUNConstants.MagicCookie_bytes.CopyTo(buf[4..8]);
        RandomNumberGenerator.Fill(buf[8..20]);
    }
}
