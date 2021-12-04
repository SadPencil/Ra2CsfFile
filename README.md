# SadPencil.Ra2CsfFile

## .NET Library
This is a .NET Standard 2.0 Library to load, edit, and save string table files (.csf) for Red Alert 2. Also, (de)serialize the string table from/to .ini files.

## Example use

See `ExampleApp/Program.cs` file.

## License

MIT

## Notes
Reference: https://modenc.renegadeprojects.com/CSF_File_Format

## Version History

```
v2.1.1: fix that some label names are not loaded successfully from .ini files
v2.1.0: api breaking change: change the behavior of Csf.AddLabel() with Add&Replace, so that the original ra2.csf file can be loaded
v2.0.2: remove the space around the "=" sign of ini file to fix a bug proceeding with values contains " = " pattern
v2.0.1: fix a bug that CSF file with non-lowercase label name can not be loaded
v2.0.0: migrate to .NET Standard 2.0; replace dependency MadMilkman.Ini with ini-parser-netstandard; add Csf.RemoveLabel() method.
v1.3.1: api breaking change: Labels.Add will be replaced with AddLabel; add encoding 1252 workaround options for the original RA2 fonts; add clone constructor for CsfFile. 
v1.2.2: space in labels is now tolerated so that the library will not complain about the string table file in RA2.
v1.2.1: fix a bug where some labels of the ini file is not loaded.
v1.2.0: api breaking change: CsfFile.Labels will now store only one value for a label, as the rest values (if any) are not used by the game; api change: deprecate CsfFile.GetCsfLang() with CsfLangHelper.GetCsfLang(); api change: deprecate CsfFile.LoadFromIniFile() with CsfFileIniHelper.LoadFromIniFile(); api change: deprecate CsfFile.WriteIniFile() with CsfFileIniHelper.WriteIniFile().
v1.1.1: add XML documentation; re-release the library with Release configuration.
v1.1.0: fix a bug where multi-line text will be trimmed mistakenly; invalid chars in label name will now be checked.
```

