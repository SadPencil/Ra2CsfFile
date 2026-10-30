using SadPencil.Ra2CsfFile;
using System.CommandLine;

namespace Ra2CsfFileCLI;

internal static class Cli
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;

    // The most conservative choice for existing Red Alert 2 game and mod files:
    // code points 0x80-0x9F are treated as Windows-1252 both when loading and
    // when saving, so legacy characters survive a .csf -> .ini -> .csf round trip.
    private const bool DefaultEncoding1252ReadWorkaround = true;
    private const bool DefaultEncoding1252WriteWorkaround = true;

    public static int Run(string[] args)
    {
        RootCommand rootCommand = BuildRootCommand();
        return rootCommand.Parse(args).Invoke();
    }

    private static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand(
            "Convert Red Alert 2 string tables between .csf and SadPencil.Ra2CsfFile .ini files.");

        rootCommand.Subcommands.Add(BuildCsfToIniCommand());
        rootCommand.Subcommands.Add(BuildIniToCsfCommand());

        return rootCommand;
    }

    private static Command BuildCsfToIniCommand()
    {
        var inputArgument = new Argument<FileInfo>("input")
        {
            Description = "The input .csf string table file.",
        };
        var outputOption = new Option<FileInfo?>("--output", "-o")
        {
            Description = "The output .ini file. Defaults to the input path with the .ini extension.",
        };
        var orderByKeyOption = new Option<bool>("--order-by-key")
        {
            Description = "Sort labels by key using natural, case-insensitive order.",
        };
        var no1252ReadOption = new Option<bool>("--no-1252-read")
        {
            Description = "Disable the Windows-1252 read workaround. It is enabled by default.",
        };
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Overwrite the output file if it already exists.",
        };

        var command = new Command("csf2ini", "Convert a .csf string table to a .ini file.");
        command.Arguments.Add(inputArgument);
        command.Options.Add(outputOption);
        command.Options.Add(orderByKeyOption);
        command.Options.Add(no1252ReadOption);
        command.Options.Add(forceOption);
        command.SetAction(parseResult => RunGuarded(() =>
        {
            FileInfo? input = parseResult.GetValue(inputArgument);
            if (input is null)
            {
                Console.Error.WriteLine("error: No input file was provided.");
                return ExitFailure;
            }

            FileInfo? output = parseResult.GetValue(outputOption);
            bool orderByKey = parseResult.GetValue(orderByKeyOption);
            bool no1252Read = parseResult.GetValue(no1252ReadOption);
            bool force = parseResult.GetValue(forceOption);

            var options = new CsfFileOptions
            {
                Encoding1252ReadWorkaround = !no1252Read,
                Encoding1252WriteWorkaround = DefaultEncoding1252WriteWorkaround,
                OrderByKey = orderByKey,
            };

            string inputPath = input.FullName;
            string outputPath = OutputFileManager.ResolvePath(inputPath, output, ".ini", force);
            ConversionSummary summary = CsfIniConverter.ConvertCsfToIni(inputPath, outputPath, options, force);
            WriteSummary(inputPath, outputPath, summary);
            return ExitSuccess;
        }));

        return command;
    }

    private static Command BuildIniToCsfCommand()
    {
        var inputArgument = new Argument<FileInfo>("input")
        {
            Description = "The input .ini string table file.",
        };
        var outputOption = new Option<FileInfo?>("--output", "-o")
        {
            Description = "The output .csf file. Defaults to the input path with the .csf extension.",
        };
        var orderByKeyOption = new Option<bool>("--order-by-key")
        {
            Description = "Sort labels by key using natural, case-insensitive order.",
        };
        var no1252WriteOption = new Option<bool>("--no-1252-write")
        {
            Description = "Disable the Windows-1252 write workaround. It is enabled by default.",
        };
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Overwrite the output file if it already exists.",
        };

        var command = new Command("ini2csf", "Convert a .ini string table to a .csf file.");
        command.Arguments.Add(inputArgument);
        command.Options.Add(outputOption);
        command.Options.Add(orderByKeyOption);
        command.Options.Add(no1252WriteOption);
        command.Options.Add(forceOption);
        command.SetAction(parseResult => RunGuarded(() =>
        {
            FileInfo? input = parseResult.GetValue(inputArgument);
            if (input is null)
            {
                Console.Error.WriteLine("error: No input file was provided.");
                return ExitFailure;
            }

            FileInfo? output = parseResult.GetValue(outputOption);
            bool orderByKey = parseResult.GetValue(orderByKeyOption);
            bool no1252Write = parseResult.GetValue(no1252WriteOption);
            bool force = parseResult.GetValue(forceOption);

            var options = new CsfFileOptions
            {
                Encoding1252ReadWorkaround = DefaultEncoding1252ReadWorkaround,
                Encoding1252WriteWorkaround = !no1252Write,
                OrderByKey = orderByKey,
            };

            string inputPath = input.FullName;
            string outputPath = OutputFileManager.ResolvePath(inputPath, output, ".csf", force);
            ConversionSummary summary = CsfIniConverter.ConvertIniToCsf(inputPath, outputPath, options, force);
            WriteSummary(inputPath, outputPath, summary);
            return ExitSuccess;
        }));

        return command;
    }

    private static void WriteSummary(string inputPath, string outputPath, ConversionSummary summary)
    {
        Console.WriteLine(
            $"Converted \"{inputPath}\" -> \"{outputPath}\": {summary.LabelCount} labels, language {summary.Language}, CSF version {summary.CsfVersion}.");
    }

    private static int RunGuarded(Func<int> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            string message = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
            Console.Error.WriteLine($"error: {message}");
            return ExitFailure;
        }
    }
}
