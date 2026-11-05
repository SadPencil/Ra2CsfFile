# INIFileParser

This directory contains the sources of [ini-parser](https://github.com/rickyah/ini-parser) 2.5.2,
copyright (c) 2008 Ricardo Amores Hernández, licensed under the MIT license in `LICENSE`.

The project is not published as a package of its own. `INIFileParser.dll` is shipped inside the
`SadPencil.Ra2CsfFile` package, and `IniFileParser.Tests` is its imported test suite.

The sources are not identical to upstream: they are patched so that a label value of a string table
survives the conversion to and from an ini file. Every deliberate difference is listed below. Keep
this list up to date when the sources are updated from upstream.

## 1. Values keep their leading and trailing whitespace

`Parser/IniDataParser.cs`

- `ProcessLine` trims the start of a line only (`TrimStart` instead of `Trim`), so the trailing
  whitespace of a line is not dropped.
- `ExtractValue` returns everything after the first assignment character without trimming it.

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

`IniFileParser.Tests` is the upstream test suite, adapted to the two changes above: the fixtures use
the format this library writes, and the expectations of the tests that compare a written ini file
with its source do not insert the assignment space anymore. `Unit/Parser/ValueWhitespaceTests.cs`
covers both changes.
