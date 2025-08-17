using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

[assembly: CLSCompliant(true)]
namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// This class reads and writes the stringtable file (.csf) that is used by RA2/YR. <br/>
    /// </summary>
    public class CsfFile : ICloneable, IEquatable<CsfFile>
    {
        // https://modenc.renegadeprojects.com/CSF_File_Format

        /// <summary>
        /// This option controls the behavior of CsfFile.
        /// </summary>
        public CsfFileOptions Options { get; set; } = new CsfFileOptions();

        private readonly Dictionary<String, String> _labels = new Dictionary<String, String>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// The labels of this file. Each label has a name, and a string, which are the dictionary keys and values.        
        /// </summary>
        public IReadOnlyDictionary<String, String> Labels => this._labels;

        /// <summary>
        /// The line break characters between the multiple lines in the label value.
        /// </summary>
        public static String LineBreakCharacters { get; } = "\n";

        /// <summary>
        /// The language of this file.
        /// </summary>
        public CsfLang Language { get; set; } = CsfLang.EnglishUS;
        /// <summary>
        /// The version number of the CSF format. <br/>
        /// RA2, YR, Generals, ZH and the BFME series use version 3. <br/>
        /// Nox uses version 2. <br/>
        /// Nothing is known about the actual difference between the versions.
        /// </summary>
        public Int32 Version { get; set; } = 3;
        /// <summary>
        /// Add or replace a label to the string table. The label name will be checked. <br/>
        /// If the label value contains multiple lines, use LineBreakCharacters to separate the line.
        /// </summary>
        /// <param name="labelName">The label name.</param>
        /// <param name="labelValue">The label value.</param>
        /// <returns>True if an existing element is found and replaced.</returns>
        public Boolean AddLabel(String labelName, String labelValue)
        {
            if (!ValidateLabelName(labelName))
            {
                throw new Exception("Invalid characters found in label name.");
            }
            if (this.Labels.ContainsKey(labelName))
            {
                this._labels[labelName] = labelValue;
                return true;
            }
            else
            {
                this._labels.Add(labelName, labelValue);
                return false;
            }
        }

        /// <summary>
        /// Remove a label from the string table.
        /// </summary>
        /// <param name="labelName">The label name.</param>
        /// <returns>True if the element is found and removed.</returns>
        public Boolean RemoveLabel(String labelName) => this._labels.Remove(labelName);

        /// <summary>
        /// Create an empty stringtable file with default options.
        /// </summary>
        public CsfFile() { }

        /// <summary>
        /// Create an empty stringtable file with given options.
        /// </summary>
        /// <param name="options">The CsfFileOptions.</param>
        public CsfFile(CsfFileOptions options) => this.Options = options;

        /// <summary>
        /// Clone the CsfFile.
        /// </summary>
        /// <param name="csf">The CsfFile object.</param>
        public CsfFile(CsfFile csf)
        {
            if (csf == null)
            {
                throw new ArgumentNullException(nameof(csf));
            }

            this.Version = csf.Version;
            this.Language = csf.Language;
            this.Options = csf.Options;
            this._labels = new Dictionary<String, String>(csf._labels, StringComparer.InvariantCultureIgnoreCase);
        }

        public Object Clone() => new CsfFile(this);

        /// <summary>
        /// Load an existing stringtable file (.csf).<br/>
        /// <br/>
        /// Note: for those labels that has more than one values, only the first value is reserved. It has no effect on gaming. <br/>
        /// Note: the 'Extra Value' of a label is ignored. It has no effect on gaming.
        /// </summary>
        /// <param name="stream">The file stream of a stringtable file (.csf).</param>
        public static CsfFile LoadFromCsfFile(Stream stream) => LoadFromCsfFile(stream, new CsfFileOptions());

        private static byte[] TruncateUtf16BytesAtDoubleZero(byte[] inputBytes)
        {
            if (inputBytes == null)
            {
                throw new ArgumentNullException(nameof(inputBytes));
            }

            // Process two bytes at a time
            for (int i = 0; i < inputBytes.Length - 1; i += 2)
            {
                // Check for 0x00, 0x00
                if (inputBytes[i] == 0x00 && inputBytes[i + 1] == 0x00)
                {
                    // Create new array up to this point (excluding the 0x00, 0x00)
                    byte[] result = new byte[i];
                    Array.Copy(inputBytes, 0, result, 0, i);
                    return result;
                }
            }

            return inputBytes;
        }

        /// <summary>
        /// Load an existing stringtable file (.csf).<br/>
        /// <br/>
        /// Note: for those labels that has more than one values, only the first value is reserved. It has no effect on gaming. <br/>
        /// Note: the 'Extra Value' of a label is ignored. It has no effect on gaming.
        /// </summary>
        /// <param name="stream">The file stream of a stringtable file (.csf).</param>
        /// <param name="options">The CsfFileOptions.</param>
        public static CsfFile LoadFromCsfFile(Stream stream, CsfFileOptions options)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var csf = new CsfFile(options);
            using (var br = new BinaryReader(stream, Encoding.ASCII)) // the file has multiple encoding, but ASCII is here used to use BinaryReader.ReadChars()
            {
                // read headers
                Byte[] headerId = br.ReadBytes(4);
                if (!headerId.SequenceEqual(Encoding.ASCII.GetBytes(" FSC")))
                {
                    throw new Exception("Invalid CSF file header.");
                }

                csf.Version = br.ReadInt32();

                Int32 labelsNum = br.ReadInt32();
                Int32 stringsNum = br.ReadInt32();
                _ = br.ReadInt32(); // unused
                csf.Language = CsfLangHelper.GetCsfLang(br.ReadInt32());

                // read labels
                for (Int32 iLabel = 0; iLabel < labelsNum; iLabel++)
                {
                    // read label names
                    while (true)
                    {
                        Byte[] labelId = br.ReadBytes(4);
                        if (labelId.SequenceEqual(Encoding.ASCII.GetBytes(" LBL")))
                        {
                            break;
                        }
                        if (labelId.Length != 4)
                        {
                            throw new Exception("Unexpected end of file.");
                        }
                    }

                    Int32 numValues = br.ReadInt32();
                    Int32 labelNameLength = br.ReadInt32();
                    Byte[] labelName = br.ReadBytes(labelNameLength);
                    String labelNameStr;
                    try
                    {
                        labelNameStr = Encoding.ASCII.GetString(labelName);
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Invalid label name at position {stream.Position}.");
                    }

                    if (!ValidateLabelName(labelNameStr))
                    {
                        throw new Exception($"Invalid characters found in label name \"{labelNameStr}\" at position {stream.Position}.");
                    }

                    // read values
                    // only the first value is preserved; others are useless
                    String labelValue = null;
                    for (Int32 iValue = 0; iValue < numValues; iValue++)
                    {
                        Byte[] labelValueType = br.ReadBytes(4);
                        Boolean labelHasExtraValue;

                        if (labelValueType.SequenceEqual(Encoding.ASCII.GetBytes(" RTS")))
                        {
                            labelHasExtraValue = false;
                        }
                        else if (labelValueType.SequenceEqual(Encoding.ASCII.GetBytes("WRTS")))
                        {
                            labelHasExtraValue = true;
                        }
                        else
                        {
                            throw new Exception($"Invalid label value type at position {stream.Position}.");
                        }

                        Int32 valueLength = br.ReadInt32();
                        Byte[] value = br.ReadBytes(valueLength * 2);

                        // If the byte array ends with (0x00, 0x00), delete the suffix
                        value = TruncateUtf16BytesAtDoubleZero(value);

                        // perform bitwise NOT to the bytes
                        value = value.Select(v => (Byte)(~v)).ToArray();

                        String valueStr;
                        try
                        {
                            valueStr = Encoding.Unicode.GetString(value);
                            if (options.Encoding1252ReadWorkaround)
                            {
                                valueStr = Encoding1252Workaround.ConvertsUnicodeToEncoding1252(valueStr);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Invalid label value string at position {stream.Position}." + ex.Message);
                        }

                        Debug.Assert(!valueStr.Contains('\uFFFE') && !valueStr.Contains('\uFFFF'), $"The CSF value contains invalid characters. ${valueStr}");

                        if (labelHasExtraValue)
                        {
                            Int32 extLength = br.ReadInt32();
                            _ = br.ReadBytes(extLength);
                        }

                        if (iValue == 0)
                        {
                            labelValue = valueStr;
                        }
                    }

                    // omit 0 values, so the behavior is consistent with .ini
                    if (labelValue != null)
                    {
                        // append
                        _ = csf.AddLabel(labelNameStr, labelValue);
                    }
                }
            }
            return csf;
        }

        /// <summary>
        /// Write a stringtable file (.csf).
        /// </summary>
        /// <param name="stream">The file stream of a new stringtable file (.csf).</param>
        public void WriteCsfFile(Stream stream)
        {
            using (var bw = new BinaryWriter(stream))
            {
                // write header
                bw.Write(Encoding.ASCII.GetBytes(" FSC")); // csf header
                bw.Write(this.Version); // version
                Int32 numLabels = this.Labels.Count;
                bw.Write(numLabels);
                Int32 numValues = this.Labels.Count;
                bw.Write(numValues);
                bw.Write((Int32)0); // unused
                bw.Write((Int32)this.Language);
                // write labels
                foreach (var labelNameValues in this.Labels)
                {
                    String labelName = labelNameValues.Key;
                    String labelValue = labelNameValues.Value;

                    if (this.Options.Encoding1252WriteWorkaround)
                    {
                        labelValue = Encoding1252Workaround.ConvertsEncoding1252ToUnicode(labelValue);
                    }

                    if (!ValidateLabelName(labelName))
                    {
                        throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                    }

                    bw.Write(Encoding.ASCII.GetBytes(" LBL"));
                    bw.Write((Int32)1);
                    Byte[] labelNameBytes = Encoding.ASCII.GetBytes(labelName);
                    bw.Write(labelNameBytes.Length);
                    bw.Write(labelNameBytes);

                    // write values 
                    bw.Write(Encoding.ASCII.GetBytes(" RTS"));
                    Byte[] valueBytes = Encoding.Unicode.GetBytes(labelValue);
                    valueBytes = valueBytes.Select(v => (Byte)(~v)).ToArray(); // perform bitwise NOT to the bytes
                    if (valueBytes.Length % 2 != 0)
                    {
                        throw new Exception("Unexpected UTF-16 LE bytes. Why do I get an odd number of bytes? It should never happens.");
                    }
                    bw.Write(valueBytes.Length / 2);
                    bw.Write(valueBytes);
                }
            }
        }

        /// <summary>
        /// Converts an Int32 integer to CsfLang enum. Return CsfLang.Unknown for unknown integers.
        /// </summary>
        /// <param name="value">The integer to be converted.</param>
        /// <returns>The corresponding CsfLang enum.</returns>
        [Obsolete("Please use CsfLangHelper.GetCsfLang() instead.")]
        public static CsfLang GetCsfLang(Int32 value) => CsfLangHelper.GetCsfLang(value);
        /// <summary>
        /// Check whether the name of a label is valid. A valid label name is an ASCII string without spaces, tabs, line breaks, and invisible characters.
        /// </summary>
        /// <param name="labelName">The name of a label to be checked.</param>
        /// <returns>Whether the name is valid or not.</returns>
        public static Boolean ValidateLabelName(String labelName) =>
        // is an ASCII string
        // do not contains tabs and line breaks
        // note: space are tolerated because in the original ra2.csf file there is a label named [GUI:Password entry box label]
            !string.IsNullOrEmpty(labelName) && !labelName.ToCharArray().Any(c => (c < 32 || c >= 127));

        /// <summary>
        /// Load an existing ini file that represent the stringtable.
        /// </summary>
        /// <param name="stream">The file stream of an ini file.</param>
        [Obsolete("Please use CsfFileIniHelper.LoadFromIniFile() instead.")]
        public static CsfFile LoadFromIniFile(Stream stream) => CsfFileIniHelper.LoadFromIniFile(stream);

        /// <summary>
        /// Write an ini file that represent the stringtable.
        /// </summary>
        /// <param name="stream">The file stream of a new ini file.</param>
        [Obsolete("Please use CsfFileIniHelper.WriteIniFile() instead.")]
        public void WriteIniFile(Stream stream) => CsfFileIniHelper.WriteIniFile(this, stream);

        public Boolean Equals(CsfFile other)
        {
            if (other == null)
            {
                return false;
            }

            Boolean result = true;
            result &= this.Language == other.Language && this.Version == other.Version && this.Options.Equals(other.Options);
            result &= this.Labels.Count == other.Labels.Count;
            if (!result)
            {
                return false;
            }

            foreach (string label in this.Labels.Keys)
            {
                if (!other.Labels.ContainsKey(label) || !this.Labels[label].Equals(other.Labels[label], StringComparison.InvariantCulture))
                {
                    Debug.WriteLine($"CSF files do not equal. Label: {label}.");
                    return false;
                }
            }

            return true;
        }

        public override Int32 GetHashCode()
        {
            return new
            {
                this.Language,
                this.Version,
                this.Options,
                this.Labels,
            }.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is CsfFile)
            {
                return Equals((CsfFile)obj);
            }

            return base.Equals(obj);
        }

        public static bool operator ==(CsfFile left, CsfFile right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(CsfFile left, CsfFile right)
        {
            return !(left == right);
        }

    }
}
