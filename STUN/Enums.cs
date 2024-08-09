namespace Funnyppt.Net.STUN;

public static class EnumConverter {
    public static AddressFamily ToAddressFamily(this IPFamily family) {
        return family switch {
            IPFamily.IPv4 => AddressFamily.InterNetwork,
            IPFamily.IPv6 => AddressFamily.InterNetworkV6,
            _ => throw new ArgumentException("unknown IP family", nameof(family)),
        };
    }
    public static ProtocolType ToProtocolType(this STUNProtocolType protocolType) {
        return protocolType switch {
            STUNProtocolType.TCP => ProtocolType.Tcp,
            STUNProtocolType.UDP => ProtocolType.Udp,
            _ => throw new ArgumentException("unknown protocol type", nameof(protocolType)),
        };
    }
}

public enum IPFamily {
    IPv4 = 0x01,
    IPv6 = 0x02,
}

public enum STUNProtocolType {
    TCP = 0,
    UDP = 1,
}
public enum MessageClass {
    Request = 0b00,
    Indication = 0b01,
    Response = 0b10,
    Error = 0b11,
}

public enum STUNMethod {
    /// <summary>
    /// Binding: Used to determine the public IP address and port as seen by the STUN server.
    /// </summary>
    Binding = 1,

    /// <summary>
    /// Allocate: Used in TURN (Traversal Using Relays around NAT) to request allocation of a relay address.
    /// </summary>
    Allocate = 3,

    /// <summary>
    /// Refresh: Used in TURN to refresh an existing allocation.
    /// </summary>
    Refresh = 4,

    /// <summary>
    /// Send: Used in TURN to request the server to send data to a peer.
    /// </summary>
    Send = 6,

    /// <summary>
    /// Data: Used in TURN to send data from the server to the client.
    /// </summary>
    Data = 7,

    /// <summary>
    /// CreatePermission: Used in TURN to create a permission to send data to a peer.
    /// </summary>
    CreatePermission = 8,

    /// <summary>
    /// ChannelBind: Used in TURN to bind a channel number to a peer.
    /// </summary>
    ChannelBind = 9
}

public enum CredentialMethod {
    None,
    LongTerm,
    ShortTerm,
}

public enum STUNErrorCode {
    /// <summary>
    /// Try Alternate: The client should contact an alternate server for this request. 
    /// This error response MUST only be sent if the request included a USERNAME attribute 
    /// and a valid MESSAGE-INTEGRITY attribute; otherwise, it MUST NOT be sent and error 
    /// code 400 (Bad Request) is suggested. This error response MUST be protected with the 
    /// MESSAGE-INTEGRITY attribute, and receivers MUST validate the MESSAGE-INTEGRITY of this 
    /// response before redirecting themselves to an alternate server.
    /// Note: Failure to generate and validate message integrity for a 300 response allows 
    /// an on-path attacker to falsify a 300 response thus causing subsequent STUN messages 
    /// to be sent to a victim.
    /// </summary>
    TryAlternate = 300,

    /// <summary>
    /// Bad Request: The request was malformed. The client SHOULD NOT retry the request 
    /// without modification from the previous attempt. The server may not be able to generate 
    /// a valid MESSAGE-INTEGRITY for this error, so the client MUST NOT expect a valid 
    /// MESSAGE-INTEGRITY attribute on this response.
    /// </summary>
    BadRequest = 400,

    /// <summary>
    /// Unauthorized: The request did not contain the correct credentials to proceed. 
    /// The client should retry the request with proper credentials.
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// Unknown Attribute: The server received a STUN packet containing a comprehension-required 
    /// attribute that it did not understand. The server MUST put this unknown attribute in the 
    /// UNKNOWN-ATTRIBUTE attribute of its error response.
    /// </summary>
    UnknownAttribute = 420,

    /// <summary>
    /// Stale Nonce: The NONCE used by the client was no longer valid. The client should retry, 
    /// using the NONCE provided in the response.
    /// </summary>
    StaleNonce = 438,

    /// <summary>
    /// Server Error: The server has suffered a temporary error. The client should try again.
    /// </summary>
    ServerError = 500
}



[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal class ReservedAttribute : Attribute { }

public enum AttributeType : ushort {
    [Reserved] Reserved = 0x0000,
    Mapped_Address = 0x0001,
    [Reserved] Response_Address = 0x0002,
    Change_Request = 0x0003,
    Source_Address = 0x0004,
    Changed_Address = 0x0005,
    Username = 0x0006,
    [Reserved] Password = 0x0007,
    Message_Integrity = 0x0008,
    Error_Code = 0x0009,
    Unknown_Attribute = 0x000A,
    [Reserved] Reflected_From = 0x000B,
    Realm = 0x0014,
    Nonce = 0x0015,
    Xor_Mapped_Address = 0x0020,
    Padding = 0x0026,
    Response_Port = 0x0027,
    Software = 0x8022,
    Alternate_Server = 0x8023,
    Fingerprint = 0x8028,
    Response_Origin = 0x802b,
    Other_Address = 0x802c,
}

public enum NATMappingType {
    EndpointIndependentMapping,
    AddressDependentMapping,
    AddressAndPortDependentMapping,
    UnrecognizedMapping
}

public enum NATFilteringType {
    EndpointIndependentFiltering,
    AddressDependentFiltering,
    AddressAndPortDependentFiltering,
    UnrecognizedFiltering
}

public enum NATType {
    FullCone,
    AddressRestrictedCone,
    PortRestrictedCone,
    SymmetricNAT,
}

[Flags]
public enum ChangeRequestFlag {
    ChangePort = 0x2,
    ChangeIP = 0x4,
}

public enum PackageHandleResult {
    OK,
    DiscardAndResend,
    WaitTimeout,
}