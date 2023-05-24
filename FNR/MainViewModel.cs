using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace FNR
{
    enum SearchType { Recursive = 1, NotRecursive}
    internal class MainViewModel : INotifyPropertyChanged
    {
        private CancellationTokenSource _source = new CancellationTokenSource();
        private CancellationToken _token;
        FileHandler _fileHandler = new FileHandler();
        public event PropertyChangedEventHandler PropertyChanged;
        public ICommand IBrowseCommand { get; }
        public ICommand IFindCommand { get; }
        public ICommand IReplaceCommand { get; }
        public ICommand ICancelCommand { get; }
        private string _path = "";
        private bool _searchTypeRecursive = false;
        private ObservableCollection<string> _filenames = new ObservableCollection<string>();
        private string _findText = "";
        private string _replaceText = "";
        private string _selectedFilename = "";
        private double _progress = 100;
        private ObservableCollection<string> _previewData = new ObservableCollection<string>();
        private string _currentFile;
        private string _mask = "";
        private string[] _excludeMask = new string[10];
        private bool _useMask = false;
        private bool _useExcludeMask = true;
        private bool _unblockInterface = true;
        private int _currentfileNum = 0;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool SearchTypeRecursive 
        {
            get { return _searchTypeRecursive; }
            set 
            { 
                if(_searchTypeRecursive != value)
                {
                    _searchTypeRecursive = value;
                }
                FilePath = _path;
                OnPropertyChanged(nameof(SearchTypeRecursive));
            }
        }

        public string FindText
        {
            get { return _findText; }
            set
            {
                Console.WriteLine(value);
                _findText = value;
                OnPropertyChanged(nameof(FindText));
            }
        }

        public string ReplaceText
        {
            get { return _replaceText; }
            set
            {
                Console.WriteLine(value);
                _replaceText = value;
                OnPropertyChanged(nameof(ReplaceText));
            }
        }


        public int FilesCount
        {
            get { return _filenames.Count; }
        }

        public string FilePath
        {
            get { return _path; }
            set
            {
                _path = value;
                FileNames = _fileHandler.getDirectoryFiles(FilePath, SearchTypeRecursive? SearchType.Recursive: SearchType.NotRecursive, UseExcludeMask? _excludeMask: new string[0]{}, UseMask? Mask:"*");
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string SelectedFilename
        {
            get 
            {
                return _selectedFilename; 
            }
            set
            {
                _selectedFilename = value;
                _source = new CancellationTokenSource();
                _token = _source.Token;
                Task.Run(() =>
                {
                    UnblockInterface = false;
                    GetFileData(_token);
                }, _token).ContinueWith(x => { UnblockInterface = true; });
                OnPropertyChanged(nameof(SelectedFilename));
                OnPropertyChanged(nameof(CurrentFilename));    
            }
        }
        private void GetFileData(CancellationToken token)
        {
            PreviewFile = _fileHandler.getFileData(SelectedFilename, FindText, _token);
            OnPropertyChanged(nameof(PreviewFile));
        }
        public ObservableCollection<string> PreviewFile
        {
            get 
            { 
                if(SelectedFilename != "" && SelectedFilename != null)
                {
                    return _previewData;
                }
                else
                {
                    return new ObservableCollection<string>();
                }
            }
            set
            {
                if (_previewData == value)
                    return;
                _previewData = value;
                OnPropertyChanged(nameof(PreviewFile));
            }
        }

        public ObservableCollection<string> FileNames
        {
            get { return _filenames; }
            set
            {
                if(_filenames != value)
                {
                    _filenames = value;
                    _currentfileNum = value.Count;
                }
                OnPropertyChanged(nameof(FileNames));
                OnPropertyChanged(nameof(FilesCount));
                OnPropertyChanged(nameof(FilesProgress));
            }
        }
        public MainViewModel()
        {
            _excludeMask[0] = "*.exe";
            _excludeMask[1] = "*.dll";
            _fileHandler.ProgressChanged += ProgressChanged;
            _fileHandler.FileChanged += FileChanged;
            _fileHandler.FileNumChanged += FileNumberChanged;
            IBrowseCommand = new RelayCommand<string>(x =>
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "Choose directory";
                dialog.ShowNewFolderButton = true;
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    FilePath = dialog.SelectedPath;
                }
            });

            IFindCommand = new RelayCommand<string>(x =>
            {
                _source = new CancellationTokenSource();
                _token = _source.Token;
                FilePath = _path;
                Console.WriteLine(Mask);
                if (FilePath == "")
                {
                    IBrowseCommand.Execute(null);
                }
                Task.Run(() =>
                {
                    UnblockInterface = false;
                    if (!String.IsNullOrWhiteSpace(FindText))
                    {   
                        FileNames = _fileHandler.findMatches(FindText, FileNames, _token);
                    }
                    SelectedFilename = SelectedFilename;
                }, _token).ContinueWith(y => {
                    UnblockInterface = true;
                    _currentfileNum = FilesCount;
                    _source = new CancellationTokenSource();
                    OnPropertyChanged(nameof(FilesProgress));
                });
            });

            IReplaceCommand = new RelayCommand<string>(x =>
            {
                _source = new CancellationTokenSource();
                _token = _source.Token;
                Task.Run(() =>
                {
                    UnblockInterface = false;
                    foreach (string filename in FileNames)
                    {
                        _fileHandler.replaceString(filename, FindText, ReplaceText, _token);
                    }
                }, _token).ContinueWith(y => { UnblockInterface = true; });
            });

            ICancelCommand = new RelayCommand<string>(x =>
            {
                if (UnblockInterface == true)
                {
                    Console.WriteLine("Interfase is not blocked");
                    return;
                }
                if (_source != null)
                {
                    Console.WriteLine("Cancel");
                    _source.Cancel();
                }
            });
        }

        public string CurrentFilename
        {
            get { return _currentFile; }
            set
            {
                Console.WriteLine(value);
                _currentFile = value;
                OnPropertyChanged(nameof(CurrentFilename));
            }
        }

        public string Mask
        {
            get
            {
                if (!UseMask)
                {
                    _mask = "*";
                }
                return _mask; 
            }
            set
            {
                _mask = value;
                OnPropertyChanged(nameof(Mask));
            }
        }

        public string ExcludeMask
        {
            get
            {
                string res = "";
                foreach(string mask in _excludeMask)
                {
                    if(!String.IsNullOrEmpty(mask))
                        res += mask + ", ";
                }
                return res;
            }
            set
            {
                var masks = new ObservableCollection<string>();
                _excludeMask = value.Replace(" ", "").Split(',');
                OnPropertyChanged(nameof(ExcludeMask));
            }
        }

        private void ProgressChanged(double progress)
        {
            Progress = progress;
        }

        private void FileChanged(string filename)
        {
            CurrentFilename = filename;
        }

        private void FileNumberChanged(int num)
        {
            CurrentFileNum = num;
        }
        public double Progress
        {
            get { return Math.Round(_progress); }
            set
            {
                _progress = value;
                if(_progress >= 100)
                {
                    _progress = 100;
                }
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(ProgressPercent));
            }
        }
        public string ProgressPercent
        {
            get { return Progress.ToString() + "%"; }
        }

        public bool UseMask
        {
            get { return _useMask;}
            set
            {
                _useMask = value;
                OnPropertyChanged(nameof(UseMask));
            }
        }

        public bool UseExcludeMask
        {
            get { return _useExcludeMask; }
            set
            {
                _useExcludeMask = value;
                OnPropertyChanged(nameof(UseExcludeMask));
            }
        }

        public bool UnblockInterface
        {
            get { return _unblockInterface; }
            set {
                _unblockInterface = value;
                OnPropertyChanged(nameof(UnblockInterface));
            }
        }

        public int CurrentFileNum
        {
            get { return _currentfileNum; }
            set {
                _currentfileNum = value;
                OnPropertyChanged(nameof(CurrentFileNum));
                OnPropertyChanged(nameof(FilesProgress));
            }
        }

        public string FilesProgress
        {
            get { return CurrentFileNum.ToString() + " / " + FilesCount; }
        }
    }
}
