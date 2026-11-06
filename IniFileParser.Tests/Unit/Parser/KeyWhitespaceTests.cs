using IniParser.Exceptions;
using IniParser.Parser;
using NUnit.Framework;

namespace IniFileParser.Tests.Unit.Parser
{
    [TestFixture]
    public class KeyWhitespaceTests
    {
        [TestCase("key = value")]
        [TestCase("key\t= value")]
        [TestCase("key   =value")]
        public void key_that_ends_with_whitespace_is_reported(string line)
        {
            ParsingException exception = Assert.Throws<ParsingException>(
                () => new IniDataParser().Parse("[section]\n" + line + "\n"));

            Assert.That(exception.Message, Does.Contain("ends with whitespace"));
        }

        [Test]
        public void the_value_may_start_with_a_space()
        {
            var data = new IniDataParser().Parse("[section]\nkey= value\n");

            Assert.That(data["section"]["key"], Is.EqualTo(" value"));
        }

        [Test]
        public void the_key_may_contain_a_space()
        {
            var data = new IniDataParser().Parse("[section]\nglobal key=value\n");

            Assert.That(data["section"]["global key"], Is.EqualTo("value"));
        }
    }
}
