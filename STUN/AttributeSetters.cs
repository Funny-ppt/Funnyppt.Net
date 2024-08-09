namespace Funnyppt.Net.STUN;
public static class AttributeSetters {
    public static readonly Crc32Setter Fingerprint = new();

    public static readonly RealmSetter Realm = new();
    public static readonly NonceSetter Nonce = new();
    public static readonly MessageIntegritySetter MessageIntegrity = new();

}