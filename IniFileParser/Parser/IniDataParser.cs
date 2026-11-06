using System;
using System.Collections.Generic;
using IniParser.Exceptions;
using IniParser.Model;
using IniParser.Model.Configuration;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace IniParser.Parser
{
	/// <summary>
	/// 	Responsible for parsing an string from an ini file, and creating
	/// 	an <see cref="IniData"/> structure.
	/// </summary>
    public class IniDataParser
    {
        #region Private
        // Holds a list of the exceptions catched while parsing
        private List<Exception> _errorExceptions;
        #endregion

        #region Initialization
        /// <summary>
        ///     Ctor
        /// </summary>
        /// <remarks>
        ///     The parser uses a <see cref="IniParserConfiguration"/> by default
        /// </remarks>
        public IniDataParser()
            : this(new IniParserConfiguration())
        { }

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="parserConfiguration">
        ///     Parser's <see cref="IniParserConfiguration"/> instance.
        /// </param>
        public IniDataParser(IniParserConfiguration parserConfiguration)
        {
            if (parserConfiguration == null)
                throw new ArgumentNullException("parserConfiguration");

            Configuration = parserConfiguration;

            _errorExceptions = new List<Exception>();
        }

        #endregion

        #region State
        /// <summary>
        ///     Configuration that defines the behaviour and constraints
        ///     that the parser must follow.
        /// </summary>
        public virtual IniParserConfiguration Configuration { get; protected set; }

        /// <summary>
        /// True is the parsing operation encounter any problem
        /// </summary>
        public bool HasError { get { return _errorExceptions.Count > 0; } }

        /// <summary>
        /// Returns the list of errors found while parsing the ini file.
        /// </summary>
        /// <remarks>
        /// If the configuration option ThrowExceptionOnError is false it can contain one element
        /// for each problem found while parsing; otherwise it will only contain the very same 
        /// exception that was raised.
        /// </remarks>

        public ReadOnlyCollection<Exception> Errors {get {return _errorExceptions.AsReadOnly();} }
		#endregion

		#region Operations

        /// <summary>
        ///     Parses a string containing valid ini data
        /// </summary>
        /// <param name="iniDataString">
        ///     String with data
        /// </param>
        /// <returns>
        ///     An <see cref="IniData"/> instance with the data contained in
        ///     the <paramref name="iniDataString"/> correctly parsed an structured.
        /// </returns>
        /// <exception cref="ParsingException">
        ///     Thrown if the data could not be parsed
        /// </exception>
        public IniData Parse(string iniDataString)
        {
            
            IniData iniData = Configuration.CaseInsensitive ? new IniDataCaseInsensitive() : new IniData();
            iniData.Configuration = this.Configuration.Clone();

            if (string.IsNullOrEmpty(iniDataString))
            {
                return iniData;
            }

            _errorExceptions.Clear();
            _currentCommentListTemp.Clear();
            _currentSectionNameTemp = null;

            try
            {
                var lines = iniDataString.Split(new []{"\n", "\r\n"}, StringSplitOptions.None);
                for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    var line = lines[lineNumber];

                    if (line.Trim() == String.Empty) continue;

                    try
                    {
                        ProcessLine(line, iniData);
                    }
                    catch (Exception ex)
                    {
                        var errorEx = new ParsingException(ex.Message, lineNumber+1, line, ex);
                        if (Configuration.ThrowExceptionsOnError) 
                        {
                            throw errorEx;
                        }
                        else
                        {
                            _errorExceptions.Add(errorEx);
                        }

                    }
                }

                // Orphan comments, assing to last section/key value
                if (_currentCommentListTemp.Count > 0)
                {
                    // Check if there are actually sections in the file
                    if (iniData.Sections.Count > 0)
                    {
                        iniData.Sections.GetSectionData(_currentSectionNameTemp).TrailingComments
                            .AddRange(_currentCommentListTemp);
                    }
                    // No sections, put the comment in the last key value pair
                    // but only if the ini file contains at least one key-value pair
                    else if (iniData.Global.Count > 0) 
                    {
                        iniData.Global.GetLast().Comments
                            .AddRange(_currentCommentListTemp);
                    }
                    
                    
                    _currentCommentListTemp.Clear();
                }

            }
            catch(Exception ex)
            {
                _errorExceptions.Add(ex);
                if (Configuration.ThrowExceptionsOnError) 
                { 
                    throw;
                }
            }


            if (HasError) return null;
            return (IniData)iniData.Clone();
        }
        #endregion

        #region Template Method Design Pattern 
        // All this methods controls the parsing behaviour, so it can be modified 
        // in derived classes.
        // See http://www.dofactory.com/Patterns/PatternTemplate.aspx for an
        // explanation of this pattern.
        // Probably for the most common cases you can change the parsing behavior
        //  using a custom configuration object rather than creating derived classes.
        // See IniParserConfiguration interface, and IniDataParser constructor
		//  to change the default configuration.

        /// <summary>
        ///     Checks if a given string contains a comment.
        /// </summary>
        /// <param name="line">
        ///     String with a line to be checked.
        /// </param>
        /// <returns>
        ///     <c>true</c> if any substring from s is a comment, <c>false</c> otherwise.
        /// </returns>
        protected virtual bool LineContainsAComment(string line)
        {
            return !string.IsNullOrEmpty(line) 
                && Configuration.CommentRegex.Match(line).Success;
        }

        /// <summary>
        ///     Checks if a given string represents a section delimiter.
        /// </summary>
        /// <param name="line">
        ///     The string to be checked.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the string represents a section, <c>false</c> otherwise.
        /// </returns>
        protected virtual bool LineMatchesASection(string line)
        {
            return !string.IsNullOrEmpty(line) 
                && Configuration.SectionRegex.Match(line).Success;
        }

        /// <summary>
        ///     Checks if a given string represents a key / value pair.
        /// </summary>
        /// <param name="line">
        ///     The string to be checked.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the string represents a key / value pair, <c>false</c> otherwise.
        /// </returns>
        protected virtual bool LineMatchesAKeyValuePair(string line)
        {
            return !string.IsNullOrEmpty(line) && line.Contains(Configuration.KeyValueAssigmentChar.ToString());
        }

        /// <summary>
        ///     Removes a comment from a string if exist, and returns the string without
        ///     the comment substring.
        /// </summary>
        /// <param name="line">
        ///     The string we want to remove the comments from.
        /// </param>
        /// <returns>
        ///     The string s without comments.
        /// </returns>
        protected virtual string ExtractComment(string line)
        {
            var match = Configuration.CommentRegex.Match(line);

            // Drop the comment string and keep everything after it. Look the comment string up inside
            // the match instead of dropping the first character: an expression such as "\s*;.*" also
            // matches the whitespace in front of it, and a comment string can be longer than one
            // character ("//" for instance).
            string matchedComment = match.Value;

            // The configuration keeps the comment string with the characters that are special to a
            // regular expression escaped, so unescape it before looking it up in the line: a marker
            // such as "\" is stored as "\\" and would otherwise be found in a UNC path like
            // "\\server\share" instead of at the start of the comment.
            string commentString = Regex.Unescape(Configuration.CommentString);
            Int32 commentIndex = commentString.Length == 0 ? -1 : matchedComment.IndexOf(commentString, StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                _currentCommentListTemp.Add(matchedComment.Substring(commentIndex + commentString.Length));
            }
            else
            {
                // The expression does not contain the comment string: keep the text without its first character.
                string trimmedStart = matchedComment.TrimStart();
                if (trimmedStart.Length > 0)
                {
                    _currentCommentListTemp.Add(trimmedStart.Substring(1));
                }
            }

            // Cut out the match itself, and not the comment text, so that the whitespace that belongs to
            // the comment ("; comment   " ends with three spaces) does not stay in the value, while the
            // whitespace in front of the comment does.
            string content = line.Remove(match.Index, match.Length);
            return String.IsNullOrWhiteSpace(content) ? String.Empty : content;
        }

        /// <summary>
        ///     Processes one line and parses the data found in that line
        ///     (section or key/value pair who may or may not have comments)
        /// </summary>
        /// <param name="currentLine">The string with the line to process</param>
        protected virtual void ProcessLine(string currentLine, IniData currentIniData)
        {
            // Trailing whitespace belongs to the value; indentation before the key does not.
            currentLine = currentLine.TrimStart();

            // Extract comments from current line and store them in a tmp field
            if (LineContainsAComment(currentLine))
            {
                currentLine = ExtractComment(currentLine);
            }

            // By default comments must span a complete line (i.e. the comment character
            // must be located at the beginning of a line, so it seems that the following
            // check is not needed.
            // But, if the comment parsing behaviour is changed in a derived class e.g. to
            // to allow parsing comment characters in the middle of a line, the implementor
            // will be forced to rewrite this complete method.
            // That was actually the behaviour for parsing comments
            // in earlier versions of the library, so checking if the current line is empty
            // (meaning the complete line was a comment) is future-proof.

            // If the entire line was a comment now should be empty,
            // so no further processing is needed.
            if (currentLine == String.Empty)
                return;

            //Process sections
            if (LineMatchesASection(currentLine))
            {
                ProcessSection(currentLine, currentIniData);
                return;
            }

            //Process keys
            if (LineMatchesAKeyValuePair(currentLine))
            {
                ProcessKeyValuePair(currentLine, currentIniData);
                return;
            }

            if (Configuration.SkipInvalidLines)
                return;

            throw new ParsingException(
                "Unknown file format. Couldn't parse the line: '" + currentLine + "'.");
        }

        /// <summary>
        ///     Proccess a string which contains an ini section.
        /// </summary>
        /// <param name="line">
        ///     The string to be processed
        /// </param>
        protected virtual void ProcessSection(string line, IniData currentIniData)
        {
            // Get section name with delimiters from line...
            string sectionName = Configuration.SectionRegex.Match(line).Value.Trim();

            // ... and remove section's delimiters to get just the name
            sectionName = sectionName.Substring(1, sectionName.Length - 2).Trim();

            // Check that the section's name is not empty
            if (sectionName == string.Empty)
            {
                throw new ParsingException("Section name is empty");
            }

            // Temporally save section name.
            _currentSectionNameTemp = sectionName;

            //Checks if the section already exists
            if (currentIniData.Sections.ContainsSection(sectionName))
            {
                if (Configuration.AllowDuplicateSections)
                {
                    return;
                }

                throw new ParsingException(string.Format("Duplicate section with name '{0}' on line '{1}'", sectionName, line));
            }


            // If the section does not exists, add it to the ini data
            currentIniData.Sections.AddSection(sectionName);

            // Save comments read until now and assign them to this section
            currentIniData.Sections.GetSectionData(sectionName).LeadingComments = _currentCommentListTemp;
            _currentCommentListTemp.Clear();

        }

        /// <summary>
        ///     Processes a string containing an ini key/value pair.
        /// </summary>
        /// <param name="line">
        ///     The string to be processed
        /// </param>
        protected virtual void ProcessKeyValuePair(string line, IniData currentIniData)
        {
            // get key and value data
            string key = ExtractKey(line);

			if (string.IsNullOrEmpty(key) && Configuration.SkipInvalidLines) return;

            string value = ExtractValue(line);

            // Check if we haven't read any section yet
            if (string.IsNullOrEmpty(_currentSectionNameTemp))
            {
                if (!Configuration.AllowKeysWithoutSection)
                {
                    throw new ParsingException("key value pairs must be enclosed in a section");
                }

                AddKeyToKeyValueCollection(key, value, currentIniData.Global, "global");
            }
            else
            {
                var currentSection = currentIniData.Sections.GetSectionData(_currentSectionNameTemp);

                AddKeyToKeyValueCollection(key, value, currentSection.Keys, _currentSectionNameTemp);
            }
        }

        /// <summary>
        ///     Extracts the key portion of a string containing a key/value pair..
        /// </summary>
        /// <param name="s">    
        ///     The string to be processed, which contains a key/value pair
        /// </param>
        /// <returns>
        ///     The name of the extracted key, as it is written.
        /// </returns>
        /// <exception cref="ParsingException">
        ///     The key ends with whitespace, as in <c>key = value</c>.
        /// </exception>
        protected virtual string ExtractKey(string s)
        {
            int index = s.IndexOf(Configuration.KeyValueAssigmentChar, 0);
            string key = s.Substring(0, index);

            if (key.Length > 0 && Char.IsWhiteSpace(key[key.Length - 1]))
            {
                throw new ParsingException(String.Format(
                    "The name of the key \"{0}\" ends with whitespace. Write the pair as \"key=value\"; write \"key= value\" when the value itself starts with a space.",
                    key));
            }

            return key;
        }

        /// <summary>
        ///     Extracts the value portion of a string containing a key/value pair..
        /// </summary>
        /// <param name="s">
        ///     The string to be processed, which contains a key/value pair
        /// </param>
        /// <returns>
        ///     The name of the extracted value.
        /// </returns>
        protected virtual string ExtractValue(string s)
        {
            int index = s.IndexOf(Configuration.KeyValueAssigmentChar, 0);

            return s.Substring(index + 1, s.Length - index - 1);
        }

        /// <summary>
        ///     Abstract Method that decides what to do in case we are trying to add a duplicated key to a section
        /// </summary>
        protected virtual void HandleDuplicatedKeyInCollection(string key, string value, KeyDataCollection keyDataCollection, string sectionName)
        {
            if (!Configuration.AllowDuplicateKeys)
            {
                throw new ParsingException(string.Format("Duplicated key '{0}' found in section '{1}", key, sectionName));
            }
            else if(Configuration.OverrideDuplicateKeys)
            {
                keyDataCollection[key] = value;
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        ///     Adds a key to a concrete <see cref="KeyDataCollection"/> instance, checking
        ///     if duplicate keys are allowed in the configuration
        /// </summary>
        /// <param name="key">
        ///     Key name
        /// </param>
        /// <param name="value">
        ///     Key's value
        /// </param>
        /// <param name="keyDataCollection">
        ///     <see cref="KeyData"/> collection where the key should be inserted
        /// </param>
        /// <param name="sectionName">
        ///     Name of the section where the <see cref="KeyDataCollection"/> is contained. 
        ///     Used only for logging purposes.
        /// </param>
        private void AddKeyToKeyValueCollection(string key, string value, KeyDataCollection keyDataCollection, string sectionName)
        {
            // Check for duplicated keys
            if (keyDataCollection.ContainsKey(key))
            {
                // We already have a key with the same name defined in the current section
                HandleDuplicatedKeyInCollection(key, value, keyDataCollection, sectionName);
            }
            else
            {
                // Save the keys
                keyDataCollection.AddKey(key, value);
            }

            keyDataCollection.GetKeyData(key).Comments = _currentCommentListTemp;
            _currentCommentListTemp.Clear();
        }
        #endregion

        #region Fields

        /// <summary>
        ///     Temp list of comments
        /// </summary>
        private readonly List<string> _currentCommentListTemp = new List<string>();

        /// <summary>
        ///     Tmp var with the name of the seccion which is being process
        /// </summary>
        private string _currentSectionNameTemp;
        #endregion
    }
}
