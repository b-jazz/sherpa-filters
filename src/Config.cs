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
using System.Windows.Forms;
using System.Xml;

using Microsoft.Win32;

namespace Sherpa
{

	// TODO: Change everything in here to be static methods and static storage so
	// we don't have to go through a convoluted path to get to the config options.

	/// <summary>
	/// Summary description for Config.
	/// </summary>
	public class Config
	{
		private const string KEYPATH = @"Software\IH8SPAM.com\Sherpa";

		private static RegistryKey rootKey;
		private static String filterFilename = "";
		private static Uri baseUri;
		private static String eServer;
		private static String uName;
		private static String defaultMbox = "Inbox";
		private static Int32 probeInterval = 15;
		private static Boolean logActions = false;
		private static String spamCorpus = "";
		private static String nonSpamCorpus = "";
		private static String defaultBayesPath = Application.LocalUserAppDataPath + @"\Bayes.bin";
		private static String bayesFilterFilename;
		private static Boolean verboseFilterDebug = false;
		private static String debugFilename = "";

		static Config()
		{
			rootKey = Registry.CurrentUser.OpenSubKey(KEYPATH, true);

			if (rootKey == null)
				return;

			string baseString = (String)rootKey.GetValue("BaseUri");
			if (baseString != null)
			{
				baseUri = new Uri(baseString);
			}
			else
			{
				eServer = (String)rootKey.GetValue("ExchangeServer");
				uName = (String)rootKey.GetValue("UserName");
				if ((eServer != null) && (uName != null))
					baseUri = new Uri(String.Format("http://{0}/exchange/{1}/", eServer, uName));
			}

			defaultMbox = (String)rootKey.GetValue("DefaultMailbox", "Inbox");
			filterFilename = (String)rootKey.GetValue("FilterFilename", "");
			debugFilename = (String)rootKey.GetValue("DebugFilename", "C:\\SherpaDebug.log");
			probeInterval = Math.Abs((Int32)rootKey.GetValue("ProbeInterval", 15));
			Int32 logTmp = (Int32)rootKey.GetValue("LogActions", 0);
			logActions = (logTmp == 0) ? false : true;
			Int32 verboseFilterDebugTmp = (Int32)rootKey.GetValue("VerboseFilterDebug", 0);
			verboseFilterDebug = (verboseFilterDebugTmp == 0) ? false : true;
			spamCorpus = (String)rootKey.GetValue("SpamCorpus", "");
			nonSpamCorpus = (String)rootKey.GetValue("NonSpamCorpus", "");
			bayesFilterFilename = (String)rootKey.GetValue("BayesFilterFilename", defaultBayesPath);
		}

		#region Properties

		public static Boolean LogActions
		{
			get
			{
				return logActions;
			}
			set
			{
				logActions = value;
				SetRegistryValue("LogActions", logActions);
			}
		}
			
		public static Boolean VerboseFilterDebug
		{
			get
			{
				return verboseFilterDebug;
			}
			set
			{
				verboseFilterDebug = value;
				SetRegistryValue("VerboseFilterDebug", verboseFilterDebug);
			}
		}
			
		public static Uri BaseUri
		{
			get
			{
				return baseUri;
			}
		}

		private static void SetBaseUri()
		{
			if ((eServer != null) && (uName != null))
				baseUri = new Uri(String.Format("http://{0}/exchange/{1}/", eServer, uName));
		}

		public static string ExchangeServer
		{
			get
			{
				return eServer;
			}
			set
			{
				eServer = value;
				SetBaseUri();
				SetRegistryValue("ExchangeServer", eServer);
			}
		}

		public static string UserName
		{
			get
			{
				return uName;
			}
			set
			{
				uName = value;
				SetBaseUri();
				SetRegistryValue("UserName", uName);
			}
		}

		public static string DefaultMailbox
		{
			get
			{
				return defaultMbox;
			}
			set
			{
				defaultMbox = value;
				SetRegistryValue("DefaultMailbox", defaultMbox);
			}
		}

		public static string FilterFilename
		{
			get
			{
				return filterFilename;
			}
			set
			{
				filterFilename = value;
				SetRegistryValue("FilterFilename", filterFilename);
			}
		}

		public static string DebugFilename
		{
			get 
			{
				return debugFilename;
			}
		}

		public static string BayesFilterFilename
		{
			get
			{
				return bayesFilterFilename;
			}
			set
			{
				bayesFilterFilename = value;
				SetRegistryValue("BayesFilterFilename", bayesFilterFilename);
			}
		}

		/// <summary>
		/// Sets the number of milliseconds between HEPA checking for new email.
		/// The number gets converted into seconds before writing it out to the
		/// registry.
		/// </summary>
		public static Int32 ProbeIntervalMSecs
		{
			get
			{
				Int32 pi; // no, not 3.14159

				try
				{
					pi = probeInterval * 1000;
				}
				catch
				{
					// overflow, just set the interval to the max.
					pi = Int32.MaxValue;
				}
				return pi;
			}
			set
			{
				// Our code will only allow integer seconds, so all values that get
				// passed here will be evenly divisable by 1000.
				probeInterval = value / 1000;
				SetRegistryValue("ProbeInterval", probeInterval);
			}
		}

		public static Int32 ProbeIntervalSecs
		{
			get
			{
				return probeInterval;
			}
			set
			{
				probeInterval = value;
				SetRegistryValue("ProbeInterval", probeInterval);
			}
		}

		public static String SpamCorpus
		{
			get
			{
				return spamCorpus;
			}
			set
			{
				spamCorpus = value;
				SetRegistryValue("SpamCorpus", spamCorpus);
			}
		}

		public static String NonSpamCorpus
		{
			get
			{
				return nonSpamCorpus;
			}
			set
			{
				nonSpamCorpus = value;
				SetRegistryValue("NonSpamCorpus", nonSpamCorpus);
			}
		}

		#endregion

		private static void SetRegistryValue(string key, Int32 val)
		{
			CreateRootPath();
			rootKey.SetValue(key, val);
		}

		private static void SetRegistryValue(string key, String val)
		{
			CreateRootPath();
			rootKey.SetValue(key, val);
		}

		private static void SetRegistryValue(string key, Boolean val)
		{
			CreateRootPath();
			rootKey.SetValue(key, (val ? 1 : 0));
		}

		public static void CreateRootPath()
		{
			if (rootKey != null) // path already exists
				return;
			
			try 
			{
				rootKey = Registry.CurrentUser.CreateSubKey(KEYPATH);
			}
			catch
			{
				// AFAIK, the chances of this happening are so drastically low that
				// I'm not going to bother with a more descriptive error message.
				MessageBox.Show("Application is unable to create the neccessary registry keys. Exiting.");
				Application.Exit();
			}
		}

	}
}
