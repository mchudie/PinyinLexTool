#nullable enable
namespace PinyinLexTool.Models;

/// <summary>表示一个自定义短语条目。</summary>
public sealed record PinyinPhrase(
    string Pinyin,
    int Index,
    string Text
);


