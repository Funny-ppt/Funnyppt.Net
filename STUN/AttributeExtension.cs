namespace Funnyppt.Net.STUN;

using System;
using System.IO.Hashing;
using System.Net;
using static STUNConstants;
using static Util;

public record ErrorCode(STUNErrorCode code, bool isKnownError, string message);
public class AttributeParseException : ParseException {
    public AttributeParseException(string? message) : base(message) {
    }
}


// TODO: 该类未充分考虑字段验证的问题
public static class AttributeExtension {

    public static string GetAsAsciiString(this STUNAttribute attr) {
        return Encoding.ASCII.GetString(attr.ContentSpan);
    }
    public static string GetAsUtf8String(this STUNAttribute attr) {
        return Encoding.UTF8.GetString(attr.ContentSpan);
    }

    public static string GetRealm(this STUNAttribute attr)
        => GetAsAsciiString(attr);

    public static string GetSoftware(this STUNAttribute attr)
        => GetAsUtf8String(attr);

    public static string GetNonce(this STUNAttribute attr)
        => GetAsUtf8String(attr);

    public static IPEndPoint GetMappedAddress(this STUNAttribute attr) {
        var span = attr.ContentSpan;
        //var ipfamily = (IPFamily)span[1];
        var port = (ushort)ntohs(span[2..4]);
        return new IPEndPoint(new IPAddress(span[4..]), port);
    }

    public static IPEndPoint GetAlternativeServer(this STUNAttribute attr)
        => GetMappedAddress(attr);

    public static IPEndPoint GetOtherAddress(this STUNAttribute attr)
        => GetMappedAddress(attr);

    public static IPEndPoint GetResponseOrigin(this STUNAttribute attr)
        => GetMappedAddress(attr);

    public static IPEndPoint GetXorMappedAddress(this STUNAttribute attr, ReadOnlySpan<byte> transactionID) {
        var span = attr.ContentSpan;
        var family = (IPFamily)span[1];
        ushort xport = (ushort)ntohs(span[2..4]);
        ushort port = (ushort)(xport ^ (MagicCookie >> 16));
        IPAddress ipAddress;
        if (family == IPFamily.IPv4) {
            uint xaddress = (uint)ntohl(span[4..8]);
            uint address = xaddress ^ MagicCookie;
            ipAddress = new IPAddress(htonl(address));
        } else if (family == IPFamily.IPv6) {
            Span<byte> xAddressBytes = stackalloc byte[16];
            span[4..20].CopyTo(xAddressBytes);

            for (int i = 0; i < 4; i++) {
                xAddressBytes[i] ^= (byte)(MagicCookie >> (24 - (i * 8)));
            }

            for (int i = 0; i < 12; i++) {
                xAddressBytes[i + 4] ^= transactionID[i];
            }
            ipAddress = new IPAddress(xAddressBytes);
        } else {
            throw new AttributeParseException("Invalid ip family");
        }
        return new(ipAddress, port);
    }

    /// <param name="body">STUN消息从开头到MessageIntegrity属性之前的部分</param>
    public static bool ValidateMessageIntegrity(this STUNAttribute attr, ReadOnlySpan<byte> body, ReadOnlySpan<byte> key) {
        Span<byte> res = stackalloc byte[20];
        Span<byte> buf = stackalloc byte[1024];
        body.CopyTo(buf);
        htons((ushort)(body.Length - 20), buf[2..4]);
        HMACSHA1.HashData(key, buf[..body.Length], res);
        return res.SequenceEqual(attr.ContentSpan);
    }

    /// <param name="body">STUN消息从开头到Fingerprint属性之前的部分</param>
    public static bool ValidateFingerprint(this STUNAttribute attr, ReadOnlySpan<byte> body) {
        var crc32 = Crc32.HashToUInt32(body);
        return (crc32 ^ FingerprintMagicNumber) == ntohl(attr.ContentSpan);
    }

    public static ErrorCode GetErrorCode(this STUNAttribute attr) {
        var span = attr.ContentSpan;
        var unparsedErrorCode = ntohl(span[0..4]);
        var hundreds = (unparsedErrorCode >> 8) & 0x7;
        var number = unparsedErrorCode & 0xff;
        if (hundreds < 3 || hundreds > 6 || number > 99) {
            throw new AttributeParseException($"Unexcepted errorcode with hundreds = {hundreds}, number = {number}");
        }
        var errorCode = (STUNErrorCode)(hundreds * 100 + number);
        var reason = Encoding.UTF8.GetString(span[4..]);
        return new(errorCode, Enum.IsDefined(errorCode), reason);
    }

    /// <returns>服务器未知的属性值的数组</returns>
    public static int[] GetUnknownAttributes(this STUNAttribute attr) {
        var span = attr.ContentSpan;
        //Note: In[RFC3489], this field was padded to 32 by duplicating the
        //  last attribute. In[RFC5389], the normal padding rules for
        //  attributes are used instead.

        // TODO: 整个库都考虑了效率问题这个方法瞎几把写, 还好应该不是热点
        var unknownAttributes = new int[span.Length / 4];
        for (int i = 0; i * 4 < span.Length; i++) {
            unknownAttributes[i] = ntohl(span.Slice(i * 4, 4));
        }
        return unknownAttributes.Distinct().ToArray();
    }
}
