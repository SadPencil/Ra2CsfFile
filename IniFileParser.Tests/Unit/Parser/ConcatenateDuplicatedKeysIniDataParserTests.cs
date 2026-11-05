using IniParser.Parser;
using NUnit.Framework;

namespace IniFileParser.Tests.Unit.Parser
{
    [TestFixture]
    public class ConcatenateDuplicatedKeysIniDataParserTests
    {
        private string iniFileStr =
            @"[MySection]
File=a.txt
[MySection]
File=b.txt
[MySection]
File=c.txt
[MySection]
File=d.txt
";

        [Test]
        public void test_duplicated_keys_are_concatenated_in_same_section()
        {
            var parser = new ConcatenateDuplicatedKeysIniDataParser();
            parser.Configuration.AllowDuplicateSections = true;

            var iniData = parser.Parse(iniFileStr);
            Assert.That(iniData["MySection"]["File"], Is.EqualTo("a.txt;b.txt;c.txt;d.txt"));


        }

        [Test]
        public void test_duplicated_keys_are_concatenated_with_different_string()
        {
            var parser = new ConcatenateDuplicatedKeysIniDataParser();
            parser.Configuration.ConcatenateSeparator = "+";
            parser.Configuration.AllowDuplicateSections = true;

            var inidata = parser.Parse(iniFileStr);
            Assert.That(inidata["MySection"]["File"], Is.EqualTo("a.txt+b.txt+c.txt+d.txt"));
        }

        [Test]
        public void duplicated_keys_keep_the_whitespace_of_each_value()
        {
            var parser = new ConcatenateDuplicatedKeysIniDataParser();
            parser.Configuration.AllowDuplicateSections = true;

            var iniData = parser.Parse("[MySection]\nFile = a.txt\n[MySection]\nFile = b.txt\n");

            Assert.That(iniData["MySection"]["File"], Is.EqualTo(" a.txt; b.txt"));
        }

    }
}
