using IniParser.Model.Configuration;
using IniParser.Parser;
using NUnit.Framework;

namespace IniFileParser.Tests.Unit.Parser
{
    [TestFixture]
    public class ValueWhitespaceTests
    {
        [Test]
        public void trim_values_by_default()
        {
            var configuration = new IniParserConfiguration();
            Assert.That(configuration.TrimProperties, Is.True);

            var data = new IniDataParser(configuration).Parse(" [ TEST ] \r\n Value \t=  a \t\r\n");
            Assert.That(data["TEST"]["Value"], Is.EqualTo("a"));
        }

        [TestCase("Value=    a\n", "    a")]
        [TestCase("Value=a    \r\n", "a    ")]
        [TestCase("\t Value \t= \t a \t \r\n", " \t a \t ")]
        [TestCase("Value=   \n", "   ")]
        [TestCase("Value=\n", "")]
        [TestCase("Value= a=b \n", " a=b ")]
        [TestCase("Value=\u00a0a\u3000\n", "\u00a0a\u3000")]
        [TestCase("Value= ;comment-like text \n", " ;comment-like text ")]
        public void preserve_value_whitespace_while_trimming_names(string line, string expected)
        {
            var configuration = new IniParserConfiguration { TrimProperties = false };
            var data = new IniDataParser(configuration).Parse(" \t[ TEST ] \t\r\n" + line);

            Assert.That(data["TEST"]["Value"], Is.EqualTo(expected));
        }

        [Test]
        public void copy_and_clone_preserve_the_whitespace_option()
        {
            var configuration = new IniParserConfiguration { TrimProperties = false };
            var copy = new IniParserConfiguration(configuration);
            var clone = configuration.Clone();

            Assert.That(copy.TrimProperties, Is.False);
            Assert.That(clone.TrimProperties, Is.False);
            // The upstream copy constructor omits unrelated settings such as the assignment
            // character. Check the new switch here; parsing uses the complete Clone() path.
            Assert.That(new IniDataParser(clone).Parse("Value=  a  \n").Global["Value"], Is.EqualTo("  a  "));
        }
    }
}
