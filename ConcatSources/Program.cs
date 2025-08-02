using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConcatSources
{
    public class Program
    {
        // 🔹 Directory names that are always ignored (case-insensitive)
        private static readonly string[] AlwaysSkipDirs = { "Migrations" };

        public static async Task<int> Main(string[] args)
        {
            // ---------- command-line ------------------------------------------
            var rootOption = new Option<DirectoryInfo>("--root")
            {
                Description = "Root folder of the C# project to scan",
                Required = true
            };

            var outputOption = new Option<FileInfo>("--output")
            {
                Description = "Path of the combined output file",
                DefaultValueFactory = _ => new FileInfo("AllSources.cs")
            };

            var excludeOption = new Option<string[]>("--exclude")
            {
                Description = "Patterns to ignore (e.g. \"*.Designer.cs\")",
                Arity = ArgumentArity.ZeroOrMore,
                AllowMultipleArgumentsPerToken = true
            };

            var cmd = new RootCommand("Concatenate C# source files")
            {
                rootOption,
                outputOption,
                excludeOption
            };
            // ------------------------------------------------------------------

            cmd.SetAction(async (parse, cancelToken) =>
            {
                var rootDir = parse.GetValue(rootOption)!;
                var outFile = parse.GetValue(outputOption)!;
                var patterns = parse.GetValue(excludeOption) ?? Array.Empty<string>();

                // Treat --output <dir> as "dir/AllSources.cs"
                if (Directory.Exists(outFile.FullName))
                    outFile = new FileInfo(Path.Combine(outFile.FullName, "AllSources.cs"));

                var excludeRx = patterns.Select(p =>
                                        new Regex("^" + Regex.Escape(p)
                                                           .Replace(@"\*", ".*")
                                                           .Replace(@"\?", ".") + "$",
                                                  RegexOptions.IgnoreCase))
                                        .ToArray();

                var csFiles = Directory.EnumerateFiles(rootDir.FullName, "*.cs",
                                   SearchOption.AllDirectories)
                                   // 🔹 skip “Migrations” folders first
                                   .Where(f => !IsInsideExcludedDir(f, rootDir.FullName))
                                   // 🔹 then apply the user-supplied pattern filters
                                   .Where(f => !excludeRx.Any(rx =>
                                           rx.IsMatch(Path.GetFileName(f))))
                                   .OrderBy(f => f)
                                   .ToList();

                Console.WriteLine($"Found {csFiles.Count} .cs files → {outFile.FullName}");

                // ----------- collect parts ------------------------------------
                var usingSet = new SortedSet<string>(StringComparer.Ordinal);
                var namespaces = new Dictionary<string, List<MemberDeclarationSyntax>>();

                foreach (var file in csFiles)
                {
                    var text = await File.ReadAllTextAsync(file, cancelToken);
                    var root = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();

                    // collect usings
                    foreach (var u in root.Usings)
                        usingSet.Add(u.ToString().Trim());

                    // collect members grouped by namespace
                    foreach (var member in root.Members)
                        AddMember(member, namespaces);
                }

                // ----------- emit combined file -------------------------------
                var sb = new StringBuilder();

                foreach (var u in usingSet)
                    sb.AppendLine(u);
                sb.AppendLine();

                foreach (var (ns, members) in namespaces.OrderBy(k => k.Key))
                {
                    if (string.IsNullOrEmpty(ns))          // global namespace
                    {
                        foreach (var m in members)
                            sb.AppendLine(m.NormalizeWhitespace().ToFullString());
                    }
                    else
                    {
                        sb.AppendLine($"namespace {ns}");
                        sb.AppendLine("{");

                        foreach (var m in members)
                        {
                            var txt = m.NormalizeWhitespace().ToFullString();
                            sb.AppendLine(Indent(txt, "    "));
                        }

                        sb.AppendLine("}");
                        sb.AppendLine();
                    }
                }

                await File.WriteAllTextAsync(outFile.FullName, sb.ToString(), cancelToken);
                Console.WriteLine("Done!");
                return 0;
            });

            return await cmd.Parse(args).InvokeAsync();
        }

        // ---------------- helpers --------------------------------------------

        private static void AddMember(MemberDeclarationSyntax member,
            Dictionary<string, List<MemberDeclarationSyntax>> map)
        {
            switch (member)
            {
                // namespace Foo.Bar { ... }
                case NamespaceDeclarationSyntax ns:
                    AddTo(ns.Name.ToString(), ns.Members, map);
                    break;

                // namespace Foo.Bar;  // file-scoped
                case FileScopedNamespaceDeclarationSyntax fs:
                    AddTo(fs.Name.ToString(), fs.Members, map);
                    break;

                // top-level statements / members (rare in libs, but legal)
                default:
                    AddTo(string.Empty, new[] { member }, map);
                    break;
            }
        }

        private static void AddTo(string ns, IEnumerable<MemberDeclarationSyntax> items,
                                  Dictionary<string, List<MemberDeclarationSyntax>> map)
        {
            if (!map.TryGetValue(ns, out var list))
            {
                list = new List<MemberDeclarationSyntax>();
                map[ns] = list;
            }
            list.AddRange(items);
        }

        private static string Indent(string text, string prefix)
            => string.Join(Environment.NewLine,
                           text.Split('\n').Select(l => prefix + l.TrimEnd('\r')));

        // 🔹 true if the file’s relative path contains an AlwaysSkipDirs segment
        private static bool IsInsideExcludedDir(string file, string root)
        {
            var rel = Path.GetRelativePath(root, file);
            char[] seps = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            return rel.Split(seps, StringSplitOptions.RemoveEmptyEntries)
                      .Any(segment => AlwaysSkipDirs
                          .Any(skip => segment.Equals(skip, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
