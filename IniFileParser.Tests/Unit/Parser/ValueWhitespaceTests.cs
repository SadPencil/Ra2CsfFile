using System;
using IniParser.Parser;
using NUnit.Framework;

namespace IniFileParser.Tests.Unit.Parser
{
    [TestFixture]
    public class ValueWhitespaceTests
    {
        [Test]
        public void preserve_values_by_default()
        {
            var data = new IniDataParser().Parse(" [ TEST ] \r\n Value \t=  a \t\r\n");
            Assert.That(data["TEST"]["Value"], Is.EqualTo("  a \t"));
        }

        [TestCase("Value=    a\n", "    a")]
        [TestCase("Value=a    \r\n", "a    ")]
        [TestCase("\t Value \t= \t a \t \r\n", " \t a \t ")]
        [TestCase("Value=   \n", "   ")]
        [TestCase("Value=\n", "")]
        [TestCase("Value= a=b \n", " a=b ")]
        [TestCase("Value=\u00a0a\u3000\n", "\u00a0a\u3000")]
        [TestCase("Value= ;comment-like text \n", " ;comment-like text ")]
        [TestCase("Value=no-space\n", "no-space")]
        public void preserve_value_whitespace_while_trimming_names(string line, string expected)
        {
            var data = new IniDataParser().Parse(" \t[ TEST ] \t\r\n" + line);

            Assert.That(data["TEST"]["Value"], Is.EqualTo(expected));
        }

        [Test]
        public void write_values_without_spaces_around_the_assignment_character()
        {
            var data = new IniDataParser().Parse("[SECTION]\r\nkey = value\r\n");

            Assert.That(data.ToString(), Is.EqualTo("[SECTION]" + Environment.NewLine + "key= value" + Environment.NewLine));
        }

        [Test]
        public void rewrite_preserves_values_across_repeated_cycles()
        {
            string text = "[SECTION]\nkey =  value \n";

            for (int i = 0; i < 3; i++)
            {
                text = new IniDataParser().Parse(text).ToString();
            }

            Assert.That(text, Is.EqualTo("[SECTION]\nkey=  value \n"));
            Assert.That(new IniDataParser().Parse(text)["SECTION"]["key"], Is.EqualTo("  value "));
        }
    }
}
