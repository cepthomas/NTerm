using System.Drawing;
using System.Windows.Forms;
using Ephemera.NBagOfUis;


namespace NTerm
{
    partial class Editor
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pgSettings = new PropertyGridEx();
            splitContainer1 = new SplitContainer();
            pgConfig = new PropertyGrid();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // pgSettings
            // 
            pgSettings.Location = new Point(40, 19);
            pgSettings.Name = "pgSettings";
            pgSettings.Size = new Size(336, 310);
            pgSettings.TabIndex = 3;
            // 
            // splitContainer1
            // 
            splitContainer1.Location = new Point(26, 26);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(pgSettings);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(pgConfig);
            splitContainer1.Size = new Size(409, 556);
            splitContainer1.SplitterDistance = 355;
            splitContainer1.TabIndex = 4;
            // 
            // pgConfig
            // 
            pgConfig.Location = new Point(24, 19);
            pgConfig.Name = "pgConfig";
            pgConfig.Size = new Size(359, 175);
            pgConfig.TabIndex = 4;
            // 
            // Editor
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(457, 623);
            Controls.Add(splitContainer1);
            Name = "Editor";
            Text = "Form1";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }


        #endregion

        private PropertyGridEx pgSettings;
        private SplitContainer splitContainer1;
        private PropertyGrid pgConfig;
    }
}
