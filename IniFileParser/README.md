# INIFileParser

This directory contains the sources of [ini-parser](https://github.com/rickyah/ini-parser) 2.5.2,
copyright (c) 2008 Ricardo Amores Hernández, licensed under the MIT license in `LICENSE`.

The patched sources build as `SadPencil.Ra2CsfFile.IniParser.dll`, in the `IniParser.*` namespaces.
That assembly ships inside the `SadPencil.Ra2CsfFile` package, which is its only package;
`IniFileParser.Tests` is its imported test suite.

The sources are not identical to upstream: they are patched so that a label value of a string table
survives the conversion to and from an ini file. Every deliberate difference is listed below. Keep
this list up to date when the sources are updated from upstream.

## 1. Values keep their leading and trailing whitespace

`Parser/IniDataParser.cs`

- `ProcessLine` trims the start of a line only (`TrimStart` instead of `Trim`), so the trailing
  whitespace of a line is not dropped.
- `ExtractValue` returns everything after the first assignment character without trimming it.
- `ExtractComment` keeps the whitespace around a comment instead of trimming it: the whitespace in
  front of the comment stays part of the value, the whitespace that belongs to the comment goes away
  with it, and the comment text that is stored keeps the whitespace of the line as well. Only the
  comment string is dropped, and it is looked up inside the match, so an expression such as
  `\s*;.*` and a comment string longer than one character both work. A line that holds nothing but
  the comment still counts as empty. The default configuration only accepts a comment on a line of
  its own, so the value part of this matters only with an inline comment expression.

A string table label value may end with a space that the game displays, and an empty label can be a
single space, so trimming it would change the string table itself. The consequence for an ini file
that is written by hand in the common `key = value` style: the space after the assignment character
belongs to the value, so it is read as `" value"`. Key and section names are still trimmed.

## 2. An ini file is written without whitespace around the assignment character

`Parser/IniDataParser.cs`, `Model/Formatting/DefaultIniDataFormatter.cs`,
`Model/Configuration/IniParserConfiguration.cs`

`IniParserConfiguration.AssigmentSpacer` does not exist anymore, and the formatter always emits
`key=value`. Because a value is written exactly as it is stored, inserting a space after the
assignment character on every write would prepend one more space to the value on every
parse/format cycle, and the value would grow without bound.

## 3. Tests

`IniFileParser.Tests` is the upstream test suite, with the following changes:

- The fixtures use `key=value`. A value that starts with a space is only interesting to the
  whitespace tests, so the remaining tests should not carry one just because their source line was
  written with spaces around the assignment character.
- The tests that compare a written ini file with its source compare text with normalized line
  breaks, so they do not depend on the line break style of the platform they run on.
- The project targets `net48` and `net8.0` and uses NUnit 3, and therefore the tests that relied on
  `[ExpectedException]` use `Assert.Throws`.
- `Unit/Parser/ValueWhitespaceTests.cs` is new and covers the whitespace behaviour of the parser above.
