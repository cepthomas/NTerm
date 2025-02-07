using Ephemera.NBagOfTricks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.Design;


namespace NTerm
{
    public partial class Editor : Form
    {
        /// <summary>Edited flag.</summary>
        //public bool Dirty { get; private set; } = false;

        /// <summary>Settings</summary>
        public UserSettings Settings { get; set; } = new();

        #region Lifecycle
        /// <summary>
        /// Create the main form.
        /// </summary>
        public Editor()
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
        #endregion
    }



    /// <summary>Converter for selecting property value from known string lists.</summary>
    public class FixedListTypeConverter : TypeConverter
    {
        /// Get the list using the property name as key.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            List<string>? rec = null;

            switch (context!.PropertyDescriptor!.Name)
            {
                case "CurrentConfig":
                    var settings = (UserSettings)context!.Instance!;
                    rec = settings.Configs.Select(x => x.Name).ToList();
                    break;

                default:
                    break;
            }

            StandardValuesCollection coll = new(rec);
            return coll;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) { return true; }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) { return true; }
    }
}
