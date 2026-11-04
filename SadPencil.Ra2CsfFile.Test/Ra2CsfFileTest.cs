using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SadPencil.Ra2CsfFile.Test
{
    [TestClass]
    public class Ra2CsfFileTest
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

            // KNOWN ISSUES
            // Some CSF labels have space characters as prefix or suffix. The ini format can't support representing such a trimmable line right now.
            var keysCopy = inputCsfFile.Labels.Keys.ToList();
            foreach (String label in keysCopy)
            {
                String value = inputCsfFile.Labels[label];
                String trimmedValue = CsfFileIniHelper.TrimMultiLine(value);
                if (!value.Equals(trimmedValue, StringComparison.InvariantCulture))
                {
                    Console.WriteLine($"Trim CSF label {label}");
                    Boolean existed = inputCsfFile.AddLabel(label, trimmedValue);
                    Assert.IsTrue(existed);
                }

                Assert.IsTrue(iniCsfFile.Labels.ContainsKey(label));
                Assert.AreEqual(iniCsfFile.Labels[label], iniCsfFile.Labels[label]);
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
        public void TestPreserveIniValueWhitespace()
        {
            const String iniContent = "[SadPencil.Ra2CsfFile.Ini]\nIniVersion=2\nCsfVersion=3\nCsfLang=0\n\n[TEST]\nValue=    a\nValueLine2=  b  \n\n[PLAIN]\nValue=no-space\n";

            // Without the option, the leading and the trailing whitespace characters of the values are trimmed.
            CsfFile defaultCsfFile;
            using (MemoryStream ms = new(Encoding.UTF8.GetBytes(iniContent)))
            {
                defaultCsfFile = CsfFileIniHelper.LoadFromIniFile(ms);
            }
            Assert.AreEqual("a\nb", defaultCsfFile.Labels["TEST"]);

            // With the option, the leading and the trailing whitespace characters of the values are preserved.
            CsfFile csfFile;
            using (MemoryStream ms = new(Encoding.UTF8.GetBytes(iniContent)))
            {
                csfFile = CsfFileIniHelper.LoadFromIniFile(ms, new CsfFileOptions() { PreserveIniValueWhitespace = true });
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

            // The two options are not equal.
            Assert.AreNotEqual(new CsfFileOptions(), new CsfFileOptions() { PreserveIniValueWhitespace = true });
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
    }
}
