#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinyinLexTool.IO;

/// <summary>.lex 文件写入器（基于你的 LINQPad 逻辑移植）。</summary>
public sealed class LexFileWriter
{
    private static readonly Encoding U16LE = Encoding.Unicode;
    private static readonly byte[] PhraseSep = new byte[] { 0x00, 0x00 };

    /// <summary>插入或更新一批短语：同拼音覆盖旧记录。</summary>
    public async Task<int> UpsertAsync(string lexPath, IReadOnlyList<(string Pinyin, int Index, string Text)> items)
    {
        if (!File.Exists(lexPath)) InitLexFile(lexPath);

        var tailBytes = GetExistingTailBytesOrDefault(lexPath);
        var existing = ReadExistingRecords(lexPath, out var learnedTail);
        if (learnedTail is { Length: >= 2 }) tailBytes = learnedTail;

        // 移除将被覆盖的拼音，并统计覆盖数
        int overwritten = 0;
        var filtered = new List<Rec>(capacity: existing.Count + items.Count);
        foreach (var r in existing)
        {
            bool willBeReplaced = items.Any(n => BytesEqual(U16LE.GetBytes(n.Pinyin), r.PinyinBytes));
            if (willBeReplaced) { overwritten++; continue; }
            filtered.Add(r);
        }

        foreach (var n in items)
        {
            var pinyinBytes = U16LE.GetBytes(n.Pinyin);
            var phraseBytes = U16LE.GetBytes(n.Text);
            var header = BuildHeader(16 + pinyinBytes.Length + PhraseSep.Length, n.Index, tailBytes);
            filtered.Add(new Rec(false, pinyinBytes, header, phraseBytes));
        }

        // 排序保证稳定
        filtered.Sort((a, b) => CompareBytes(a.PinyinBytes, b.PinyinBytes));

        WriteAll(lexPath, filtered);
        await Task.CompletedTask;
        return overwritten;
    }

    private static ReadOnlySpan<byte> HeaderMagic => "mschxudp"u8; // ASCII
    private const int HeaderLen = 16 + 4;           // 20
    private const int Phrase64Pos = HeaderLen;      // 0x14
    private const int TotalBytesPos = HeaderLen + 4;// 0x18
    private const int PhraseCntPos = HeaderLen + 8; // 0x1C
    private const int PhraseLenFirstPos = PhraseCntPos + 40; // 0x44

    private sealed record Rec(bool IsOld, byte[] PinyinBytes, byte[] Header16, byte[] PhraseBytes);

    private static void InitLexFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs, U16LE, leaveOpen: true);

        var hdr = new List<byte>();
        hdr.AddRange(HeaderMagic);                 // 8B
        hdr.AddRange(new byte[] { 0x02, 0x00, 0x60, 0x00, 0x01, 0x00, 0x00, 0x00 }); // 8B
        hdr.AddRange(new byte[] { 0x40, 0, 0, 0, 0x40, 0, 0, 0, 0, 0, 0, 0 }); // 12B
        bw.Write(hdr.ToArray());                   // 28B
        bw.Write(new byte[] { 0, 0, 0, 0 });
        bw.Write(new byte[] { 0x38, 0xD2, 0xA3, 0x65 });
        bw.Write(new byte[32]); // 总 68B
    }

    private static List<Rec> ReadExistingRecords(string path, out byte[]? learnedTail)
    {
        learnedTail = null;
        var list = new List<Rec>();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var br = new BinaryReader(fs, U16LE, leaveOpen: true);
        var data = br.ReadBytes((int)fs.Length);
        if (data.Length < PhraseLenFirstPos) return list;

        int phraseCnt = ReadU32(data, PhraseCntPos);
        if (phraseCnt <= 0) return list;

        int tableStart = PhraseLenFirstPos;
        int firstBlockPos = PhraseLenFirstPos + 4 * (phraseCnt - 1);
        int lastPos = 0;
        for (int i = 0; i < phraseCnt; i++)
        {
            int blockPos = (i == phraseCnt - 1) ? -1 : ReadU32(data, PhraseLenFirstPos + i * 4);
            int blockLen = (blockPos == -1) ? -1 : (blockPos - lastPos);
            var seg = ReadSliceFromEnd(data, firstBlockPos + lastPos, blockLen);
            lastPos = blockPos;

            if (seg.Length < 16) continue;
            learnedTail ??= seg.Skip(14).Take(2).ToArray();
            if (seg.Length > 9 && seg[9] == 0x00)
            {
                var body = seg.Skip(16).ToArray();
                var parts = SplitBy00(body, 2);
                if (parts.Count >= 2)
                {
                    list.Add(new Rec(true, parts[0], seg.Take(16).ToArray(), parts[1]));
                }
            }
        }
        return list;
    }

    private static void WriteAll(string path, List<Rec> records)
    {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            fs.SetLength(PhraseLenFirstPos);
        }

        int tolast = 0;
        int totalSize = PhraseLenFirstPos;

        using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        using (var bw = new BinaryWriter(fs, U16LE, leaveOpen: true))
        {
            fs.Position = PhraseLenFirstPos;
            for (int i = 0; i < records.Count - 1; i++)
            {
                int phraseLen = records[i].Header16.Length
                              + records[i].PinyinBytes.Length
                              + PhraseSep.Length
                              + records[i].PhraseBytes.Length
                              + PhraseSep.Length;
                tolast += phraseLen;
                bw.Write(BitConverter.GetBytes(tolast));
                totalSize += PhraseSep.Length * 2;
            }

            foreach (var r in records)
            {
                bw.Write(r.Header16);
                bw.Write(r.PinyinBytes); bw.Write(PhraseSep);
                bw.Write(r.PhraseBytes); bw.Write(PhraseSep);
                totalSize += r.Header16.Length + r.PinyinBytes.Length + r.PhraseBytes.Length + PhraseSep.Length * 2;
            }
        }

        ReplaceBytes(path, Phrase64Pos, BitConverter.GetBytes(64 + records.Count * 4));
        ReplaceBytes(path, PhraseCntPos, BitConverter.GetBytes(records.Count));
        ReplaceBytes(path, TotalBytesPos, BitConverter.GetBytes(totalSize));
    }

    private static byte[] GetExistingTailBytesOrDefault(string lexPath)
    {
        try
        {
            var recs = ReadExistingRecords(lexPath, out var tail);
            if (tail is { Length: >= 2 }) return tail;
        }
        catch { }
        return new byte[] { 0xA5, 0x2C };
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

    private static int CompareBytes(byte[] a, byte[] b)
    {
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++) { int d = a[i].CompareTo(b[i]); if (d != 0) return d; }
        return a.Length.CompareTo(b.Length);
    }

    private static bool BytesEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    private static byte[] ReadSliceFromEnd(byte[] data, int start, int len)
    {
        if (len < 0) return data[start..];
        return data[start..(start + len)];
    }

    private static int ReadU32(byte[] buf, int off) => BitConverter.ToInt32(buf, off);
    private static void WriteU16(byte[] buf, int off, int v) => BitConverter.GetBytes((ushort)v).CopyTo(buf, off);
    private static void WriteU32(byte[] buf, int off, uint v) => BitConverter.GetBytes(v).CopyTo(buf, off);

    private static byte[] BuildHeader(int headerPinyinLen, int index, ReadOnlySpan<byte> tail)
    {
        var h = new byte[16];
        WriteU16(h, 0, 0x0010);
        WriteU16(h, 2, 0x0010);
        WriteU16(h, 4, checked((ushort)headerPinyinLen));
        WriteU32(h, 6, (uint)index);
        WriteU16(h, 10, 0x0006);
        WriteU16(h, 12, 0x0000);
        h[14] = tail.Length > 0 ? tail[0] : (byte)0xA5;
        h[15] = tail.Length > 1 ? tail[1] : (byte)0x2C;
        return h;
    }

    private static void ReplaceBytes(string path, int position, byte[] value)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        fs.Position = position;
        fs.Write(value, 0, value.Length);
    }
}


