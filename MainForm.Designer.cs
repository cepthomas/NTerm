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
            cliIn = new Ephemera.NBagOfUis.CliInput();
            cliOut = new Ephemera.NBagOfUis.TextViewer();
            btnGo = new Button();
            SuspendLayout();
            // 
            // cliIn
            // 
            cliIn.BorderStyle = BorderStyle.FixedSingle;
            cliIn.Location = new Point(48, 573);
            cliIn.Name = "cliIn";
            cliIn.Prompt = "???";
            cliIn.Size = new Size(815, 52);
            cliIn.TabIndex = 0;
            // 
            // cliOut
            // 
            cliOut.BorderStyle = BorderStyle.FixedSingle;
            cliOut.Location = new Point(52, 92);
            cliOut.MaxText = 50000;
            cliOut.Name = "cliOut";
            cliOut.Prompt = "";
            cliOut.Size = new Size(811, 444);
            cliOut.TabIndex = 1;
            cliOut.WordWrap = true;
            // 
            // btnGo
            // 
            btnGo.Location = new Point(61, 18);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(94, 29);
            btnGo.TabIndex = 2;
            btnGo.Text = "Go!!";
            btnGo.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1082, 656);
            Controls.Add(btnGo);
            Controls.Add(cliOut);
            Controls.Add(cliIn);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.CliInput cliIn;
        private Ephemera.NBagOfUis.TextViewer cliOut;
        private Button btnGo;
    }
}
