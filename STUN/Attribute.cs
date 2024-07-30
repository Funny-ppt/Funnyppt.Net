namespace Funnyppt.Net.STUN;

public record class Attribute {
    public AttributeType Type { get; }
    public string Name => _attributeNames.GetValueOrDefault((ushort)Type, "UNKNOWN");
    public ReadOnlyMemory<byte> Bytes { get; }
    public ReadOnlyMemory<byte> ContentBytes => Bytes[4..];

    Dictionary<ushort, string> _attributeNames =
        Enum.GetValues<AttributeType>()
        .ToDictionary(
            t => (ushort)t,
            t => t.ToString().ToUpper().Replace('_', '-')
        );
}