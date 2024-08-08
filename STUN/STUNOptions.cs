namespace Funnyppt.Net.STUN;
public record STUNOptions(
    STUNProtocolType ProtocolType,
    IPFamily IPFamily = IPFamily.IPv4,
    bool AsciiOnly = true,
    CredentialMethod CredentialMethod = CredentialMethod.LongTerm
);
