namespace Funnyppt.Net.STUN;
public class RealmSetter : IAttributeSetter {
    public AttributeType Type => AttributeType.Realm;

    public ushort GetLength(in STUNWriter writer) {
        return (ushort)writer.Context.Realm.Length;
    }

    public void WriteData(ref STUNWriter writer) {
        Encoding.ASCII.GetBytes(writer.Context.Realm, writer.Buffer);
    }
}
