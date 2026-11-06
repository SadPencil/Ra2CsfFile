# SadPencil.Ra2CsfFile

## .NET Library

This repository publishes two .NET Standard 2.0 libraries:

- **`SadPencil.Ra2CsfFile`** loads, edits, and saves string table files (.csf) for Red Alert 2.
- **`SadPencil.Ra2CsfFile.Ini`** (de)serializes the string table from/to .ini files.

`SadPencil.Ra2CsfFile.Ini` references `SadPencil.Ra2CsfFile`, and it is the only one of the two
libraries that contains ini code: the `CsfFileIniHelper` class and the bundled patched ini parser.
A project that only works with .csf files needs `SadPencil.Ra2CsfFile` alone.

## Example use

See `ExampleApp/Program.cs` file. The example app references both libraries.

## License

MIT

## Notes
Reference: https://modenc.renegadeprojects.com/CSF_File_Format
The vendored ini-parser sources in `IniFileParser/` are patched; see `IniFileParser/README.md` for the list of differences.

## Bundled ini parser

`SadPencil.Ra2CsfFile.Ini` ships a patched [ini-parser](https://github.com/rickyah/ini-parser) as
`SadPencil.Ra2CsfFile.IniParser.dll`, with the `IniParser.*` namespaces, so a label value keeps its
whitespace through the conversion.

Do not reference `ini-parser` or `ini-parser-netstandard` as well: they define the same types as the
bundled copy, and the upstream parser trims the whitespace of a label value. The build reports
SPCSFINI001 when a project that references `SadPencil.Ra2CsfFile.Ini` also references an upstream
`INIFileParser` assembly.

## Version History

```
v3.0.0: split the ini support out of SadPencil.Ra2CsfFile into the new SadPencil.Ra2CsfFile.Ini package, which is the only package that carries ini code, including the bundled patched ini-parser as SadPencil.Ra2CsfFile.IniParser 3.0.0; remove the obsolete CsfFile.LoadFromIniFile, CsfFile.WriteIniFile and CsfFile.GetCsfLang methods; normalize the line breaks of label values loaded from a csf file to LF; preserve the whitespace of label values loaded from an ini file; drop the WhiteSpaceWarning key from generated ini files; remove CsfFileIniHelper.TrimMultiLine; fix a bug where labels were not sorted when writing an ini file; warn at build time when the upstream ini parser is referenced too; reject an ini key that ends with whitespace instead of trimming it
v2.2.5: update existing label with correct key case
v2.2.4: add trimmable line warning comment when generating an ini file
v2.2.3: ignore case when sorting keys; use natural sort order to make sure "key10" is placed after "key9"
v2.2.2: add OrderByKey option
v2.2.1: fix a bug where labels were not case-insensitive after cloning; support case-insensitive labels in INI
v2.2.0: allow CSF labels with capitcal letters included
v2.1.3: handle invalid CSF values with extra zeros
v2.1.2: disable Encoding1252WriteWorkaround by default; add CLSCompliant attribute to namespace SadPencil.Ra2CsfFile
v2.1.1: fix that some label names are not loaded successfully from .ini files
v2.1.0: api breaking change: change the behavior of Csf.AddLabel() with Add&Replace, so that the original ra2.csf file can be loaded
v2.0.2: remove the space around the "=" sign of ini file to fix a bug processing values contains " = " pattern
v2.0.1: fix a bug that CSF file with non-lowercase label name can not be loaded
v2.0.0: migrate to .NET Standard 2.0; replace dependency MadMilkman.Ini with ini-parser-netstandard; add Csf.RemoveLabel() method.
v1.3.1: api breaking change: Labels.Add will be replaced with AddLabel; add encoding 1252 workaround options for the original RA2 fonts; add clone constructor for CsfFile. 
v1.2.2: space in labels is now tolerated so that the library will not complain about the string table file in RA2.
v1.2.1: fix a bug where some labels of the ini file is not loaded.
v1.2.0: api breaking change: CsfFile.Labels will now store only one value for a label, as the rest values (if any) are not used by the game; api change: deprecate CsfFile.GetCsfLang() with CsfLangHelper.GetCsfLang(); api change: deprecate CsfFile.LoadFromIniFile() with CsfFileIniHelper.LoadFromIniFile(); api change: deprecate CsfFile.WriteIniFile() with CsfFileIniHelper.WriteIniFile().
v1.1.1: add XML documentation; re-release the library with Release configuration.
v1.1.0: fix a bug where multi-line text will be trimmed mistakenly; invalid chars in label name will now be checked.
```
