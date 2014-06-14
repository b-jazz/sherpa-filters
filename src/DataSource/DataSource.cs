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

namespace Sherpa.DataSource
{
	/// <summary>
	/// Summary description for DataSource.
	/// </summary>
	abstract public class DataSource
	{
		public DataSource()
		{
			//
		}

		/// <summary>
		/// Search through the specified Contacts database for all email addresses
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		abstract public Hashtable Contacts(string path);

		abstract public ArrayList NewEmail();

		abstract public ArrayList MailboxEmail(string path);
		abstract public bool FolderExists(string path);

		abstract public bool MoveEmail(Email msg, string path);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="callingObj">Sherpa.Email so that we can find out the URI or the index of the message.</param>
		/// <returns></returns>
		abstract public Stream EmailStream(Email callingObj);
	
	}
}
