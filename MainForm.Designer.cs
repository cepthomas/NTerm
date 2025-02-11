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
            tvOut = new Ephemera.NBagOfUis.TextViewer();
            cliIn = new Ephemera.NBagOfUis.CliInput();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            btnSettings = new System.Windows.Forms.ToolStripButton();
            btnHelp = new System.Windows.Forms.ToolStripButton();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tvOut
            // 
            tvOut.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tvOut.Location = new System.Drawing.Point(10, 28);
            tvOut.MaxText = 10000;
            tvOut.Name = "tvOut";
            tvOut.Prompt = "";
            tvOut.Size = new System.Drawing.Size(778, 364);
            tvOut.TabIndex = 0;
            tvOut.WordWrap = true;
            // 
            // cliIn
            // 
            cliIn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            cliIn.Location = new System.Drawing.Point(10, 398);
            cliIn.Name = "cliIn";
            cliIn.Prompt = "";
            cliIn.Size = new System.Drawing.Size(778, 40);
            cliIn.TabIndex = 1;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(18, 18);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnSettings, btnHelp });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(800, 25);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnSettings
            // 
            btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btnSettings.Image = Properties.Resources.glyphicons_137_cogwheel;
            btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new System.Drawing.Size(26, 22);
            btnSettings.Text = "toolStripButton1";
            btnSettings.Click += Settings_Click;
            // 
            // btnHelp
            // 
            btnHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btnHelp.Image = Properties.Resources.glyphicons_195_question_sign;
            btnHelp.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new System.Drawing.Size(26, 22);
            btnHelp.Text = "toolStripButton1";
            btnHelp.Click += Help_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(toolStrip1);
            Controls.Add(cliIn);
            Controls.Add(tvOut);
            Name = "MainForm";
            Text = "Form1";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Ephemera.NBagOfUis.TextViewer tvOut;
        private Ephemera.NBagOfUis.CliInput cliIn;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnSettings;
        private System.Windows.Forms.ToolStripButton btnHelp;
    }
}
