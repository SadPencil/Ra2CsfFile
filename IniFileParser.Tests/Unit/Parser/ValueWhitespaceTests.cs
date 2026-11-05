using System;
using System.Text.RegularExpressions;
using IniParser.Model.Configuration;
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
        public void preserve_value_whitespace_before_an_inline_comment()
        {
            var configuration = new IniParserConfiguration
            {
                CommentRegex = new Regex(";.*"),
            };

            var data = new IniDataParser(configuration).Parse("[SECTION]\nkey = value   ; a comment\n");

            Assert.That(data["SECTION"]["key"], Is.EqualTo(" value   "));
            Assert.That(data["SECTION"].GetKeyData("key").Comments[0], Is.EqualTo(" a comment"));
        }

        [Test]
        public void the_stored_comment_text_is_trimmed()
        {
            // A comment is not data: it is stored without the whitespace around it, which is why the
            // comment character is the first character of what Substring(1) skips.
            var configuration = new IniParserConfiguration
            {
                CommentRegex = new Regex(@"\s*;.*"),
            };

            var data = new IniDataParser(configuration).Parse("[SECTION]\nkey=value ; a comment   \n");
            Assert.That(data["SECTION"].GetKeyData("key").Comments[0], Is.EqualTo(" a comment"));

            var fullLine = new IniDataParser().Parse("[SECTION]\n; a comment   \nkey=value\n");
            Assert.That(fullLine["SECTION"].GetKeyData("key").Comments[0], Is.EqualTo(" a comment"));

            // This expression starts at the whitespace in front of the comment, so that whitespace is
            // part of the match and goes away with the comment. An expression like ";.*" leaves it in
            // the value instead, which the_whitespace_of_the_comment_is_not_part_of_the_value pins down.
            Assert.That(data["SECTION"]["key"], Is.EqualTo("value"));
        }

        [Test]
        public void the_whitespace_of_the_comment_is_not_part_of_the_value()
        {
            var configuration = new IniParserConfiguration
            {
                CommentRegex = new Regex(";.*"),
            };

            var data = new IniDataParser(configuration).Parse("[SECTION]\nkey=value  ; a comment   \n");

            Assert.That(data["SECTION"]["key"], Is.EqualTo("value  "));
            Assert.That(data["SECTION"].GetKeyData("key").Comments[0], Is.EqualTo(" a comment"));
        }

        [Test]
        public void a_line_that_holds_nothing_but_a_comment_is_still_skipped()
        {
            var configuration = new IniParserConfiguration
            {
                CommentRegex = new Regex(";.*"),
            };

            var data = new IniDataParser(configuration).Parse("[SECTION]\nkey = value\n   ; a comment   \n");

            Assert.That(data["SECTION"].Count, Is.EqualTo(1));
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
            // The formatter writes with Environment.NewLine, so the expected text has to use it too.
            string newLine = Environment.NewLine;
            string text = "[SECTION]" + newLine + "key =  value " + newLine;

            for (int i = 0; i < 3; i++)
            {
                text = new IniDataParser().Parse(text).ToString();
            }

            Assert.That(text, Is.EqualTo("[SECTION]" + newLine + "key=  value " + newLine));
            Assert.That(new IniDataParser().Parse(text)["SECTION"]["key"], Is.EqualTo("  value "));
        }
    }
}
