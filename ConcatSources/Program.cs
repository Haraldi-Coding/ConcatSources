using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConcatSources
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // ---- define options -------------------------------------------------
            var rootOption = new Option<DirectoryInfo>("--root")
            {
                Description = "Root folder of the C# project to scan",
                Required = true                                // IsRequired ➜ Required :contentReference[oaicite:0]{index=0}
            };

            var outputOption = new Option<FileInfo>("--output")
            {
                Description = "Path of the combined output file",
                DefaultValueFactory = _ => new FileInfo("AllSources.cs")
            };

            var excludeOption = new Option<string[]>("--exclude")
            {
                Description = "Glob or regex patterns to ignore (e.g. \"*.Designer.cs\")",
                Arity = ArgumentArity.ZeroOrMore,
                AllowMultipleArgumentsPerToken = true
            };
            // ---------------------------------------------------------------------

            var cmd = new RootCommand("Concatenate C# source files")
            {
                rootOption,
                outputOption,
                excludeOption
            };

            // SetAction replaces the old SetHandler extension  :contentReference[oaicite:1]{index=1}
            cmd.SetAction(async (parseResult, cancelToken) =>
            {
                var rootDir = parseResult.GetValue(rootOption)!;
                var outFile = parseResult.GetValue(outputOption)!;
                var patterns = parseResult.GetValue(excludeOption) ?? Array.Empty<string>();

                var excludeRx = patterns.Select(p =>
                                    new Regex("^" + Regex.Escape(p)
                                                       .Replace(@"\*", ".*")
                                                       .Replace(@"\?", ".") + "$",
                                              RegexOptions.IgnoreCase))
                                        .ToArray();

                var csFiles = Directory.EnumerateFiles(rootDir.FullName, "*.cs",
                                   SearchOption.AllDirectories)
                                   .Where(f => !excludeRx.Any(rx =>
                                           rx.IsMatch(Path.GetFileName(f))))
                                   .OrderBy(f => f);

                Console.WriteLine($"Found {csFiles.Count()} .cs files → {outFile.FullName}");

                await using var writer = new StreamWriter(outFile.FullName, false, Encoding.UTF8);
                foreach (var file in csFiles)
                {
                    await writer.WriteLineAsync(
                        $"// ===== {Path.GetRelativePath(rootDir.FullName, file)} =====");
                    await writer.WriteAsync(await File.ReadAllTextAsync(file, cancelToken));
                    await writer.WriteLineAsync();
                }

                Console.WriteLine("Done!");
                return 0;      // exit code
            });

            // In beta 5+ invocation happens on ParseResult, not RootCommand  :contentReference[oaicite:2]{index=2}
            return await cmd.Parse(args).InvokeAsync();
        }
    }
}
