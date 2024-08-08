namespace Funnyppt.Net.STUN;
public static class STUNConstants {
    // defined in RFC5389
    public const int MagicCookie = 0x2112A442;
    public static readonly byte[] MagicCookie_bytes = [0x21, 0x12, 0xA4, 0x42];

    public const uint FingerprintMagicNumber = 0x5354554e;
}
