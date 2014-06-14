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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace Sherpa
{
	/// <summary>
	/// Summary description for FilterEditor.
	/// </summary>
	public class FilterEditor : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox filterTextBox;
		private System.Windows.Forms.Button saveButton;
		private System.Windows.Forms.Button cancelButton;
		private XmlDocument xmlDoc;
		private Boolean textChanged = false;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public FilterEditor()
		{
			XmlTextReader xmlReader;
			XmlValidatingReader vReader;
			String filterName;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			filterName = Config.FilterFilename;
			if (filterName == null || filterName == "")
				filterName = "file:///C:/Program Files/IH8SPAM/Sherpa Filters/TemplateFilter.xml";

			try
			{
				xmlDoc = new XmlDocument();
				xmlDoc.PreserveWhitespace = true;
				xmlReader = new XmlTextReader(filterName);
				vReader = new XmlValidatingReader(xmlReader);
				xmlDoc.Load(vReader);
				vReader.Close();
				xmlReader.Close();
			}
			catch (Exception e)
			{
				switch (e.GetType().Name) 
				{
					case "XmlSchemaException" :
					case "XmlException" :
						string msgStr = e.Message;
						msgStr += "\n\nThe filters will not be loaded. Please edit the filters and try again.";
						MessageBox.Show(msgStr, "Loading XML Filters", MessageBoxButtons.OK);
						break;
					case "FileNotFoundException" :
					case "DirectoryNotFoundException" :
						// TODO: messagebox.show, clean this up.
						String msg = e.Message + "\n\nWe won't load something, blah blah";
						MessageBox.Show(msg, "Filter Not Found", MessageBoxButtons.OK);
						return;
					default :
						throw;
				}
			}

			filterTextBox.Text = xmlDoc.OuterXml; // TODO: this will probably fail if the XML is bad.
			filterTextBox.Select(0,0);
			textChanged = false;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
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
			this.filterTextBox = new System.Windows.Forms.TextBox();
			this.saveButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.SuspendLayout();
			// 
			// filterTextBox
			// 
			this.filterTextBox.AcceptsReturn = true;
			this.filterTextBox.AcceptsTab = true;
			this.filterTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.filterTextBox.Font = new System.Drawing.Font("Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.filterTextBox.Location = new System.Drawing.Point(0, 0);
			this.filterTextBox.MaxLength = 2000000;
			this.filterTextBox.Multiline = true;
			this.filterTextBox.Name = "filterTextBox";
			this.filterTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.filterTextBox.Size = new System.Drawing.Size(864, 648);
			this.filterTextBox.TabIndex = 0;
			this.filterTextBox.Text = "";
			this.filterTextBox.WordWrap = false;
			this.filterTextBox.TextChanged += new System.EventHandler(this.FilterTextChanged);
			// 
			// saveButton
			// 
			this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.saveButton.Location = new System.Drawing.Point(680, 656);
			this.saveButton.Name = "saveButton";
			this.saveButton.TabIndex = 1;
			this.saveButton.Text = "Save";
			this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(768, 656);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.TabIndex = 2;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// saveFileDialog
			// 
			this.saveFileDialog.DefaultExt = "xml";
			this.saveFileDialog.FileName = "SherpaFilter.xml";
			this.saveFileDialog.Filter = "XML Files (*.xml)|*.xml";
			// 
			// FilterEditor
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(864, 686);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.saveButton);
			this.Controls.Add(this.filterTextBox);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(200, 200);
			this.Name = "FilterEditor";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "Filter Editor";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.FormClosing);
			this.ResumeLayout(false);

		}
		#endregion

		private void saveButton_Click(object sender, System.EventArgs eArgs)
		{
			if (Config.FilterFilename == "") 
			{
				// bring up the save as panel if the filterfilename hasn't been specified.
				if (saveFileDialog.ShowDialog() == DialogResult.OK)
					Config.FilterFilename = saveFileDialog.FileName;
				else
					return; // keep the editor open to force them to "cancel" their changes.
			}

			String filterName = Config.FilterFilename;
			String tmpName = filterName + ".tmp";
			String bakName = filterName + ".bak";

			// Save the text box text out to disk
			FileStream fs = File.Create(tmpName);
			UTF8Encoding encoding = new UTF8Encoding();
			Byte[] buffer = encoding.GetBytes(filterTextBox.Text);
			fs.Write(buffer, 0, buffer.Length);
			fs.Close();

			// Load it back in to verify that the format is right
			// HACK: Yuck, we already have code for this above, we should reuse it.
			XmlDocument tmpDoc = new XmlDocument();
			tmpDoc.PreserveWhitespace = true;
			XmlTextReader xmlReader = new XmlTextReader(tmpName);
			XmlValidatingReader vReader = new XmlValidatingReader(xmlReader);

			try
			{
				tmpDoc.Load(vReader);
				vReader.Close();
				xmlReader.Close();

				MainForm.reloadMutex.WaitOne();
				MainForm.reloadFlag = true;
				File.Delete(bakName);
				try
				{File.Move(filterName, bakName);}
				catch
				{}
				File.Move(tmpName, filterName);
				MainForm.reloadMutex.ReleaseMutex();
				textChanged = false;
				this.Close();
			}
			catch (Exception e)
			{
				switch (e.GetType().Name) 
				{
					case "XmlException" :
					case "XmlSchemaException" :

						// clean up the readers, open files, and saved tmp file
						vReader.Close();
						xmlReader.Close();
						File.Delete(tmpName); // will not throw an exception for 'file not found'

						String msg = e.Message + "\n\n";
						msg += "Would you like to fix the error or discard the changes?\n\n";
						msg += "Yes: fix the error\n";
						msg += "No: discard the changes\n";
						if (MessageBox.Show(msg, "XML Format Error", MessageBoxButtons.YesNo) ==
							DialogResult.Yes) 
						{
							// TODO: it would be nice to select the error, but it is a little difficult
							// to calculate.

							// attempting to fix error, keep the window around.
							return;
						}
						break;
					default :
						throw;
				}						
			}

			this.Close();
		}

		private void cancelButton_Click(object sender, System.EventArgs e)
		{
			if (textChanged) 
			{
				if (DialogResult.OK == MessageBox.Show("You have made changes to the filter. Do you want to discard those changes?",
					"Discard Changes", MessageBoxButtons.OKCancel)) 
				{
					this.Close();
				}
			}
			else
			{
				this.Close();
			}
			// user changed their mind and wanted to keep the window around to save later.
		}

		private void FilterTextChanged(object sender, System.EventArgs e)
		{
			textChanged = true;
		}

		private void FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (textChanged) 
			{
				if (DialogResult.Cancel == MessageBox.Show("You have made changes to the filter. Do you want to discard those changes?",
					"Discard Changes", MessageBoxButtons.OKCancel)) 
				{
					e.Cancel = true;
				}
			}
		}
	}
}
