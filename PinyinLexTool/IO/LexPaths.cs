#nullable enable
using System;

namespace PinyinLexTool.IO;

/// <summary>.lex 路径工具。</summary>
public static class LexPaths
{
    /// <summary>获取当前用户的微软拼音自定义短语 .lex 文件路径。</summary>
    public static string GetUserLexPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return System.IO.Path.Combine(
            appData,
            "Microsoft",
            "InputMethod",
            "Chs",
            "ChsPinyinEUDPv1.lex"
        );
    }
}


