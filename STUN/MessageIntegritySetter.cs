namespace Funnyppt.Net.STUN;
public class MessageIntegritySetter : IAttributeSetter {
    public AttributeType Type => AttributeType.Message_Integrity;

    public ushort GetLength(in STUNWriter writer) => 20; // HMACSHA1.HashSizeInBytes = 20

    public void WriteData(ref STUNWriter writer) {
        Span<byte> buf = stackalloc byte[1024];
        Debug.Assert(writer.Context.SASLprep_Username != null);
        var passwd = writer.Context.SASLprep_Password
                ??= SASLprep.PrepareBytes(writer.Context.Password, writer.Options.AsciiOnly);
        var key = writer.Context.MessageIntegrityKey;
        if (key == null) {
            key = writer.Context.MessageIntegrityKey = new byte[16];
            if (writer.Options.CredentialMethod is CredentialMethod.LongTerm) {
                // key = MD5(SASLprep(username) ":" realm ":" SASLprep(password))
                var username = writer.Context.SASLprep_Username;
                var realm = writer.Context.Realm;
                var len = username.Length + realm.Length + passwd.Length + 2;
                Debug.Assert(len <= 1024);
                var cur = 0;
                username.CopyTo(buf);
                cur += username.Length;
                buf[cur++] = (byte)':';
                cur += Encoding.ASCII.GetBytes(realm, buf[cur..]);
                buf[cur++] = (byte)':';
                passwd.CopyTo(buf[cur..]);
                cur += passwd.Length;
                Debug.Assert(cur == len);
                MD5.HashData(buf[..len], key);
            } else if (writer.Options.CredentialMethod is CredentialMethod.ShortTerm) {
                // key = MD5(SASLprep(password))
                MD5.HashData(passwd, key);
            } else {
                throw new InvalidOperationException("No credential method is set");
            }
        }
        writer.RawBuffer[..writer.Pos].CopyTo(buf);
        // plus 24 for MESSAGE-INTEGRITY length(including header), sub 20 for STUN header
        BitConverter.TryWriteBytes(buf[2..4], (ushort)(writer.Pos + 24 - 20));
        HMACSHA1.HashData(key, buf[..writer.Pos], writer.Buffer);
    }
}
