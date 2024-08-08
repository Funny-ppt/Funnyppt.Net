namespace Funnyppt.Net.STUN;
public interface IAttributeSetter {
    public AttributeType Type { get; }
    /// <summary>
    /// 该方法返回的长度必须不包括属性头、不考虑补齐长度到4的倍数
    /// </summary>
    public ushort GetLength(in STUNWriter writer);
    public void WriteData(ref STUNWriter writer);
}
