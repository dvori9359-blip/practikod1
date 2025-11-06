

//using System.CommandLine;
//using System.CommandLine.Invocation;
//var bundelOption = new Option<FileInfo>("--output","file path and name");
//var bundelCommand = new Command("bundel", "Bundle code files to a single file");
//bundelCommand.AddOption(bundelOption);
//bundelCommand.SetHandler((output) =>
//{
//    try
//    {
//        File.Create(output.FullName);
//        Console.WriteLine("File was created");
//    }
//    catch (DirectoryNotFoundException ex)
//    {
//        Console.WriteLine("Error:File is invalid");
//    }
//},bundelOption);
//var rootCommand = new RootCommand("Root command for file Bundel CLI");
//rootCommand.AddCommand(bundelCommand);
//rootCommand.InvokeAsync(args);

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Code Bundler CLI");

        // ======================
        // פקודת BUNDLE
        // ======================
        var bundleCommand = new Command("bundle", "Bundle code files into a single file");

        // OPTIONS
        var languageOption = new Option<string[]>(
            aliases: new[] { "--language", "-l" },
            description: "Programming languages to include in the bundle or 'all' for all code files")
        { IsRequired = true };

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output bundle file path")
        { IsRequired = true };

        var noteOption = new Option<bool>(
            aliases: new[] { "--note", "-n" },
            description: "Include code origin as comment in the bundle");

        var sortOption = new Option<string>(
            aliases: new[] { "--sort", "-s" },
            getDefaultValue: () => "name",
            description: "Sort order: name (default) or type");

        var removeEmptyLinesOption = new Option<bool>(
            aliases: new[] { "--remove-empty-lines", "-r" },
            description: "Remove empty lines");

        var authorOption = new Option<string>(
            aliases: new[] { "--author", "-a" },
            description: "Author name to include in the bundle");

        bundleCommand.AddOption(languageOption);
        bundleCommand.AddOption(outputOption);
        bundleCommand.AddOption(noteOption);
        bundleCommand.AddOption(sortOption);
        bundleCommand.AddOption(removeEmptyLinesOption);
        bundleCommand.AddOption(authorOption);

        // HANDLER
        bundleCommand.SetHandler(
            (string[] languages, string output, bool note, string sort, bool removeEmptyLines, string author) =>
            {
                string currentDir = Directory.GetCurrentDirectory();

                bool includeAll = languages.Any(l => l.Equals("all", StringComparison.OrdinalIgnoreCase));

                var allowedExtensions = includeAll
                    ? new string[] { ".cs", ".js", ".ts", ".py", ".java" }
                    : languages.Select(lang => lang.ToLower() switch
                    {
                        "c#" or "cs" => ".cs",
                        "js" => ".js",
                        "ts" => ".ts",
                        "python" or "py" => ".py",
                        "java" => ".java",
                        _=> ""
                    }).Where(e => !string.IsNullOrEmpty(e)).ToArray();

                if (allowedExtensions.Length == 0)
                {
                    Console.WriteLine("No valid languages specified.");
                    return;
                }

                var files = Directory.GetFiles(currentDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .Where(f => !f.Split(Path.DirectorySeparatorChar)
                                  .Any(d => d.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                            d.Equals("debug", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (!files.Any())
                {
                    Console.WriteLine("No code files found to bundle.");
                    return;
                }

                if (sort == "name")
                    files.Sort();
                else if (sort == "type")
                    files = files.OrderBy(f => Path.GetExtension(f)).ThenBy(f => f).ToList();

                try
                {
                    using var writer = new StreamWriter(output);

                    if (!string.IsNullOrEmpty(author))
                        writer.WriteLine($"// Author: {author}");

                    foreach (var file in files)
                    {
                        if (note)
                            writer.WriteLine($"// Source: {Path.GetRelativePath(currentDir, file)}");

                        var lines = File.ReadAllLines(file);

                        if (removeEmptyLines)
                            lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

                        foreach (var line in lines)
                            writer.WriteLine(line);

                        writer.WriteLine(); // רווח בין קבצים
                    }

                    Console.WriteLine($"Bundle created: {output}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing bundle: {ex.Message}");
                }

            },
            languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption
        );

        rootCommand.AddCommand(bundleCommand);

        // ======================
        // פקודת CREATE-RSP
        // ======================
        var createRspCommand = new Command("create-rsp", "Create a response file for bundle command");

        createRspCommand.SetHandler(() =>
        {
            string Ask(string message) { Console.Write(message + ": "); return Console.ReadLine() ?? ""; }

            var lang = Ask("Languages (comma separated, or 'all')");
            var output = Ask("Output file path");
            var note = Ask("Include code origin? (true/false)");
            var sort = Ask("Sort order (name/type)");
            var remove = Ask("Remove empty lines? (true/false)");
            var author = Ask("Author name");

            string rspContent = $"bundle --language {lang} --output \"{output}\"";
            if (note.ToLower() == "true") rspContent += " --note";
            if (sort != "name") rspContent += $" --sort {sort}";
            if (remove.ToLower() == "true") rspContent += " --remove-empty-lines";
            if (!string.IsNullOrEmpty(author)) rspContent += $" --author \"{author}\"";

            string rspFile = "bundle.rsp";
            File.WriteAllText(rspFile, rspContent);
            Console.WriteLine($"Response file created: {rspFile}");
        });

        rootCommand.AddCommand(createRspCommand);

        return await rootCommand.InvokeAsync(args);
    }
}





