using SadPencil.Ra2CsfFile;

namespace Ra2CsfFileCLI;

internal readonly record struct ConversionSummary(int LabelCount, CsfLang Language, int CsfVersion);

internal sealed class CliException(string message, Exception? innerException = null)
    : Exception(message, innerException);

internal static class CsfIniConverter
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    /// Validate the input path and resolve the output path, including the
    /// overwrite policy. Returned paths are absolute.
    /// </summary>
    public static string ResolveOutputPath(string inputPath, FileInfo? output, string defaultExtension, bool force)
    {
        if (Directory.Exists(inputPath))
        {
            throw new CliException($"Input path is a directory: \"{inputPath}\".");
        }

        if (!File.Exists(inputPath))
        {
            throw new CliException($"Input file not found: \"{inputPath}\".");
        }

        string inputFullPath = Path.GetFullPath(inputPath);
        string outputFullPath = output is null
            ? Path.ChangeExtension(inputFullPath, defaultExtension)
            : Path.GetFullPath(output.FullName);

        if (Directory.Exists(outputFullPath))
        {
            throw new CliException($"Output path is a directory: \"{outputFullPath}\".");
        }

        if (string.Equals(inputFullPath, outputFullPath, PathComparison))
        {
            throw new CliException("The input and output paths refer to the same file.");
        }

        string? outputDirectory = Path.GetDirectoryName(outputFullPath);
        if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            throw new CliException($"Output directory does not exist: \"{outputDirectory}\".");
        }

        if (!force && File.Exists(outputFullPath))
        {
            throw new CliException($"Output file already exists: \"{outputFullPath}\". Use --force to overwrite it.");
        }

        return outputFullPath;
    }

    public static ConversionSummary ConvertCsfToIni(string inputPath, string outputPath, CsfFileOptions options)
    {
        try
        {
            CsfFile csf;
            using (FileStream input = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                csf = CsfFile.LoadFromCsfFile(input, options);
            }

            // Build the complete output in memory before touching the destination,
            // so a conversion error cannot leave a partially written file behind.
            using var buffer = new MemoryStream();
            CsfFileIniHelper.WriteIniFile(csf, buffer);
            File.WriteAllBytes(outputPath, buffer.ToArray());

            return new ConversionSummary(csf.Labels.Count, csf.Language, csf.Version);
        }
        catch (Exception ex)
        {
            throw new CliException($"Failed to convert \"{inputPath}\" to \"{outputPath}\": {ex.Message}", ex);
        }
    }

    public static ConversionSummary ConvertIniToCsf(string inputPath, string outputPath, CsfFileOptions options)
    {
        try
        {
            CsfFile csf;
            using (FileStream input = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                csf = CsfFileIniHelper.LoadFromIniFile(input, options);
            }

            // Build the complete output in memory before touching the destination,
            // so a conversion error cannot leave a partially written file behind.
            using var buffer = new MemoryStream();
            csf.WriteCsfFile(buffer);
            File.WriteAllBytes(outputPath, buffer.ToArray());

            return new ConversionSummary(csf.Labels.Count, csf.Language, csf.Version);
        }
        catch (Exception ex)
        {
            throw new CliException($"Failed to convert \"{inputPath}\" to \"{outputPath}\": {ex.Message}", ex);
        }
    }
}
