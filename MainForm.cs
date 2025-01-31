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
    public partial class MainForm : Form
    {
        /// <summary>Edited flag.</summary>
        public bool Dirty { get; private set; } = false;


        #region Fields
        /// <summary>Settings</summary>
        UserSettings _settings;

        /// <summary>My logger</summary>
        //readonly Logger _logger = LogManager.CreateLogger("MainForm");
        #endregion



        #region Lifecycle
        /// <summary>
        /// Create the main form.
        /// </summary>
        public MainForm(UserSettings settings)
        {
            InitializeComponent();

            _settings = settings;

            ShowIcon = false;
            ShowInTaskbar = false;

            btnGo.Click += BtnGo_Click;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            pgSettings.PropertySort = PropertySort.Categorized;
            pgSettings.SelectedObject = _settings;
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
        /// Do test stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BtnGo_Click(object? sender, EventArgs e)
        {
            try
            {
                // Works:
                //AsyncUsingTcpClient();

                //StartServer(_config.Port);

                //var res = _client.Send("djkdjsdfksdf;s");
                //cliOut.AppendLine($"{res}: {_client.Response}");


                //var ser = new Serial();
                //var ports = ser.GetSerialPorts();

            }
            catch (Exception ex)
            {
                //_logger.Error($"Fatal error: {ex.Message}");
            }
        }
    }
}
