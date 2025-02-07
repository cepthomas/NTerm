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
            SuspendLayout();
            // 
            // tvOut
            // 
            tvOut.Ansi = false;
            tvOut.BufferSize = 120;
            tvOut.Location = new System.Drawing.Point(12, 12);
            tvOut.MaxText = 50000;
            tvOut.Mode = Ephemera.NBagOfUis.TextViewer.ModeT.Static;
            tvOut.Name = "tvOut";
            tvOut.Prompt = "";
            tvOut.Size = new System.Drawing.Size(744, 312);
            tvOut.TabIndex = 0;
            tvOut.WordWrap = true;
            // 
            // cliIn
            // 
            cliIn.Location = new System.Drawing.Point(10, 340);
            cliIn.Name = "cliIn";
            cliIn.Prompt = "???";
            cliIn.Size = new System.Drawing.Size(746, 48);
            cliIn.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(cliIn);
            Controls.Add(tvOut);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.TextViewer tvOut;
        private Ephemera.NBagOfUis.CliInput cliIn;
    }
}
