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
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

using Sherpa.DataSource;

namespace Sherpa
{
	/// <summary>
	/// Summary description for FilterController.
	/// </summary>
	public class FilterController
	{
		public static Sherpa.DataSource.DataSource dataSource;
		public static Sherpa.FilterSet filters;
		public static Hashtable contactHash;
		public static Bayes bayes;

		private EmailList emailList;

		public FilterController()
		{
			
		}

		public void ThreadStart()
		{
			Stream fromStream;
			IFormatter formatter;
			Thread.CurrentThread.Name = "Sherpa:FilterController";

			MainForm.staticStatusLabel.Text = "Loading Filters";
			filters = new FilterSet();

			if (filters.Count == 0)
			{
				MainForm.staticStatusLabel.Text = "No Filters To Process";
				return;
			}

			dataSource = new WebDAV();
			MainForm.staticStatusLabel.Text = "Loading Contacts";
			try
			{
				contactHash = dataSource.Contacts("Contacts");
			}
			catch (Exception e)
			{
				Log.Error(e.Message);
			}

			MainForm.staticStatusLabel.Text = "Loading Bayes Filters";
	
			// TODO: catch errors
			// TODO: do the right thing if we haven't taken snapshot of bayes object first.
			// TODO: store the bayes.bin file in application data directory
			//       Application.LocalUserAppDataPath;
			try
			{
				formatter = new BinaryFormatter();
				fromStream = new FileStream(Config.BayesFilterFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
				if (fromStream.Length != 0) 
				{
					bayes = (Bayes)formatter.Deserialize(fromStream);
					fromStream.Close();
				}
			}
			catch (FileNotFoundException)
			{
				try
				{
					bayes = new Bayes();
				}
				catch (Exception)
				{
					// bayes will be null, this is fine.
				}
			}

			try
			{
				while (true) 
				{
					MainForm.staticStatusLabel.Text = "Listing New Email";
					emailList = new EmailList();
					emailList.ProcessNewEmail();
					MainForm.reloadMutex.WaitOne();
					if (MainForm.reloadFlag) 
					{
						filters.Reload();
						// destroy c:\temp\bayes.bin ??
						// bayes = new Bayes(); // ??
						MainForm.reloadFlag = false;
					} 
					else 
					{
						MainForm.staticStatusLabel.Text = "Saving Filters";
						filters.Save(); // TODO: only save it if there are files that were moved.
					}
					MainForm.reloadMutex.ReleaseMutex();
					MainForm.staticStatusLabel.Text = "Waiting For Timer";
					Thread.Sleep(Config.ProbeIntervalMSecs);
				}
			}
			catch (WebException)
			{
				// TODO: we can get here by trying to hit http://svc-msg-04/exchange/jasmerb2/Contacts
				// TODO: something is wrong, deal with it
			}
			catch (ThreadAbortException e)
			{
				Log.Error(e.Message);
				Thread.ResetAbort();
			}
			finally
			{
				SaveBayes();
				filters.Save();
				MainForm.staticStatusLabel.Text = "Working Thread Died...";
			}
		}

		private void SaveBayes()
		{
			Stream stream;
			IFormatter formatter = new BinaryFormatter();
			FileInfo bayesFileInfo = new FileInfo(Config.BayesFilterFilename);
			
			if (!bayesFileInfo.Directory.Exists)
			{
				bayesFileInfo.Directory.Create();
			}

			try
			{
				// TODO: make sure bayes isn't null at this point
				stream = new FileStream(bayesFileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
				formatter.Serialize(stream, bayes);
				stream.Close();
			}
			catch (FileNotFoundException)
			{
				// TODO: catch some of the exceptions that can come from the above 'try' block
			}
			catch (System.ArgumentNullException)
			{
				// TODO: bayes is probably null and we should handle this above. Ignore for now 
				// since we are probably doing some debugging and don't have the Bayes file set
				// correctly.
			}
		}
	}
}
