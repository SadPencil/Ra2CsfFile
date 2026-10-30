namespace Ra2CsfFileCLI;

/// <summary>
/// Resolves the output path and writes conversion output, including the
/// overwrite policy.
/// </summary>
internal static class OutputFileManager
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public static string ResolvePath(string inputPath, FileInfo? output, string defaultExtension, bool force)
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
            // Friendly early failure. Write enforces the same policy atomically
            // with FileMode.CreateNew to close the check/write race.
            throw new CliException($"Output file already exists: \"{outputFullPath}\". Use --force to overwrite it.");
        }

        return outputFullPath;
    }

    public static void Write(string outputPath, byte[] contents, bool force)
    {
        if (force)
        {
            File.WriteAllBytes(outputPath, contents);
            return;
        }

        FileStream output;
        try
        {
            // Enforce the no-overwrite policy atomically when opening the
            // destination. Only this open can mean "already exists"; write and
            // flush failures must keep their real cause.
            output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
        catch (IOException ex) when (File.Exists(outputPath))
        {
            throw new CliException($"Output file already exists: \"{outputPath}\". Use --force to overwrite it.", ex);
        }

        try
        {
            using (output)
            {
                output.Write(contents);
            }
        }
        catch
        {
            // We created this file; do not leave a partial result behind.
            TryDelete(outputPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
