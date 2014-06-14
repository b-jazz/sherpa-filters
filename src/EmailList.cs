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
using System.Threading;

namespace Sherpa
{
	/// <summary>
	/// Summary description for EmailList.
	/// </summary>
	public class EmailList
	{
		private ArrayList emails;

		public EmailList()
		{
			emails = FilterController.dataSource.NewEmail();
		}

		public void ProcessNewEmail()
		{
			if (emails.Count == 0) 
			{
				FilterController.filters.UpdateCounterDisplay();
				FilterController.filters.UpdateTimerDisplay();
			}
			else
			{
				foreach (Email email in emails)
				{
					// TODO:(2.0) check to see if the user clicked the pause/cancel/stop
					// button in the main thread.
					FilterController.filters.ApplyFilters(email);
				}
			}


		}
	}
}
