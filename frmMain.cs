using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GoogleJpgCompact {
    public partial class frmMain : Form {
        public bool _IsDebug = false;

        public frmMain() {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMain_Load(object sender, EventArgs e) {
            Form.CheckForIllegalCrossThreadCalls = false;
            if (_IsDebug) {
                txtOrgPath_TextChanged(sender, e);
            }
            else {
                txtOrgPath.Text = "";
                txtTargetPath.Text = "";
            }
        }

        /// <summary>
        /// btnBrowser1_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowser1_Click(object sender, EventArgs e) {
            string _filePath = txtOrgPath.Text.Trim();
            if (_filePath != "" && Directory.Exists(_filePath)) {
                folderBrowserDialog1.SelectedPath = _filePath;
            }
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                txtOrgPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        /// <summary>
        /// btnBrowser2_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowser2_Click(object sender, EventArgs e) {
            string _filePath = txtTargetPath.Text.Trim();
            if (_filePath != "" && Directory.Exists(_filePath)) {
                folderBrowserDialog1.SelectedPath = _filePath;
            }
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                txtTargetPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        /// <summary>
        /// btnStart_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e) {
            int _count = Func.GetSelectedFilesCount(lvwFiles);
            if (_count <= 0) {
                MessageBox.Show("未选择要压缩的jpg文件！");
                return;
            }

            string _filePath = txtTargetPath.Text.Trim();
            if (!Directory.Exists(_filePath)) {
                MessageBox.Show("未指定导出路径！");
                return;
            }
            // 初始化
            CompactInfo.TotalTaskCount = _count;
            CompactInfo.CompleteTaskCount = 0;
            CompactInfo.CurrentTaskCount = 0;
            CompactInfo.MaxThreadCount = Convert.ToInt32(cmbThreadQty.Text);

            CompactInfo.SelectItems.Clear();
            foreach (ListViewItem _lvi in lvwFiles.SelectedItems) {
                CompactInfo.SelectItems.Add(_lvi);
            }
            SetControlState(true);
        }

        /// <summary>
        /// SetControlState
        /// </summary>
        /// <param name="enable"></param>
        private void SetControlState(bool enable) {

            timer1.Enabled = enable;
            btnStart.Enabled = !enable;
            txtOrgPath.Enabled = !enable;
            txtTargetPath.Enabled = !enable;
            chkAll.Enabled = !enable;
            cmbThreadQty.Enabled = !enable;
            chkRecursive.Enabled = !enable;
        }

        /// <summary>
        /// timer1_Tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e) {

            // 压缩中状态动态化
            foreach (ListViewItem _lvi in CompactInfo.SelectItems) {
                if (_lvi.SubItems[6].Text.StartsWith("压缩中")) {
                    if (_lvi.SubItems[6].Text == "压缩中") {
                        _lvi.SubItems[6].Text = "压缩中.";
                    }
                    else if (_lvi.SubItems[6].Text == "压缩中.") {
                        _lvi.SubItems[6].Text = "压缩中..";
                    }
                    else if (_lvi.SubItems[6].Text == "压缩中..") {
                        _lvi.SubItems[6].Text = "压缩中...";
                    }
                    else {
                        _lvi.SubItems[6].Text = "压缩中";
                    }
                }
            }

            if (CompactInfo.TotalTaskCount > CompactInfo.CurrentTaskCount + CompactInfo.CompleteTaskCount
                && CompactInfo.CurrentTaskCount < CompactInfo.MaxThreadCount) {
                Thread _thread = new Thread(new ParameterizedThreadStart(DoCompact));
                ListViewItem _lvi = CompactInfo.SelectItems[CompactInfo.CurrentTaskCount + CompactInfo.CompleteTaskCount];
                string _relativeFilePath = _lvi.Text;
                _lvi.SubItems[6].Text = "压缩中";
                _thread.Start(txtOrgPath.Text.Trim() + "|" + txtTargetPath.Text.Trim() + "|" + _relativeFilePath + "|" + Application.StartupPath + "\\guetzli\\" + "guetzli_windows_x86-64.exe" + "|" + _lvi.SubItems[2].Text);
                CompactInfo.CurrentTaskCount++;
                int _remainSecond = Func.GetRemainTime();
                lblMsg3.Text = "预计剩余时间：" + Func.GetDisplayTime(_remainSecond) + "。";
            }
        }
        public delegate void CompleteCompactInvoke(string relativeFilePath, int executeTime);

        public void DoCompact(object compactInfo) {

            string _compactInfo = compactInfo.ToString();
            //压缩图片目录
            string _orgPath = _compactInfo.Split('|')[0];
            //导出目录
            string _targetPath = _compactInfo.Split('|')[1];
            //相对路径（包含文件名）
            string _relativeFilePath = _compactInfo.Split('|')[2];
            //guetzli文件路径（包含文件名）
            string _guetzliFilePath = _compactInfo.Split('|')[3];
            //文件大小
            int _size = Convert.ToInt32(_compactInfo.Split('|')[4]);

            if (!_orgPath.EndsWith("\\")) {
                _orgPath += "\\";
            }

            if (!_targetPath.EndsWith("\\")) {
                _targetPath += "\\";
            }

            if (_relativeFilePath.StartsWith("\\")) {
                _relativeFilePath = _relativeFilePath.Substring(1);
            }

            //递推创建导出目录
            string _tmp = _targetPath;
            string[] _tmps = _relativeFilePath.Split('\\');
            for (int _i = 0; _i < _tmps.Length - 1; _i++) {
                _tmp += _tmps[_i] + "\\";
                if (!Directory.Exists(_tmp)) {
                    Directory.CreateDirectory(_tmp);
                }
            }
            string _args = "\"" + _orgPath + _relativeFilePath + "\"" + " " + "\"" + _targetPath + _relativeFilePath + "\"";

            int _retry = 0;
            DateTime _time = DateTime.Now;
            while (!File.Exists(_targetPath + _relativeFilePath) && _retry++ < 10) {
                _time = DateTime.Now;
                Func.RunCmd(_guetzliFilePath, _args);
                Thread.Sleep(3000);
            }
            
            int _executeTime = Convert.ToInt32((DateTime.Now - _time).TotalSeconds);
            Cmn.Log.WriteToFile("ExcuteTime", _args + "\n" + _size + ":" + (DateTime.Now - _time).TotalSeconds);
            CompleteCompactInvoke _completeCompact = new CompleteCompactInvoke(CompleteCompact);
            BeginInvoke(_completeCompact, new object[] { _relativeFilePath, _executeTime });
        }

        /// <summary>
        /// CompleteCompact
        /// </summary>
        /// <param name="param1"></param>
        public void CompleteCompact(string relativeFilePath, int executeTime) {
            CompactInfo.CurrentTaskCount--;
            CompactInfo.CompleteTaskCount++;

            lblMsg2.Text = "压缩进度：" + Math.Round(1.00 * CompactInfo.CompleteTaskCount / CompactInfo.TotalTaskCount, 4) * 100 + "%。";

            foreach (ListViewItem _lvi in CompactInfo.SelectItems) {
                if (_lvi.Text == relativeFilePath) {
                    _lvi.SubItems[6].Text = "压缩完成";

                    string _targetPath = txtTargetPath.Text.Trim();
                    if (!_targetPath.EndsWith("\\")) {
                        _targetPath += "\\";
                    }
                    try {
                        _lvi.SubItems[7].Text = Func.GetDisplayTime(executeTime);
                        FileInfo _fi = new FileInfo(_targetPath + relativeFilePath);
                        _lvi.SubItems[8].Text = Func.GetDisplaySize(_fi.Length);
                        int _size = Convert.ToInt32(_lvi.SubItems[2].Text);
                        _lvi.SubItems[9].Text = Math.Round(1.00 * _fi.Length / _size, 2) * 100 + "%";
                    }
                    catch (Exception ex) {
                        _lvi.SubItems[6].Text = "压缩失败"; 
                        Cmn.Log.WriteToFile("CompactError", _targetPath + relativeFilePath + "\n" + ex.Message + "\n" + ex.StackTrace);
                    }
                }
            }

            int _remainSecond = Func.GetRemainTime();

            lblMsg3.Text = "预计剩余时间：" + Func.GetDisplayTime(_remainSecond) + "。";

            if (CompactInfo.TotalTaskCount == CompactInfo.CompleteTaskCount) {
                SetControlState(false);
                MessageBox.Show("压缩完成！");
            }
        }

        /// <summary>
        /// txtOrgPath_TextChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtOrgPath_TextChanged(object sender, EventArgs e) {

            lvwFiles.Items.Clear();
            string _filePath = txtOrgPath.Text.Trim();
            if (_filePath == "") {
                return;
            }
            if (!Directory.Exists(_filePath)) {
                MessageBox.Show("目录不存在！");
                return;
            }
            Func.FillLvwFiles(lvwFiles, chkRecursive.Checked, _filePath, "");

            chkAll.Checked = false;
        }

        /// <summary>
        /// chkRecursive_CheckedChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkRecursive_CheckedChanged(object sender, EventArgs e) {
            lvwFiles.Items.Clear();
            string _filePath = txtOrgPath.Text.Trim();
            if (_filePath == "") {
                return;
            }
            if (!Directory.Exists(_filePath)) {
                MessageBox.Show("目录不存在！");
            }
            Func.FillLvwFiles(lvwFiles, chkRecursive.Checked, _filePath, "");
        }

        /// <summary>
        /// lvwFiles_ItemSelectionChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvwFiles_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e) {
            int _count = Func.GetSelectedFilesCount(lvwFiles);
            lblMsg.Text = "当前选中了" + _count + "个jpg文件。";
        }

        /// <summary>
        /// chkAll_CheckedChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkAll_CheckedChanged(object sender, EventArgs e) {
            lvwFiles.Focus();
            foreach (ListViewItem _item in lvwFiles.Items) {
                _item.Selected = chkAll.Checked;
            }
        }

    }
}
