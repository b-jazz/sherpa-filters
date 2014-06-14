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
using System.IO;
using System.Diagnostics;

namespace Sherpa
{
	/// <summary>
	/// Summary description for Log.
	/// </summary>
	public class Log
	{
		private static EventLog eLog;
		private static StreamWriter fileLog;

		/// <summary>
		/// 
		/// </summary>
		static Log()
		{

		}

		public static void Warning(string msg)
		{
			if (eLog == null)
				Log.CreateEventLog();

			eLog.WriteEntry(msg, EventLogEntryType.Warning);
		}

		public static void Information(string msg)
		{
			if (eLog == null)
				Log.CreateEventLog();

			eLog.WriteEntry(msg, EventLogEntryType.Information);
		}

		public static void Error(string msg)
		{
			if (eLog == null)
				Log.CreateEventLog();

			eLog.WriteEntry(msg, EventLogEntryType.Error);
		}

		private static void CreateEventLog()
		{
			if(!EventLog.SourceExists("Sherpa"))
			{
				EventLog.CreateEventSource("Sherpa", "Sherpa");
			}

			eLog = new EventLog();
			eLog.Source = "Sherpa";
 		}

		public static void WriteMessage(string msg)
		{
			if (fileLog == null)
				Log.CreateFileLog();

			fileLog.WriteLine(msg);
		}

		private static void CreateFileLog()
		{
			String debugFilename = Config.DebugFilename;

			if (debugFilename == "")
				throw(new Exception("DebugFilename was empty. Must be set in the registry."));

			if (File.Exists(debugFilename))
			{
				try
				{
					File.Delete(debugFilename);
				}
				catch (Exception)
				{
					// We really don't care if we can't delete it. We will care later if we can't create
					// or write to it.
				}
			}

			try 
			{
				fileLog = File.CreateText(debugFilename);
			}
			catch (Exception e)
			{
				// TODO: figure out what to do with exceptions here.
				Console.WriteLine(e.Message);
			}

		}

	}
}
