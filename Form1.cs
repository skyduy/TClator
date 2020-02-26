using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinTools
{
    public partial class Form1 : Form
    {
        private bool mouseDown;
        private Point lastLocation;
        private readonly Timer timer = new Timer();

        private ListBox resultList = new ListBox();
        private Size initFormSize;

        public Form1()
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
            if (e.Button == MouseButtons.Left)//判断鼠标的按键
            {
                //点击时判断form是否显示,显示就隐藏,隐藏就显示
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
                //右键退出事件
                if (MessageBox.Show("是否需要关闭程序？", "提示:", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)//出错提示
                {
                    //关闭窗口
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

            timer.Interval = 500;//延时500毫秒
            timer.Tick += new EventHandler(timer_Tick);

            //注册热键Shift+Space，Id号为100。
            SystemHotKey.RegHotKey(this.Handle, 100, SystemHotKey.KeyModifiers.Alt, Keys.Q);
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
                            this.Show();
                            this.Activate();
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //注销Id号为100的热键设定
            SystemHotKey.UnRegHotKey(this.Handle, 100); //销毁热键
        }
    }

    public class SystemHotKey
    {
        /// <summary>
        /// 如果函数执行成功，返回值不为0。
        /// 如果函数执行失败，返回值为0。要得到扩展错误信息，调用GetLastError。
        /// </summary>
        /// <param name="hWnd">要定义热键的窗口的句柄</param>
        /// <param name="id">定义热键ID（不能与其它ID重复）</param>
        /// <param name="fsModifiers">标识热键是否在按Alt、Ctrl、Shift、Windows等键时才会生效</param>
        /// <param name="vk">定义热键的内容</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, Keys vk);

        /// <summary>
        /// 注销热键
        /// </summary>
        /// <param name="hWnd">要取消热键的窗口的句柄</param>
        /// <param name="id">要取消热键的ID</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// 辅助键名称。
        /// Alt, Ctrl, Shift, WindowsKey
        /// </summary>
        [Flags()]
        public enum KeyModifiers { None = 0, Alt = 1, Ctrl = 2, Shift = 4, WindowsKey = 8 }

        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="hotKey_id">热键ID</param>
        /// <param name="keyModifiers">组合键</param>
        /// <param name="key">热键</param>
        public static void RegHotKey(IntPtr hwnd, int hotKeyId, KeyModifiers keyModifiers, Keys key)
        {
            if (!RegisterHotKey(hwnd, hotKeyId, keyModifiers, key))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == 1409)
                {
                    MessageBox.Show("热键被占用 ！");
                }
                else
                {
                    MessageBox.Show("注册热键失败！错误代码：" + errorCode);
                }
            }
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="hotKey_id">热键ID</param>
        public static void UnRegHotKey(IntPtr hwnd, int hotKeyId)
        {
            //注销指定的热键
            UnregisterHotKey(hwnd, hotKeyId);
        }
    }
}