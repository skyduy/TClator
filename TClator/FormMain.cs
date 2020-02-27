using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TClator
{
    public partial class FormMain : Form
    {
        private bool mouseDown;
        private Size initFormSize;
        private Point lastLocation;

        private readonly Timer timer = new Timer();
        private readonly List<int> hotkeyIDs = new List<int>();

        private List<string> answers = new List<string>();
        private readonly Calculator calc = new Calculator();
        private readonly Translator trans = new Translator();

        public FormMain()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;

            this.initFormSize = this.Size;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lock (this)
            {
                timer.Stop();
                this.answers.Clear();
                this.ResultList.Items.Clear();
                string content = TextBox.Text.Trim();
                if (content != string.Empty)
                {
                    if (Char.IsDigit(content[0]) || content[0] == '-')
                    {
                        this.calc.Response(content, ref this.answers);
                    }
                    else
                    {
                        this.trans.response(content, ref this.answers);
                    }

                    this.ResultList.Height = Math.Min(this.answers.Count, 5) * this.ResultList.Font.Height;
                    this.Size = new Size(this.initFormSize.Width, this.initFormSize.Height + this.ResultList.Height);
                    foreach (string item in this.answers)
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
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            lock (this)
            {
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

            // delay 300ms
            timer.Interval = 300;
            timer.Tick += new EventHandler(Timer_Tick);

            // Hotkey register
            this.hotkeyIDs.Clear();
            int Alt_Q = 100;
            if (SystemHotKey.RegHotKey(this.Handle, Alt_Q, SystemHotKey.KeyModifiers.Alt, Keys.Q, "Alt+Q"))
            {
                this.hotkeyIDs.Add(Alt_Q);
            }
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
            else if (e.Button == MouseButtons.Right)
            {
                if (MessageBox.Show("是否需要关闭程序？", "提示:", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)//出错提示
                {
                    DialogResult = DialogResult.No;
                    Dispose();
                    Close();
                }
            }
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
                            this.TextBox.SelectAll();
                            this.TextBox.Focus();
                            this.Show();
                            this.Activate();
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
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
            else if (e.Control == true && e.KeyCode == Keys.C)
            {
                string s = this.ResultList.SelectedItem.ToString();
                Clipboard.SetData(DataFormats.StringFormat, s);
            }
        }
    }
}