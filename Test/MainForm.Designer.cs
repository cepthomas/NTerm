namespace Test
{
    partial class MainForm
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
            TxtDisplay = new System.Windows.Forms.RichTextBox();
            BtnGo = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // TxtDisplay
            // 
            TxtDisplay.Location = new System.Drawing.Point(7, 96);
            TxtDisplay.Name = "TxtDisplay";
            TxtDisplay.Size = new System.Drawing.Size(925, 528);
            TxtDisplay.TabIndex = 0;
            TxtDisplay.Text = "";
            // 
            // BtnGo
            // 
            BtnGo.Location = new System.Drawing.Point(40, 21);
            BtnGo.Name = "BtnGo";
            BtnGo.Size = new System.Drawing.Size(86, 26);
            BtnGo.TabIndex = 1;
            BtnGo.Text = "Go!!!";
            BtnGo.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(944, 636);
            Controls.Add(BtnGo);
            Controls.Add(TxtDisplay);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.RichTextBox TxtDisplay;
        private System.Windows.Forms.Button BtnGo;
    }
}
