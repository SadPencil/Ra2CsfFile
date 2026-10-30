# Ra2CsfFileCLI

`Ra2CsfFileCLI` is a .NET 8 command-line converter for Red Alert 2 string tables. It converts SadPencil.Ra2CsfFile `.ini` files to `.csf` files and back, using the `SadPencil.Ra2CsfFile` library in this repository.

## Requirements

- To build: .NET 10 SDK (see `../global.json`).
- To run: .NET 8 runtime.

The project targets `net8.0` even though the SDK is .NET 10.

## Build

```sh
dotnet build Ra2CsfFileCLI/Ra2CsfFileCLI.csproj -c Release
```

## Usage

```text
Ra2CsfFileCLI csf2ini <input.csf> [-o|--output <output.ini>]
                                   [--order-by-key]
                                   [--no-1252-read]
                                   [-f|--force]

Ra2CsfFileCLI ini2csf <input.ini> [-o|--output <output.csf>]
                                   [--order-by-key]
                                   [--no-1252-write]
                                   [-f|--force]
```

Examples:

```sh
dotnet run --project Ra2CsfFileCLI -- csf2ini ra2.csf
dotnet run --project Ra2CsfFileCLI -- ini2csf ra2.ini --force
```

The default output is the input path with the other extension in the same directory. The command determines the input format; the file extension is not used for validation.

## Defaults

- **Windows-1252 workarounds: both enabled.** Code points 0x80-0x9F are treated as Windows-1252 when reading a `.csf` and converted back to those code points when writing a `.csf`. This is the most conservative setting for existing game and mod files, because it preserves the legacy mapping used by the original `game.fnt`. It intentionally differs from `Ra2CsfToolsGUI`'s default (read workaround on, write workaround off). Use `--no-1252-read` or `--no-1252-write` to opt out for one direction.
- **Label order:** the input order is preserved. Use `--order-by-key` to sort labels with the library's natural, case-insensitive comparison.
- **Output files:** an existing output file is not overwritten unless `--force` is specified.
- **Encoding:** generated `.ini` files are UTF-8 without BOM. The generated `.csf` reuses the `CsfVersion` and `CsfLang` values from the `.ini` header.
- **Exit codes:** 0 on success, 1 on parse, validation, conversion, or I/O errors.

## Known limitations

These come from the underlying `SadPencil.Ra2CsfFile` library and its INI representation:

- Leading and trailing whitespace on a line cannot be represented faithfully in the `.ini` format. The generated `.ini` contains a `WhiteSpaceWarning` key for affected labels, but converting back to `.csf` trims those spaces.
- Only the first value of a label with multiple values is kept; extra values are ignored.
- Label names are case-insensitive; if the input contains duplicate labels (for example, the original `ra2.csf` contains several case-insensitive duplicates), later values replace earlier ones.
- Label names must use the library's allowed ASCII character set.
- The generated `.csf` is semantically equivalent, not byte-for-byte identical to the input; the library normalizes some CSF structures.

Only `.csf` and `.ini` conversion is provided. The GUI's YAML/JSON formats are not part of this project.
