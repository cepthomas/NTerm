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
            SuspendLayout();
            // 
            // pgSettings
            // 
            pgSettings.Dock = DockStyle.Fill;
            pgSettings.Location = new Point(0, 0);
            pgSettings.Name = "pgSettings";
            pgSettings.Size = new Size(457, 623);
            pgSettings.TabIndex = 3;
            // 
            // Editor
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(457, 623);
            Controls.Add(pgSettings);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "Editor";
            Text = "Edit Settings";
            ResumeLayout(false);
        }


        #endregion

        private PropertyGridEx pgSettings;
    }
}
