using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MadMilkman.Ini;

namespace SadPencil.Ra2CsfFile
{
    public partial class CsfFile
    {
        private const string INI_TYPE_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_SECTION_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_INI_VERSION_KEY = "IniVersion";
        private const string INI_FILE_HEADER_CSF_VERSION_KEY = "CsfVersion";
        private const string INI_FILE_HEADER_CSF_LANGUAGE_KEY = "CsfLang";

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
    }
}
