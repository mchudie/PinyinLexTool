# PinyinLexTool

![Windows x64](https://img.shields.io/badge/Windows-x64-0078D4)
![.NET 11](https://img.shields.io/badge/.NET-11-512BD4)
![Native AOT](https://img.shields.io/badge/CLI-Native%20AOT-2E7D32)
[![MIT License](https://img.shields.io/badge/License-MIT-2E7D32)](LICENSE)
![Developed with Codex](https://img.shields.io/badge/Developed%20with-Codex-181717)

**轻量、开源的微软拼音自定义短语管理工具。**

当需要添加几十甚至上百条自定义短语时，在微软拼音中逐条录入既耗时又不方便。PinyinLexTool 支持通过 TXT 文件批量管理短语，提供图形界面（GUI）和命令行（CLI）两种使用方式。

[下载最新版本](https://github.com/mchudie/PinyinLexTool/releases/latest) · [历史版本](https://github.com/mchudie/PinyinLexTool/releases) · [问题反馈](https://github.com/mchudie/PinyinLexTool/issues)

## 功能特性

- 通过 TXT 文件**批量导入和导出**，便于编辑、迁移和备份自定义短语。
- **为同一拼音设置多个短语**及各自的候选位置。
- **查询已有短语**，支持按拼音筛选。
- 导入前默认**备份已有词库**。支持仅校验而不写入文件，并显示导入统计和跳过原因。
- WPF 图形界面适合日常使用。Native AOT 命令行版适合脚本与自动化，无需安装 .NET 运行时。

## 下载

| 版本 | 说明 | 下载 |
| --- | --- | --- |
| 图形界面（GUI） | 双击运行，需要 [.NET Desktop Runtime 11 x64](https://dotnet.microsoft.com/download/dotnet/11.0) | [PinyinLexToolUI.exe](https://github.com/mchudie/PinyinLexTool/releases/latest/download/PinyinLexToolUI.exe) |
| 命令行（CLI） | Native AOT 独立 EXE，约 4 MiB | [PinyinLexTool.exe](https://github.com/mchudie/PinyinLexTool/releases/latest/download/PinyinLexTool.exe) |

当前构建面向 Windows x64，适用于使用 `ChsPinyinEUDPv1.lex` 词库格式的微软拼音（简体中文）。Windows 支持范围以 Microsoft 的兼容列表和操作系统生命周期为准。旧版本的运行时要求请参阅对应 Release 的说明。[.NET 11 支持的 Windows 版本](https://github.com/dotnet/core/blob/main/release-notes/11.0/supported-os.md#windows)

## 使用方法

### 1. 准备短语

创建一个 UTF-8 编码的 `phrases.txt` 文件，每行依次写入 `拼音 位置 输出文本`。

```text
ni 1 你好
ni 2 您好
hao 1 好
clc 1 Claude Code
cgp 1 ChatGPT
pgs 2 PostgreSQL
mss 1 Microsoft SQL Server
rq 1 %yyyy%-%MM%-%dd%
```

| 字段 | 规则 |
| --- | --- |
| 拼音 | 1 到 32 个英文字母，可以以 `u` 或 `v` 开头。导入时统一转为小写 |
| 位置 | 候选位置为 `1` 至 `9` |
| 输出文本 | 最多 64 个 UTF-16 代码单元，可以包含空格 |

字段之间使用空白分隔，空行和以 `#` 开头的注释行会被跳过。日期模板（如 `rq` 对应的文本）会原样写入词库。

**默认导入会替换词库中同一拼音的旧条目。** 其他拼音不受影响。如果需要保留某个拼音的多个候选，请在 TXT 中同时提供这些条目。导出的 TXT 使用相同格式。

### 2. 图形界面

运行 `PinyinLexToolUI.exe`，然后按需导入、查询或导出。

- 在「导入短语」中选择 TXT 文件，也可以在未选择文件时直接粘贴短语文本。按需设置备份、仅校验和详细信息，再点击「导入」。
- 在「查看短语」中点击「查询」查看短语列表，也可以输入部分拼音进行筛选。
- 在「导出短语」中选择目标 TXT 文件并点击「导出」，也可以将导出内容复制到剪贴板。

### 3. 命令行

在 `PinyinLexTool.exe` 所在目录打开 PowerShell，按需运行以下命令。

```powershell
# 查看帮助
.\PinyinLexTool.exe --help

# 查询短语
.\PinyinLexTool.exe list
.\PinyinLexTool.exe list --filter clc

# 试运行（不修改词库）
.\PinyinLexTool.exe import .\phrases.txt --dry-run --verbose

# 导入短语（默认备份已有词库）
.\PinyinLexTool.exe import .\phrases.txt

# 导出短语
.\PinyinLexTool.exe export .\my_phrases.txt

# 查看诊断信息
.\PinyinLexTool.exe debug --verbose
```

| 参数 | 说明 |
| --- | --- |
| `--filter <拼音>` | `list` 按完整拼音筛选，不区分拼音大小写 |
| `--lex <路径>` | 为 `list`、`import` 或 `export` 指定词库文件 |
| `--dry-run` | `import` 仅校验，不写入、不创建备份 |
| `--replace-all` | `import` 全量替换词库，只保留本次 TXT 中的短语 |
| `--no-backup` | `import` 不创建备份 |
| `--verbose` | `import` 显示详细统计和跳过原因。`debug` 在成功读取默认词库时显示短语数量和最多前五条短语 |
| `--help` | 查看程序或指定命令的帮助 |

选项名使用小写，同一选项只传一次。命令执行成功返回 `0`，参数或执行错误返回 `-1`。

使用 `import --verbose` 会显示合规行、跳过原因和写入结果。

```text
读取完成：共 5 行，合规 3，跳过 2
第2行：拼音不合法
第4行：位置超出 1..9
导入完成：写入 3 条，覆盖 1 条，跳过 2 条
```

同时使用 `--dry-run` 时，最后一行会显示预期写入数量。

```text
干运行：将写入 3 条（未实际落盘）
```

#### 全量替换词库

当 TXT 文件代表完整词库时，可以使用 `--replace-all`。试运行会显示将移除和写入的条数。

```powershell
.\PinyinLexTool.exe import .\phrases.txt --replace-all --dry-run
.\PinyinLexTool.exe import .\phrases.txt --replace-all
```

正式替换时，词库只保留这次 TXT 中的短语，默认在同一目录保留原词库的备份。TXT 至少要包含一条有效短语，任一短语行未通过校验都会取消整个替换操作。

## 词库与注意事项

工具默认操作当前 Windows 用户的词库。

```text
%APPDATA%\Microsoft\InputMethod\Chs\ChsPinyinEUDPv1.lex
```

图形版可通过窗口顶部的「浏览」选择其他 `.lex` 文件。命令行版使用 `--lex` 指定路径。

```powershell
.\PinyinLexTool.exe import .\phrases.txt --lex 'C:\词库\短语.lex'
```

- 导入时默认备份已有词库，备份与原文件位于同一目录，例如 `ChsPinyinEUDPv1.20250109_143022.bak.lex`。
- 导入后若候选框未更新，可以尝试切换输入法或重开正在使用的应用。
- 如果系统同步覆盖或回滚了修改，请检查词库路径及微软拼音的同步设置。
- Windows 10 1607、1703 的早期微软拼音可能使用不同的词库格式，本工具不支持这类旧格式。

## 开发

### 项目结构

```text
PinyinLexTool/          CLI 命令行程序（System.CommandLine）
PinyinLexTool.WPF/      WPF 图形界面
PinyinLexTool.Core/     公共词库模型、文件读写及业务逻辑
```

CLI 与 WPF 共用 Core，避免重复实现词库处理逻辑。

### 构建与运行

开发需要 .NET 11 SDK。生成 Native AOT 版 CLI 还需要 Visual Studio 的「使用 C++ 的桌面开发」工作负载及 Windows SDK。[Native AOT 工具链](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=windows)

```powershell
git clone https://github.com/mchudie/PinyinLexTool.git
Set-Location PinyinLexTool
dotnet restore
dotnet build .\PinyinLexTool.sln -c Release

# 运行 CLI 或 GUI
dotnet run --project .\PinyinLexTool -- --help
dotnet run --project .\PinyinLexTool.WPF
```

### 发布构建

```powershell
# CLI：Native AOT 独立 EXE
dotnet publish .\PinyinLexTool\PinyinLexTool.csproj -c Release -r win-x64 --self-contained true

# GUI：框架依赖单文件
dotnet publish .\PinyinLexTool.WPF\PinyinLexTool.WPF.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

产物分别位于 `PinyinLexTool/bin/Release/net11.0/win-x64/publish/` 和 `PinyinLexTool.WPF/bin/Release/net11.0-windows/win-x64/publish/`。CLI 发布目录同时包含 `PinyinLexTool.pdb` 调试符号。

当前仓库未包含独立测试项目，可使用临时词库验证导入、导出、查询、自动备份及试运行，并检查图形界面的启动与操作。

后续计划加入 JSON 格式输出。

## AI 辅助开发

本项目最初使用 Cursor 辅助开发，自 `v1.0.2` 的开发与发布起，由 **OpenAI Codex** 辅助开发和持续维护。需求设计、技术选型及发布决策由维护者负责。AI 参与代码实现、问题分析、测试验证和文档维护。

欢迎通过 Issue 和 Pull Request 参与项目建设。

## 致谢

- 感谢 @xinshoushangdao 贡献 WPF 图形界面。[GitHub 主页](https://github.com/xinshoushangdao) · [PR #2](https://github.com/mchudie/PinyinLexTool/pull/2)
- 感谢所有提交 Issue、贡献代码和参与测试的用户。
- 感谢社区对微软拼音 `.lex` 文件格式的研究，以及相关开源脚本与 Python 原型提供的参考。

## 许可证

本项目采用 [MIT License](LICENSE)。
