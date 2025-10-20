using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PinyinLexTool.Models;

namespace PinyinLexTool.WPF.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _lexFilePath = string.Empty;
        private string _importFilePath = string.Empty;
        private string _exportFilePath = string.Empty;
        private string _importContent = string.Empty;
        private string _exportContent = string.Empty;
        private string _filterText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _createBackup = true;
        private bool _dryRun = false;
        private bool _verbose = true;

        public ObservableCollection<PinyinPhrase> PhraseList { get; } = new ObservableCollection<PinyinPhrase>();

        public string LexFilePath
        {
            get => _lexFilePath;
            set => SetProperty(ref _lexFilePath, value);
        }

        public string ImportFilePath
        {
            get => _importFilePath;
            set => SetProperty(ref _importFilePath, value);
        }

        public string ExportFilePath
        {
            get => _exportFilePath;
            set => SetProperty(ref _exportFilePath, value);
        }

        public string ImportContent
        {
            get => _importContent;
            set => SetProperty(ref _importContent, value);
        }

        public string ExportContent
        {
            get => _exportContent;
            set => SetProperty(ref _exportContent, value);
        }

        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool CreateBackup
        {
            get => _createBackup;
            set => SetProperty(ref _createBackup, value);
        }

        public bool DryRun
        {
            get => _dryRun;
            set => SetProperty(ref _dryRun, value);
        }

        public bool Verbose
        {
            get => _verbose;
            set => SetProperty(ref _verbose, value);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}