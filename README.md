# PinyinLexTool

微软拼音自定义短语管理工具，支持命令行和图形界面两种使用方式。

本项目最初使用 Cursor 辅助开发。自 `v1.0.2` 的开发与发布起，使用 OpenAI Codex 辅助开发和持续维护，欢迎社区贡献。

## 项目结构

- **PinyinLexTool.Core**: 核心业务逻辑库，包含数据模型、文件读写和服务
- **PinyinLexTool**: 命令行界面 (CLI) 应用程序
- **PinyinLexTool.WPF**: Windows 图形界面 (GUI) 应用程序

## 功能特性

- 📝 **导入短语**: 从 TXT 文件导入自定义短语到微软拼音
- 📤 **导出短语**: 将当前自定义短语导出为 TXT 文件
- 🔍 **查看短语**: 列出现有短语，支持按拼音过滤
- 🖥️ **双界面**: 提供命令行和图形界面两种操作方式
- 🔧 **调试工具**: 内置调试命令，便于问题排查

## 使用方法

### 命令行界面 (CLI)

```bash
# 查看帮助
dotnet run --project PinyinLexTool -- --help

# 列出现有短语
dotnet run --project PinyinLexTool -- list

# 按拼音过滤
dotnet run --project PinyinLexTool -- list --filter "ni"

# 导出到文件
dotnet run --project PinyinLexTool -- export phrases.txt

# 从文件导入
dotnet run --project PinyinLexTool -- import phrases.txt

# 显示调试信息
dotnet run --project PinyinLexTool -- debug --verbose
```

请按示例使用小写选项名，同一选项只传一次。命令成功时返回 `0`，参数错误或执行失败时返回 `-1`。

### 图形界面 (GUI)

```bash
# 运行图形界面
dotnet run --project PinyinLexTool.WPF
```

图形界面提供直观的操作体验，支持：
- 点击按钮导入/导出短语
- 实时查看短语列表
- 按拼音搜索过滤

## 发布版本

### CLI 版本
```powershell
# 生成 Windows x64 Native AOT 独立可执行文件
dotnet publish PinyinLexTool/PinyinLexTool.csproj -c Release -r win-x64 --self-contained true
```

### GUI 版本
```powershell
# 生成 Windows x64 框架依赖的单文件可执行程序
dotnet publish PinyinLexTool.WPF/PinyinLexTool.WPF.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

发布后的可执行文件位于：
- CLI：`PinyinLexTool/bin/Release/net11.0/win-x64/publish/PinyinLexTool.exe`
- GUI：`PinyinLexTool.WPF/bin/Release/net11.0-windows/win-x64/publish/PinyinLexToolUI.exe`

## 系统要求

- 当前构建面向 Windows x64。Windows 11 与仍在支持期内的指定 Windows 10 版本以 Microsoft 列表为准。[.NET 11 支持的操作系统](https://github.com/dotnet/core/blob/main/release-notes/11.0/supported-os.md#windows)
- CLI 为 Native AOT 独立程序，从终端运行即可。GUI 需要 [.NET Desktop Runtime 11 x64](https://dotnet.microsoft.com/download/dotnet/11.0)。
- 微软拼音输入法

## 文件格式

### TXT 格式
每行一个短语，格式：`拼音 序号 短语内容`
```
ni 1 你好
hao 1 好
```

### .lex 格式
微软拼音自定义短语的二进制格式，由工具自动处理。

## 开发

从源码构建需要 .NET 11 SDK。生成 Windows x64 Native AOT 版 CLI 还需要 Visual Studio 的「使用 C++ 的桌面开发」工作负载及 Windows SDK。[Native AOT 工具链](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=windows)

```bash
# 克隆项目
git clone https://github.com/mchudie/PinyinLexTool.git
cd PinyinLexTool

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test
```

## 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件。

---

## 安装与使用

### 获取
前往项目 Releases 下载可执行文件后解压使用。
既有发布版本的运行时要求以对应 Release 的更新说明为准。

### 短语格式
准备一个 `phrases.txt` 文本文件，每行一个短语，格式为：

```
拼音  位置  输出文本
```

示例：

```
clc 1 Claude Code
cgp 1 ChatGPT
pgs 2 PostgreSQL
rq  1 %yyyy%-%MM%-%dd%
```

### 批量导入

#### 命令行方式

```powershell
# 基本导入（自动备份）
PinyinLexTool import phrases.txt

# 指定 .lex 文件路径
PinyinLexTool import phrases.txt --lex "C:\Users\用户名\AppData\Roaming\Microsoft\InputMethod\Chs\ChsPinyinEUDPv1.lex"

# 禁用备份
PinyinLexTool import phrases.txt --no-backup

# 干运行模式（仅校验，不写入）
PinyinLexTool import phrases.txt --dry-run --verbose

# 显示详细统计信息
PinyinLexTool import phrases.txt --verbose
```

#### 图形界面方式

1. 启动 PinyinLexToolUI 程序
2. 切换到"导入短语"选项卡
3. 点击"选择文件..."按钮选择要导入的TXT文件，或直接在文本框中输入短语内容
4. 根据需要设置"创建备份"、"仅校验"和"显示详细信息"选项
5. 点击"导入"按钮执行导入操作

导入完成后重新部署/切换输入法以生效。

### 批量导出

#### 命令行方式

```powershell
# 导出到指定文件
PinyinLexTool export my_phrases.txt

# 指定 .lex 文件路径
PinyinLexTool export my_phrases.txt --lex "C:\Users\用户名\AppData\Roaming\Microsoft\InputMethod\Chs\ChsPinyinEUDPv1.lex"
```

#### 图形界面方式

1. 启动 PinyinLexToolUI 程序
2. 切换到"导出短语"选项卡
3. 点击"选择文件..."按钮选择导出目标文件
4. 点击"导出"按钮执行导出操作
5. 导出完成后，内容会显示在文本框中，可以点击"复制到剪贴板"按钮复制内容

### 列出现有短语

#### 命令行方式

```powershell
# 列出所有短语
PinyinLexTool list

# 按拼音过滤
PinyinLexTool list --filter clc

# 指定 .lex 文件路径
PinyinLexTool list --lex "C:\Users\用户名\AppData\Roaming\Microsoft\InputMethod\Chs\ChsPinyinEUDPv1.lex"
```

#### 图形界面方式

1. 启动 PinyinLexToolUI 程序
2. 切换到"查看短语"选项卡
3. 可选：在"拼音过滤"文本框中输入要筛选的拼音
4. 点击"查询"按钮显示短语列表
5. 结果将以表格形式显示，包含拼音、位置和短语文本

### 查看帮助

```powershell
PinyinLexTool --help
PinyinLexTool import --help
PinyinLexTool export --help
PinyinLexTool list --help
```

---

## 注意事项

- **自动备份**：导入时默认会创建时间戳备份文件（如 `ChsPinyinEUDPv1.20250109_143022.bak.lex`）
- **拼音规则**：最多32个字母，不以 u/v 开头，大小写均可（内部统一转小写）
- **位置范围**：候选位置必须在 1-9 之间
- **短语长度**：输出文本最大64个字符
- **文件路径**：默认操作 `%APPDATA%\Microsoft\InputMethod\Chs\ChsPinyinEUDPv1.lex`
- 某些系统会启用"不同用户自定义短语隔离"，可能覆盖或回滚修改，建议关闭相关同步后使用
- 目前仅支持"中文（简体）- 微软拼音"，不支持其它输入法
- Windows 10 早期版本（1607/1703）可能使用不同的 `.lex` 文件格式，暂不兼容

---

## 导入统计示例

使用 `--verbose` 参数时，会显示详细的导入统计：

```
读取完成：共 5 行，合规 3，跳过 2
第2行：拼音不合法
第4行：位置超出 1..9
导入完成：写入 3 条，覆盖 1 条，跳过 2 条
```

使用 `--dry-run` 时，仅校验不写入：

```
读取完成：共 5 行，合规 3，跳过 2
第2行：拼音不合法
第4行：位置超出 1..9
干运行：将写入 3 条（未实际落盘）
```

---

## 使用场景示例

- 开发者：`clc -> Claude Code`，`cgp -> ChatGPT`
- 数据库工程师：`pgs -> PostgreSQL`，`mss -> Microsoft SQL Server`
- 时间日期模板：`rq -> %yyyy%-%MM%-%dd%`

---

## 规划

- [x] 导入功能
- [x] 导出功能
- [x] 列表功能
- [x] 自动备份
- [x] 干运行模式
- [x] 详细统计与错误报告
- [x] GUI 前端（WPF实现）
- [ ] 显示系统版本/诊断信息
- [ ] JSON 格式输出支持

---

## 许可协议

MIT License

---

## 致谢

- 感谢 @xinshoushangdao 贡献 WPF 图形界面。[PR #2](https://github.com/mchudie/PinyinLexTool/pull/2)
- 感谢所有提交 Issue、贡献代码和参与测试的用户。
- 参考了社区对微软拼音 `.lex` 文件格式的研究
- 借鉴了部分开源脚本与实现思路（如 Python 原型）

---

如果你有改进建议或遇到问题，欢迎提交 Issue 或 PR。


