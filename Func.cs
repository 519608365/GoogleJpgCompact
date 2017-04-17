using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GoogleJpgCompact {

    /// <summary>
    /// 
    /// </summary>
    public class CompactInfo {
        public static int TotalTaskCount = 0;
        public static int CompleteTaskCount = 0;
        public static int CurrentTaskCount = 0;
        public static int MaxThreadCount = 0;
        public static List<ListViewItem> SelectItems = new List<ListViewItem>();
    }

    public class Func {

        /// <summary>
        /// FillLvwFiles
        /// </summary>
        /// <param name="lvwFiles"></param>
        /// <param name="recursive"></param>
        /// <param name="filePath"></param>
        /// <param name="parentPath"></param>
        public static void FillLvwFiles(ListView lvwFiles, bool recursive, string filePath, string parentPath) {

            DirectoryInfo dir = new DirectoryInfo(filePath);

            FileInfo[] _files = dir.GetFiles();
            Array.Sort<FileInfo>(_files, new FileInfoComparer());

            foreach (FileInfo _file in _files) {
                if (_file.Extension.ToLower() == ".jpg") {
                    ListViewItem _lvi = new ListViewItem(parentPath + _file.Name);
                    _lvi.SubItems.Add(_file.Extension.ToLower());
                    _lvi.SubItems.Add(_file.Length.ToString());
                    _lvi.SubItems.Add(GetDisplaySize(_file.Length));
                    _lvi.SubItems.Add(string.Format("{0:yyyy-MM-dd HH:mm:ss}", _file.CreationTime));
                    _lvi.SubItems.Add(string.Format("{0:yyyy-MM-dd HH:mm:ss}", _file.LastWriteTime));
                    _lvi.SubItems.Add("未压缩");
                    _lvi.SubItems.Add(""); //压缩时间
                    _lvi.SubItems.Add(""); //压缩后大小
                    _lvi.SubItems.Add(""); //压缩比
                    lvwFiles.Items.Add(_lvi);

                }
            }

            if (!recursive) {
                return;
            }

            DirectoryInfo[] _dirs = dir.GetDirectories();
            Array.Sort<DirectoryInfo>(_dirs, new DirectoryInfoComparer());

            foreach (DirectoryInfo _dir in _dirs) {
                FillLvwFiles(lvwFiles, recursive, _dir.FullName, parentPath + _dir.Name + "\\");
            }
        }

        /// <summary>
        /// GetDisplaySize
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetDisplaySize(long size) {
            if (size < 100) {
                return size + " Byte";
            }
            else if (size < 10000) {
                return Math.Round(1.00 * size / 1024, 2) + " KB";
            }
            else {
                return Math.Round(1.00 * size / 1024 / 1024, 2) + " MB";
            }
        }

        /// <summary>
        /// GetDisplayTime
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string GetDisplayTime(int time) {
            string _msg = ":" + ((time % 60).ToString().Length == 1 ? "0" : "") + (time % 60).ToString();
            time = time / 60;
            _msg = ":" + ((time % 60).ToString().Length == 1 ? "0" : "") + (time % 60).ToString() + _msg;
            time = time / 60;
            _msg = ((time % 60).ToString().Length == 1 ? "0" : "") + (time % 60).ToString() + _msg;
            return _msg;
        }

        /// <summary>
        /// FileInfoComparer
        /// </summary>
        public class FileInfoComparer : IComparer<FileInfo> {
            public int Compare(FileInfo x, FileInfo y) {
                return x.Name.CompareTo(y.Name);
            }
        }

        /// <summary>
        /// DirectoryInfoComparer
        /// </summary>
        public class DirectoryInfoComparer : IComparer<DirectoryInfo> {
            public int Compare(DirectoryInfo x, DirectoryInfo y) {
                return x.Name.CompareTo(y.Name);
            }
        }

        /// <summary>
        /// GetSelectedFilesCount
        /// </summary>
        /// <returns></returns>
        public static int GetSelectedFilesCount(ListView lvwFiles) {
            int _count = 0;
            if (lvwFiles.SelectedItems != null) {
                _count = lvwFiles.SelectedItems.Count;
            }
            return _count;
        }

        /// <summary>
        /// GetRemainTime
        /// </summary>
        /// <returns></returns>
        public static int GetRemainTime() {
            int _maxCompactingSize = 0;
            int _totalNotCompactSize = 0;
            foreach (ListViewItem _lvi in CompactInfo.SelectItems) {
                int _size = Convert.ToInt32(_lvi.SubItems[2].Text);
                if (_lvi.SubItems[6].Text.StartsWith("压缩中")) {
                    if (_maxCompactingSize < _size) {
                        _maxCompactingSize = _size;
                    }
                }
                if (_lvi.SubItems[6].Text == "未压缩") {
                    _totalNotCompactSize += _size;
                }
            }

            int _remainSize = _maxCompactingSize + _totalNotCompactSize;
            int _remainSecond = Convert.ToInt32(_remainSize / 5000);
            return _remainSecond;
        }

        /// <summary>
        /// 执行Dos命令
        /// </summary>
        public static void RunCmd(string filePath, string args) {
            try {

                Cmn.Log.WriteToFile("RunCmd", filePath + " " + args);
                string _cmdFile = filePath;
                Process _process = new Process();
                _process.StartInfo.FileName = "cmd.exe";
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.CreateNoWindow = true;
                _process.StartInfo.RedirectStandardInput = true; //重定向输入（一定是true） 
                _process.StartInfo.RedirectStandardOutput = true; //重定向输出 
                _process.StartInfo.RedirectStandardError = true;

                if (_process.Start()) {
                    _process.StandardInput.WriteLine(filePath + " " + args);
                    //StreamReader sr = _process.StandardOutput;
                    //string s = sr.ReadToEnd();
                    //Cmn.Log.WriteToFile("RunCmd", s);

                    _process.StandardInput.WriteLine("exit");
                    StreamReader sr = _process.StandardOutput;
                    string s = sr.ReadToEnd();
                    Cmn.Log.WriteToFile("RunCmd", s);
                    _process.Close();
                }
            }
            catch (Exception ex) {
                Cmn.Log.WriteToFile("RunCmd", ex.Message + ex.StackTrace);
            }
        }

    }
}
