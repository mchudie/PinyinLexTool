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
