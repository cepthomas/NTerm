using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks; // TODO update this.
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    public class SettingsEditor : Form
    {
        /// <summary>Edited flag.</summary>
        public bool Dirty { get; private set; } = false;

        /// <summary>Settings</summary>
        public UserSettings Settings { get; set; } = new();

        #region Lifecycle
        /// <summary>
        /// Create the main form.
        /// </summary>
        public SettingsEditor()
        {
            InitializeComponent();

            ShowIcon = false;
            ShowInTaskbar = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            pgSettings.PropertySort = PropertySort.Categorized;
            pgSettings.SelectedObject = Settings;
            pgSettings.ExpandAllGridItems();

            // Detect changes of interest.
            pgSettings.PropertyValueChanged += (sdr, args) =>
            {
                var name = args.ChangedItem!.PropertyDescriptor!.Name;
                var cat = args.ChangedItem!.PropertyDescriptor!.Category;
                Dirty = true;
            };

            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

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
        #endregion


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
            pgSettings = new PropertyGrid();
            SuspendLayout();
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
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private PropertyGrid pgSettings;


        
    }

    /// <summary>Select config.</summary>
    public class ConfigSelector : UITypeEditor
    {
        IWindowsFormsEditorService? _service = null;

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            var name = (string)value!;
            _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            var settings = (UserSettings)context!.Instance!;
            var ls = settings.Configs.Select(x => x.Name).ToList();
            var ind = ls.IndexOf(name);

            var lb = new ListBox
            {
                SelectionMode = SelectionMode.One
            };
            ls.ForEach(s => { lb.Items.Add(s); });
            lb.SelectedIndex = ind < 0 ? 0 : ind;
            lb.SelectedIndexChanged += (_, __) =>
            {
                value = lb.SelectedItem!.ToString();
                _service!.CloseDropDown();
            };

            // Drop the list control.
            _service!.DropDownControl(lb);

            return value;
        }
    }
}
