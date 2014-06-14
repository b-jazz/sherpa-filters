#region GPL
// Sherpa Filters
// Copyright (C) 2002
// This program is free software; you can redistribute it and/or modify it 
// under the terms of the GNU General Public License as published by the 
// Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc.; 59 Temple Place, Suite 330; Boston, MA 02111-1307 USA
#endregion GPL

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

using Sherpa.DataSource;


namespace Sherpa
{
	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		public static Mutex fileMutex = new Mutex(false);
		public static Mutex reloadMutex = new Mutex(false);
		public static Boolean reloadFlag = false;

		private FilterEditor fEditor;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem menuItem10;

		public static bool configComplete = false;

		private Thread filterThread;
		private FilterController filterCtrl;
		private System.Windows.Forms.MenuItem FileExitItem;
		private System.Windows.Forms.TabPage GeneralTab;
		private System.Windows.Forms.TabPage OptionsTab;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label StatusLabel;
		private System.Windows.Forms.GroupBox ApplicationGroup;
		private System.Windows.Forms.Label label3;

		public static Label staticTotalFilteredLabel;
		public static Label staticStatusLabel;
		public static Label staticSpamFilteredLabel;
		public static Label staticSpamPercentageLabel;
		public static Label staticSpamPerDayLabel;
		public static Label staticNormalPerDayLabel;
		public static Label staticNormalFilteredLabel;
		public static Label staticElapsedTimeLabel;

		public System.Windows.Forms.Label elapsedTimeLabel;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.OpenFileDialog openFilterDialog;
		private System.Windows.Forms.TabPage FiltersTab;
		private System.Windows.Forms.CheckedListBox filtersListBox;
		private System.Windows.Forms.Label label21;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TextBox defaultMboxTxt;
		private System.Windows.Forms.TextBox filterLocationTxt;
		private System.Windows.Forms.TextBox userNameTxt;
		private System.Windows.Forms.TextBox exchangeServerTxt;
		private System.Windows.Forms.Button browseButton;
		public System.Windows.Forms.Label spamPerDayLabel;
		public System.Windows.Forms.Label spamPercentageLabel;
		public System.Windows.Forms.Label spamFilteredLabel;
		public System.Windows.Forms.Label normalFilteredLabel;
		public System.Windows.Forms.Label totalFilteredLabel;
		private System.Windows.Forms.Button optionsApplyButton;
		private System.Windows.Forms.Button optionsResetButton;
		private System.ComponentModel.IContainer components;

		private bool uriOptionChanged = false;
		private bool filterLocationOptionChanged = false;
		private System.Windows.Forms.NumericUpDown probeIntervalTxt;
		private System.Windows.Forms.MenuItem HelpAboutMenu;
		public System.Windows.Forms.Label normalPerDayLabel;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.CheckBox logActionsCheck;
		private System.Windows.Forms.Button editFiltersButton;
		private System.Windows.Forms.TabPage BayesTab;
		private System.Windows.Forms.TextBox nonSpamFolderTxt;
		private System.Windows.Forms.Label nonSpamCorpusLabel;
		private System.Windows.Forms.TextBox spamFolderTxt;
		private System.Windows.Forms.Label spamCorpusLabel;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button bayesApplyButton;
		private System.Windows.Forms.Button bayesResetButton;
		private System.Windows.Forms.Button recomputeButton;
		private System.Windows.Forms.MenuItem helpContentsMenu;
		private System.Windows.Forms.MenuItem helpLicenseItem;
		private System.Windows.Forms.NotifyIcon systemTray;
		public static System.Windows.Forms.NotifyIcon staticSystemTray;
		private System.Windows.Forms.CheckBox verboseFilterDebugCheck;
		private bool miscOptionsChanged = false;

		public MainForm()
		{
			Thread.CurrentThread.Name = "Sherpa:MainForm";

			// Required for Windows Form Designer support
			InitializeComponent();

			staticTotalFilteredLabel = totalFilteredLabel;
			staticNormalFilteredLabel = normalFilteredLabel;
			staticSpamFilteredLabel = spamFilteredLabel;
			staticSpamPercentageLabel = spamPercentageLabel;
			staticSpamPerDayLabel = spamPerDayLabel;
			staticNormalPerDayLabel = normalPerDayLabel;
			staticElapsedTimeLabel = elapsedTimeLabel;
			
			staticStatusLabel = StatusLabel;
			staticSystemTray = systemTray;
			
			staticStatusLabel.Text = "Loading Configuration";

			TestAndSpawn();
		}

		private void TestAndSpawn()
		{
			staticStatusLabel.Text = "Testing Network Connection";
			if (TestConnection(Config.BaseUri))
			{
				SpawnFilterThread();
			}
			else
			{
				staticStatusLabel.Text = "Network Connection Failed";
				tabControl.SelectedIndex = 1; 
			}
		}

		private void SpawnFilterThread()
		{
			staticStatusLabel.Text = "Spawning Worker Thread";
			filterCtrl = new FilterController();
			filterThread = new Thread(new ThreadStart(filterCtrl.ThreadStart));
			filterThread.Start();
		}

		private void FillInFilterDefaults()
		{
			verboseFilterDebugCheck.Checked = Config.VerboseFilterDebug;
		}
		
		private void FillInDefaults()
		{
			exchangeServerTxt.Text = Config.ExchangeServer;
			userNameTxt.Text = Config.UserName;
			probeIntervalTxt.Value = Config.ProbeIntervalSecs;
			defaultMboxTxt.Text = Config.DefaultMailbox;
			filterLocationTxt.Text = Config.FilterFilename;
			logActionsCheck.Checked = Config.LogActions;

			// Setting up all of the fields above will trigger the 'changed' events
			// so we need to reset all of the button states and booleans that track changes.
			uriOptionChanged = false;
			filterLocationOptionChanged = false;
			miscOptionsChanged = false;
			optionsResetButton.Enabled = false;
			optionsApplyButton.Enabled = false;
		}

		private bool TestConnection(Uri uri)
		{
			WebRequest req;
			WebResponse resp;

			if (uri == null) 
			{
				return false;
			}

			try 
			{
				req = WebRequest.Create(uri);
				req.Method = "OPTIONS";
				req.Credentials = System.Net.CredentialCache.DefaultCredentials;
				resp = req.GetResponse();
			}
			catch (WebException e)
			{
				switch (e.Status)
				{
					case WebExceptionStatus.NameResolutionFailure :
						MessageBox.Show(String.Format("Unable to resolve hostname: {0}", uri.Host), 
							"Lookup Failure", MessageBoxButtons.OK);
						break;
					case WebExceptionStatus.ConnectFailure :
						MessageBox.Show(String.Format("Unable to open connection to remote server: {0}", uri.Host),
							"Connection Failure", MessageBoxButtons.OK);
						break;
//					case WebExceptionStatus.Timeout :
//						// TODO: server too slow
//						break;
//						// TODO: there must be some other failures for wrong login name, wrong default mailbox, etc.
					default :
						MessageBox.Show(e.Message, "Unknown Connection Failure", MessageBoxButtons.OK);
						break;
				}

				return false;
			}

			return true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.FileExitItem = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.helpContentsMenu = new System.Windows.Forms.MenuItem();
			this.helpLicenseItem = new System.Windows.Forms.MenuItem();
			this.menuItem10 = new System.Windows.Forms.MenuItem();
			this.HelpAboutMenu = new System.Windows.Forms.MenuItem();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.GeneralTab = new System.Windows.Forms.TabPage();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.normalPerDayLabel = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.spamPerDayLabel = new System.Windows.Forms.Label();
			this.spamPercentageLabel = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.spamFilteredLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.normalFilteredLabel = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.totalFilteredLabel = new System.Windows.Forms.Label();
			this.ApplicationGroup = new System.Windows.Forms.GroupBox();
			this.elapsedTimeLabel = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.StatusLabel = new System.Windows.Forms.Label();
			this.BayesTab = new System.Windows.Forms.TabPage();
			this.recomputeButton = new System.Windows.Forms.Button();
			this.bayesApplyButton = new System.Windows.Forms.Button();
			this.bayesResetButton = new System.Windows.Forms.Button();
			this.spamFolderTxt = new System.Windows.Forms.TextBox();
			this.spamCorpusLabel = new System.Windows.Forms.Label();
			this.nonSpamFolderTxt = new System.Windows.Forms.TextBox();
			this.nonSpamCorpusLabel = new System.Windows.Forms.Label();
			this.OptionsTab = new System.Windows.Forms.TabPage();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.browseButton = new System.Windows.Forms.Button();
			this.optionsApplyButton = new System.Windows.Forms.Button();
			this.optionsResetButton = new System.Windows.Forms.Button();
			this.label20 = new System.Windows.Forms.Label();
			this.label19 = new System.Windows.Forms.Label();
			this.defaultMboxTxt = new System.Windows.Forms.TextBox();
			this.label18 = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label16 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.filterLocationTxt = new System.Windows.Forms.TextBox();
			this.userNameTxt = new System.Windows.Forms.TextBox();
			this.exchangeServerTxt = new System.Windows.Forms.TextBox();
			this.label15 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.probeIntervalTxt = new System.Windows.Forms.NumericUpDown();
			this.logActionsCheck = new System.Windows.Forms.CheckBox();
			this.FiltersTab = new System.Windows.Forms.TabPage();
			this.editFiltersButton = new System.Windows.Forms.Button();
			this.label21 = new System.Windows.Forms.Label();
			this.filtersListBox = new System.Windows.Forms.CheckedListBox();
			this.openFilterDialog = new System.Windows.Forms.OpenFileDialog();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.systemTray = new System.Windows.Forms.NotifyIcon(this.components);
			this.verboseFilterDebugCheck = new System.Windows.Forms.CheckBox();
			this.tabControl.SuspendLayout();
			this.GeneralTab.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.ApplicationGroup.SuspendLayout();
			this.BayesTab.SuspendLayout();
			this.OptionsTab.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.probeIntervalTxt)).BeginInit();
			this.FiltersTab.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem1,
																					  this.menuItem3});
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.FileExitItem});
			this.menuItem1.Text = "&File";
			// 
			// FileExitItem
			// 
			this.FileExitItem.Index = 0;
			this.FileExitItem.Text = "Exit";
			this.FileExitItem.Click += new System.EventHandler(this.FileExitItem_Click);
			// 
			// menuItem3
			// 
			this.menuItem3.Index = 1;
			this.menuItem3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.helpContentsMenu,
																					  this.helpLicenseItem,
																					  this.menuItem10,
																					  this.HelpAboutMenu});
			this.menuItem3.Text = "&Help";
			// 
			// helpContentsMenu
			// 
			this.helpContentsMenu.Index = 0;
			this.helpContentsMenu.Text = "Contents...";
			this.helpContentsMenu.Click += new System.EventHandler(this.helpContentsMenu_Click);
			// 
			// helpLicenseItem
			// 
			this.helpLicenseItem.Index = 1;
			this.helpLicenseItem.Text = "License...";
			this.helpLicenseItem.Click += new System.EventHandler(this.helpLicenseItem_Click);
			// 
			// menuItem10
			// 
			this.menuItem10.Index = 2;
			this.menuItem10.Text = "-";
			// 
			// HelpAboutMenu
			// 
			this.HelpAboutMenu.Index = 3;
			this.HelpAboutMenu.Text = "About Sherpa Filters...";
			this.HelpAboutMenu.Click += new System.EventHandler(this.HelpAboutMenu_Click);
			// 
			// tabControl
			// 
			this.tabControl.Controls.Add(this.GeneralTab);
			this.tabControl.Controls.Add(this.BayesTab);
			this.tabControl.Controls.Add(this.OptionsTab);
			this.tabControl.Controls.Add(this.FiltersTab);
			this.tabControl.HotTrack = true;
			this.tabControl.Location = new System.Drawing.Point(8, 16);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(320, 320);
			this.tabControl.TabIndex = 0;
			this.tabControl.TabStop = false;
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabChanged);
			// 
			// GeneralTab
			// 
			this.GeneralTab.BackColor = System.Drawing.SystemColors.Control;
			this.GeneralTab.Controls.Add(this.groupBox1);
			this.GeneralTab.Controls.Add(this.ApplicationGroup);
			this.GeneralTab.Location = new System.Drawing.Point(4, 22);
			this.GeneralTab.Name = "GeneralTab";
			this.GeneralTab.Size = new System.Drawing.Size(312, 294);
			this.GeneralTab.TabIndex = 0;
			this.GeneralTab.Text = "General";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.normalPerDayLabel);
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.spamPerDayLabel);
			this.groupBox1.Controls.Add(this.spamPercentageLabel);
			this.groupBox1.Controls.Add(this.label9);
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.spamFilteredLabel);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.normalFilteredLabel);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.totalFilteredLabel);
			this.groupBox1.Location = new System.Drawing.Point(16, 96);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(280, 136);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Filtering";
			// 
			// normalPerDayLabel
			// 
			this.normalPerDayLabel.Location = new System.Drawing.Point(160, 72);
			this.normalPerDayLabel.Name = "normalPerDayLabel";
			this.normalPerDayLabel.Size = new System.Drawing.Size(80, 16);
			this.normalPerDayLabel.TabIndex = 13;
			this.normalPerDayLabel.Text = "0";
			this.normalPerDayLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(48, 72);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(104, 16);
			this.label7.TabIndex = 12;
			this.label7.Text = "Normal per day:";
			// 
			// spamPerDayLabel
			// 
			this.spamPerDayLabel.Location = new System.Drawing.Point(160, 88);
			this.spamPerDayLabel.Name = "spamPerDayLabel";
			this.spamPerDayLabel.Size = new System.Drawing.Size(80, 16);
			this.spamPerDayLabel.TabIndex = 11;
			this.spamPerDayLabel.Text = "0";
			this.spamPerDayLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// spamPercentageLabel
			// 
			this.spamPercentageLabel.Location = new System.Drawing.Point(160, 108);
			this.spamPercentageLabel.Name = "spamPercentageLabel";
			this.spamPercentageLabel.Size = new System.Drawing.Size(80, 16);
			this.spamPercentageLabel.TabIndex = 10;
			this.spamPercentageLabel.Text = "0%";
			this.spamPercentageLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(48, 88);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(104, 16);
			this.label9.TabIndex = 9;
			this.label9.Text = "Spam per day:";
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(32, 108);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(104, 16);
			this.label8.TabIndex = 8;
			this.label8.Text = "Spam Percentage:";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(48, 56);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(104, 16);
			this.label6.TabIndex = 6;
			this.label6.Text = "Spam:";
			// 
			// spamFilteredLabel
			// 
			this.spamFilteredLabel.Location = new System.Drawing.Point(136, 56);
			this.spamFilteredLabel.Name = "spamFilteredLabel";
			this.spamFilteredLabel.Size = new System.Drawing.Size(104, 16);
			this.spamFilteredLabel.TabIndex = 7;
			this.spamFilteredLabel.Text = "0";
			this.spamFilteredLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(48, 40);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(104, 16);
			this.label4.TabIndex = 4;
			this.label4.Text = "Normal:";
			// 
			// normalFilteredLabel
			// 
			this.normalFilteredLabel.Location = new System.Drawing.Point(136, 40);
			this.normalFilteredLabel.Name = "normalFilteredLabel";
			this.normalFilteredLabel.Size = new System.Drawing.Size(104, 16);
			this.normalFilteredLabel.TabIndex = 5;
			this.normalFilteredLabel.Text = "0";
			this.normalFilteredLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(32, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(104, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "Messages Filtered:";
			// 
			// totalFilteredLabel
			// 
			this.totalFilteredLabel.Location = new System.Drawing.Point(136, 24);
			this.totalFilteredLabel.Name = "totalFilteredLabel";
			this.totalFilteredLabel.Size = new System.Drawing.Size(104, 16);
			this.totalFilteredLabel.TabIndex = 3;
			this.totalFilteredLabel.Text = "0";
			this.totalFilteredLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// ApplicationGroup
			// 
			this.ApplicationGroup.Controls.Add(this.elapsedTimeLabel);
			this.ApplicationGroup.Controls.Add(this.label3);
			this.ApplicationGroup.Controls.Add(this.label2);
			this.ApplicationGroup.Controls.Add(this.StatusLabel);
			this.ApplicationGroup.Location = new System.Drawing.Point(16, 16);
			this.ApplicationGroup.Name = "ApplicationGroup";
			this.ApplicationGroup.Size = new System.Drawing.Size(280, 72);
			this.ApplicationGroup.TabIndex = 4;
			this.ApplicationGroup.TabStop = false;
			this.ApplicationGroup.Text = "Application";
			// 
			// elapsedTimeLabel
			// 
			this.elapsedTimeLabel.Location = new System.Drawing.Point(72, 40);
			this.elapsedTimeLabel.Name = "elapsedTimeLabel";
			this.elapsedTimeLabel.Size = new System.Drawing.Size(192, 16);
			this.elapsedTimeLabel.TabIndex = 5;
			this.elapsedTimeLabel.Text = "00:00:00";
			this.elapsedTimeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 40);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(104, 16);
			this.label3.TabIndex = 4;
			this.label3.Text = "Running:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(16, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(104, 16);
			this.label2.TabIndex = 1;
			this.label2.Text = "Status:";
			// 
			// StatusLabel
			// 
			this.StatusLabel.Location = new System.Drawing.Point(72, 24);
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Size = new System.Drawing.Size(192, 16);
			this.StatusLabel.TabIndex = 2;
			this.StatusLabel.Text = "Loading Application";
			this.StatusLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// BayesTab
			// 
			this.BayesTab.Controls.Add(this.recomputeButton);
			this.BayesTab.Controls.Add(this.bayesApplyButton);
			this.BayesTab.Controls.Add(this.bayesResetButton);
			this.BayesTab.Controls.Add(this.spamFolderTxt);
			this.BayesTab.Controls.Add(this.spamCorpusLabel);
			this.BayesTab.Controls.Add(this.nonSpamFolderTxt);
			this.BayesTab.Controls.Add(this.nonSpamCorpusLabel);
			this.BayesTab.Location = new System.Drawing.Point(4, 22);
			this.BayesTab.Name = "BayesTab";
			this.BayesTab.Size = new System.Drawing.Size(312, 294);
			this.BayesTab.TabIndex = 4;
			this.BayesTab.Text = "Bayes";
			// 
			// recomputeButton
			// 
			this.recomputeButton.Enabled = false;
			this.recomputeButton.Location = new System.Drawing.Point(56, 120);
			this.recomputeButton.Name = "recomputeButton";
			this.recomputeButton.Size = new System.Drawing.Size(192, 23);
			this.recomputeButton.TabIndex = 11;
			this.recomputeButton.Text = "Recompute Bayes Probabilities...";
			this.recomputeButton.Click += new System.EventHandler(this.RecomputeBayes);
			// 
			// bayesApplyButton
			// 
			this.bayesApplyButton.Enabled = false;
			this.bayesApplyButton.Location = new System.Drawing.Point(136, 260);
			this.bayesApplyButton.Name = "bayesApplyButton";
			this.bayesApplyButton.TabIndex = 9;
			this.bayesApplyButton.Text = "Apply";
			this.bayesApplyButton.Click += new System.EventHandler(this.bayesApplyButton_Click);
			// 
			// bayesResetButton
			// 
			this.bayesResetButton.Enabled = false;
			this.bayesResetButton.Location = new System.Drawing.Point(224, 260);
			this.bayesResetButton.Name = "bayesResetButton";
			this.bayesResetButton.TabIndex = 10;
			this.bayesResetButton.Text = "Reset";
			this.bayesResetButton.Click += new System.EventHandler(this.bayesResetButton_Click);
			// 
			// spamFolderTxt
			// 
			this.spamFolderTxt.Location = new System.Drawing.Point(16, 80);
			this.spamFolderTxt.Name = "spamFolderTxt";
			this.spamFolderTxt.Size = new System.Drawing.Size(264, 20);
			this.spamFolderTxt.TabIndex = 6;
			this.spamFolderTxt.Text = "";
			this.toolTip1.SetToolTip(this.spamFolderTxt, "The partial pathname of the mailbox folder\nthat contains a collection of spam ema" +
				"ils\n(i.e. Mailboxes/Sherpa/SpamCorpus)");
			this.spamFolderTxt.TextChanged += new System.EventHandler(this.CorpusChanged);
			// 
			// spamCorpusLabel
			// 
			this.spamCorpusLabel.Location = new System.Drawing.Point(16, 64);
			this.spamCorpusLabel.Name = "spamCorpusLabel";
			this.spamCorpusLabel.Size = new System.Drawing.Size(104, 16);
			this.spamCorpusLabel.TabIndex = 5;
			this.spamCorpusLabel.Text = "Spam Corpus:";
			// 
			// nonSpamFolderTxt
			// 
			this.nonSpamFolderTxt.Location = new System.Drawing.Point(16, 32);
			this.nonSpamFolderTxt.Name = "nonSpamFolderTxt";
			this.nonSpamFolderTxt.Size = new System.Drawing.Size(264, 20);
			this.nonSpamFolderTxt.TabIndex = 4;
			this.nonSpamFolderTxt.Text = "";
			this.toolTip1.SetToolTip(this.nonSpamFolderTxt, "The partial pathname of the mailbox folder\nthat contains a collection of normal e" +
				"mails\n(i.e. Mailboxes/Sherpa/NonSpamCorpus)");
			this.nonSpamFolderTxt.TextChanged += new System.EventHandler(this.CorpusChanged);
			// 
			// nonSpamCorpusLabel
			// 
			this.nonSpamCorpusLabel.Location = new System.Drawing.Point(16, 16);
			this.nonSpamCorpusLabel.Name = "nonSpamCorpusLabel";
			this.nonSpamCorpusLabel.Size = new System.Drawing.Size(104, 16);
			this.nonSpamCorpusLabel.TabIndex = 3;
			this.nonSpamCorpusLabel.Text = "Non-Spam Corpus:";
			// 
			// OptionsTab
			// 
			this.OptionsTab.Controls.Add(this.groupBox4);
			this.OptionsTab.Controls.Add(this.browseButton);
			this.OptionsTab.Controls.Add(this.optionsApplyButton);
			this.OptionsTab.Controls.Add(this.optionsResetButton);
			this.OptionsTab.Controls.Add(this.label20);
			this.OptionsTab.Controls.Add(this.label19);
			this.OptionsTab.Controls.Add(this.defaultMboxTxt);
			this.OptionsTab.Controls.Add(this.label18);
			this.OptionsTab.Controls.Add(this.label17);
			this.OptionsTab.Controls.Add(this.groupBox3);
			this.OptionsTab.Controls.Add(this.label16);
			this.OptionsTab.Controls.Add(this.groupBox2);
			this.OptionsTab.Controls.Add(this.filterLocationTxt);
			this.OptionsTab.Controls.Add(this.userNameTxt);
			this.OptionsTab.Controls.Add(this.exchangeServerTxt);
			this.OptionsTab.Controls.Add(this.label15);
			this.OptionsTab.Controls.Add(this.label14);
			this.OptionsTab.Controls.Add(this.label13);
			this.OptionsTab.Controls.Add(this.label12);
			this.OptionsTab.Controls.Add(this.probeIntervalTxt);
			this.OptionsTab.Controls.Add(this.logActionsCheck);
			this.OptionsTab.Location = new System.Drawing.Point(4, 22);
			this.OptionsTab.Name = "OptionsTab";
			this.OptionsTab.Size = new System.Drawing.Size(312, 294);
			this.OptionsTab.TabIndex = 1;
			this.OptionsTab.Text = "Options";
			// 
			// groupBox4
			// 
			this.groupBox4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.groupBox4.Location = new System.Drawing.Point(92, 196);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(204, 8);
			this.groupBox4.TabIndex = 15;
			this.groupBox4.TabStop = false;
			// 
			// browseButton
			// 
			this.browseButton.Location = new System.Drawing.Point(224, 55);
			this.browseButton.Name = "browseButton";
			this.browseButton.Size = new System.Drawing.Size(72, 23);
			this.browseButton.TabIndex = 1;
			this.browseButton.Text = "Browse...";
			this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
			// 
			// optionsApplyButton
			// 
			this.optionsApplyButton.Enabled = false;
			this.optionsApplyButton.Location = new System.Drawing.Point(136, 260);
			this.optionsApplyButton.Name = "optionsApplyButton";
			this.optionsApplyButton.TabIndex = 7;
			this.optionsApplyButton.Text = "Apply";
			this.optionsApplyButton.Click += new System.EventHandler(this.optionsApplyButton_Click);
			// 
			// optionsResetButton
			// 
			this.optionsResetButton.Enabled = false;
			this.optionsResetButton.Location = new System.Drawing.Point(224, 260);
			this.optionsResetButton.Name = "optionsResetButton";
			this.optionsResetButton.TabIndex = 8;
			this.optionsResetButton.Text = "Reset";
			this.optionsResetButton.Click += new System.EventHandler(this.optionsResetButton_Click);
			// 
			// label20
			// 
			this.label20.Location = new System.Drawing.Point(16, 196);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(80, 16);
			this.label20.TabIndex = 16;
			this.label20.Tag = "";
			this.label20.Text = "Misc. Settings";
			// 
			// label19
			// 
			this.label19.Location = new System.Drawing.Point(101, 236);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(187, 16);
			this.label19.TabIndex = 14;
			this.label19.Text = "secs between probes for new email";
			// 
			// defaultMboxTxt
			// 
			this.defaultMboxTxt.Location = new System.Drawing.Point(176, 168);
			this.defaultMboxTxt.Name = "defaultMboxTxt";
			this.defaultMboxTxt.TabIndex = 4;
			this.defaultMboxTxt.Text = "Inbox";
			this.defaultMboxTxt.TextChanged += new System.EventHandler(this.UriComponentChanged);
			// 
			// label18
			// 
			this.label18.Location = new System.Drawing.Point(176, 152);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(104, 16);
			this.label18.TabIndex = 12;
			this.label18.Text = "Default Mailbox:";
			// 
			// label17
			// 
			this.label17.Location = new System.Drawing.Point(16, 88);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(80, 16);
			this.label17.TabIndex = 11;
			this.label17.Tag = "";
			this.label17.Text = "Email Settings";
			// 
			// groupBox3
			// 
			this.groupBox3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.groupBox3.Location = new System.Drawing.Point(96, 88);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(200, 8);
			this.groupBox3.TabIndex = 10;
			this.groupBox3.TabStop = false;
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(16, 16);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(80, 16);
			this.label16.TabIndex = 9;
			this.label16.Tag = "";
			this.label16.Text = "Filter Settings";
			// 
			// groupBox2
			// 
			this.groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.groupBox2.Location = new System.Drawing.Point(96, 17);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(200, 8);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			// 
			// filterLocationTxt
			// 
			this.filterLocationTxt.Location = new System.Drawing.Point(16, 56);
			this.filterLocationTxt.Name = "filterLocationTxt";
			this.filterLocationTxt.Size = new System.Drawing.Size(200, 20);
			this.filterLocationTxt.TabIndex = 0;
			this.filterLocationTxt.Text = "";
			this.filterLocationTxt.TextChanged += new System.EventHandler(this.FilterLocationChanged);
			// 
			// userNameTxt
			// 
			this.userNameTxt.Location = new System.Drawing.Point(176, 128);
			this.userNameTxt.Name = "userNameTxt";
			this.userNameTxt.TabIndex = 3;
			this.userNameTxt.Text = "";
			this.userNameTxt.TextChanged += new System.EventHandler(this.UriComponentChanged);
			// 
			// exchangeServerTxt
			// 
			this.exchangeServerTxt.Location = new System.Drawing.Point(16, 128);
			this.exchangeServerTxt.Name = "exchangeServerTxt";
			this.exchangeServerTxt.Size = new System.Drawing.Size(128, 20);
			this.exchangeServerTxt.TabIndex = 2;
			this.exchangeServerTxt.Text = "";
			this.exchangeServerTxt.TextChanged += new System.EventHandler(this.UriComponentChanged);
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point(16, 236);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(32, 16);
			this.label15.TabIndex = 3;
			this.label15.Text = "Wait";
			// 
			// label14
			// 
			this.label14.Location = new System.Drawing.Point(16, 40);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(144, 16);
			this.label14.TabIndex = 2;
			this.label14.Text = "Sherpa Filter location:";
			// 
			// label13
			// 
			this.label13.Location = new System.Drawing.Point(176, 112);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(104, 16);
			this.label13.TabIndex = 1;
			this.label13.Text = "User Name:";
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(16, 112);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(104, 16);
			this.label12.TabIndex = 0;
			this.label12.Text = "Exchange Server:";
			// 
			// probeIntervalTxt
			// 
			this.probeIntervalTxt.Location = new System.Drawing.Point(48, 236);
			this.probeIntervalTxt.Maximum = new System.Decimal(new int[] {
																			 1000000,
																			 0,
																			 0,
																			 0});
			this.probeIntervalTxt.Minimum = new System.Decimal(new int[] {
																			 1,
																			 0,
																			 0,
																			 0});
			this.probeIntervalTxt.Name = "probeIntervalTxt";
			this.probeIntervalTxt.Size = new System.Drawing.Size(48, 20);
			this.probeIntervalTxt.TabIndex = 6;
			this.probeIntervalTxt.Value = new System.Decimal(new int[] {
																		   15,
																		   0,
																		   0,
																		   0});
			this.probeIntervalTxt.TextChanged += new System.EventHandler(this.MiscSettingsChanged);
			this.probeIntervalTxt.ValueChanged += new System.EventHandler(this.MiscSettingsChanged);
			// 
			// logActionsCheck
			// 
			this.logActionsCheck.Location = new System.Drawing.Point(20, 211);
			this.logActionsCheck.Name = "logActionsCheck";
			this.logActionsCheck.Size = new System.Drawing.Size(244, 24);
			this.logActionsCheck.TabIndex = 5;
			this.logActionsCheck.Text = "Log actions and errors to Event Log";
			this.logActionsCheck.CheckedChanged += new System.EventHandler(this.MiscSettingsChanged);
			// 
			// FiltersTab
			// 
			this.FiltersTab.Controls.Add(this.verboseFilterDebugCheck);
			this.FiltersTab.Controls.Add(this.editFiltersButton);
			this.FiltersTab.Controls.Add(this.label21);
			this.FiltersTab.Controls.Add(this.filtersListBox);
			this.FiltersTab.Location = new System.Drawing.Point(4, 22);
			this.FiltersTab.Name = "FiltersTab";
			this.FiltersTab.Size = new System.Drawing.Size(312, 294);
			this.FiltersTab.TabIndex = 3;
			this.FiltersTab.Text = "Filters";
			this.toolTip1.SetToolTip(this.FiltersTab, "Does a Console.WriteLine of every single test performed on each mail message.");
			// 
			// editFiltersButton
			// 
			this.editFiltersButton.Location = new System.Drawing.Point(64, 40);
			this.editFiltersButton.Name = "editFiltersButton";
			this.editFiltersButton.Size = new System.Drawing.Size(184, 23);
			this.editFiltersButton.TabIndex = 2;
			this.editFiltersButton.Text = "Edit Filters... (v1.0)";
			this.editFiltersButton.Click += new System.EventHandler(this.editFiltersButton_Click);
			// 
			// label21
			// 
			this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label21.ForeColor = System.Drawing.SystemColors.GrayText;
			this.label21.Location = new System.Drawing.Point(8, 96);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(248, 40);
			this.label21.TabIndex = 1;
			this.label21.Text = "Coming in v2.0...";
			// 
			// filtersListBox
			// 
			this.filtersListBox.ForeColor = System.Drawing.SystemColors.GrayText;
			this.filtersListBox.Items.AddRange(new object[] {
																"IN: Default Message",
																"DELETE: Spam Headers",
																"LIST: Outlook Developers",
																"ETC: etc. etc."});
			this.filtersListBox.Location = new System.Drawing.Point(8, 136);
			this.filtersListBox.Name = "filtersListBox";
			this.filtersListBox.ScrollAlwaysVisible = true;
			this.filtersListBox.Size = new System.Drawing.Size(264, 154);
			this.filtersListBox.TabIndex = 0;
			// 
			// openFilterDialog
			// 
			this.openFilterDialog.DefaultExt = "xml";
			this.openFilterDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
			this.openFilterDialog.Title = "Load Filter";
			// 
			// toolTip1
			// 
			this.toolTip1.AutoPopDelay = 7500;
			this.toolTip1.InitialDelay = 500;
			this.toolTip1.ReshowDelay = 100;
			// 
			// systemTray
			// 
			this.systemTray.Icon = ((System.Drawing.Icon)(resources.GetObject("systemTray.Icon")));
			this.systemTray.Text = "Sherpa Filters";
			this.systemTray.Visible = true;
			this.systemTray.DoubleClick += new System.EventHandler(this.systemTray_Click);
			this.systemTray.Click += new System.EventHandler(this.systemTray_Click);
			// 
			// verboseFilterDebugCheck
			// 
			this.verboseFilterDebugCheck.Location = new System.Drawing.Point(88, 64);
			this.verboseFilterDebugCheck.Name = "verboseFilterDebugCheck";
			this.verboseFilterDebugCheck.Size = new System.Drawing.Size(136, 24);
			this.verboseFilterDebugCheck.TabIndex = 3;
			this.verboseFilterDebugCheck.Text = "Verbose Filter Debug";
			this.verboseFilterDebugCheck.CheckedChanged += new System.EventHandler(this.verboseFilterDebugCheck_CheckedChanged);
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(336, 345);
			this.Controls.Add(this.tabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Menu = this.mainMenu1;
			this.Name = "MainForm";
			this.Text = "Sherpa Filters";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
			this.tabControl.ResumeLayout(false);
			this.GeneralTab.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.ApplicationGroup.ResumeLayout(false);
			this.BayesTab.ResumeLayout(false);
			this.OptionsTab.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.probeIntervalTxt)).EndInit();
			this.FiltersTab.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new MainForm());
		}

		public void Exit()
		{
			if (filterThread != null) 
			{
				filterThread.Abort();
				filterThread.Join();
			}
			Application.Exit();
		}

		private void EditorFormClosed(object sender, System.EventArgs e)
		{
			if (filterThread == null || filterThread.IsAlive == false)
				TestAndSpawn();

			// get rid of reference to the form so we can recreate it later.
			fEditor = null;
		}

		private void FileExitItem_Click(object sender, System.EventArgs e)
		{
			this.Exit();
		}

		/// <summary>
		/// Don't allow the MainForm to close. Just hide the form and cancel the request to 
		/// close the form. You can always get the form back by clicking on the icon in the
		/// system tray.
		/// </summary>
		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			this.Hide();
			e.Cancel = true;

//			if (filterThread != null) 
//			{
//				filterThread.Abort();
//				filterThread.Join();
//			}
			// application will exit at this point.
		}

		#region Options Handling Code

		private void browseButton_Click(object sender, System.EventArgs e)
		{
			if(openFilterDialog.ShowDialog() == DialogResult.OK)
			{
				filterLocationTxt.Text = openFilterDialog.FileName;
				filterLocationOptionChanged = true; // probably already set by the event handler for 'text changed'
			}
		}

		private void optionsResetButton_Click(object sender, System.EventArgs e)
		{
			// Ignore any changes the user made and fill in the fields from the 
			// config object.
			FillInDefaults();
		}

		private void optionsApplyButton_Click(object sender, System.EventArgs e)
		{
			bool restartThread = false;

			// Check and apply every option on the panel

			//
			// Filter Location
			//
			if (filterLocationOptionChanged)
			{
				if (filterLocationTxt.Text != Config.FilterFilename)
				{
					Config.FilterFilename = filterLocationTxt.Text;
					restartThread = true;
				}
			}

			// URI Components

			if (uriOptionChanged) 
			{
				Uri newUri = new Uri(String.Format("http://{0}/exchange/{1}/{2}", 
					exchangeServerTxt.Text, userNameTxt.Text, defaultMboxTxt.Text));
				if (TestConnection(newUri)) 
				{
					// set all of the values
					Config.ExchangeServer = exchangeServerTxt.Text;
					Config.UserName = userNameTxt.Text;
					Config.DefaultMailbox = defaultMboxTxt.Text;
					restartThread = true;
				}
				else
				{
					// user will have been given error panels already. Just reset all fields
					exchangeServerTxt.Text = Config.ExchangeServer;
					userNameTxt.Text = Config.UserName;
					defaultMboxTxt.Text = Config.DefaultMailbox;
				}
			}

			//
			// Probe Interval, Log Actions, etc.
			//
			if (miscOptionsChanged) 
			{
				Config.ProbeIntervalSecs = (Int32)probeIntervalTxt.Value;
				Config.LogActions = logActionsCheck.Checked;
			}

			if (restartThread == true)
			{
				if (filterThread != null) 
				{
					staticStatusLabel.Text = "Aborting Worker Thread";
					filterThread.Abort();
					filterThread.Join();
				}
				SpawnFilterThread();
			}


			// reset bools and buttons
			uriOptionChanged = false;
			filterLocationOptionChanged = false;
			miscOptionsChanged = false;
			optionsApplyButton.Enabled = false;
			optionsResetButton.Enabled = false;
		}

		private void UriComponentChanged(object sender, System.EventArgs e)
		{
			uriOptionChanged = true;
			optionsApplyButton.Enabled = true;
			optionsResetButton.Enabled = true;
		}

		private void FilterLocationChanged(object sender, System.EventArgs e)
		{
			filterLocationOptionChanged = true;
			optionsApplyButton.Enabled = true;
			optionsResetButton.Enabled = true;
		}

		private void MiscSettingsChanged(object sender, System.EventArgs e)
		{
			miscOptionsChanged = true;
			optionsApplyButton.Enabled = true;
			optionsResetButton.Enabled = true;
		}


		#endregion

		private void HelpAboutMenu_Click(object sender, System.EventArgs e)
		{
			About about = new About();
			about.Show();
		}

		private void editFiltersButton_Click(object sender, System.EventArgs e)
		{
			if (fEditor == null) 
			{
				fEditor = new FilterEditor();
				fEditor.Closed += new System.EventHandler(this.EditorFormClosed);
			}

			fEditor.Show();
			fEditor.Activate();
		}

		private void tabChanged(object sender, System.EventArgs e)
		{
			TabControl tc = (TabControl)sender;
			if (tc.SelectedIndex == 2) // OptionsTab
				FillInDefaults();
			else if (tc.SelectedIndex == 1) // BayesTab
				FillInBayesDefaults();
			else if (tc.SelectedIndex == 3) // FilterTab
				FillInFilterDefaults();
		}

		private void FillInBayesDefaults()
		{
			spamFolderTxt.Text = Config.SpamCorpus;
			nonSpamFolderTxt.Text = Config.NonSpamCorpus;

			// Setting up all of the fields above will trigger the 'changed' events
			// so we need to reset all of the button states and booleans that track changes.
			bayesResetButton.Enabled = false;
			bayesApplyButton.Enabled = false;
		}

		private void bayesResetButton_Click(object sender, System.EventArgs e)
		{
			// Ignore any changes the user made and fill in the fields from the 
			// config object.
			FillInBayesDefaults();
		}

		private void bayesApplyButton_Click(object sender, System.EventArgs e)
		{
			Config.SpamCorpus = spamFolderTxt.Text;
			Config.NonSpamCorpus = nonSpamFolderTxt.Text;

			// reset buttons
			bayesApplyButton.Enabled = false;
			bayesResetButton.Enabled = false;
		}

		/// <summary>
		/// The event called whenever someone edits the text fields that specify which
		/// mailbox to use for spam and nonspam corpora.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CorpusChanged(object sender, System.EventArgs e)
		{
			bayesApplyButton.Enabled = true;
			bayesResetButton.Enabled = true;
		}

		private void RecomputeBayes(object sender, System.EventArgs e)
		{
			if (filterCtrl == null)
			{
				// TODO: error message saying that a thread isn't running
				return;
			}

			// kill off existing thread.
			// remove the serialized file on disk.
			// spawn off a new thread.
		}

		private void helpContentsMenu_Click(object sender, System.EventArgs e)
		{
			Help.ShowHelp(this, "SherpaHelp.chm");
		}

		private void helpLicenseItem_Click(object sender, System.EventArgs e)
		{
			Form licenseForm = new License();
			licenseForm.Show();
		}

		private void systemTray_Click(object sender, System.EventArgs e)
		{
			this.Show();
			//this.Visible = true;
			this.Activate();
		}

		private void verboseFilterDebugCheck_CheckedChanged(object sender, System.EventArgs e)
		{
			Config.VerboseFilterDebug = verboseFilterDebugCheck.Checked;
		}

	}
}
