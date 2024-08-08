namespace Funnyppt.Net.STUN;
public class NonceSetter : IAttributeSetter {
    public AttributeType Type => AttributeType.Realm;

    public ushort GetLength(in STUNWriter writer) {
        return (ushort)writer.Context.Nonce.Length;
    }

    public void WriteData(ref STUNWriter writer) {
        Encoding.ASCII.GetBytes(writer.Context.Nonce, writer.Buffer);
    }
}