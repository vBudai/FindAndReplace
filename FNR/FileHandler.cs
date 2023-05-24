using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace FNR
{
    internal class FileHandler
    {
        public double progress = 0;
        public string currentFile;
        public int fileNumber = 0;
        public event Action<double> ProgressChanged;
        public event Action<string> FileChanged;
        public event Action<int> FileNumChanged;
        private void OnProgressChanged(double progress)
        {
            var eh = ProgressChanged;
            if (eh != null)
            {
                eh(progress);
            }
        }
        private void OnFileNumChanged(int num)
        {
            var eh = FileNumChanged;
            if (eh != null)
            {
                eh(num);
            }
        }

        private void OnFileChanged(string filename)
        {
            var eh = FileChanged;
            if (eh != null)
            {
                eh(filename);
            }
        }

        public ObservableCollection<string> getDirectoryFiles(string targetDirectory, SearchType searchType, string[] excludeMasks, string mask = "*")
        {
            progress = 0;
            OnProgressChanged(progress);
            if (Directory.Exists(targetDirectory))
            {
                ObservableCollection<string> files = new ObservableCollection<string>();
                if (SearchType.NotRecursive == searchType)
                {
                    progress = 0;
                    string[] fileEntries = Directory.GetFiles(targetDirectory, mask);
                    double step = 100.0 / fileEntries.Length;
                    foreach (string fileName in fileEntries)
                    {
                        currentFile = fileName;
                        OnFileChanged(fileName);
                        files.Add(fileName);
                        progress += step;
                        OnProgressChanged(progress);
                    }

                    foreach (string emsk in excludeMasks)
                    {
                        if (emsk != "" && emsk != null)
                        {
                            var filesToExclude = Directory.GetFiles(targetDirectory, emsk);
                            foreach (var file in filesToExclude)
                            {
                                int index = files.IndexOf(file);
                                if (index != -1)
                                {
                                    files.RemoveAt(index);
                                }
                            }
                        }
                    }
                }
                else
                {
                    string[] fileEntries = Directory.GetFiles(targetDirectory, mask, SearchOption.AllDirectories);
                    double step = 100.0 / fileEntries.Length;
                    foreach (string fileName in fileEntries)
                    {
                        currentFile = fileName;
                        OnFileChanged(currentFile);
                        files.Add(fileName);
                        progress += step;
                        OnProgressChanged(progress);
                    }
                    foreach (string emsk in excludeMasks)
                    {
                        if (emsk != "" && emsk != null)
                        {
                            var filesToExclude = Directory.GetFiles(targetDirectory, emsk, SearchOption.AllDirectories);
                            foreach(var file in filesToExclude)
                            {
                                int index = files.IndexOf(file);
                                if (index != -1)
                                {
                                    files.RemoveAt(index);
                                }
                            }
                        }
                    }

                }
                return files;
            }
            else
            {
                return new ObservableCollection<string>();
            }
        }

        public ObservableCollection<string> findMatches(string findText, ObservableCollection<string> filenames, CancellationToken token)
        {
            progress = 0;
            OnProgressChanged(progress);
            ObservableCollection<string> result = new ObservableCollection<string>();
            double step = 100.0 / filenames.Count;
            int number = 1;
            foreach (string filename in filenames)
            {
                fileNumber = number; 
                currentFile = filename;
                OnFileChanged(currentFile);
                OnFileNumChanged(fileNumber);
                //var path = filename.Replace("\\", "/");
                ObservableCollection<string> data = getFileData(filename, "", token);
                foreach(string str in data)
                {
                    if (token != null)
                    {
                        if (token.IsCancellationRequested)
                            break;
                    }
                    if (str.Contains(findText))
                    {
                        result.Add(filename);
                        progress += step;
                        OnProgressChanged(progress);
                        break;
                    }
                }
                number++;
            }
            return result;
        }

        public ObservableCollection<string> getFileData(string filename, string findString, CancellationToken token)
        {
            currentFile = filename;
            OnFileChanged(currentFile);
            if (filename == null || filename == "")
                return new ObservableCollection<string>();
            progress = 0;
            OnProgressChanged(progress);
            var path = filename.Replace("\\", "/");
            if (findString == "")
            {
                ObservableCollection<string> res = new ObservableCollection<string>();
                using (StreamReader sr = new StreamReader(filename))
                {
                    while (sr.EndOfStream == false)
                    {
                        if(token != null)
                        {
                            if (token.IsCancellationRequested)
                                break;
                        }
                        var line = sr.ReadLine();
                        res.Add(line);
                        progress = ((double)sr.BaseStream.Position / sr.BaseStream.Length) * 100;
                        OnProgressChanged(progress);
                    }
                }
                return res;
            }
            else
            {
                ObservableCollection<string> res = new ObservableCollection<string>();
                using (StreamReader sr = new StreamReader(filename, System.Text.Encoding.ASCII))
                {
                    while (sr.EndOfStream == false)
                    {
                        var line = sr.ReadLine();
                        if (line.Contains(findString))
                            res.Add(line);
                        progress = ((double)sr.BaseStream.Position / sr.BaseStream.Length) * 100;
                        OnProgressChanged(progress);
                    }
                }
                return res;
            }
        }

        public void replaceString(string filename, string targetStr, string newString, CancellationToken token)
        {
            currentFile = filename;
            OnFileChanged(currentFile);
            if(targetStr != "")
            {
                ObservableCollection<string> newFileData = new ObservableCollection<string>();
                using (StreamReader sr = new StreamReader(filename))
                {
                    while (sr.EndOfStream == false)
                    {
                        if(token != null)
                        {
                            if (token.IsCancellationRequested)
                                break;
                        }
                        var line = sr.ReadLine();
                        if (line.Contains(targetStr))
                            newFileData.Add(line.Replace(targetStr, newString));
                        progress = ((double)sr.BaseStream.Position / sr.BaseStream.Length) * 100;
                        OnProgressChanged(progress);
                    }
                }
                File.Create(filename).Close();
                File.WriteAllLines(filename, newFileData);
                
            }
        }
    }
}
