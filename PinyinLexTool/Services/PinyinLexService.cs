#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PinyinLexTool.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PinyinLexTool;

/// <summary>拼音短语导入/导出服务。</summary>
public sealed class PinyinLexService
{
    private readonly LexFileReader _reader;

    /// <summary>创建服务。</summary>
    public PinyinLexService(LexFileReader reader) => _reader = reader;

    /// <summary>将 .lex 中的短语导出为 TXT 文件。</summary>
    /// <param name="lexPath">.lex 路径。</param>
    /// <param name="outputPath">输出 TXT 路径。</param>
    public async Task ExportAsync(string lexPath, string outputPath)
    {
        var phrases = await _reader.ReadAllAsync(lexPath);

        var lines = phrases
            .OrderBy(p => p.Pinyin, System.StringComparer.Ordinal)
            .ThenBy(p => p.Index)
            .Select(p => $"{p.Pinyin} {p.Index} {p.Text}");

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllLinesAsync(outputPath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>从 TXT 导入短语到 .lex 文件（相同拼音会替换现有条目）。</summary>
    /// <param name="lexPath">.lex 文件路径。</param>
    /// <param name="inputPath">输入 TXT，格式：拼音 空格 索引 空格 文本。</param>
    public async Task ImportAsync(string lexPath, string inputPath, bool backup, bool dryRun = false, bool verbose = false)
    {
        var text = await File.ReadAllTextAsync(inputPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var lines = text.Replace("\r\n", "\n").Split('\n');

        int total = 0, ok = 0, skipped = 0;
        var reasons = new List<string>();
        var items = new List<(string Pinyin, int Index, string Text)>();
        foreach (var raw in lines)
        {
            total++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var m = Regex.Match(line, "^\\s*(\\S+)\\s+(\\d+)\\s+(.+?)\\s*$");
            if (!m.Success) { skipped++; reasons.Add($"第{total}行：格式不匹配"); continue; }
            var pinyin = m.Groups[1].Value.Trim();
            var indexStr = m.Groups[2].Value;
            var textPart = m.Groups[3].Value.Trim();

            if (!int.TryParse(indexStr, out var index)) { skipped++; reasons.Add($"第{total}行：位置不是数字"); continue; }
            if (!ValidatePinyin(pinyin)) { skipped++; reasons.Add($"第{total}行：拼音不合法"); continue; }
            if (index < 1 || index > 9) { skipped++; reasons.Add($"第{total}行：位置超出 1..9"); continue; }
            if (textPart.Length == 0 || textPart.Length > 64) { skipped++; reasons.Add($"第{total}行：短语长度超限"); continue; }

            items.Add((pinyin.ToLowerInvariant(), index, textPart));
            ok++;
        }

        if (verbose)
        {
            System.Console.WriteLine($"读取完成：共 {total} 行，合规 {ok}，跳过 {skipped}");
            foreach (var r in reasons) System.Console.WriteLine(r);
        }

        if (dryRun)
        {
            System.Console.WriteLine($"干运行：将写入 {ok} 条（未实际落盘）");
            return;
        }

        if (backup && File.Exists(lexPath))
        {
            var dir = Path.GetDirectoryName(lexPath)!;
            var name = Path.GetFileNameWithoutExtension(lexPath);
            var ext = Path.GetExtension(lexPath);
            var ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var bak = Path.Combine(dir, $"{name}.{ts}.bak{ext}");
            File.Copy(lexPath, bak, overwrite: false);
        }

        var writer = new LexFileWriter();
        int overwritten = await writer.UpsertAsync(lexPath, items);

        System.Console.WriteLine($"导入完成：写入 {ok} 条，覆盖 {overwritten} 条，跳过 {skipped} 条");
    }

    /// <summary>校验拼音：最多32个小写字母，且不以 u/v 开头。</summary>
    private static bool ValidatePinyin(string pinyin)
    {
        if (pinyin.Length == 0 || pinyin.Length > 32) return false;
        if (pinyin.StartsWith("u") || pinyin.StartsWith("v")) return false;
        for (int i = 0; i < pinyin.Length; i++)
        {
            char c = pinyin[i];
            if (c >= 'A' && c <= 'Z') continue; // 允许后续统一转小写
            if (c >= 'a' && c <= 'z') continue;
            return false;
        }
        return true;
    }
}


