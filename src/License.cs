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
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Sherpa
{
	/// <summary>
	/// Summary description for License.
	/// </summary>
	public class License : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox licenseText;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public License()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			FileStream gplStream = File.OpenRead("GPL.txt");
			Byte[] gplBytes = new Byte[16384];
			gplStream.Read(gplBytes, 0, 16384);
			licenseText.Text = Encoding.ASCII.GetString(gplBytes, 0, 16384);

			licenseText.Select(0, 0);
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
			this.licenseText = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// licenseText
			// 
			this.licenseText.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right);
			this.licenseText.Multiline = true;
			this.licenseText.Name = "licenseText";
			this.licenseText.ReadOnly = true;
			this.licenseText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.licenseText.Size = new System.Drawing.Size(488, 456);
			this.licenseText.TabIndex = 0;
			this.licenseText.Text = "";
			// 
			// License
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(488, 454);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.licenseText});
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(100, 100);
			this.Name = "License";
			this.Text = "License";
			this.ResumeLayout(false);

		}
		#endregion

	}
}
