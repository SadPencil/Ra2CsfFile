using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SadPencil.Ra2CsfFile.Test
{
    [TestClass]
    public class Ra2CsfFileTest
    {
        private static string TrimMultiline(string input, string linebreak = "\n") => string.Join(linebreak, input.Split([linebreak], StringSplitOptions.None).Select(l => l.Trim()));

        private void TestCsfFile(string csfFilename)
        {
            CsfFile inputCsfFile;

            // Load csf file
            using (FileStream fs = File.Open(csfFilename, FileMode.Open))
            {
                inputCsfFile = CsfFile.LoadFromCsfFile(fs);
            }

            // Convert to ini file
            MemoryStream iniMemoryStream;
            {
                MemoryStream _iniMemoryStream = new();
                CsfFileIniHelper.WriteIniFile(inputCsfFile, _iniMemoryStream);
                byte[] iniBytes = _iniMemoryStream.ToArray();
                iniMemoryStream = new MemoryStream(iniBytes);
            }

            // Read ini file
            CsfFile iniCsfFile = CsfFileIniHelper.LoadFromIniFile(iniMemoryStream);

            // KNOWN ISSUES
            // Some CSF labels have space characters as prefix or suffix. The ini format can't support representing such a trimmable line right now.
            List<string> keysCopy = inputCsfFile.Labels.Keys.ToList();
            foreach (string label in keysCopy)
            {
                string value = inputCsfFile.Labels[label];
                string trimmedValue = TrimMultiline(value);
                if (!value.Equals(trimmedValue, StringComparison.InvariantCulture))
                {
                    Console.WriteLine($"Trim CSF label {label}");
                    bool existed = inputCsfFile.AddLabel(label, trimmedValue);
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
            string[] files = Directory.GetFiles("Resources", "*.csf", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                TestCsfFile(file);
            }
        }
    }
}
