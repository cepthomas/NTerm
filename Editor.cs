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
        public bool Dirty { get; private set; } = false;

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
        /// Edit the common options in a property grid.
        /// </summary>
        public void Edit()
        {
            PropertyGrid pgMain = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.NoSort,
                SelectedObject = this
            };

            using Form f = new()
            {
                Text = "User Settings",
                AutoScaleMode = AutoScaleMode.None,
                Location = Cursor.Position,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            f.ClientSize = new(450, 600); // must do after construction
            f.Controls.Add(pgMain);
            pgMain.ExpandAllGridItems();

            // Detect changes of interest. TODO more smart
            pgMain.PropertyValueChanged += (sdr, args) =>
            {
                var name = args.ChangedItem!.PropertyDescriptor!.Name;
                var cat = args.ChangedItem!.PropertyDescriptor!.Category;
                Console.WriteLine($"+++ PropertyValueChanged {name} {cat}");
                //Dirty = true;
            };

            f.ShowDialog();
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

        #endregion
    }

    /// <summary>Generic property editor for lists of strings.</summary>
    public class StringListEditor : UITypeEditor
    {
        IWindowsFormsEditorService? _service = null;

        public override object EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            List<string> ls = value is null ? [] : (List<string>)value;

            TextBox tb = new()
            {
                Multiline = true,
                ReadOnly = false,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Both,
                Height = 100,
                Text = string.Join(Environment.NewLine, ls)
            };
            tb.Select(0, 0);

            _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            _service?.DropDownControl(tb);
            ls = tb.Text.SplitByToken(Environment.NewLine);

            return ls;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }

    /// <summary>Select config.</summary>
    public class ConfigSelector : UITypeEditor
    {
        IWindowsFormsEditorService? _service = null;

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            var name = (string)value!;
            _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            var settings = (UserSettings)context!.Instance!;
            var ls = settings.Configs.Select(x => x.Name).ToList();
            var ind = ls.IndexOf(name);

            var lb = new ListBox { SelectionMode = SelectionMode.One };
            ls.ForEach(s => { lb.Items.Add(s); });
            lb.SelectedIndex = ind < 0 ? 0 : ind;
            lb.SelectedIndexChanged += (_, __) =>
            {
                value = lb.SelectedItem!.ToString();
                _service!.CloseDropDown();
            };

            // Drop the list control.
            _service!.DropDownControl(lb);
            // Done.
            return value;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }




    /// <summary>
    /// Convert a list of strings into a readable property.
    /// </summary>
    public class StringListConverter : TypeConverter
    {
        /// <summary>Overrides the ConvertTo method of TypeConverter.</summary>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value != null && value is List<string> && destinationType == typeof(string))
            {
                List<string> v = value as List<string>;
                string sret = string.Join("|||", v.ToArray());
                return sret;
            }
            else
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        /// <summary>Overrides the ConvertFrom method of TypeConverter.</summary>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value != null && value is string)
            {
                List<string> vals = value.ToString().SplitByToken("|||");
                return vals;
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }

    /// <summary>
    /// Converter for selecting property value from defined/known string lists.
    /// </summary>
    public class SystemFixedListTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
        {
            return true;
        }

        /// Get the list using the property name as key.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            List<string> rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "CurrentConfig":

                    var settings = (UserSettings)context!.Instance!;
                    rec = settings.Configs.Select(x => x.Name).ToList();



                    //rec = new List<string>();
                    //Array itypes = Enum.GetValues(typeof(Instrument.InstrumentType));
                    //foreach (object val in itypes)
                    //{
                    //    if (val.ToString() != "None")
                    //    {
                    //        rec.Add(val.ToString());
                    //    }
                    //}
                    break;

                //case "DbServer":
                //    rec = Globals.Instance.ServerNames.Where(n => n.Contains("ECON")).ToList();
                //    break;

                //case "ADDServer":
                //    rec = Globals.Instance.ServerNames.Where(n => n.Contains("ADD")).ToList();
                //    break;

                //case "InstrumentDesc":
                //    rec = Globals.Instance.UserSettings.FavoriteInstruments;
                //    break;

                //case "TimeTemplateId":
                //    rec = Globals.Instance.TimeTemplates.GetAllIds();
                //    break;

                default:
                    break;
            }

            StandardValuesCollection coll = new StandardValuesCollection(rec);

            return coll;
        }
    }

}
