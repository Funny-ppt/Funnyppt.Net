namespace Funnyppt.Net.STUN;
public class STUNOptions {
    public STUNProtocolType ProtocolType { get; init; } = STUNProtocolType.UDP;
    public IPFamily IPFamily { get; init; } = IPFamily.IPv4;
    public bool AsciiOnly { get; init; } = true;
    public bool UseFingerprint { get; init; } = true;
    public CredentialMethod CredentialMethod { get; init; } = CredentialMethod.LongTerm;
}
