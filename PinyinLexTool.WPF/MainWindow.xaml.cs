using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PinyinLexTool.IO;
using PinyinLexTool.Models;
using PinyinLexTool.WPF.ViewModels;

namespace PinyinLexTool.WPF;

/// <summary>
/// 微软拼音自定义短语工具主窗口
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化ViewModel
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        // 加载默认词库路径
        _viewModel.LexFilePath = LexPaths.GetUserLexPath();
        _viewModel.StatusMessage = "就绪";
    }

    private void BrowseLexFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "词库文件 (*.lex)|*.lex|所有文件 (*.*)|*.*",
            Title = "选择词库文件"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.LexFilePath = dialog.FileName;
        }
    }

    private void BrowseImportFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Title = "选择导入文件"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ImportFilePath = dialog.FileName;
            try
            {
                _viewModel.ImportContent = File.ReadAllText(dialog.FileName);
                _viewModel.StatusMessage = $"已加载导入文件: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "读取文件失败";
            }
        }
    }

    private void BrowseExportFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Title = "选择导出文件",
            DefaultExt = ".txt"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportFilePath = dialog.FileName;
            _viewModel.StatusMessage = $"已选择导出文件: {Path.GetFileName(dialog.FileName)}";
        }
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.ImportContent))
        {
            MessageBox.Show("请先输入或加载要导入的短语内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _viewModel.StatusMessage = "正在导入...";
            this.Cursor = System.Windows.Input.Cursors.Wait;

            // 如果是直接输入的内容，先保存到临时文件
            string tempFile = null;
            string importPath = _viewModel.ImportFilePath;
            
            if (string.IsNullOrEmpty(importPath) || !File.Exists(importPath))
            {
                tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, _viewModel.ImportContent);
                importPath = tempFile;
            }

            // 创建服务并执行导入
            var reader = new LexFileReader();
            var service = new PinyinLexService(reader);
            
            // 捕获控制台输出
            using (var consoleOutput = new StringWriter())
            {
                Console.SetOut(consoleOutput);
                
                await service.ImportAsync(
                    _viewModel.LexFilePath,
                    importPath,
                    _viewModel.CreateBackup,
                    _viewModel.DryRun,
                    _viewModel.Verbose);
                
                // 显示结果
                string result = consoleOutput.ToString();
                _viewModel.StatusMessage = _viewModel.DryRun ? "导入验证完成" : "导入完成";
                MessageBox.Show(result, "导入结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // 清理临时文件
            if (tempFile != null && File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.StatusMessage = "导入失败";
        }
        finally
        {
            this.Cursor = null;
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.ExportFilePath))
        {
            MessageBox.Show("请先选择导出文件路径", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _viewModel.StatusMessage = "正在导出...";
            this.Cursor = System.Windows.Input.Cursors.Wait;

            // 创建服务并执行导出
            var reader = new LexFileReader();
            var service = new PinyinLexService(reader);
            
            await service.ExportAsync(_viewModel.LexFilePath, _viewModel.ExportFilePath);
            
            // 读取导出内容并显示
            _viewModel.ExportContent = File.ReadAllText(_viewModel.ExportFilePath);
            _viewModel.StatusMessage = "导出完成";
            
            MessageBox.Show($"已成功导出到: {_viewModel.ExportFilePath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.StatusMessage = "导出失败";
        }
        finally
        {
            this.Cursor = null;
        }
    }

    private void CopyExport_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_viewModel.ExportContent))
        {
            Clipboard.SetText(_viewModel.ExportContent);
            _viewModel.StatusMessage = "已复制到剪贴板";
        }
    }

    private async void List_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.StatusMessage = "正在查询...";
            this.Cursor = System.Windows.Input.Cursors.Wait;

            // 创建服务并查询短语
            var reader = new LexFileReader();
            var phrases = await reader.ReadAllAsync(_viewModel.LexFilePath);
            
            // 应用过滤
            if (!string.IsNullOrEmpty(_viewModel.FilterText))
            {
                phrases = phrases.Where(p => p.Pinyin.Contains(_viewModel.FilterText.ToLowerInvariant())).ToList();
            }
            
            // 更新列表
            _viewModel.PhraseList.Clear();
            foreach (var phrase in phrases.OrderBy(p => p.Pinyin).ThenBy(p => p.Index))
            {
                _viewModel.PhraseList.Add(phrase);
            }
            
            _viewModel.StatusMessage = $"查询完成，共 {phrases.Count} 条短语";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.StatusMessage = "查询失败";
        }
        finally
        {
            this.Cursor = null;
        }
    }
}