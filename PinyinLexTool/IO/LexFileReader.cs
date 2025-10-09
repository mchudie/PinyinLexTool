#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PinyinLexTool.Models;

namespace PinyinLexTool.IO;

/// <summary>.lex 文件读取器。</summary>
public sealed class LexFileReader
{
    private static readonly Encoding U16LE = Encoding.Unicode;

    /// <summary>读取 .lex 文件中的所有短语。</summary>
    /// <param name="lexPath">.lex 文件完整路径。</param>
    /// <returns>短语序列。</returns>
    public async Task<IReadOnlyList<PinyinPhrase>> ReadAllAsync(string lexPath)
    {
        // 解析思路来自你的 LINQPad 草稿：读取头、根据偏移表切分记录区，每条记录：
        // [16B header][pinyin UTF-16LE][00 00][phrase UTF-16LE][00 00]
        // 过滤“已删除”（seg[9] == 0 代表未删除），并解析候选 index 与 tail 不参与导出。

        byte[] data;
        await using (var fs = new FileStream(lexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            data = new byte[fs.Length];
            _ = await fs.ReadAsync(data.AsMemory(0, data.Length));
        }

        if (data.Length < 0x44) return System.Array.Empty<PinyinPhrase>();

        int phraseCount = ReadU32(data, 0x1C);
        if (phraseCount <= 0) return System.Array.Empty<PinyinPhrase>();

        int firstOffsetPos = 0x44; // 偏移表首 DWORD 位置
        int firstBlockPos = firstOffsetPos + 4 * (phraseCount - 1);

        var result = new List<PinyinPhrase>(capacity: phraseCount);
        int lastPos = 0;
        for (int i = 0; i < phraseCount; i++)
        {
            int blockPos = (i == phraseCount - 1) ? -1 : ReadU32(data, firstOffsetPos + i * 4);
            int blockLen = (blockPos == -1) ? -1 : (blockPos - lastPos);
            var seg = ReadSliceFromEnd(data, firstBlockPos + lastPos, blockLen);
            lastPos = blockPos;

            if (seg.Length < 16) continue;
            if (seg.Length > 9 && seg[9] != 0x00) continue; // 只保留未删除

            var body = seg.Skip(16).ToArray();
            var parts = SplitBy00(body, minPartBytes: 2);
            if (parts.Count < 2) continue;

            var pinyin = U16LE.GetString(parts[0]);
            var phrase = U16LE.GetString(parts[1]);

            int index = (int)ReadU32(seg, 6); // header [6..9] 为 index DWORD

            // 规范化：
            pinyin = pinyin.Trim();
            phrase = phrase.Replace("\r\n", "\n").Trim();
            if (string.IsNullOrWhiteSpace(pinyin) || string.IsNullOrWhiteSpace(phrase)) continue;

            result.Add(new PinyinPhrase(pinyin, index, phrase));
        }

        // 排序：按拼音、index
        result.Sort((a, b) =>
        {
            int c = string.Compare(a.Pinyin, b.Pinyin, System.StringComparison.Ordinal);
            if (c != 0) return c;
            return a.Index.CompareTo(b.Index);
        });

        return result;
    }

    private static List<byte[]> SplitBy00(byte[] buf, int minPartBytes)
    {
        var list = new List<byte[]>();
        var cur = new List<byte>();
        for (int i = 0; i + 1 < buf.Length; i += 2)
        {
            if (buf[i] == 0x00 && buf[i + 1] == 0x00)
            {
                if (cur.Count >= minPartBytes) list.Add(cur.ToArray());
                cur.Clear();
            }
            else { cur.Add(buf[i]); cur.Add(buf[i + 1]); }
        }
        if (cur.Count >= minPartBytes) list.Add(cur.ToArray());
        return list;
    }

    private static int ReadU32(IReadOnlyList<byte> buf, int off) => System.BitConverter.ToInt32(buf.ToArray(), off);

    private static byte[] ReadSliceFromEnd(byte[] data, int start, int len)
    {
        if (len < 0) return data[start..];
        return data[start..(start + len)];
    }
}


