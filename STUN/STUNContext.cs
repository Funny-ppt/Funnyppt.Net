namespace Funnyppt.Net.STUN;
public class STUNContext {
    public string Realm { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public byte[]? SASLprep_Username { get; set; }
    public byte[]? SASLprep_Password { get; set; }
    public byte[]? MessageIntegrityKey { get; set; }
}
