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
            btnGo = new Button();
            pgSettings = new PropertyGrid();
            SuspendLayout();
            // 
            // btnGo
            // 
            btnGo.Location = new Point(61, 17);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(94, 28);
            btnGo.TabIndex = 2;
            btnGo.Text = "Go!!";
            btnGo.UseVisualStyleBackColor = true;
            // 
            // pgSettings
            // 
            pgSettings.Location = new Point(33, 74);
            pgSettings.Name = "pgSettings";
            pgSettings.Size = new Size(318, 438);
            pgSettings.TabIndex = 3;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1082, 623);
            Controls.Add(pgSettings);
            Controls.Add(btnGo);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button btnGo;
        private PropertyGrid pgSettings;
    }
}
