using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel; // for attributes if needed
using PinyinLexTool.IO;

namespace PinyinLexTool;

internal sealed class Program
{
    private static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(cfg =>
        {
            cfg.SetApplicationName("PinyinLexTool");
            cfg.AddCommand<ListCommand>("list")
               .WithDescription("列出现有短语；支持按拼音过滤");
            cfg.AddCommand<ExportCommand>("export")
               .WithDescription("导出系统当前自定义短语到 TXT 文件");
            cfg.AddCommand<ImportCommand>("import")
               .WithDescription("从 TXT 导入短语到 .lex（相同拼音会替换现有条目）");
            cfg.AddCommand<DebugCommand>("debug")
               .WithDescription("显示调试信息，用于问题排查");
        });
        return app.Run(args);
    }
}

/// <summary>导出命令。</summary>
public sealed class ExportCommand : Command<ExportCommand.Settings>
{
    /// <summary>导出参数。</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>输出文件路径（TXT）。</summary>
        [CommandArgument(0, "<output>")]
        public required string Output { get; init; }

        /// <summary>可选：指定 .lex 文件路径；默认读取当前用户路径。</summary>
        [CommandOption("--lex")]
        public string? LexPath { get; init; }
    }

    /// <summary>执行导出逻辑。</summary>
    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var lexPath = settings.LexPath ?? LexPaths.GetUserLexPath();
        var service = new PinyinLexService(new LexFileReader());
        service.ExportAsync(lexPath, settings.Output).GetAwaiter().GetResult();
        return 0;
    }
}

/// <summary>导入命令。</summary>
public sealed class ImportCommand : Command<ImportCommand.Settings>
{
    /// <summary>导入参数。</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>输入 TXT 文件路径。</summary>
        [CommandArgument(0, "<input>")]
        public required string Input { get; init; }

        /// <summary>可选：指定 .lex 文件路径；默认读取当前用户路径。</summary>
        [CommandOption("--lex")]
        public string? LexPath { get; init; }

        /// <summary>禁用备份（默认会在写入前创建 .bak）。</summary>
        [CommandOption("--no-backup")]
        public bool NoBackup { get; init; }

        /// <summary>只校验不落盘。</summary>
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }

        /// <summary>输出更多详情。</summary>
        [CommandOption("--verbose")]
        public bool Verbose { get; init; }
    }

    /// <summary>执行导入逻辑。</summary>
    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var lexPath = settings.LexPath ?? LexPaths.GetUserLexPath();
        var service = new PinyinLexService(new LexFileReader());
        var doBackup = !settings.NoBackup;
        service.ImportAsync(lexPath, settings.Input, doBackup, settings.DryRun, settings.Verbose).GetAwaiter().GetResult();
        return 0;
    }
}

/// <summary>列出短语命令。</summary>
public sealed class ListCommand : Command<ListCommand.Settings>
{
    /// <summary>参数。</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>可选：按拼音过滤。</summary>
        [CommandOption("--filter")]
        public string? Filter { get; init; }

        /// <summary>可选：指定 .lex 文件路径；默认读取当前用户路径。</summary>
        [CommandOption("--lex")]
        public string? LexPath { get; init; }
    }

    /// <summary>执行列表逻辑。</summary>
    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
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
public sealed class DebugCommand : Command<DebugCommand.Settings>
{
    /// <summary>调试参数。</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>显示详细信息。</summary>
        [CommandOption("--verbose")]
        public bool Verbose { get; init; }
    }

    /// <summary>执行调试逻辑。</summary>
    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Console.WriteLine("=== PinyinLexTool 调试信息 ===");
        Console.WriteLine($"版本: 1.0.1");
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
