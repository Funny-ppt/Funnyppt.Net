namespace Funnyppt.Net.STUN;

public struct NATTestResult {
    public NATFilteringType FilteringType { get; set; }
    public NATMappingType MappingType { get; set; }
    public NATType InferredNATType { get; set; }
}