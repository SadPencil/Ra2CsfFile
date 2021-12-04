using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SadPencil.Ra2CsfFile
{
    internal static class Encoding1252Workaround
    {
        public static IReadOnlyDictionary<char, char> Encoding1252ToUnicode { get; }
        public static IReadOnlyDictionary<char, char> UnicodeToEncoding1252 { get; }

        static Encoding1252Workaround()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Dictionary<char, char> encoding1252ToUnicode = new Dictionary<char, char>();
            Dictionary<char, char> unicodeToEncoding1252 = new Dictionary<char, char>();
            for (int i = 128; i <= 159; i++)
            {
                char[] unicode = Encoding.Unicode.GetChars(new byte[] { (byte)i, (byte)0 });
                Debug.Assert(unicode.Length == 1);
                char unicodeChar = unicode[0];

                char[] encoding1252 = Encoding.GetEncoding(1252).GetChars(new byte[] { (byte)i });
                Debug.Assert(encoding1252.Length == 1);
                char encoding1252char = encoding1252[0];

                if (!char.IsControl(encoding1252char))
                {
                    encoding1252ToUnicode.Add(encoding1252char, unicodeChar);
                    unicodeToEncoding1252.Add(unicodeChar, encoding1252char);
                }
            }

            Debug.Assert(encoding1252ToUnicode.Count == 27);

            Encoding1252ToUnicode = encoding1252ToUnicode;
            UnicodeToEncoding1252 = unicodeToEncoding1252;
        }

        public static string ConvertsEncoding1252ToUnicode(string value) => new string(value.ToCharArray().Select(c => (Encoding1252ToUnicode.ContainsKey(c) ? Encoding1252ToUnicode[c] : c)).ToArray());
        public static string ConvertsUnicodeToEncoding1252(string value) => new string(value.ToCharArray().Select(c => (UnicodeToEncoding1252.ContainsKey(c) ? UnicodeToEncoding1252[c] : c)).ToArray());

    }
}
