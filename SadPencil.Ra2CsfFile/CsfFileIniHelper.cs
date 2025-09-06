using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// A helper class for CsfFile class. It focus on loading/writing .ini files that represents the string table, which can be regarded as a replacement of .csf files.
    /// </summary>
    public static class CsfFileIniHelper
    {
        private const String INI_TYPE_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const String INI_FILE_HEADER_SECTION_NAME = "SadPencil.Ra2CsfFile.Ini";
        private const String INI_FILE_HEADER_INI_VERSION_KEY = "IniVersion";
        private const String INI_FILE_HEADER_CSF_VERSION_KEY = "CsfVersion";
        private const String INI_FILE_HEADER_CSF_LANGUAGE_KEY = "CsfLang";

        private const Int32 INI_VERSION = 2;

        private static IniParserConfiguration IniParserReadConfiguration { get; } = new IniParserConfiguration()
        {
            AllowDuplicateKeys = true,
            AllowDuplicateSections = true,
            AllowKeysWithoutSection = false,
            CommentRegex = new System.Text.RegularExpressions.Regex("a^"), // match nothing
            CaseInsensitive = true,
            AssigmentSpacer = String.Empty,
            SectionRegex = new Regex("^(\\s*?)\\[{1}\\s*[\\p{L}\\p{P}\\p{M}_\\\"\\'\\{\\}\\#\\+\\;\\*\\%\\(\\)\\=\\?\\&\\$\\^\\<\\>\\`\\^|\\,\\:\\/\\.\\-\\w\\d\\s\\\\\\~]+\\s*\\](\\s*?)$"),
        };

        private static IniParserConfiguration IniParserWriteConfiguration { get; } = new IniParserConfiguration()
        {
            AllowDuplicateKeys = false,
            AllowDuplicateSections = false,
            AllowKeysWithoutSection = false,
            CommentRegex = new System.Text.RegularExpressions.Regex("a^"), // match nothing
            CaseInsensitive = true,
            AssigmentSpacer = String.Empty,
            SectionRegex = new Regex("^(\\s*?)\\[{1}\\s*[\\p{L}\\p{P}\\p{M}_\\\"\\'\\{\\}\\#\\+\\;\\*\\%\\(\\)\\=\\?\\&\\$\\^\\<\\>\\`\\^|\\,\\:\\/\\.\\-\\w\\d\\s\\\\\\~]+\\s*\\](\\s*?)$"),
        };

        private static IniDataParser GetIniDataParser() => new IniDataParser(IniParserReadConfiguration);

        private static IniData ParseIni(Stream stream)
        {
            var parser = GetIniDataParser();

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
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var csf = new CsfFile(options);

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
            String iniVersion = header[INI_FILE_HEADER_INI_VERSION_KEY];
            if (Convert.ToInt32(iniVersion, CultureInfo.InvariantCulture) != INI_VERSION)
            {
                throw new Exception($"Unknown {INI_TYPE_NAME} file version. The version should be {INI_VERSION}. Is this a {INI_TYPE_NAME} file from future?");
            }

            if (!header.ContainsKey(INI_FILE_HEADER_CSF_VERSION_KEY))
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_CSF_VERSION_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            String csfVersion = header[INI_FILE_HEADER_CSF_VERSION_KEY];
            csf.Version = Convert.ToInt32(csfVersion, CultureInfo.InvariantCulture);

            if (!header.ContainsKey(INI_FILE_HEADER_CSF_LANGUAGE_KEY))
            {
                throw new Exception($"Invalid {INI_TYPE_NAME} file. Missing key \"{INI_FILE_HEADER_CSF_LANGUAGE_KEY}\" in section [{INI_FILE_HEADER_SECTION_NAME}].");
            }
            String csfLang = header[INI_FILE_HEADER_CSF_LANGUAGE_KEY];
            csf.Language = CsfLangHelper.GetCsfLang(Convert.ToInt32(csfLang, CultureInfo.InvariantCulture));

            // load all labels
            var labelSections = new Dictionary<String, KeyDataCollection>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var (k, v) in ini.Sections.Where(section => section.SectionName != INI_FILE_HEADER_SECTION_NAME)
                .Select(section => (section.SectionName, ini.Sections[section.SectionName])))
            {
                labelSections.Add(k, v);
            }

            foreach (var keyValuePair in labelSections)
            {
                String labelName = keyValuePair.Key;
                var key = keyValuePair.Value;

                if (!CsfFile.ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                var valueSplited = new List<String>();
                for (Int32 iLine = 1; ; iLine++)
                {
                    String keyName = GetIniLabelValueKeyName(iLine);

                    if (!key.ContainsKey(keyName))
                    {
                        break;
                    }

                    valueSplited.Add(key[keyName]);
                }

                if (valueSplited.Count != 0)
                {
                    String labelValue = String.Join(CsfFile.LineBreakCharacters, valueSplited);
                    _ = csf.AddLabel(labelName, labelValue);
                }

            }

            if (options.OrderByKey)
            {
                csf = csf.OrderByKey();
            }

            return csf;
        }

        private static String GetIniLabelValueKeyName(Int32 lineIndex) => "Value" + ((lineIndex == 1) ? String.Empty : $"Line{lineIndex.ToString(CultureInfo.InvariantCulture)}");

        /// <summary>
        /// Write an ini file that represent the stringtable.
        /// </summary>
        /// <param name="csf">The CsfFile object to be written.</param>
        /// <param name="stream">The file stream of a new ini file.</param>
        public static void WriteIniFile(CsfFile csf, Stream stream)
        {
            if (csf == null)
            {
                throw new ArgumentNullException(nameof(csf));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var ini = new IniData() { Configuration = IniParserWriteConfiguration };

            // write headers
            _ = ini.Sections.AddSection(INI_FILE_HEADER_SECTION_NAME);
            var header = ini.Sections[INI_FILE_HEADER_SECTION_NAME];
            _ = header.AddKey(INI_FILE_HEADER_INI_VERSION_KEY, INI_VERSION.ToString(CultureInfo.InvariantCulture));
            _ = header.AddKey(INI_FILE_HEADER_CSF_VERSION_KEY, csf.Version.ToString(CultureInfo.InvariantCulture));
            _ = header.AddKey(INI_FILE_HEADER_CSF_LANGUAGE_KEY, ((Int32)csf.Language).ToString(CultureInfo.InvariantCulture));

            // write labels
            IEnumerable<KeyValuePair<String, String>> csfLabels = csf.Labels;
            if (csf.Options.OrderByKey)
            {
                csfLabels = csfLabels.OrderBy(csfLabel => csfLabel.Key, NaturalStringComparer.InvariantCultureIgnoreCase);
            }

            foreach (var labelNameValues in csf.Labels)
            {
                String labelName = labelNameValues.Key;
                String labelValue = labelNameValues.Value;

                if (!CsfFile.ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                _ = ini.Sections.AddSection(labelName);
                var labelSection = ini.Sections[labelName];

                String value = labelValue;
                String[] valueSplited = value.Split(CsfFile.LineBreakCharacters.ToCharArray());
                for (Int32 iLine = 1; iLine <= valueSplited.Length; iLine++)
                {
                    String keyName = GetIniLabelValueKeyName(iLine);
                    String keyValue = valueSplited[iLine - 1];

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
