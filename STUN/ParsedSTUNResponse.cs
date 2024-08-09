namespace Funnyppt.Net.STUN;
public class ParsedSTUNResponse {
    public STUNResponse Response { get; }
    public STUNMethod Method => Response.Method;
    public MessageClass Class => Response.Class;
    public ReadOnlyMemory<byte> TranscationID => Response.TranscationID;

    /// <summary>
    /// 表示该次请求是否成功. 但请求成功不一定返回有效载荷, 还需要检查Valid属性
    /// </summary>
    public bool Success { get; }
    /// <summary>
    /// 表示本次请求是否有效(Fingerprint, MessageIntegrity等属性通过校验)
    /// </summary>
    public bool Valid { get; }
    public IPEndPoint? Address { get; }
    public IPEndPoint? ServerOriginAddress { get; }
    public IPEndPoint? ServerOtherAddress { get; }
    public string? Nonce { get; }
    public string? Software { get; }
    public string? Realm { get; }
    public ErrorCode? ErrorCode { get; }
    public int[]? UnknownAttributes { get; }
    public STUNAttribute[]? ComprehensionOptionalAttrs { get; }

    public ParsedSTUNResponse(STUNResponse resp, STUNContext ctx) {
        Response = resp;
        Success = true;
        Valid = true;
        // 本地函数不允许访问只读自动属性, 所以使用本地变量
        IPEndPoint? ep = null;
        var rawspan = resp.RawContent.Span;
        int offset = 20;

        const int MAXCOATTRS = 64;
        int comprehensionOptionalAttrs = 0;
        Span<int> coabuf = stackalloc int[MAXCOATTRS];

        for (int i = 0; i < resp.Attributes.Length; i++) {
            ref readonly var attr = ref resp.Attributes[i];
            switch (attr.Type) {
                case AttributeType.Mapped_Address:
                    SetAddress(attr.GetMappedAddress());
                    break;
                case AttributeType.Xor_Mapped_Address:
                    SetAddress(attr.GetXorMappedAddress(TranscationID.Span));
                    break;
                case AttributeType.Response_Origin:
                    ServerOriginAddress = attr.GetMappedAddress();
                    break;
                case AttributeType.Other_Address:
                    ServerOtherAddress = attr.GetMappedAddress();
                    break;
                case AttributeType.Message_Integrity:
                    Valid &= attr.ValidateMessageIntegrity(
                        rawspan[..offset], ctx.MessageIntegrityKey);
                    break;
                case AttributeType.Fingerprint:
                    Valid &= attr.ValidateFingerprint(rawspan[..offset]);
                    break;
                case AttributeType.Error_Code:
                    Success = false;
                    ErrorCode = attr.GetErrorCode();
                    break;
                case AttributeType.Realm:
                    Realm = attr.GetRealm();
                    break;
                case AttributeType.Nonce:
                    Nonce = attr.GetNonce();
                    break;
                case AttributeType.Software:
                    Software = attr.GetSoftware();
                    break;
                case AttributeType.Unknown_Attribute:
                    UnknownAttributes = attr.GetUnknownAttributes();
                    break;
                default:
                    if ((int)attr.Type >= 0x8000 && comprehensionOptionalAttrs < 64) {
                        coabuf[comprehensionOptionalAttrs++] = i;
                    } else {
                        Success = false;
                    }
                    break;
            }
            offset += attr.ContentLength + 4;
        }
        Address = ep;
        if (comprehensionOptionalAttrs > 0) {
            ComprehensionOptionalAttrs = new STUNAttribute[comprehensionOptionalAttrs];
            for (int i = 0; i < comprehensionOptionalAttrs; i++) {
                ComprehensionOptionalAttrs[i] = resp.Attributes[coabuf[i]];
            }
        }

        void SetAddress(IPEndPoint new_ep) {
            if (ep == null) {
                ep = new_ep;
            } else {
                if (!ep.Equals(new_ep)) {
                    throw new ParseException("Incosistent ip address from multiple attribute");
                }
            }
        }
    }
}
