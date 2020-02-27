using System;
using System.Windows.Forms;
using System.Drawing;

namespace TClator
{
    public partial class FormYoudao : Form
    {
        public string appKey;
        public string appSecret;

        public FormYoudao()
        {
            InitializeComponent();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FormYoudao_Load(object sender, EventArgs e)
        {
            this.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                          (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 4); ;
            this.Update();

            this.textBox1.Text = this.appKey;
            this.textBox2.Text = this.appSecret;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            this.appKey = this.textBox1.Text;
            this.appSecret = this.textBox2.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}