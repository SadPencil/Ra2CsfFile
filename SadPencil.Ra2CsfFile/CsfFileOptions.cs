using System;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// This class controls the behavior of CsfFile
    /// </summary>
    public class CsfFileOptions : IEquatable<CsfFileOptions>
    {
        /// <summary>
        /// For code points 128-159 (0x80-0x9F), the original font file of RA2 mistakenly treat these characters as Windows-1252, instead of Unicode (ISO-8859-1). <br/>
        /// Enabling this option will corrects there charaters to Unicode ones when loading a .csf file.
        /// </summary>
        public Boolean Encoding1252ReadWorkaround { get; set; } = true;
        /// <summary>
        /// For code points 128-159 (0x80-0x9F), the original font file of RA2 mistakenly treat these characters as Windows-1252, instead of Unicode (ISO-8859-1). <br/>
        /// Enabling this option will converts there charaters from Unicode ones back to Windows-1252 when saving the .csf file. <br/>
        /// Note: it is recommended to turn this option off. In the original game.fnt file, except for Trade Mark Sign ™, other influenced characters have the correct font data in their Unicode code point. 
        /// </summary>
#pragma warning disable CA1805
        public Boolean Encoding1252WriteWorkaround { get; set; } = false;
#pragma warning restore CA1805

        public Boolean Equals(CsfFileOptions other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Encoding1252ReadWorkaround == other.Encoding1252ReadWorkaround && this.Encoding1252WriteWorkaround == other.Encoding1252WriteWorkaround;
        }

        public override Int32 GetHashCode()
        {
            return new
            {
                this.Encoding1252ReadWorkaround,
                this.Encoding1252WriteWorkaround,
            }.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is CsfFileOptions)
            {
                return Equals((CsfFileOptions)obj);
            }

            return base.Equals(obj);
        }

        public static bool operator ==(CsfFileOptions left, CsfFileOptions right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(CsfFileOptions left, CsfFileOptions right)
        {
            return !(left == right);
        }

    }
}
