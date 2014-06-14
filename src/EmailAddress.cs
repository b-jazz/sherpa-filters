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
using System.Text.RegularExpressions;

namespace Sherpa
{
	/// <summary>
	/// Summary description for EmailAddress.
	/// </summary>
	public class EmailAddress : IComparable
	{
		public const string addressRegex = @"([\w\.-]+@[a-zA-Z\d\.-]+\.[a-zA-Z]{2,4})";
		private string fullAddress; //  "Bryce Jasmer (Work) <JasmerB@work.com>"
		private string shortAddress; // "jasmerb@work.com"
		private bool isEmpty;

		public EmailAddress(string address)
		{
			fullAddress = address;
			
			// Look for a null (but still valid) email address, often used to stop
			// email loops.
			if (fullAddress == "<>")
			{
				shortAddress = "";
				isEmpty = true;
				return;
			}

			Match match;

			Regex re = new Regex(addressRegex, RegexOptions.ECMAScript);
			match = re.Match(address);

			if (match.Success)
				shortAddress = match.Value.ToLower();
			else
				throw new EmailAddressException(String.Format("\"{0}\" doesn't appear to be a valid email address", address));
		}

		#region Properties

		/// <summary>
		/// Specifies whether or not the address is a null address "<>" used to stop
		/// email bounce loops.
		/// </summary>
		public bool IsEmpty
		{
			get 
			{
				return isEmpty;
			}
		}

		/// <summary>
		/// Gets just the essential address for delivering mail without the full
		/// user name and any comments.
		/// </summary>
		public string ShortAddress
		{
			get
			{
				return shortAddress;
			}
		}

		#endregion

		public int CompareTo(object a)
		{
			EmailAddress b = (EmailAddress)a;
			if (a.GetType() != this.GetType())
				return -1;
			else
				return shortAddress.CompareTo(b.shortAddress);
		}


		public bool Equals(EmailAddress ea)
		{
			return shortAddress.Equals(ea.ShortAddress);
		}

		public override bool Equals(object o)
		{
			return (shortAddress == o.ToString());
		}

		public override int GetHashCode()
		{
			return shortAddress.GetHashCode();
		}

		public static bool operator ==(EmailAddress a, EmailAddress b)
		{
			return (a.ShortAddress == b.ShortAddress);
		}

		public static bool operator !=(EmailAddress a, EmailAddress b)
		{
			return (a.ShortAddress != b.ShortAddress);
		}

		public override string ToString()
		{
			return shortAddress;
		}
	}

	#region EmailAddressException

	public class EmailAddressException : Exception
	{
		public EmailAddressException(string message) : base(message)
		{
			
		}
	}

	#endregion

}
