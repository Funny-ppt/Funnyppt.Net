namespace Funnyppt.Net.STUN;
public class ChangeRequestSetter(ChangeRequestFlag flag) : IAttributeSetter {
    public AttributeType Type => AttributeType.Change_Request;
    public ushort GetLength(in STUNWriter writer) => 4;
    public void WriteData(ref STUNWriter writer) {
        Util.htonl((int)flag, writer.Buffer);
    }
}