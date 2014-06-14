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
using System.Windows.Forms;

namespace Sherpa
{
	/// <summary>
	/// Summary description for About.
	/// </summary>
	public class About : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.PictureBox pictureBox1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public About()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(About));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(112, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(168, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "IH8SPAM.com (c) Sherpa Filters";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(112, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(144, 23);
			this.label2.TabIndex = 1;
			this.label2.Text = "Version 1.1.0.0";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(112, 48);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(192, 23);
			this.label3.TabIndex = 2;
			this.label3.Text = "Copyright 2002-2006 IH8SPAM.com";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 88);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(288, 56);
			this.label4.TabIndex = 3;
			this.label4.Text = "This program is \'sold\' as Postcard-Ware. Please send a postcard from your hometow" +
				"n or your next exotic vacation to \"Head Sherpa; 2255 Showers Dr Apt 196; Mountai" +
				"n View, CA 94040-1279\"";
			this.label4.Click += new System.EventHandler(this.label4_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(40, 24);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(32, 32);
			this.pictureBox1.TabIndex = 4;
			this.pictureBox1.TabStop = false;
			// 
			// About
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(304, 150);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "About";
			this.Text = "About";
			this.ResumeLayout(false);

		}
		#endregion

		private void label4_Click(object sender, System.EventArgs e)
		{
		
		}

	}
}
