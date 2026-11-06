using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace SadPencil.Ra2CsfFile.Test
{
    [TestClass]
    public class Ra2CsfFileTest
    {
        [TestMethod]
        public void TestNormalizeLineBreaks()
        {
            // A carriage return and a line feed is a single line break.
            Assert.AreEqual("a\nb", CsfFile.NormalizeLineBreaks("a\r\nb"));
            // A run of carriage returns before a line feed is still a single line break.
            Assert.AreEqual("a\nb", CsfFile.NormalizeLineBreaks("a\r\r\nb"));
            // A lone carriage return is a line break too.
            Assert.AreEqual("a\nb", CsfFile.NormalizeLineBreaks("a\rb"));
            // Consecutive carriage returns without a line feed are separate line breaks, so an empty line stays empty.
            Assert.AreEqual("a\n\nb", CsfFile.NormalizeLineBreaks("a\r\rb"));
            // The end of the value is handled as well, not only the inner line breaks.
            Assert.AreEqual("a\nb\n", CsfFile.NormalizeLineBreaks("a\r\nb\r\n"));
            // Two line breaks stay two line breaks.
            Assert.AreEqual("a\n\nb", CsfFile.NormalizeLineBreaks("a\r\n\r\nb"));
            // Values that already use the line break characters are returned as they are.
            Assert.AreEqual("a\nb", CsfFile.NormalizeLineBreaks("a\nb"));
            Assert.IsNull(CsfFile.NormalizeLineBreaks(null));
        }

        [TestMethod]
        public void TestLineBreakNormalizationOnAddLabel()
        {
            CsfFile csf = new CsfFile();
            csf.AddLabel("MULTILINE", "first\r\nsecond\r\r\nthird");

            Assert.AreEqual("first\nsecond\nthird", csf.Labels["MULTILINE"]);

            // Replacing an existing label normalizes the value as well.
            csf.AddLabel("MULTILINE", "first\r\nsecond");
            Assert.AreEqual("first\nsecond", csf.Labels["MULTILINE"]);
        }

        [TestMethod]
        public void TestLoadedLabelValuesUseLineBreakCharacters()
        {
            // This file stores some of its label values with CRLF, and one of them even with CR CR LF.
            String csfFilename = Path.Combine("Resources", "Stringtables", "RA3EP1", "ChineseT", "gamestrings.csf");
            CsfFile csfFile;
            using (Stream stream = File.Open(csfFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                csfFile = CsfFile.LoadFromCsfFile(stream);
            }

            foreach (KeyValuePair<String, String> label in csfFile.Labels)
            {
                Assert.IsFalse(label.Value.IndexOf("\r", StringComparison.Ordinal) >= 0, $"The label {label.Key} still contains a carriage return.");
            }

            // The line breaks of this label are normalized, and the number of lines is unchanged.
            Assert.AreEqual("在緊急狀況下，左鍵點擊按鈕！\n» 最多可取得50,000儲金。\n» 賦予所有可用部隊英雄階級。\n» 註：完成挑戰時間將不算數。",
                csfFile.Labels["DESC:RedAlertButton"]);
        }
    }
}
