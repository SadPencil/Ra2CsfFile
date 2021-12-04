using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IniParser;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// A helper class for CsfFile class. It focus on loading/writing .ini files that represents the string table, which can be regarded as a replacement of .csf files.
    /// </summary>
    public static class CsfFileIniHelper
    {
        private const string INI_TYPE_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_SECTION_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const string INI_FILE_HEADER_INI_VERSION_KEY = "IniVersion";
        private const string INI_FILE_HEADER_CSF_VERSION_KEY = "CsfVersion";
        private const string INI_FILE_HEADER_CSF_LANGUAGE_KEY = "CsfLang";

        private const int INI_VERSION = 2;
        private static IniParserConfiguration IniParserConfiguration { get; } = new IniParserConfiguration()
        {
            AllowDuplicateKeys = false,
            AllowDuplicateSections = false,
            AllowKeysWithoutSection = false,
            CommentRegex = new System.Text.RegularExpressions.Regex("a^"), // match nothing
            CaseInsensitive = true,
        };

        private static IniData GetIniData() => new IniData() { Configuration = IniParserConfiguration, };

        private static IniDataParser GetIniDataParser() => new IniDataParser(IniParserConfiguration);

        private static IniData ParseIni(Stream stream)
        {
            var parser = new IniDataParser(IniParserConfiguration);

            using (var sr = new StreamReader(stream, new UTF8Encoding(false)))
            {
                return parser.Parse(sr.ReadToEnd());
            }
        }

        /// <summary>
        /// Load an existing ini file that represent the stringtable.
        /// </summary>
        /// <param name="stream">The file stream of an ini file.</param>
        public static CsfFile LoadFromIniFile(Stream stream) => LoadFromIniFile(stream, new CsfFileOptions());
        /// <summary>
        /// Load an existing ini file that represent the stringtable.
        /// </summary>
        /// <param name="stream">The file stream of an ini file.</param>
        /// <param name="options">The CsfFileOptions.</param>
        public static CsfFile LoadFromIniFile(Stream stream, CsfFileOptions options)
        {
            CsfFile csf = new CsfFile(options);

            var ini = ParseIni(stream);
            if (!ini.Sections.ContainsSection(INI_FILE_HEADER_SECTION_NAME))
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            var header = ini.Sections[INI_FILE_HEADER_SECTION_NAME];


            // load header
            if (!header.ContainsKey(INI_FILE_HEADER_INI_VERSION_KEY))
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_INI_VERSION_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            var iniVersion = header[INI_FILE_HEADER_INI_VERSION_KEY];
            if (Convert.ToInt32(iniVersion, CultureInfo.InvariantCulture) != INI_VERSION)
            {
                throw new Exception($"Unknown {INI_TYPE_NAME} file version. The version should be {INI_VERSION}. Is this a {INI_TYPE_NAME} file from future?");
            }

            if (!header.ContainsKey(INI_FILE_HEADER_CSF_VERSION_KEY))
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_CSF_VERSION_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            var csfVersion = header[INI_FILE_HEADER_CSF_VERSION_KEY];
            csf.Version = Convert.ToInt32(csfVersion, CultureInfo.InvariantCulture);

            if (!header.ContainsKey(INI_FILE_HEADER_CSF_LANGUAGE_KEY))
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_CSF_LANGUAGE_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            var csfLang = header[INI_FILE_HEADER_CSF_LANGUAGE_KEY];
            csf.Language = CsfLangHelper.GetCsfLang(Convert.ToInt32(csfLang, CultureInfo.InvariantCulture));

            // load all labels
            var labelSections = new Dictionary<string, KeyDataCollection>();
            foreach (var (k, v) in ini.Sections.Where(section => section.SectionName != INI_FILE_HEADER_SECTION_NAME)
                .Select(section => (section.SectionName, ini.Sections[section.SectionName])))
            {
                labelSections.Add(k, v);
            }

            foreach (var keyValuePair in labelSections)
            {
                string labelName = keyValuePair.Key;
                var key = keyValuePair.Value;

                if (!CsfFile.ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                List<string> valueSplited = new List<string>();
                for (int iLine = 1; ; iLine++)
                {
                    string keyName = GetIniLabelValueKeyName(iLine);

                    if (!key.ContainsKey(keyName))
                    {
                        break;
                    }

                    valueSplited.Add(key[keyName]);
                }

                if (valueSplited.Count != 0)
                {
                    string labelValue = string.Join(CsfFile.LineBreakCharacters, valueSplited);
                    labelName = CsfFile.LowercaseLabelName(labelName);
                    csf.AddLabel(labelName, labelValue);
                }

            }

            return csf;
        }

        private static string GetIniLabelValueKeyName(int lineIndex) => "Value" + ((lineIndex == 1) ? String.Empty : $"Line{lineIndex}");

        /// <summary>
        /// Write an ini file that represent the stringtable.
        /// </summary>
        /// <param name="csf">The CsfFile object to be written.</param>
        /// <param name="stream">The file stream of a new ini file.</param>
        public static void WriteIniFile(CsfFile csf, Stream stream)
        {
            var ini = GetIniData();

            // write headers
            _ = ini.Sections.AddSection(INI_FILE_HEADER_SECTION_NAME);
            var header = ini.Sections[INI_FILE_HEADER_SECTION_NAME];
            _ = header.AddKey(INI_FILE_HEADER_INI_VERSION_KEY, INI_VERSION.ToString(CultureInfo.InvariantCulture));
            _ = header.AddKey(INI_FILE_HEADER_CSF_VERSION_KEY, csf.Version.ToString(CultureInfo.InvariantCulture));
            _ = header.AddKey(INI_FILE_HEADER_CSF_LANGUAGE_KEY, ((int)csf.Language).ToString(CultureInfo.InvariantCulture));

            // write labels
            foreach (var labelNameValues in csf.Labels)
            {
                var labelName = labelNameValues.Key;
                var labelValue = labelNameValues.Value;

                if (!CsfFile.ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                _ = ini.Sections.AddSection(labelName);
                var labelSection = ini.Sections[labelName];

                var value = labelValue;
                var valueSplited = value.Split(CsfFile.LineBreakCharacters.ToCharArray());
                for (int iLine = 1; iLine <= valueSplited.Length; iLine++)
                {
                    string keyName = GetIniLabelValueKeyName(iLine);
                    string keyValue = valueSplited[iLine - 1];

                    _ = labelSection.AddKey(keyName, keyValue);
                }
            }

            using (var sw = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                sw.Write(ini.ToString());
            }
        }
    }
}
