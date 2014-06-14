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
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace Sherpa
{
	/// <summary>
	/// Summary description for FilterSet.
	/// </summary>
	public class FilterSet
	{
		public XmlDocument xmlDoc;
		private ArrayList filters;
		private Int32 totalFilteredCount;
		private Int32 normalFilteredCount;
		private Int32 spamFilteredCount;
		private DateTime filterStartTime;
		private TimeSpan oneDay;
		private TimeSpan ts;

		public FilterSet()
		{
			filterStartTime = DateTime.Now;
			oneDay = TimeSpan.FromDays(1);
			filters = new ArrayList(20);

			try
			{
				LoadFilterFile();
				ProcessXml();
			}
			catch (FileNotFoundException)
			{
				// do nothing, the filters will already be empty and the rest of the 
				// code already deals with that case.
			}
		}

		public void Reload()
		{
			xmlDoc = null; // get rid of pre-existing document

			try
			{
				LoadFilterFile();
				ProcessXml();
			}
			catch (FileNotFoundException)
			{
				// already dealt with in LoadFilterFile()
			}
		}

		/// <summary>
		/// Reads the default filter file, verifies the XML against the schema, and creates the XmlDocument.
		/// </summary>
		private void LoadFilterFile()
		{
			XmlTextReader xmlReader;
			XmlValidatingReader vReader;

			String filterName = Config.FilterFilename;
			if (filterName == null || filterName == "")
				return;

			bool appExit = false;

			try
			{
				xmlDoc = new XmlDocument();
				xmlDoc.PreserveWhitespace = true;
				xmlReader = new XmlTextReader(filterName); // could throw filenotfound
				vReader = new XmlValidatingReader(xmlReader);

				xmlDoc.Load(vReader); // could throw xmlschema or xml exceptions
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

						appExit = true; // TODO: we should just bring up the editor at this point.
						return;
					case "FileNotFoundException" :
						MessageBox.Show("The filter file (" + filterName + ") could not be found. Go to the Options panel to specify a new file.",
							"Filter Not Found");
						return;
					default :
						throw;
				}
			}
			finally
			{
				// TODO: We really need to ask Sherpa to exit for us so it does the right thing
				// with cleaning up threads.
				if (appExit)
					Application.Exit();
			}
		}

		/// <summary>
		/// Takes the existing XmlDocument and breaks it apart and creates the Array of filters.
		/// </summary>
		public void ProcessXml()
		{
			filters.Clear();

			if (xmlDoc == null)
				return;

			CheckFilterVersion();

			foreach (XmlNode node in xmlDoc.DocumentElement.SelectNodes("Filter")) 
			{
				filters.Add(new Filter(node));
			}
		}

		/// <summary>
		/// Future versions of this routine will handle values other than '1' and will 
		/// automatically upgrade the filter to the later format.
		/// </summary>
		private void CheckFilterVersion()
		{
			Int32 vers = Int32.Parse(xmlDoc.SelectSingleNode("Filters").Attributes.GetNamedItem("Version").Value);
			if (vers != 1)
			{
				throw(new Exception("Invalid filter version number."));
			}
		}

		public void ApplyFilters(Email msg)
		{

			UpdateTimerDisplay();

			// I don't think that filters can be null here, it can only be empty.
			// TEST: what happens if filters is null here (not just empty, but null).
			if (filters.Count == 0) 
			{
				return; // no filters to apply
			}

			MainForm.staticStatusLabel.Text = "Testing Filter";
			foreach (Filter filter in filters)
			{
				if (filter.FilterEmail(msg))
				{
					Filter.TagTypes tagType = filter.TagType;
					if (tagType != Filter.TagTypes.Ignore)
					{
						totalFilteredCount++;
						if (tagType == Filter.TagTypes.Normal)
							normalFilteredCount++;
						if (tagType == Filter.TagTypes.Spam)
							spamFilteredCount++;
					}
					UpdateCounterDisplay();
					
					// we are done with the message, it has been filtered, stop testing filters
					// and break on outta here.

					// TODO:(2.0) check the continue flag to see if we should keep filtering
					break;
				}
			}
		}

		public void UpdateTimerDisplay()
		{
			Regex tsRE = new Regex(@"(.*)\.\d+$"); // grab 1.02:15:59 from 1.02:15:59.393847

			ts = DateTime.Now.Subtract(filterStartTime);
			Match m = tsRE.Match(ts.ToString());
			MainForm.staticElapsedTimeLabel.Text = m.Groups[1].Value;
		}

		public void UpdateCounterDisplay()
		{
			// TODO: the FilterSet class shouldn't be responsible for updating some UI
			// elements. They should provide the stats, but not update UI.
			MainForm.staticTotalFilteredLabel.Text = totalFilteredCount.ToString();
			MainForm.staticNormalFilteredLabel.Text = normalFilteredCount.ToString();
			MainForm.staticSpamFilteredLabel.Text = spamFilteredCount.ToString();

			try
			{ MainForm.staticSpamPercentageLabel.Text = String.Format("{0}%", (spamFilteredCount * 100) / totalFilteredCount); }
			catch (DivideByZeroException)
			{ MainForm.staticSpamPercentageLabel.Text = "0%"; }

			MainForm.staticSystemTray.Text = String.Format("{0} spam: ({1})", spamFilteredCount.ToString(), 
				MainForm.staticSpamPercentageLabel.Text);

			Int32 spd = (Int32)(spamFilteredCount / (ts.TotalSeconds / oneDay.TotalSeconds));
			MainForm.staticSpamPerDayLabel.Text = spd.ToString();
			Int32 npd = (Int32)(normalFilteredCount / (ts.TotalSeconds / oneDay.TotalSeconds));
			MainForm.staticNormalPerDayLabel.Text = npd.ToString();
		}

		public void Save()
		{
			String filterName = Config.FilterFilename;
			if (xmlDoc == null)
				return;

			if (filterName == null || filterName == "")
				return;

			xmlDoc.Save(Config.FilterFilename);
		}

		#region Property Methods

		public Int32 Count
		{
			get
			{
				return filters.Count;
			}
		}

		#endregion

	}
}
