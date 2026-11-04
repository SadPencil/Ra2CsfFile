# Upstream source and branch comparison

The parser implementation and its tests are imported from
[rickyah/ini-parser](https://github.com/rickyah/ini-parser), licensed under MIT.
The source revision is the 2.5.2 `master` tip,
[`c4dbf0eed984b0d57841bd6288040a2d5250e66e`](https://github.com/rickyah/ini-parser/commit/c4dbf0eed984b0d57841bd6288040a2d5250e66e).
The directory names `IniFileParser` and `IniFileParser.Tests` match that revision.

## Import and build adaptations

- The 17 parser implementation source files, 18 upstream test source files,
  11 test data files, and license are unchanged apart from checkout line endings.
- The library project is adapted from the original .NET Framework 2.0 project to
  an SDK-style `netstandard2.0` project. The SDK generates assembly metadata.
  The parser remains a separate assembly named `INIFileParser` and is referenced
  through `ProjectReference`.
- The test project is adapted to SDK-style `net48`, matching the existing CSF
  tests. It retains NUnit 2.6.3 and uses NUnitTestAdapter 2.3.0 with
  Microsoft.NET.Test.Sdk 17.14.1, so the upstream tests need no API migration.
- Several tests contain verbatim expected strings that depend on Windows line
  endings. The test directory pins C# checkouts to CRLF. Fixtures are copied to
  the output directory and its legacy `../../` location, keeping writes under
  `bin` instead of modifying source fixtures.
- Both projects are registered in the solution. Warning and style suppression
  is scoped to the imported projects. The parser DLL and license are included
  in the CSF NuGet package; the parser and its tests are not published separately.

Run both suites on Windows with the SDK selected by `global.json`:

```powershell
dotnet test SadPencil.Ra2CsfFile.slnx --configuration Release
```

The imported parser suite contains 91 tests. The existing CSF suite contains
2 tests before the whitespace feature is added.

## Master and development comparison

The branches were compared on 2026-11-04. At that time `development` was
[`d300241a75bec4dcabc0751c8690f3b12b3eaf43`](https://github.com/rickyah/ini-parser/commit/d300241a75bec4dcabc0751c8690f3b12b3eaf43),
70 commits ahead of `master`. Its project declares version 3.0. It changes the
assembly name to `IniParser`, reorganizes namespaces and data structures, removes
older APIs, and replaces the regex parser with a `StringBuffer`/`IniScheme`
implementation. It is not a compatible 2.5.2 patch release.

The changes relevant to this import are:

| Change | Evidence and relevance |
| --- | --- |
| Configurable whitespace trimming | `development` adds `TrimProperties`, `TrimSections`, and `TrimComments`. Its `TrimProperties=false` preserves whitespace in both keys and values. An isolated net48 probe confirmed this; CSF loading needs values preserved while key lookup remains trimmed. |
| StringBuffer prefix matching | [`d483de6`](https://github.com/rickyah/ini-parser/commit/d483de6dedc0b2848f69e73a81c4810ef9937b21) corrects indexing when a buffer has a nonzero start. This concerns the new buffer implementation, which does not exist in 2.5.2. |
| Multi-character section delimiters | [`82b630a`](https://github.com/rickyah/ini-parser/commit/82b630a76d5fbc17fc40175517aabd7826078563) corrects the closing delimiter length in the new scheme parser. A probe confirmed `[[section]]` works. CSF INI files use ordinary brackets. |
| Configuration and comments | [`c7a1522`](https://github.com/rickyah/ini-parser/commit/c7a1522958842121ba77cbc3f5682a524a4b4ed6) adds configuration tests, handles indentation before comments in the new parser, and implements its duplicate-value concatenation branch. CSF loading disables comment parsing, and 2.5.2 has its own duplicate-key implementation. |
| Formatter output | [`edf2827`](https://github.com/rickyah/ini-parser/commit/edf2827e791b9df3b8162d755ab44aeb21fdf5a7) removes the final newline from the new formatter. Its unconditional removal also throws `ArgumentOutOfRangeException` for empty data in the net48 probe. It is not a drop-in fix for the 2.5.2 formatter. |
| Build and tests | `development` introduces SDK-style projects, a netstandard2.0 target, NUnit 3, and fixture-path changes. These are useful migration references, but its tests use the new API. The 2.5.2 tests are retained with matching source and project names. |

This import therefore keeps the compatible 2.5.2 implementation and its matching
tests. The whitespace behavior is a separate follow-up change. No development
runtime changes are included in the import. The comparison covers commit diffs,
the relevant source, and focused net48 probes; it does not certify every change
in the development branch or claim that 2.5.2 has no other defects.
