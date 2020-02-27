using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace TClator
{
    public partial class FormMain : Form
    {
        private List<int> hotkeyIDs= new List<int>();
        private bool mouseDown;
        private Point lastLocation;
        private readonly Timer timer = new Timer();

        private ListBox resultList = new ListBox();
        private Size initFormSize;

        public FormMain()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;

            this.initFormSize = this.Size;
            this.resultList.Location = new Point(this.textBox1.Location.X, this.textBox1.Location.Y + this.textBox1.Size.Height);
            this.resultList.Font = this.textBox1.Font;
            this.resultList.Width = this.textBox1.Width;
            this.resultList.IntegralHeight = false;
            this.resultList.Hide();
            this.Controls.Add(resultList);
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
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

        private void timer_Tick(object sender, EventArgs e)
        {
            lock (this)
            {
                timer.Stop();
                if (textBox1.Text != string.Empty)
                {
                    if (textBox1.Text == "add")
                    {
                        int num = 5;
                        for (int i = 0; i < num; i++)
                        {
                            resultList.Items.Add(textBox1.Text);
                        }

                        this.resultList.Height = Math.Min(num, 5) * this.resultList.Font.Height;
                        this.Size = new Size(this.initFormSize.Width, this.initFormSize.Height + this.resultList.Size.Height);
                        this.resultList.Show();
                    }
                }
                else
                {
                    this.resultList.Hide();
                    this.Size = this.initFormSize;
                    this.resultList.Items.Clear();
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                timer.Stop();
                timer.Start();
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.lastLocation = new Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                          (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 4);
            this.Location = lastLocation;
            this.Update();

            // delay 500ms
            timer.Interval = 500;
            timer.Tick += new EventHandler(timer_Tick);

            // Hotkey register
            this.hotkeyIDs.Clear();
            int Alt_Q = 100;
            if (SystemHotKey.RegHotKey(this.Handle, Alt_Q, SystemHotKey.KeyModifiers.Alt, Keys.Q, "Alt+Q"))
            {
                this.hotkeyIDs.Add(Alt_Q);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
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
                            this.textBox1.Text = string.Empty;
                            this.Show();
                            this.Activate();
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

    }
}
