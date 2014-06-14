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
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Sherpa.DataSource;

namespace Sherpa
{
	/// <summary>
	/// Summary description for Email.
	/// </summary>
	public class Email
	{
		private Uri uri; // We'll access the Exchange Provider by supplying the URI.
		private Int32 msgNumber; // Providers like POP3 and IMAP will refer to the message as a message number
		private string rawText;
		private Hashtable hash;
		private Hashtable tokens;
		private bool isLoaded;
		public enum AddressHeaderEnum { From, To, Cc, ToCc };
		private ArrayList relayList;

		// keep track of the header size since it is bothersome to calculate after
		// the fact. The body size and total size can be snagged from the Length 
		// properties of their respective objects.
		private Int32 headerSize;
		
		#region Constructors

		public Email(Int32 number)
		{
			msgNumber = number;
			isLoaded = false;
		}

		public Email(Uri uri) 
		{
			this.uri = uri;
			isLoaded = false;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public Uri Uri
		{
			get
			{
				return uri;
			}
		}

		public Int32 HeaderSize
		{
			get
			{
				return headerSize;
			}
		}

		public Int32 BodySize
		{
			get
			{
				String tmp = (String)hash["Body:"];
				return tmp.Length;
			}
		}

		public Int32 TotalSize
		{
			get
			{
				return rawText.Length;
			}
		}

		public Hashtable Hash
		{
			get
			{
				this.Load();
				return hash;
			}
		}

		public Hashtable Tokens
		{
			get
			{
				this.Load();
				if (tokens == null)
					tokens = Bayes.TokenizeEmail(this);
				return tokens;
			}
		}

		public string RawText
		{
			get
			{
				this.Load();
				return rawText;
			}
		}

		#endregion Properties

		private void Load()
		{
			Int32 count;

			if (isLoaded)
				return;

			MainForm.staticStatusLabel.Text = "Reading Email";
			try
			{
				Byte[] buffer = new Byte[8192];
				Stream eStream = Sherpa.FilterController.dataSource.EmailStream(this);
				// PARSE AWAY
				while ((count = eStream.Read(buffer, 0, 8192)) > 0) 
				{
					// Maybe I'm just being paranoid from my VB days where
					// a String couldn't be over 2MB. Let's see if C# and
					// .Net can handle >2MB emails.
					// TODO: Test
					rawText += Encoding.ASCII.GetString(buffer, 0, count);
				}
				ParseRawText();
				isLoaded = true;
			}
			catch (WebException e)
			{
				// TODO: need to fill out this exception handler
				string msg = String.Format("Email.Load threw an exception: {0}", e.Message);
				MessageBox.Show(msg, "Loading Email Message", MessageBoxButtons.OK);
				throw(e);
				// TODO: this might kill off the thread, but not the app. Test some more.
			}

		}

		private void ParseRawText()
		{
			Regex lineRE = new Regex(@"(.*)\r\n");
			Regex headerRE = new Regex(@"([^:]+)\s*:\s*(.+)");
			Regex hwspRE = new Regex(@"^\s+(.+)", RegexOptions.ECMAScript); // header white space (continued header line)
			Int32 i;
			String key;
			String val;

			hash = new Hashtable(10);

			MatchCollection lines = lineRE.Matches(rawText);
			for (i = 0; i < lines.Count; i++) 
			{
				if (lines[i].Groups[1].Value != "") // Still working on headers
				{
					Match headerParts = headerRE.Match(lines[i].Groups[1].Value);
					if (headerParts.Success) 
					{
						key = headerParts.Groups[1].Value;
						val = headerParts.Groups[2].Value;
						while ((lines[i+1].Groups[0].Value != null) &&
								((lines[i+1].Groups[0].Value[0] == '\t') ||
								(lines[i+1].Groups[0].Value[0] == ' ')))
						{ // read in continuation headers
							Match contParts = hwspRE.Match(lines[i+1].Groups[1].Value);
							if (contParts.Success) 
							{
								val += contParts.Groups[1].Value;
							}
							i++;
						}

						try
						{
							// I hate to have to do this since it goes agains RFC2822, but
							// there are some mailers out there that don't follow the RFC
							// and capitalize their From, To, and/or Cc headers.
							switch (key)
							{
								case "FROM" :
									key = "From";
									break;
								case "TO" :
									key = "To";
									break;
								case "CC" :
									key = "Cc";
									break;
								default :
									break;
							}

							if (hash.ContainsKey(key))
							{
								switch (key) 
								{
									case "Received" :
										hash[key] += " " + val; // append the received lines
										ArrayList list = (ArrayList)hash["Received:"];
										list.Add(val); // add item to array for special use (RelayList)
										break;
									case "To" :
									case "Cc" :
										hash[key] += val; // append
										hash["ToCc"] += "; " + val;
										break;
									default:
										hash.Remove(key); // last non-unique header line wins
										hash.Add(key, val);
										break;
								}
							}
							else
							{
								switch (key)
								{
									case "Received" :
										ArrayList list = new ArrayList(5);
										list.Add(val);
										hash.Add("Received:", list);
										hash[key] = val;
										break;
									case "To" :
									case "Cc" :
										hash.Add(key, val);
										if (hash.ContainsKey("ToCc"))
											hash["ToCc"] += "; " + val;
										else
											hash["ToCc"] = val;
										break;
									default :
										hash.Add(key, val);
										break;
								}
							}
						}
						catch (Exception e)
						{
							MessageBox.Show(e.Message);
						}
					}
				}
				else 
				{
					if (lines.Count > (i + 1))
					{
						headerSize = lines[i+1].Index; // how far into raw text is the double newline
						hash.Add("Body:", String.Copy(rawText.Substring(lines[i+1].Index)));
					}
					else
					{
						headerSize = lines[i].Index;
						hash.Add("Body:", "");
					}
					break;
				}
			}
		} // ParseRawText

		public ArrayList AddressList(AddressHeaderEnum header)
		{
			string aString; // string of address from header value
			string hString; // header string ("From", "To", etc.)
			ArrayList aList = new ArrayList();

			switch (header) 
			{
				case AddressHeaderEnum.From :
					hString = "From";
					break;
				case AddressHeaderEnum.To :
					hString = "To";
					break;
				case AddressHeaderEnum.Cc :
					hString = "Cc";
					break;
				case AddressHeaderEnum.ToCc :
					hString = "ToCc";
					break;
				default :
					return aList;
			}

			if (! hash.ContainsKey(hString))
				return aList;

			aString = (String)hash[hString];

			Regex re = new Regex(EmailAddress.addressRegex);
			foreach (Match match in re.Matches(aString)) 
			{
				// TEST: what happens if the address string is "" ?
				aList.Add(new EmailAddress(match.Value));
			}

			return aList;
		}

		public ArrayList RelayList()
		{
			this.Load();

			if (relayList != null)
				return relayList;

			relayList = new ArrayList(6);
			Regex rcvdRE = new Regex(@"from (\S+).*by (\S+).*$", RegexOptions.IgnoreCase);

			if (! hash.ContainsKey("Received:"))
				return relayList; // null

			foreach (string rcvdLine in (ArrayList)hash["Received:"])
			{
				foreach(Match match in rcvdRE.Matches(rcvdLine))
				{
					if (match.Success) 
					{
						// Yes, this is supposed to add the second match before the first. That makes
						// the list read better from beginning (destination) to end (source).
						relayList.Add(match.Groups[2].Value);
						relayList.Add(match.Groups[1].Value);
					}
				}
			}

			return relayList;
		}
	}
}
