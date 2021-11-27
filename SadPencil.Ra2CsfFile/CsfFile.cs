using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// This class reads and writes the stringtable file (.csf) that is used by RA2/YR. <br/>
    /// Note: the 'Extra Value' of a label is currently ignored. It has no effect on gaming.
    /// </summary>
    public partial class CsfFile
    {
        // https://modenc.renegadeprojects.com/CSF_File_Format

        /// <summary>
        /// The labels of this file. Each label has a name, and a list of strings, which are the dictionary keys and values.        
        /// </summary>
        public IDictionary<string, List<string>> Labels { get; } = new Dictionary<string, List<string>>();
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
        /// Create an empty stringtable file.
        /// </summary>
        public CsfFile() { }

        /// <summary>
        /// Load an existing stringtable file (.csf).
        /// </summary>
        /// <param name="stream">The file stream of a stringtable file (.csf).</param>
        public static CsfFile LoadFromCsfFile(Stream stream)
        {
            var csf = new CsfFile();
            using (var br = new BinaryReader(stream, Encoding.ASCII)) // the file has multiple encoding, but ASCII is here used to use BinaryReader.ReadChars()
            {
                // read headers
                var headerId = br.ReadBytes(4);
                if (!headerId.SequenceEqual(Encoding.ASCII.GetBytes(" FSC")))
                {
                    throw new Exception("Invalid CSF file header.");
                }

                csf.Version = br.ReadInt32();

                var labelsNum = br.ReadInt32();
                var stringsNum = br.ReadInt32();
                _ = br.ReadInt32(); // unused
                csf.Language = GetCsfLang(br.ReadInt32());

                // read labels
                for (int iLabel = 0; iLabel < labelsNum; iLabel++)
                {
                    // read label names
                    while (true)
                    {
                        var labelId = br.ReadBytes(4);
                        if (labelId.SequenceEqual(Encoding.ASCII.GetBytes(" LBL")))
                        {
                            break;
                        }
                        if (labelId.Length != 4)
                        {
                            throw new Exception("Unexpected end of file.");
                        }
                    }

                    int numValues = br.ReadInt32();
                    int labelNameLength = br.ReadInt32();
                    byte[] labelName = br.ReadBytes(labelNameLength);
                    string labelNameStr;
                    try
                    {
                        labelNameStr = Encoding.ASCII.GetString(labelName);
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Invalid label name at position { stream.Position}.");
                    }

                    if (!ValidateLabelName(labelNameStr))
                    {
                        throw new Exception($"Invalid characters found in label name \"{labelNameStr}\" at position { stream.Position}.");
                    }


                    // read values
                    List<string> values = new List<string>();
                    for (int iValue = 0; iValue < numValues; iValue++)
                    {
                        var labelValueType = br.ReadBytes(4);
                        bool labelHasExtraValue;

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
                            throw new Exception($"Invalid label value type at position { stream.Position}.");
                        }

                        int valueLength = br.ReadInt32();
                        byte[] value = br.ReadBytes(valueLength * 2);
                        value = value.Select(v => (byte)(~v)).ToArray(); // perform bitwise NOT to the bytes
                        string valueStr;
                        try
                        {
                            valueStr = Encoding.Unicode.GetString(value);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Invalid label value string at position { stream.Position}." + ex.Message);
                        }

                        if (labelHasExtraValue)
                        {
                            int extLength = br.ReadInt32();
                            _ = br.ReadBytes(extLength);
                        }

                        values.Add(valueStr);
                    }

                    // append
                    csf.Labels.Add(labelNameStr, values);
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
                int numLabels = this.Labels.Keys.Count;
                bw.Write(numLabels);
                int numValues = this.Labels.Values.Select(list => list.Count).Sum();
                bw.Write(numValues);
                bw.Write((Int32)0); // unused
                bw.Write((Int32)this.Language);
                // write labels
                foreach (var labelNameValues in this.Labels)
                {
                    var labelName = labelNameValues.Key;
                    var labelValues = labelNameValues.Value;

                    if (!ValidateLabelName(labelName))
                    {
                        throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                    }

                    bw.Write(Encoding.ASCII.GetBytes(" LBL"));
                    bw.Write(labelValues.Count);
                    var labelNameBytes = Encoding.ASCII.GetBytes(labelName);
                    bw.Write(labelNameBytes.Length);
                    bw.Write(labelNameBytes);

                    // write values
                    foreach (var valueStr in labelValues)
                    {
                        bw.Write(Encoding.ASCII.GetBytes(" RTS"));
                        var valueBytes = Encoding.Unicode.GetBytes(valueStr);
                        valueBytes = valueBytes.Select(v => (byte)(~v)).ToArray(); // perform bitwise NOT to the bytes
                        if (valueBytes.Length % 2 != 0)
                        {
                            throw new Exception("Unexpected UTF-16 LE bytes. Why do I get an odd number of bytes? It should never happens.");
                        }
                        bw.Write(valueBytes.Length / 2);
                        bw.Write(valueBytes);
                    }
                }
            }
        }

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
        /// <summary>
        /// Check whether the name of a label is valid. A valid label name is an ASCII string without spaces, tabs, line breaks, and invisible characters.
        /// </summary>
        /// <param name="labelName">The name of a label to be checked.</param>
        /// <returns>Whether the name is valid or not.</returns>
        public static bool ValidateLabelName(string labelName)
        {
            // is an ASCII string
            // do not contains spaces, tabs and line breaks
            return labelName.ToCharArray().Where(c => (c <= 32 || c >= 127)).Count() == 0;
        }
    }
}
