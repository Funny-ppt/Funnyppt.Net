namespace Funnyppt.Net.STUN;

public class UsernameSetter : IAttributeSetter {
    public AttributeType Type => AttributeType.Username;

    public ushort GetLength(in STUNWriter writer) {
        return (ushort)(
            writer.Context.SASLprep_Username ??=
                SASLprep.PrepareBytes(writer.Context.Username, writer.Options.AsciiOnly)
            ).Length;
    }

    public void WriteData(ref STUNWriter writer) {
        Debug.Assert(writer.Context.SASLprep_Username != null);
        writer.Context.SASLprep_Username.CopyTo(writer.Buffer);
    }
}
