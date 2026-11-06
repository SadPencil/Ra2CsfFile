using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SadPencil.Ra2CsfFile.Ini.Test
{
    [TestClass]
    public class Ra2CsfFileIniTest
    {
        private void TestCsfFile(String csfFilename, bool orderByKey = false)
        {
            CsfFile inputCsfFile;

            // Load csf file
            using (var fs = File.Open(csfFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                inputCsfFile = CsfFile.LoadFromCsfFile(fs);
            }

            this.TestCsfFile(inputCsfFile, orderByKey);
        }

        private void TestCsfFile(CsfFile inputCsfFile, bool orderByKey = false)
        {

            // Convert to ini file
            MemoryStream iniMemoryStream;
            {
                MemoryStream _iniMemoryStream = new();
                CsfFileIniHelper.WriteIniFile(inputCsfFile, _iniMemoryStream);
                Byte[] iniBytes = _iniMemoryStream.ToArray();
                iniMemoryStream = new MemoryStream(iniBytes);
            }

            // Read ini file
            var iniCsfFile = CsfFileIniHelper.LoadFromIniFile(iniMemoryStream, new CsfFileOptions() { OrderByKey = orderByKey });

            foreach (String label in inputCsfFile.Labels.Keys)
            {
                Assert.IsTrue(iniCsfFile.Labels.ContainsKey(label));
                Assert.AreEqual(inputCsfFile.Labels[label], iniCsfFile.Labels[label]);
            }

            // Compare them
            Assert.AreEqual(inputCsfFile, iniCsfFile);
        }

        [TestMethod]
        public void TestStringTables()
        {
            String[] files = Directory.GetFiles("Resources/Stringtables", "*.csf", SearchOption.AllDirectories);
            foreach (String file in files)
            {
                this.TestCsfFile(file, orderByKey: false);
                this.TestCsfFile(file, orderByKey: true);
            }
        }

        [TestMethod]
        public void TestLoadFromIniFilePreservesValueWhitespace()
        {
            const String iniContent = "[SadPencil.Ra2CsfFile.Ini]\nIniVersion=2\nCsfVersion=3\nCsfLang=0\n\n[TEST]\nValue=    a\nValueLine2=  b  \n\n[PLAIN]\nValue=no-space\n";

            CsfFile csfFile;
            using (MemoryStream ms = new(Encoding.UTF8.GetBytes(iniContent)))
            {
                csfFile = CsfFileIniHelper.LoadFromIniFile(ms);
            }
            Assert.AreEqual("    a\n  b  ", csfFile.Labels["TEST"]);
            Assert.AreEqual("no-space", csfFile.Labels["PLAIN"]);

            // The preserved whitespace characters survive the .csf round trip.
            Byte[] csfBytes;
            using (MemoryStream csfMs = new())
            {
                csfFile.WriteCsfFile(csfMs);
                csfBytes = csfMs.ToArray();
            }
            using (MemoryStream roundTripMs = new(csfBytes))
            {
                CsfFile roundTripCsfFile = CsfFile.LoadFromCsfFile(roundTripMs);
                Assert.AreEqual("    a\n  b  ", roundTripCsfFile.Labels["TEST"]);
            }

            // Saving to INI and loading it again also keeps every original value.
            this.TestCsfFile(csfFile);
        }

        [TestMethod]
        public void TestIniValuesWithoutSurroundingWhitespace()
        {
            const String iniContent = "[SadPencil.Ra2CsfFile.Ini]\r\nIniVersion=2\r\nCsfVersion=3\r\nCsfLang=0\r\n\r\n[TEST]\r\nValue=hello  world\r\nValueLine2=a=b;literal\r\n\r\n[EMPTY]\r\nValue=\r\n";

            CsfFile csfFile;
            using (MemoryStream ms = new(Encoding.UTF8.GetBytes(iniContent)))
            {
                csfFile = CsfFileIniHelper.LoadFromIniFile(ms, new CsfFileOptions());
            }

            Assert.AreEqual("hello  world\na=b;literal", csfFile.Labels["TEST"]);
            Assert.AreEqual(String.Empty, csfFile.Labels["EMPTY"]);
            this.TestCsfFile(csfFile);
        }

        [TestMethod]
        public void TestCaseInsensitive()
        {
            String[] iniFiles = Directory.GetFiles("Resources/CaseInsensitiveTest", "*.ini", SearchOption.AllDirectories);
            foreach (String iniFilename in iniFiles)
            {
                CsfFile csfFile;
                using (Stream stream = File.Open(iniFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    csfFile = CsfFileIniHelper.LoadFromIniFile(stream);
                }

                this.TestCsfFile(csfFile);
            }
        }



        [TestMethod]
        public void TestIniFileHasNoWhiteSpaceWarning()
        {
            CsfFile csfFile = new CsfFile();
            csfFile.AddLabel("TRAILING", "abc   ");

            Byte[] iniBytes;
            using (MemoryStream ms = new())
            {
                CsfFileIniHelper.WriteIniFile(csfFile, ms);
                iniBytes = ms.ToArray();
            }
            String iniText = Encoding.UTF8.GetString(iniBytes);

            // The whitespace of a label value is preserved, so the generated ini file no longer carries a warning about it.
            Assert.IsFalse(iniText.IndexOf("WhiteSpaceWarning", StringComparison.Ordinal) >= 0, "The generated ini file must not contain a whitespace warning.");
            Assert.IsTrue(iniText.IndexOf("Value=abc   ", StringComparison.Ordinal) >= 0, "The trailing whitespace of the label value must be preserved.");
        }


        [TestMethod]
        public void TestIniLineBreakStyleDoesNotAffectTheLoadedValues()
        {
            // The writer uses Environment.NewLine, so the same string table produces CRLF on Windows and LF
            // elsewhere. Reading the file must not care: both are the line separator of the ini file, and
            // neither may end up inside a value.
            const String content =
                "[SadPencil.Ra2CsfFile.Ini]\nIniVersion=2\nCsfVersion=3\nCsfLang=0\n\n" +
                "[MULTI]\nValue=first\nValueLine2=   second   \n\n" +
                "[EMPTYLINE]\nValue=a\nValueLine2=\nValueLine3=b\n";
            String crlfContent = String.Join("\r\n", content.Split('\n'));

            CsfFile fromLf;
            using (MemoryStream ms = new(Encoding.UTF8.GetBytes(content)))
            {
                fromLf = CsfFileIniHelper.LoadFromIniFile(ms);
            }

            CsfFile fromCrlf;
            using (MemoryStream ms = new(Encoding.UTF8.GetBytes(crlfContent)))
            {
                fromCrlf = CsfFileIniHelper.LoadFromIniFile(ms);
            }

            Assert.AreEqual("first\n   second   ", fromLf.Labels["MULTI"]);
            Assert.AreEqual("a\n\nb", fromLf.Labels["EMPTYLINE"]);

            Assert.AreEqual(fromLf.Labels.Count, fromCrlf.Labels.Count);
            foreach (KeyValuePair<String, String> label in fromLf.Labels)
            {
                Assert.AreEqual(label.Value, fromCrlf.Labels[label.Key],
                    $"The label {label.Key} depends on the line break style of the ini file.");
            }
        }
    }
}
