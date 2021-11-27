using MadMilkman.Ini;
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
    public class CsfFile
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

        private const string INI_TYPE_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_SECTION_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_INI_VERSION_KEY = "IniVersion";
        private const string INI_FILE_HEADER_CSF_VERSION_KEY = "CsfVersion";
        private const string INI_FILE_HEADER_CSF_LANGUAGE_KEY = "CsfLang";


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
        /// Load an existing ini file that represent the stringtable.
        /// </summary>
        /// <param name="stream">The file stream of an ini file.</param>
        public static CsfFile LoadFromIniFile(Stream stream)
        {
            CsfFile csf = new CsfFile();

            IniFile ini = new IniFile();
            using (var sr = new StreamReader(stream, new UTF8Encoding(false)))
            {
                ini.Load(sr);
            }

            var header = ini.Sections.FirstOrDefault(section => section.Name == INI_FILE_HEADER_SECTION_NAME);
            if (header == null)
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing section [{INI_FILE_HEADER_SECTION_NAME}].");
            }

            // load header
            var iniVersion = header.Keys.FirstOrDefault(key => key.Name == INI_FILE_HEADER_INI_VERSION_KEY)?.Value;
            if (iniVersion == null)
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_INI_VERSION_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            if (Convert.ToInt32(iniVersion) != 1)
            {
                throw new Exception($"Unknown {INI_TYPE_NAME} file version. The version should be 1. Is this a {INI_TYPE_NAME} file from future?");
            }

            var csfVersion = header.Keys.FirstOrDefault(key => key.Name == INI_FILE_HEADER_CSF_VERSION_KEY)?.Value;
            if (csfVersion == null)
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_CSF_VERSION_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            csf.Version = Convert.ToInt32(csfVersion, CultureInfo.InvariantCulture);

            var csfLang = header.Keys.FirstOrDefault(key => key.Name == INI_FILE_HEADER_CSF_LANGUAGE_KEY)?.Value;
            if (csfLang == null)
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_CSF_LANGUAGE_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            csf.Language = GetCsfLang(Convert.ToInt32(csfLang, CultureInfo.InvariantCulture));

            // load all labels
            var labelSections = ini.Sections.Where(section => section.Name != INI_FILE_HEADER_SECTION_NAME);
            foreach (var labelSection in labelSections)
            {
                string labelName = labelSection.Name;
                if (!ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                List<string> values = new List<string>();
                for (int iValue = 1; ; iValue++)
                {
                    List<string> valueSplited = new List<string>();
                    for (int iLine = 1; ; iLine++)
                    {
                        string keyName = GetIniLabelValueKeyName(iValue, iLine);
                        var value = labelSection.Keys.FirstOrDefault(key => key.Name == keyName);

                        if (value == null)
                        {
                            break;
                        }

                        valueSplited.Add(value.Value);
                    }

                    if (valueSplited.Count == 0)
                    {
                        break;
                    }
                    values.Add(string.Join("\n", valueSplited));
                }

                csf.Labels.Add(labelName, values);
            }

            return csf;
        }

        private static string GetIniLabelValueKeyName(int valueIndex, int lineIndex) => ((valueIndex == 1) ? "Value" : $"Value{valueIndex}") + ((lineIndex == 1) ? String.Empty : $"Line{lineIndex}");

        /// <summary>
        /// Write an ini file that represent the stringtable.
        /// </summary>
        /// <param name="stream">The file stream of a new ini file.</param>
        public void WriteIniFile(Stream stream)
        {
            IniFile ini = new IniFile();

            // write headers
            _ = ini.Sections.Add(INI_FILE_HEADER_SECTION_NAME, new Dictionary<string, string>()
            {
                { INI_FILE_HEADER_INI_VERSION_KEY, 1.ToString(CultureInfo.InvariantCulture) },
                { INI_FILE_HEADER_CSF_VERSION_KEY, this.Version.ToString(CultureInfo.InvariantCulture) },
                { INI_FILE_HEADER_CSF_LANGUAGE_KEY, ((int)this.Language).ToString(CultureInfo.InvariantCulture) },
            });

            // write labels
            foreach (var labelNameValues in this.Labels)
            {
                var labelName = labelNameValues.Key;
                var labelValues = labelNameValues.Value;

                if (!ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                var labelSection = ini.Sections.Add(labelName);

                for (int iValue = 1; iValue <= labelValues.Count; iValue++)
                {
                    var value = labelValues[iValue - 1];
                    var valueSplited = value.Split('\n');
                    for (int iLine = 1; iLine <= valueSplited.Length; iLine++)
                    {
                        string keyName = GetIniLabelValueKeyName(iValue, iLine);
                        string keyValue = valueSplited[iLine - 1];
                        _ = labelSection.Keys.Add(keyName, keyValue);
                    }
                }
            }

            using (var sw = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                ini.Save(sw);
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
