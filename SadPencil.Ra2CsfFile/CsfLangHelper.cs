using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// A helper class for CsfLang enum.
    /// </summary>
    public static class CsfLangHelper
    {
        /// <summary>
        /// Converts an Int32 integer to CsfLang enum. Return CsfLang.Unknown for unknown integers.
        /// </summary>
        /// <param name="value">The integer to be converted.</param>
        /// <returns>The corresponding CsfLang enum.</returns>
        public static CsfLang GetCsfLang(Int32 value)
        {
            if (typeof(CsfLang).IsEnumDefined(value))
            {
                return (CsfLang)value;
            }
            else
            {
                return CsfLang.Unknown;
            }
        }
    }
}
