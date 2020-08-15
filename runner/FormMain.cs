using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TClator
{
    public partial class FormMain : Form
    {
        private readonly int BUF_SIZE = 1024;
        private readonly StringBuilder DLLResult;

        private bool mouseDown;
        private Size initFormSize;
        private Point lastLocation;

        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly List<int> hotkeyIDs = new List<int>();

        private bool inSetting = false;
        private readonly FormYoudao youdao = new FormYoudao();
        private Setting setting = new Setting();
        private readonly string fn = "config.json";

        private readonly string calcFirst = "0123456789-.(（";

        private BackgroundWorker bgw;

        public FormMain()
        {
            InitializeComponent();

            this.DLLResult = new StringBuilder(BUF_SIZE);

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.initFormSize = this.Size;

            this.bgw = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            this.bgw.DoWork += new DoWorkEventHandler(GetAnswers);
            this.bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ShowAnswers);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Hide();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            //按快捷键
            switch (m.Msg)
            {
                case WM_HOTKEY:
                    switch (m.WParam.ToInt32())
                    {
                        case 100:    // Alt + Q
                            this.WindowState = FormWindowState.Normal;
                            this.ResultList.SelectedIndex = -1;
                            this.Show();
                            this.Activate();
                            this.TextBox.SelectAll();
                            this.TextBox.Focus();
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        [DllImport(@"C:\Users\jun\src\toys\out\build\x86-Debug\bin\TCLATOR.dll",
            EntryPoint = "calculate", CallingConvention = CallingConvention.StdCall)]
        private static extern void Calculate(StringBuilder answer, int len, string expression);

        [DllImport(@"C:\Users\jun\src\toys\out\build\x86-Debug\bin\TCLATOR.dll",
            EntryPoint = "translate", CallingConvention = CallingConvention.StdCall)]
        private static extern void Translate(StringBuilder dst, int len, string src);

        private void GetAnswers(object sender, DoWorkEventArgs e)
        {
            string content = (string)e.Argument;
            BackgroundWorker worker = sender as BackgroundWorker;
            while (!worker.CancellationPending)
            {
                // Perform a time consuming operation
                if (content != string.Empty)
                {
                    if (calcFirst.Contains(content[0]))
                    {
                        Calculate(this.DLLResult, BUF_SIZE, content);
                        System.Threading.Thread.Sleep(3000);
                        Console.WriteLine("Complete " + content);
                    }
                    else
                    {
                        Translate(this.DLLResult, BUF_SIZE, this.setting.appKey + "," + this.setting.appSecret + "," + content);
                    }
                }
                e.Result = this.DLLResult.ToString();
                break;
            }
            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private void ShowAnswers(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                return;
            }
            string dllOut = (string)e.Result;
            char token = ','; // TODO 待定
            string[] answers = dllOut.Split(token);

            if (answers.Length > 0)
            {
                this.ResultList.Height = Math.Min(answers.Length, 5) * this.ResultList.Font.Height;
                this.Size = new Size(this.initFormSize.Width, this.initFormSize.Height + this.ResultList.Height);
                foreach (string item in answers)
                {
                    this.ResultList.Items.Add(item);
                }
                this.ResultList.Show();
            }
            else
            {
                this.ResultList.Hide();
                this.Size = this.initFormSize;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lock (this)
            {
                // timer activate
                timer.Stop();

                // 准备参数
                this.ResultList.Items.Clear();
                string content = TextBox.Text.Trim();
                Console.WriteLine("Get task " + content);

                // 开始执行
                if (this.bgw.WorkerSupportsCancellation && this.bgw.IsBusy)
                {
                    // Cancel the asynchronous operation.
                    this.bgw.CancelAsync();

                    this.bgw = new BackgroundWorker
                    {
                        WorkerSupportsCancellation = true
                    };
                    this.bgw.DoWork += new DoWorkEventHandler(GetAnswers);
                    this.bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ShowAnswers);
                }

                Console.WriteLine("Reader Run " + content);
                this.bgw.RunWorkerAsync(content);
            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                // refresh timer
                timer.Stop();
                timer.Start();
            }
        }

        private void Form_Load(object sender, EventArgs e)
        {
            this.lastLocation = new Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                          (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 4);
            this.Location = lastLocation;
            this.Update();

            this.ResultList.Hide();

            // set timer 300ms
            timer.Interval = 300;
            timer.Tick += new EventHandler(Timer_Tick);

            // Hotkey register
            this.hotkeyIDs.Clear();
            int Alt_Q = 100;
            if (SystemHotKey.RegHotKey(this.Handle, Alt_Q, SystemHotKey.KeyModifiers.Alt, Keys.Q, "Alt+Q"))
            {
                this.hotkeyIDs.Add(Alt_Q);
            }

            // load setting
            this.LoadJson();
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void Form_Deactivate(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // cancel hotkey
            foreach (int i in this.hotkeyIDs)
            {
                SystemHotKey.UnRegHotKey(this.Handle, i);
            }
            this.hotkeyIDs.Clear();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (this.ResultList.Items.Count > 0)
                {
                    this.ResultList.SelectedIndex = 0;
                    this.ResultList.Focus();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
            }
        }

        private void ResultList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up && this.ResultList.SelectedIndex == 0)
            {
                this.ResultList.SelectedIndex = -1;
                this.TextBox.SelectAll();
                this.TextBox.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || (e.Control && e.KeyCode == Keys.C))
            {
                string s = this.ResultList.SelectedItem.ToString();
                Clipboard.SetData(DataFormats.StringFormat, s);
                e.SuppressKeyPress = true;
            }
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.Hide();
                }
                else if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                }
            }
        }

        private void ToolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            Dispose();
            Close();
        }

        private void ToolStripMenuItemKeySetting_Click(object sender, EventArgs e)
        {
            if (this.inSetting)
            {
                this.youdao.Focus();
                return;
            }
            this.inSetting = true;
            this.LoadJson();
            youdao.appKey = this.setting.appKey;
            youdao.appSecret = this.setting.appSecret;
            if (this.youdao.ShowDialog() == DialogResult.OK)
            {
                this.setting.appKey = youdao.appKey;
                this.setting.appSecret = youdao.appSecret;
            };
            this.DumpJson();
            this.inSetting = false;
        }

        private void LoadJson()
        {
            if (File.Exists(fn))
            {
                using (StreamReader r = new StreamReader(this.fn))
                {
                    string json = r.ReadToEnd();
                    this.setting = JsonConvert.DeserializeObject<Setting>(json);
                }
            }
        }

        private void DumpJson()
        {
            string json = JsonConvert.SerializeObject(this.setting);
            File.WriteAllText(this.fn, json);
        }
    }

    public class Setting
    {
        public string appKey = "";
        public string appSecret = "";
    }
}
