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
        public static IReadOnlyDictionary<Char, Char> Encoding1252ToUnicode { get; }
        public static IReadOnlyDictionary<Char, Char> UnicodeToEncoding1252 { get; }

        static Encoding1252Workaround()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var encoding1252ToUnicode = new Dictionary<Char, Char>();
            var unicodeToEncoding1252 = new Dictionary<Char, Char>();
            for (Int32 i = 128; i <= 159; i++)
            {
                Char[] unicode = Encoding.Unicode.GetChars(new Byte[] { (Byte)i, (Byte)0 });
                Debug.Assert(unicode.Length == 1);
                Char unicodeChar = unicode[0];

                Char[] encoding1252 = Encoding.GetEncoding(1252).GetChars(new Byte[] { (Byte)i });
                Debug.Assert(encoding1252.Length == 1);
                Char encoding1252char = encoding1252[0];

                if (!Char.IsControl(encoding1252char))
                {
                    encoding1252ToUnicode.Add(encoding1252char, unicodeChar);
                    unicodeToEncoding1252.Add(unicodeChar, encoding1252char);
                }
            }

            Debug.Assert(encoding1252ToUnicode.Count == 27);

            Encoding1252ToUnicode = encoding1252ToUnicode;
            UnicodeToEncoding1252 = unicodeToEncoding1252;
        }

        public static String ConvertsEncoding1252ToUnicode(String value) => new String(value.ToCharArray().Select(c => (Encoding1252ToUnicode.ContainsKey(c) ? Encoding1252ToUnicode[c] : c)).ToArray());
        public static String ConvertsUnicodeToEncoding1252(String value) => new String(value.ToCharArray().Select(c => (UnicodeToEncoding1252.ContainsKey(c) ? UnicodeToEncoding1252[c] : c)).ToArray());

    }
}
