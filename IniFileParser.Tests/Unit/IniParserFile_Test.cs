using System;
using System.IO;
using IniParser.Exceptions;
using IniParser.Model;
using NUnit.Framework;
using IniParser;

namespace IniFileParser.Tests.Unit
{
    [TestFixture, Category("File loading/saving tests")]
    public class IniParserFileTest
    {

        public readonly string strGoodINIFilePath = Path.Combine(AppContext.BaseDirectory, "INIFileGOOD.ini");
        public readonly string strBadINIFilePath = Path.Combine(AppContext.BaseDirectory, "INIfileBAD.ini");
        public readonly string strEmptyINIFilePath = Path.Combine(AppContext.BaseDirectory, "INIFileEMPTY.ini");
        public readonly string strBadSectionINIFilePath = Path.Combine(AppContext.BaseDirectory, "INIFileBADSection.ini");
        public readonly string strBadKeysINIFilePath = Path.Combine(AppContext.BaseDirectory, "INIfileBADKeys.ini");

        public FileIniDataParser iniParser = new FileIniDataParser();

        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test, Description("Checks existence of test INI files")]
        public void CheckTestFilesExists()
        {
            Assert.That(File.Exists(strGoodINIFilePath), "Correct INI file {0} does not exists", strGoodINIFilePath);
            Assert.That(File.Exists(strBadINIFilePath), "Invalid INI file {0} does not exists", strBadINIFilePath);
            Assert.That(File.Exists(strEmptyINIFilePath), "Empty INI file {0} does not exists", strEmptyINIFilePath);
        }

        [Test, Description("Checks correct parsing of an empty INI file")]
        public void CheckParseEmptyFileSuccess()
        {
            IniData parsedData = iniParser.ReadFile(strEmptyINIFilePath);

            Assert.That(parsedData, Is.Not.Null);
        }

        [Test, Description("Checks correct parsing of a well formed INI file")]
        public void CheckParseGoodFileSuccess()
        {
            IniData parsedData = iniParser.ReadFile(strGoodINIFilePath);

            Assert.That(parsedData, Is.Not.Null);
        }

        [Test, Description("Checks error when parsing a bad formed INI file")]
        public void CheckParsingFailure()
        {
            _ = Assert.Throws<ParsingException>(() => iniParser.ReadFile(strBadINIFilePath));
        }

        [Test, Description("Checks correct saving of a file")]
        public void CheckCorrectSave()
        {
            string fileString = strGoodINIFilePath + "_test.ini";

            IniData parsedData = iniParser.ReadFile(strGoodINIFilePath);
            iniParser.WriteFile(fileString, parsedData);

            Assert.That(File.Exists(fileString));
        }

        [Test, Description("Checks bad formed INI file: Two sections with same name")]
        public void CheckCollideSectionNames()
        {
            _ = Assert.Throws<ParsingException>(() => iniParser.ReadFile(strBadSectionINIFilePath));
        }

        [Test, Description("Checks bad formed INI file: Two keys in the same section with same name")]
        public void CheckCollideKeysNames()
        {
            _ = Assert.Throws<ParsingException>(() => iniParser.ReadFile(strBadKeysINIFilePath));
        }
    }
}
