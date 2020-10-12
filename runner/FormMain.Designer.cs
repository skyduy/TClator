namespace toys
{
    partial class FormMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.TextBox = new MyTextBox();
            this.ResultList = new System.Windows.Forms.ListBox();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItemKeySetting = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemExit = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.DetailBox = new System.Windows.Forms.RichTextBox();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // TextBox
            // 
            this.TextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.TextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.TextBox.Font = new System.Drawing.Font("微软雅黑", 18.15126F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TextBox.Location = new System.Drawing.Point(7, 8);
            this.TextBox.Margin = new System.Windows.Forms.Padding(2);
            this.TextBox.Name = "TextBox";
            this.TextBox.Size = new System.Drawing.Size(698, 39);
            this.TextBox.TabIndex = 0;
            this.TextBox.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            this.TextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_KeyDown);
            // 
            // ResultList
            // 
            this.ResultList.Font = new System.Drawing.Font("微软雅黑", 18.15126F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ResultList.FormattingEnabled = true;
            this.ResultList.IntegralHeight = false;
            this.ResultList.ItemHeight = 31;
            this.ResultList.Location = new System.Drawing.Point(7, 47);
            this.ResultList.Margin = new System.Windows.Forms.Padding(2);
            this.ResultList.Name = "ResultList";
            this.ResultList.Size = new System.Drawing.Size(698, 9);
            this.ResultList.TabIndex = 1;
            this.ResultList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ResultList_KeyDown);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemKeySetting,
            this.toolStripMenuItemExit});
            this.contextMenuStrip.Name = "contextMenuStrip1";
            this.contextMenuStrip.Size = new System.Drawing.Size(146, 48);
            // 
            // toolStripMenuItemKeySetting
            // 
            this.toolStripMenuItemKeySetting.Name = "toolStripMenuItemKeySetting";
            this.toolStripMenuItemKeySetting.Size = new System.Drawing.Size(145, 22);
            this.toolStripMenuItemKeySetting.Text = "有道Key设置";
            this.toolStripMenuItemKeySetting.Click += new System.EventHandler(this.ToolStripMenuItemKeySetting_Click);
            // 
            // toolStripMenuItemExit
            // 
            this.toolStripMenuItemExit.Name = "toolStripMenuItemExit";
            this.toolStripMenuItemExit.Size = new System.Drawing.Size(145, 22);
            this.toolStripMenuItemExit.Text = "Exit";
            this.toolStripMenuItemExit.Click += new System.EventHandler(this.ToolStripMenuItemExit_Click);
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "toys";
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.NotifyIcon_MouseClick);
            // 
            // DetailBox
            // 
            this.DetailBox.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DetailBox.Location = new System.Drawing.Point(7, 47);
            this.DetailBox.Name = "DetailBox";
            this.DetailBox.Size = new System.Drawing.Size(698, 177);
            this.DetailBox.TabIndex = 3;
            this.DetailBox.Text = "";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(711, 53);
            this.Controls.Add(this.DetailBox);
            this.Controls.Add(this.ResultList);
            this.Controls.Add(this.TextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormMain";
            this.Opacity = 0.85D;
            this.Text = "TClator";
            this.Deactivate += new System.EventHandler(this.Form_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_FormClosing);
            this.Load += new System.EventHandler(this.Form_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Form_MouseUp);
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox TextBox;
        private System.Windows.Forms.ListBox ResultList;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemKeySetting;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemExit;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.RichTextBox DetailBox;
    }

    class MyTextBox : System.Windows.Forms.TextBox
    {
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            // Trap WM_PASTE:
            if (m.Msg == 0x302 && System.Windows.Forms.Clipboard.ContainsText())
            {
                this.SelectedText = System.Windows.Forms.Clipboard.GetText().Replace(
                    System.Environment.NewLine, "");
                return;
            }
            base.WndProc(ref m);
        }
    }
}

