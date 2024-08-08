namespace Funnyppt.Net;
// TODO: WIP class
internal class StringPrep {
    // Table C.1.2 ASCII space characters
    public static readonly int[] AsciiSpaces = [0x0020];

    // Table C.1.2 Non-ASCII space characters
    public static readonly int[] NonAsciiSpaces = [
        0x00A0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004,
        0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A, 0x200B,
        0x202F, 0x205F, 0x3000
    ];
    public static readonly int[] AllSpaces = [.. AsciiSpaces, .. NonAsciiSpaces];

    // Table C.2.1 ASCII control characters
    public static readonly int[] AsciiControl = [
        0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006,
        0x0007, 0x0008, 0x0009, 0x000A, 0x000B, 0x000C, 0x000D,
        0x000E, 0x000F, 0x0010, 0x0011, 0x0012, 0x0013, 0x0014,
        0x0015, 0x0016, 0x0017, 0x0018, 0x0019, 0x001A, 0x001B,
        0x001C, 0x001D, 0x001E, 0x001F, 0x007F
    ];

    // Table C.2.2 Non-ASCII control characters
    public static readonly int[] NonAsciiControl = [
        0x0080, 0x0081, 0x0082, 0x0083, 0x0084, 0x0085, 0x0086,
        0x0087, 0x0088, 0x0089, 0x008A, 0x008B, 0x008C, 0x008D,
        0x008E, 0x008F, 0x0090, 0x0091, 0x0092, 0x0093, 0x0094,
        0x0095, 0x0096, 0x0097, 0x0098, 0x0099, 0x009A, 0x009B,
        0x009C, 0x009D, 0x009E, 0x009F
    ];
    public static readonly int[] AllControls = [.. AsciiControl, .. NonAsciiControl];

    // Table C.3 Private use characters
    public static readonly int[] PrivateUse = {
        0xE000, 0xF8FF, 0xF0000, 0xFFFFD, 0x100000, 0x10FFFD
    };

    // Table C.4 Non-character code points
    public static readonly int[] NonCharacterCodePoints = {
        0xFDD0, 0xFDEF, 0xFFFE, 0xFFFF, 0x1FFFE, 0x1FFFF, 0x2FFFE,
        0x2FFFF, 0x3FFFE, 0x3FFFF, 0x4FFFE, 0x4FFFF, 0x5FFFE,
        0x5FFFF, 0x6FFFE, 0x6FFFF, 0x7FFFE, 0x7FFFF, 0x8FFFE,
        0x8FFFF, 0x9FFFE, 0x9FFFF, 0xAFFFE, 0xAFFFF, 0xBFFFE,
        0xBFFFF, 0xCFFFE, 0xCFFFF, 0xDFFFE, 0xDFFFF, 0xEFFFE,
        0xEFFFF, 0xFFFFE, 0xFFFFF, 0x10FFFE, 0x10FFFF
    };

    // Table C.5 Surrogate codes
    public static readonly int[] SurrogateCodes = [
        0xD800, 0xDB7F, 0xDB80, 0xDBFF, 0xDC00, 0xDFFF
    ];

    // Table C.6 Inappropriate for plain text
    public static readonly int[] InappropriateForPlainText = [
        0xFFF9, 0xFFFA, 0xFFFB
    ];

    // Table C.7 Inappropriate for canonical representation
    public static readonly int[] InappropriateForCanonical = [
        0x2FF0, 0x2FF1, 0x2FF2, 0x2FF3, 0x2FF4, 0x2FF5, 0x2FF6,
        0x2FF7, 0x2FF8, 0x2FF9, 0x2FFA, 0x2FFB
    ];

    // Table C.8 Change display properties or deprecated
    public static readonly int[] ChangeDisplayProperties = [
        0x0340, 0x0341, 0x200E, 0x200F, 0x202A, 0x202B, 0x202C,
        0x202D, 0x202E, 0x206A, 0x206B, 0x206C, 0x206D, 0x206E,
        0x206F
    ];

    // Table C.9 Tagging characters
    public static readonly int[] TaggingCharacters = {
        0xE0001, 0xE0020, 0xE007F
    };

    public static List<int> GetUnicodeCodePoints(string input) {
        List<int> codePoints = [];

        for (int i = 0; i < input.Length; i++) {
            if (char.IsHighSurrogate(input[i])) {
                codePoints.Add(char.ConvertToUtf32(input, i));
                i++;
            } else {
                codePoints.Add(input[i]);
            }
        }

        return codePoints;
    }
}
