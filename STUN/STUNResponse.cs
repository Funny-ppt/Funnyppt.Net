namespace Funnyppt.Net.STUN;
using static STUNConstants;
using static Util;

// CONSIDER: class or readonly struct?
public class STUNResponse {
    public STUNMethod Method { get; init; }
    public MessageClass Class { get; init; }
    public int BodyLength { get; init; }
    public STUNAttribute[] Attributes { get; init; } = null!;
    public ReadOnlyMemory<byte> TranscationID { get; init; }
    public ReadOnlyMemory<byte> RawContent { get; init; }

    internal STUNResponse() { }

    public static STUNResponse FromBytes(ReadOnlyMemory<byte> bytes) {
        // 开头两个bit恒定为0, 第一个字节的最后一个字符和第二个字节的最后一个字符为STUN_CLASS对应的值
        // 其余部分为STUN_METHOD对应的值, 分别为 5bitM 1bitC 3bitM 1bitC 4bitM
        var span = bytes.Span;
        int type = ntohs(span[0..2]);
        var method = (STUNMethod)((((type >> 9) & 0x1f) << 7) | (((type >> 5) & 0x7) << 4) | (type & 0xf));
        var msgClass = (MessageClass)(((type & 0x100) >> 7) | ((type & 0x10) >> 4));
        int len = ntohs(span[2..4]);
        if (20 + len != bytes.Length) {
            // TODO: handle it
        }
        var magicCookie = span[4..8];
        if (!magicCookie.SequenceEqual(MagicCookie_bytes)) {
            // TODO: handle it
        }
        var transcationID = bytes[8..20];

        int cur = 20, n = 0;
        const int MAX_ATTRS = 64; // should not exceed 64 attributes?
        Span<int> lens = stackalloc int[MAX_ATTRS];
        while (cur < span.Length && n < 64) {
            cur += 4 + (lens[n++] = ntohs(span.Slice(cur + 2, 2)));
        }
        if (n == MAX_ATTRS && cur < span.Length) {
            throw new InvalidOperationException("too much attributes");
        }
        var attrs = new STUNAttribute[n];
        cur = 20;
        for (int i = 0; i < n; i++) {
            attrs[i] = new STUNAttribute(bytes.Slice(cur, 4 + lens[i]));
            cur += 4 + lens[i];
        }
        return new STUNResponse {
            Method = method,
            Class = msgClass,
            BodyLength = len,
            Attributes = attrs,
            TranscationID = transcationID,
            RawContent = bytes,
        };
    }
}