namespace NTerm
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            btnSettings = new System.Windows.Forms.ToolStripButton();
            btnHelp = new System.Windows.Forms.ToolStripButton();
            btnClear = new System.Windows.Forms.ToolStripButton();
            btnWrap = new System.Windows.Forms.ToolStripButton();
            rtbOut = new System.Windows.Forms.RichTextBox();
            rtbIn = new System.Windows.Forms.RichTextBox();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(18, 18);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnSettings, btnHelp, btnClear, btnWrap });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(800, 26);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnSettings
            // 
            btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new System.Drawing.Size(61, 23);
            btnSettings.Text = "settings";
            btnSettings.ToolTipText = "Edit settings";
            // 
            // btnHelp
            // 
            btnHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnHelp.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new System.Drawing.Size(39, 23);
            btnHelp.Text = "help";
            btnHelp.ToolTipText = "You need some help";
            btnHelp.Click += Help_Click;
            // 
            // btnClear
            // 
            btnClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnClear.Image = (System.Drawing.Image)resources.GetObject("btnClear.Image");
            btnClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnClear.Name = "btnClear";
            btnClear.Size = new System.Drawing.Size(41, 23);
            btnClear.Text = "clear";
            btnClear.ToolTipText = "Clear output";
            // 
            // btnWrap
            // 
            btnWrap.CheckOnClick = true;
            btnWrap.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnWrap.Image = (System.Drawing.Image)resources.GetObject("btnWrap.Image");
            btnWrap.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnWrap.Name = "btnWrap";
            btnWrap.Size = new System.Drawing.Size(43, 23);
            btnWrap.Text = "wrap";
            btnWrap.ToolTipText = "Wrap output";
            // 
            // rtbOut
            // 
            rtbOut.BorderStyle = System.Windows.Forms.BorderStyle.None;
            rtbOut.Location = new System.Drawing.Point(22, 40);
            rtbOut.Name = "rtbOut";
            rtbOut.Size = new System.Drawing.Size(753, 362);
            rtbOut.TabIndex = 3;
            rtbOut.Text = "";
            // 
            // rtbIn
            // 
            rtbIn.BorderStyle = System.Windows.Forms.BorderStyle.None;
            rtbIn.Location = new System.Drawing.Point(22, 417);
            rtbIn.Name = "rtbIn";
            rtbIn.Size = new System.Drawing.Size(753, 38);
            rtbIn.TabIndex = 4;
            rtbIn.Text = "";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 485);
            Controls.Add(rtbIn);
            Controls.Add(rtbOut);
            Controls.Add(toolStrip1);
            Name = "MainForm";
            Text = "Form1";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnSettings;
        private System.Windows.Forms.ToolStripButton btnHelp;
        private System.Windows.Forms.ToolStripButton btnClear;
        private System.Windows.Forms.ToolStripButton btnWrap;
        private System.Windows.Forms.RichTextBox rtbOut;
        private System.Windows.Forms.RichTextBox rtbIn;
    }
}
