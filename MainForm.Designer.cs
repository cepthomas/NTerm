using System.Drawing;
using System.Windows.Forms;

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
            toolStrip1 = new ToolStrip();
            btnSettings = new ToolStripButton();
            btnHelp = new ToolStripButton();
            btnClear = new ToolStripButton();
            btnWrap = new ToolStripButton();
            rtbOut = new RichTextBox();
            rtbIn = new RichTextBox();
            btnDebug = new ToolStripButton();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(18, 18);
            toolStrip1.Items.AddRange(new ToolStripItem[] { btnSettings, btnHelp, btnClear, btnWrap, btnDebug });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(800, 26);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnSettings
            // 
            btnSettings.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSettings.ImageTransparentColor = Color.Magenta;
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(61, 23);
            btnSettings.Text = "settings";
            btnSettings.ToolTipText = "Edit settings";
            // 
            // btnHelp
            // 
            btnHelp.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnHelp.ImageTransparentColor = Color.Magenta;
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(39, 23);
            btnHelp.Text = "help";
            btnHelp.ToolTipText = "You need some help";
            btnHelp.Click += Help_Click;
            // 
            // btnClear
            // 
            btnClear.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnClear.ImageTransparentColor = Color.Magenta;
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(41, 23);
            btnClear.Text = "clear";
            btnClear.ToolTipText = "Clear output";
            // 
            // btnWrap
            // 
            btnWrap.CheckOnClick = true;
            btnWrap.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnWrap.ImageTransparentColor = Color.Magenta;
            btnWrap.Name = "btnWrap";
            btnWrap.Size = new Size(43, 23);
            btnWrap.Text = "wrap";
            btnWrap.ToolTipText = "Wrap output";
            // 
            // rtbOut
            // 
            rtbOut.BorderStyle = BorderStyle.None;
            rtbOut.ForeColor = Color.Black;
            rtbOut.Location = new Point(22, 40);
            rtbOut.Name = "rtbOut";
            rtbOut.ReadOnly = true;
            rtbOut.Size = new Size(753, 362);
            rtbOut.TabIndex = 3;
            rtbOut.Text = "";
            // 
            // rtbIn
            // 
            rtbIn.BorderStyle = BorderStyle.None;
            rtbIn.ForeColor = Color.Black;
            rtbIn.Location = new Point(22, 417);
            rtbIn.Multiline = false;
            rtbIn.Name = "rtbIn";
            rtbIn.ScrollBars = RichTextBoxScrollBars.Horizontal;
            rtbIn.Size = new Size(753, 38);
            rtbIn.TabIndex = 4;
            rtbIn.Text = "";
            // 
            // btnDebug
            // 
            btnDebug.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDebug.Image = (Image)resources.GetObject("btnDebug.Image");
            btnDebug.ImageTransparentColor = Color.Magenta;
            btnDebug.Name = "btnDebug";
            btnDebug.Size = new Size(52, 23);
            btnDebug.Text = "debug";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 485);
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
        private ToolStripButton btnDebug;
    }
}
