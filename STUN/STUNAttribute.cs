using System.Reflection;

namespace Funnyppt.Net.STUN;

public readonly struct STUNAttribute {
    internal STUNAttribute(ReadOnlyMemory<byte> memory) {
        Bytes = memory;
    }

    public ReadOnlyMemory<byte> Bytes { get; }
    public ReadOnlyMemory<byte> Content => Bytes.Slice(4, ContentLength);
    public ReadOnlySpan<byte> ContentSpan => Bytes.Span.Slice(4, ContentLength);
    public string Name => _attributeNames.GetValueOrDefault(Type, "UNKNOWN");
    public AttributeType Type => (AttributeType)Util.ntohs(Bytes.Span);
    public int ContentLength => Bytes.Length - 4;

    static Dictionary<AttributeType, string> _attributeNames =
        Enum.GetValues<AttributeType>()
        //.Where(t => !IsReserved(t))
        .ToDictionary(
            t => t,
            t => t.ToString().ToUpper().Replace('_', '-')
        );

    static bool IsReserved(AttributeType value) {
        FieldInfo? fieldInfo = typeof(AttributeType).GetField(value.ToString());
        Debug.Assert(fieldInfo != null);
        return fieldInfo.GetCustomAttribute<ReservedAttribute>() != null;
    }
}