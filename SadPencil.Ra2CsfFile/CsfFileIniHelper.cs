using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MadMilkman.Ini;

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
        private static IniFile NewIniFile()
        {
            IniOptions iniOptions = new IniOptions();
            iniOptions.SectionDuplicate = IniDuplication.Disallowed;
            iniOptions.KeyDuplicate = IniDuplication.Disallowed;
            iniOptions.SectionNameCaseSensitive = false;
            iniOptions.CommentStarter = IniCommentStarter.Hash;

            IniFile iniFile = new IniFile();
            return iniFile;
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

            IniFile ini = NewIniFile();
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
            if (Convert.ToInt32(iniVersion, CultureInfo.InvariantCulture) != INI_VERSION)
            {
                throw new Exception($"Unknown {INI_TYPE_NAME} file version. The version should be {INI_VERSION}. Is this a {INI_TYPE_NAME} file from future?");
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
            csf.Language = CsfLangHelper.GetCsfLang(Convert.ToInt32(csfLang, CultureInfo.InvariantCulture));

            // load all labels
            var labelSections = ini.Sections.Where(section => section.Name != INI_FILE_HEADER_SECTION_NAME);
            foreach (var labelSection in labelSections)
            {
                string labelName = labelSection.Name;
                if (!CsfFile.ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                List<string> valueSplited = new List<string>();
                for (int iLine = 1; ; iLine++)
                {
                    string keyName = GetIniLabelValueKeyName(iLine);
                    var value = labelSection.Keys.FirstOrDefault(key => key.Name == keyName);

                    if (value == null)
                    {
                        break;
                    }

                    valueSplited.Add(value.Value);
                }

                if (valueSplited.Count != 0)
                {
                    string labelValue = string.Join(CsfFile.LineBreakCharacters, valueSplited);
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
            IniFile ini = NewIniFile();

            // write headers
            _ = ini.Sections.Add(INI_FILE_HEADER_SECTION_NAME, new Dictionary<string, string>()
            {
                { INI_FILE_HEADER_INI_VERSION_KEY, INI_VERSION.ToString(CultureInfo.InvariantCulture) },
                { INI_FILE_HEADER_CSF_VERSION_KEY, csf.Version.ToString(CultureInfo.InvariantCulture) },
                { INI_FILE_HEADER_CSF_LANGUAGE_KEY, ((int)csf.Language).ToString(CultureInfo.InvariantCulture) },
            });

            // write labels
            foreach (var labelNameValues in csf.Labels)
            {
                var labelName = labelNameValues.Key;
                var labelValue = labelNameValues.Value;

                if (!CsfFile.ValidateLabelName(labelName))
                {
                    throw new Exception($"Invalid characters found in label name \"{labelName}\".");
                }

                var labelSection = ini.Sections.Add(labelName);

                var value = labelValue;
                var valueSplited = value.Split(CsfFile.LineBreakCharacters.ToCharArray());
                for (int iLine = 1; iLine <= valueSplited.Length; iLine++)
                {
                    string keyName = GetIniLabelValueKeyName(iLine);
                    string keyValue = valueSplited[iLine - 1];
                    _ = labelSection.Keys.Add(keyName, keyValue);

                    if (keyName.Contains("#") || keyValue.Contains("#"))
                    {
                        throw new Exception("When saving to INI file, the character # is not allowed in the label name or value.");
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
