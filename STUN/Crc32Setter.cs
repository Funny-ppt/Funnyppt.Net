using System.IO.Hashing;
using static Funnyppt.Net.STUN.STUNConstants;

namespace Funnyppt.Net.STUN;
public class Crc32Setter : IAttributeSetter {
    public AttributeType Type => AttributeType.Fingerprint;
    public ushort GetLength(in STUNWriter writer) => 4;
    public void WriteData(ref STUNWriter writer) {
        var crc32 = Crc32.HashToUInt32(writer.WrittenData);
        Util.htonl(crc32 ^ FingerprintMagicNumber, writer.Buffer);
    }
}
