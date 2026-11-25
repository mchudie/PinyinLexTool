using System.CommandLine;
using PinyinLexTool.Core.IO;
using PinyinLexTool.Core.Services;

namespace PinyinLexTool;

internal sealed class Program
{
    private const int ErrorExitCode = -1;

    private static int Main(string[] args)
    {
        var root = new RootCommand("微软拼音自定义短语管理工具");
        root.Options.Remove(root.Options.OfType<VersionOption>().Single());

        var lex = new Option<string?>("--lex") { Description = "指定 .lex 文件路径，默认使用当前用户词库" };
        var verbose = new Option<bool>("--verbose") { Description = "显示详细信息" };

        var list = new Command("list", "列出现有短语，支持按拼音过滤");
        var filter = new Option<string?>("--filter") { Description = "按拼音筛选短语" };
        list.Options.Add(lex);
        list.Options.Add(filter);
        list.SetAction(result => ListCommand.Execute(new()
        {
            LexPath = result.GetValue(lex),
            Filter = result.GetValue(filter)
        }));

        var export = new Command("export", "导出系统当前自定义短语到 TXT 文件");
        var output = new Argument<string>("output")
        {
            Description = "输出 TXT 文件路径",
            Arity = ArgumentArity.ExactlyOne
        };
        export.Arguments.Add(output);
        export.Options.Add(lex);
        export.SetAction(result => ExportCommand.Execute(new()
        {
            Output = result.GetRequiredValue(output),
            LexPath = result.GetValue(lex)
        }));

        var import = new Command("import", "从 TXT 导入短语到 .lex（相同拼音会替换现有条目）");
        var input = new Argument<string>("input")
        {
            Description = "输入 TXT 文件路径",
            Arity = ArgumentArity.ExactlyOne
        };
        var noBackup = new Option<bool>("--no-backup") { Description = "写入前跳过备份" };
        var dryRun = new Option<bool>("--dry-run") { Description = "只校验，不写入词库" };
        import.Arguments.Add(input);
        import.Options.Add(lex);
        import.Options.Add(noBackup);
        import.Options.Add(dryRun);
        import.Options.Add(verbose);
        import.SetAction(result => ImportCommand.Execute(new()
        {
            Input = result.GetRequiredValue(input),
            LexPath = result.GetValue(lex),
            NoBackup = result.GetValue(noBackup),
            DryRun = result.GetValue(dryRun),
            Verbose = result.GetValue(verbose)
        }));

        var debug = new Command("debug", "显示调试信息，用于问题排查");
        debug.Options.Add(verbose);
        debug.SetAction(result => DebugCommand.Execute(new()
        {
            Verbose = result.GetValue(verbose)
        }));

        root.Subcommands.Add(list);
        root.Subcommands.Add(export);
        root.Subcommands.Add(import);
        root.Subcommands.Add(debug);

        var invocation = new InvocationConfiguration { EnableDefaultExceptionHandler = false };
        try
        {
            int exitCode = root.Parse(args.Length == 0 ? ["--help"] : args).Invoke(invocation);
            return exitCode == 0 ? 0 : ErrorExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ErrorExitCode;
        }
    }
}

/// <summary>导出命令。</summary>
public sealed class ExportCommand
{
    /// <summary>导出参数。</summary>
    public sealed class Settings
    {
        /// <summary>输出文件路径（TXT）。</summary>
        public required string Output { get; init; }

        /// <summary>可选：指定 .lex 文件路径；默认读取当前用户路径。</summary>
        public string? LexPath { get; init; }
    }

    /// <summary>执行导出逻辑。</summary>
    public static int Execute(Settings settings)
    {
        var lexPath = settings.LexPath ?? LexPaths.GetUserLexPath();
        var service = new PinyinLexService(new LexFileReader());
        service.ExportAsync(lexPath, settings.Output).GetAwaiter().GetResult();
        return 0;
    }
}

/// <summary>导入命令。</summary>
public sealed class ImportCommand
{
    /// <summary>导入参数。</summary>
    public sealed class Settings
    {
        /// <summary>输入 TXT 文件路径。</summary>
        public required string Input { get; init; }

        /// <summary>可选：指定 .lex 文件路径；默认读取当前用户路径。</summary>
        public string? LexPath { get; init; }

        /// <summary>禁用备份（默认会在写入前创建 .bak）。</summary>
        public bool NoBackup { get; init; }

        /// <summary>只校验不落盘。</summary>
        public bool DryRun { get; init; }

        /// <summary>输出更多详情。</summary>
        public bool Verbose { get; init; }
    }

    /// <summary>执行导入逻辑。</summary>
    public static int Execute(Settings settings)
    {
        var lexPath = settings.LexPath ?? LexPaths.GetUserLexPath();
        var service = new PinyinLexService(new LexFileReader());
        var doBackup = !settings.NoBackup;
        service.ImportAsync(lexPath, settings.Input, doBackup, settings.DryRun, settings.Verbose).GetAwaiter().GetResult();
        return 0;
    }
}

/// <summary>列出短语命令。</summary>
public sealed class ListCommand
{
    /// <summary>参数。</summary>
    public sealed class Settings
    {
        /// <summary>可选：按拼音过滤。</summary>
        public string? Filter { get; init; }

        /// <summary>可选：指定 .lex 文件路径；默认读取当前用户路径。</summary>
        public string? LexPath { get; init; }
    }

    /// <summary>执行列表逻辑。</summary>
    public static int Execute(Settings settings)
    {
        var lexPath = settings.LexPath ?? LexPaths.GetUserLexPath();
        var reader = new LexFileReader();
        var list = reader.ReadAllAsync(lexPath).GetAwaiter().GetResult();
        var q = list.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(settings.Filter))
        {
            q = q.Where(p => p.Pinyin.Equals(settings.Filter, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var p in q)
        {
            System.Console.WriteLine($"{p.Pinyin} {p.Index} {p.Text}");
        }
        return 0;
    }
}

/// <summary>调试命令。</summary>
public sealed class DebugCommand
{
    /// <summary>调试参数。</summary>
    public sealed class Settings
    {
        /// <summary>显示详细信息。</summary>
        public bool Verbose { get; init; }
    }

    /// <summary>执行调试逻辑。</summary>
    public static int Execute(Settings settings)
    {
        Console.WriteLine("=== PinyinLexTool 调试信息 ===");
        Console.WriteLine($"版本: {typeof(DebugCommand).Assembly.GetName().Version?.ToString(3)}");
        Console.WriteLine($"运行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"操作系统: {Environment.OSVersion}");
        Console.WriteLine($"运行时: {Environment.Version}");
        Console.WriteLine($"工作目录: {Environment.CurrentDirectory}");
        
        var lexPath = LexPaths.GetUserLexPath();
        Console.WriteLine($"默认 .lex 路径: {lexPath}");
        
        if (File.Exists(lexPath))
        {
            var fileInfo = new FileInfo(lexPath);
            Console.WriteLine($"文件存在: 是");
            Console.WriteLine($"文件大小: {fileInfo.Length} 字节");
            Console.WriteLine($"最后修改: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            
            if (settings.Verbose)
            {
                try
                {
                    var reader = new LexFileReader();
                    var phrases = reader.ReadAllAsync(lexPath).GetAwaiter().GetResult();
                    Console.WriteLine($"短语数量: {phrases.Count}");
                    
                    if (phrases.Count > 0)
                    {
                        Console.WriteLine("前 5 个短语:");
                        foreach (var phrase in phrases.Take(5))
                        {
                            Console.WriteLine($"  {phrase.Pinyin} {phrase.Index} {phrase.Text}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"读取文件时出错: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine($"文件存在: 否");
        }
        
        Console.WriteLine("=== 调试信息结束 ===");
        return 0;
    }
}
