using SadPencil.Ra2CsfFile;

namespace Ra2CsfFileCLI;

internal readonly record struct ConversionSummary(int LabelCount, CsfLang Language, int CsfVersion);

internal sealed class CliException(string message, Exception? innerException = null)
    : Exception(message, innerException);

internal static class CsfIniConverter
{
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
            OutputFileManager.Write(outputPath, buffer.ToArray(), force);

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
            OutputFileManager.Write(outputPath, buffer.ToArray(), force);

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
}
