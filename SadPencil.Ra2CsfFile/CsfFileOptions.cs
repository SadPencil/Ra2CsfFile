using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// This class controls the behavior of CsfFile
    /// </summary>
    public class CsfFileOptions
    {
        /// <summary>
        /// For code points 128-159 (0x80-0x9F), the original font file of RA2 mistakenly treat these characters as Windows-1252, instead of Unicode (ISO-8859-1). <br/>
        /// Enabling this option will corrects there charaters to Unicode ones.
        /// </summary>
        public bool Encoding1252ReadWorkaround { get; set; } = true;
        /// <summary>
        /// For code points 128-159 (0x80-0x9F), the original font file of RA2 mistakenly treat these characters as Windows-1252, instead of Unicode (ISO-8859-1). <br/>
        /// Enabling this option will corrects there charaters from Unicode ones back to Windows-1252.
        /// </summary>
        public bool Encoding1252WriteWorkaround { get; set; } = true;
    }
}
