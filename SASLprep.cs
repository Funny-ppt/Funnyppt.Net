using System.Globalization;

namespace Funnyppt.Net;
/// <summary>
/// 
/// </summary>
public class SASLprep {
    // Checks if a character is commonly mapped to nothing by RFC 4013
    public static bool IsCommonlyMappedToNothing(int c) {
        return c == 0x00AD || c == 0x034F || c == 0x1806 || c == 0x180B ||
               c == 0x180C || c == 0x180D || c == 0x200B || c == 0x200C ||
               c == 0x200D || c == 0x2060 || (c >= 0xFE00 && c <= 0xFE0F) || c == 0xFEFF;
    }

    // Checks if a character is prohibited by RFC 4013
    public static bool IsProhibited(int c) {
        return (c >= 0x0000 && c <= 0x001F) || c == 0x007F ||
               (c >= 0x0080 && c <= 0x009F) || c == 0x06DD ||
               c == 0x070F || (c >= 0x180E && c <= 0x180F) ||
               (c >= 0x200C && c <= 0x200F) || (c >= 0x202A && c <= 0x202E) ||
               (c >= 0x2060 && c <= 0x2063) || c == 0x206A || c == 0x206B ||
               c == 0x206C || c == 0x206D || c == 0x206E || c == 0x206F ||
               c == 0xFEFF || c == 0xFFF9 || c == 0xFFFA || c == 0xFFFB ||
               (c >= 0x1D173 && c <= 0x1D17A) || (c >= 0xE0000 && c <= 0xE0FFF);
    }

    public static string Prepare(string input, bool asciiOnly) {
        return asciiOnly ? Prepare_AsciiOnly(input) : Prepare(input);
    }

    public static byte[] PrepareBytes(string input, bool isAsciiOnly) {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input is empty", nameof(input));
        if (isAsciiOnly) {
            var bytes = new byte[input.Length];
            var bytesWrite = Prepare_AsciiOnly(input, bytes);
            Debug.Assert(bytesWrite == input.Length);
            if (bytesWrite == -1) throw new ArgumentException($"Invalid input '{input}'", nameof(input));
            return bytes;
        } else {
            string res = Prepare(input);
            return Encoding.UTF8.GetBytes(res);
        }
    }

    // SASLprep implementation based on RFC 4013
    public static string Prepare(string input) {
        ArgumentNullException.ThrowIfNull(input);

        // Step 1: Map
        var mapped = new StringBuilder();
        for (int i = 0; i < input.Length; ++i) {
            int c = input[i];
            if (char.IsHighSurrogate((char)c)) {
                c = char.ConvertToUtf32(input, i);
                ++i;
            }
            if (!IsCommonlyMappedToNothing(c)) {
                if (StringPrep.NonAsciiSpaces.Contains(c)) mapped.Append('\u0020');
                else mapped.Append(c);
            }
        }

        // Step 2: Normalize
        string normalized = mapped.ToString().Normalize(NormalizationForm.FormKC);

        // Step 3&4: Prohibit & Check for unassigned code points
        foreach (char c in normalized) {
            if (IsProhibited(c)) {
                throw new ArgumentException("The input string contains prohibited characters.", nameof(input));
            }
            if (char.GetUnicodeCategory(c) == UnicodeCategory.OtherNotAssigned) {
                throw new ArgumentException("The input string contains unassigned code points.", nameof(input));
            }
        }

        return normalized;
    }

    public static string Prepare_AsciiOnly(string src) {
        return string.Create(src.Length, src, (dst, src) => {
            int pOut = 0;
            for (int pIn = 0; pIn < src.Length; ++pIn) {
                char c = src[pIn];

                switch ((int)c) {
                    case 0xAD:
                        break;
                    case 0xA0:
                    case 0x20:
                        dst[pOut++] = (char)0x20;
                        break;
                    case 0x7F:
                        throw new ArgumentException(null, nameof(src));
                    default:
                        if (c < 0x1F || c >= 0x80 && c <= 0x9F) {
                            throw new ArgumentException(null, nameof(src));
                        }
                        dst[pOut++] = c;
                        break;
                };
            }
        });
    }
    /// <returns>-1 means fails; otherwise equals bytes written</returns>
    public static int Prepare_AsciiOnly(ReadOnlySpan<byte> src, Span<byte> dst) {
        int pOut = 0;
        for (int pIn = 0; pIn < src.Length; ++pIn) {
            byte c = src[pIn];

            switch (c) {
                case 0xAD:
                    break;
                case 0xA0:
                case 0x20:
                    dst[pOut++] = 0x20;
                    break;
                case 0x7F:
                    return -1;
                default:
                    if (c < 0x1F) {
                        return -1;
                    }
                    if (c >= 0x80 && c <= 0x9F) {
                        return -1;
                    }
                    dst[pOut++] = c;
                    break;
            };
        }

        return pOut;
    }
    /// <returns>-1 means fails; otherwise equals bytes written</returns>
    public static int Prepare_AsciiOnly(ReadOnlySpan<char> src, Span<byte> dst) {
        int pOut = 0;
        for (int pIn = 0; pIn < src.Length; ++pIn) {
            byte c = (byte)src[pIn];

            switch (c) {
                case 0xAD:
                    break;
                case 0xA0:
                case 0x20:
                    dst[pOut++] = 0x20;
                    break;
                case 0x7F:
                    return -1;
                default:
                    if (c < 0x1F) {
                        return -1;
                    }
                    if (c >= 0x80 && c <= 0x9F) {
                        return -1;
                    }
                    dst[pOut++] = c;
                    break;
            };
        }

        return pOut;
    }
}