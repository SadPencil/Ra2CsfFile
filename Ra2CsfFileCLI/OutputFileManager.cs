namespace Ra2CsfFileCLI;

/// <summary>
/// Resolves the output path and writes conversion output, including the
/// overwrite policy and protection against writing through links to the input.
/// </summary>
internal static class OutputFileManager
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private const int MaxSymbolicLinkDepth = 64;

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

        if (string.Equals(inputFullPath, outputFullPath, PathComparison) ||
            (File.Exists(outputFullPath) &&
             string.Equals(ResolveFinalPath(inputFullPath), ResolveFinalPath(outputFullPath), PathComparison)))
        {
            // Catch identical spellings and paths that resolve to the same input
            // file through symbolic links. A hard link at the output path is
            // handled by Write, which replaces the directory entry instead of
            // truncating the input file.
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
        if (!force)
        {
            FileStream output;
            try
            {
                // Enforce the no-overwrite policy atomically when opening the
                // destination. Only this open can mean "already exists"; write
                // and flush failures must keep their real cause.
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

    /// <summary>
    /// Fully resolve symlinks in a path, including symlinks in the parent
    /// components of other symlink targets. Resolution is bounded so that a
    /// symlink cycle produces an error instead of unbounded recursion.
    /// </summary>
    private static string ResolveFinalPath(string fullPath) => ResolveFinalPath(fullPath, 0);

    private static string ResolveFinalPath(string fullPath, int depth)
    {
        if (depth > MaxSymbolicLinkDepth)
        {
            throw new CliException($"Too many levels of symbolic links while resolving \"{fullPath}\".");
        }

        string path = Path.GetFullPath(fullPath);
        string? root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
        {
            return path;
        }

        string[] segments = path[root.Length..].Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return root;
        }

        string resolvedDirectory = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            resolvedDirectory = ResolveLinkIfAny(Path.Combine(resolvedDirectory, segments[i]), depth);
        }

        string resolvedPath = Path.Combine(resolvedDirectory, segments[^1]);
        return Path.GetFullPath(ResolveLinkIfAny(resolvedPath, depth));
    }

    private static string ResolveLinkIfAny(string path, int depth)
    {
        FileSystemInfo? target;
        if (Directory.Exists(path))
        {
            target = new DirectoryInfo(path).ResolveLinkTarget(returnFinalTarget: false);
        }
        else if (File.Exists(path))
        {
            target = new FileInfo(path).ResolveLinkTarget(returnFinalTarget: false);
        }
        else
        {
            return path;
        }

        return target is not null ? ResolveFinalPath(target.FullName, depth + 1) : path;
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
