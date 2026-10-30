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

        if (string.Equals(inputFullPath, outputFullPath, PathComparison) ||
            (File.Exists(outputFullPath) &&
             string.Equals(ResolveFinalPath(inputFullPath), ResolveFinalPath(outputFullPath), PathComparison)))
        {
            // Catch identical spellings and paths that resolve to the same input
            // file through symbolic links. A hard link at the output path is
            // handled by WriteOutput, which replaces the directory entry instead
            // of truncating the input file.
            throw new CliException("The input and output paths refer to the same file.");
        }

        string? outputDirectory = Path.GetDirectoryName(outputFullPath);
        if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            throw new CliException($"Output directory does not exist: \"{outputDirectory}\".");
        }

        if (!force && File.Exists(outputFullPath))
        {
            // Friendly early failure. WriteOutput enforces the same policy
            // atomically with FileMode.CreateNew to close the check/write race.
            throw new CliException($"Output file already exists: \"{outputFullPath}\". Use --force to overwrite it.");
        }

        return outputFullPath;
    }

    /// <summary>
    /// Resolve symlinks in the directory components and in the final file so
    /// that different path spellings of the same input file can be detected.
    /// </summary>
    private static string ResolveFinalPath(string fullPath)
    {
        string? root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(root))
        {
            return fullPath;
        }

        string[] segments = fullPath[root.Length..].Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return root;
        }

        string resolvedDirectory = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            resolvedDirectory = ResolveDirectoryLink(Path.Combine(resolvedDirectory, segments[i]));
        }

        string resolvedPath = Path.Combine(resolvedDirectory, segments[^1]);
        if (File.Exists(resolvedPath))
        {
            resolvedPath = ResolveFileLink(resolvedPath);
        }

        return Path.GetFullPath(resolvedPath);
    }

    private static string ResolveDirectoryLink(string path)
    {
        var directory = new DirectoryInfo(path);
        if (directory.Exists)
        {
            FileSystemInfo? target = directory.ResolveLinkTarget(returnFinalTarget: true);
            if (target is not null)
            {
                return target.FullName;
            }
        }

        return path;
    }

    private static string ResolveFileLink(string path)
    {
        FileSystemInfo? target = new FileInfo(path).ResolveLinkTarget(returnFinalTarget: true);
        return target is not null ? target.FullName : path;
    }

    public static ConversionSummary ConvertCsfToIni(string inputPath, string outputPath, CsfFileOptions options, bool force)
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
            WriteOutput(outputPath, buffer.ToArray(), force);

            return new ConversionSummary(csf.Labels.Count, csf.Language, csf.Version);
        }
        catch (CliException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CliException($"Failed to convert \"{inputPath}\" to \"{outputPath}\": {ex.Message}", ex);
        }
    }

    public static ConversionSummary ConvertIniToCsf(string inputPath, string outputPath, CsfFileOptions options, bool force)
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
            WriteOutput(outputPath, buffer.ToArray(), force);

            return new ConversionSummary(csf.Labels.Count, csf.Language, csf.Version);
        }
        catch (CliException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CliException($"Failed to convert \"{inputPath}\" to \"{outputPath}\": {ex.Message}", ex);
        }
    }

    private static void WriteOutput(string outputPath, byte[] contents, bool force)
    {
        if (!force)
        {
            try
            {
                // FileMode.CreateNew makes the no-overwrite policy atomic: if the
                // destination appears after ResolveOutputPath checked it, this
                // open fails instead of silently truncating another conversion.
                using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                output.Write(contents);
            }
            catch (IOException ex) when (File.Exists(outputPath))
            {
                throw new CliException($"Output file already exists: \"{outputPath}\". Use --force to overwrite it.", ex);
            }

            return;
        }

        // Replace the destination directory entry through a temporary file.
        // Unlike File.WriteAllBytes, this does not truncate an existing entry in
        // place, so a hard link that points at the input file is not overwritten.
        // Symlinks that resolve to the input are rejected earlier.
        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            throw new CliException($"Unable to determine the output directory: \"{outputPath}\".");
        }

        string temporaryPath = Path.Combine(outputDirectory, $".ra2csfcli-{Guid.NewGuid():N}.tmp");
        bool temporaryCreated = false;
        try
        {
            using (var temporary = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                temporaryCreated = true;
                temporary.Write(contents);
            }

            File.Move(temporaryPath, outputPath, overwrite: true);
            temporaryCreated = false;
        }
        finally
        {
            if (temporaryCreated)
            {
                TryDelete(temporaryPath);
            }
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
