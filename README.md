### PinyinLexTool
微软拼音输入法自定义短语批量导入/导出工具

Windows 10/11 自带的微软拼音输入法（Microsoft Pinyin IME）没有提供官方的批量短语导入功能。
当你需要一次性维护几十上百条自定义短语时，手工逐条录入既耗时又容易出错。

PinyinLexTool 通过直接操作 `%APPDATA%\Microsoft\InputMethod\Chs\ChsPinyinEUDPv1.lex` 文件，
实现自定义短语的批量导入、导出与备份。

---

## 功能特性
- 批量导入/导出 TXT 短语（拼音 + 位置索引 + 输出文本）
- 从系统现有短语导出为 TXT
- 列出现有短语，支持按拼音过滤
- 同拼音下支持多候选条目
- 导入时自动备份原文件
- 支持干运行模式（仅校验不写入）
- 详细的导入统计与错误报告
- 支持 Windows 10 1709+ 与 Windows 11
- 绿色单文件，无需安装或更改输入法

---

## 安装与使用

### 获取
前往项目 Releases 下载可执行文件后解压使用。

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

- 参考了社区对微软拼音 `.lex` 文件格式的研究
- 借鉴了部分开源脚本与实现思路（如 Python 原型）

---

如果你有改进建议或遇到问题，欢迎提交 Issue 或 PR。


