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
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace Sherpa
{
	/// <summary>
	/// Summary description for Filter.
	/// </summary>
	public class Filter
	{
		public enum TagTypes { Ignore, Spam, Normal };
		private TagTypes tagType;
		private const string criteriaTypes = "And|Or|Not|All|HeaderMatch|BodyMatch|MailingListMatch|HeaderExists|ValueCompare|ContactMatch|RelayDomain|Bayes";
		private const string actionTypes = "Move|Copy|Delete|ListMove|PlaySound|ChangeCursor";
		private const string destTypes = "Destination|MailingListFolder";
		private Email msg; // temporary variable to refer to Email message object.
		private Hashtable msgHash; // holds the emails key/value hash to we don't have to call msg.Hash all the time.

		private XmlNode root;
		private XmlNode criteria;
		private XmlNode frequency;
		private XmlNode actions;
		private XmlNode stats;

		private EmailAddress matchedAddress; // used for mailing list filters
		private DateTime actionLastTaken = DateTime.MinValue; // this is the Date: header of the email, and when the message was last filtered due to frequency restrictions.
		private static Hashtable monthNameHash;

		static Filter()
		{
			monthNameHash = new Hashtable(13);
			Int32 i = 0;
			foreach (String m in new String[] {"xxx", "jan", "feb", "mar", "apr", "may", 
												"jun", "jul", "aug", "sep", "oct", "nov", "dec"})
			{
				monthNameHash.Add(m, i++);
			}
		}

		public Filter(XmlNode node)
		{
			root = node;
			criteria = root.SelectSingleNode("Criteria").SelectSingleNode(criteriaTypes);
			frequency = root.SelectSingleNode("Frequency");
			actions = root.SelectSingleNode("Actions");
			stats = root.SelectSingleNode("Stats");

			switch (root.Attributes.GetNamedItem("Tag").Value)
			{
				case "Ignore" :
					tagType = TagTypes.Ignore;
					break;
				case "Normal" :
					tagType = TagTypes.Normal;
					break;
				case "Spam" :
					tagType = TagTypes.Spam;
					break;
				default :
					break;
			}
		}

		#region Properties

		public TagTypes TagType
		{
			get
			{
				return tagType;
			}
		}
		
		public string FilterName
		{
			get
			{
				return root.Attributes.GetNamedItem("Name").Value;
			}
			set
			{
				root.Attributes.GetNamedItem("Name").Value = value;
			}
		}

		public bool Enabled
		{
			get 
			{
				return XmlConvert.ToBoolean(root.Attributes.GetNamedItem("Enabled").Value);
			}
			set
			{
				root.Attributes.GetNamedItem("Enabled").Value = XmlConvert.ToString(value);
			}
		}

		public bool ContinueFiltering
		{
			get
			{
				return true; // TODO:(2.0) 'nuf said.
			}
		}

		public Int32 TestCounter
		{
			get
			{
				return XmlConvert.ToInt32(stats.SelectSingleNode("TestCounter").InnerText);
			}
		}

		public Int32 MatchCounter
		{
			get
			{
				return XmlConvert.ToInt32(stats.SelectSingleNode("MatchCounter").InnerText);
			}
		}

		public DateTime LastMatchTime
		{
			get
			{
				string dateStr = stats.SelectSingleNode("LastMatchTime").InnerText;
				if (dateStr == "")
					return DateTime.MinValue;
				else
					return XmlConvert.ToDateTime(dateStr);
			}
			set
			{
				stats.SelectSingleNode("LastMatchTime").InnerText = XmlConvert.ToString(value);
			}
		}

		#endregion Properties

		private void IncrementStatsCounter(string statsName)
		{
			Int32 current = XmlConvert.ToInt32(stats.SelectSingleNode(statsName).InnerXml);
			stats.SelectSingleNode(statsName).InnerXml = XmlConvert.ToString(++current);
		}

		#region Action Methods

		private bool FrequencyAllowsAction()
		{
			XmlNode freqType;

			if (frequency == null)
				return true;

			freqType = frequency.SelectSingleNode("Time");
			if (freqType != null) // Time
			{
				TimeSpan span;

				try
				{
					span = TimeSpan.Parse(freqType.InnerText);
				}
				catch (Exception)
				{
					// I can't think of a reason that we'd actually get here since the schema
					// pretty much forces the filter to be in the right format.
					if (Config.LogActions)
					{
						Log.Error(String.Format("Can't parse ({0}) as a valid TimeSpan format.",
							freqType.InnerText));
					}

					// we want to filter all messages if they made a syntax error
					return true;
				}

				DateTime dateHeader = ConvertRfc2822Date((string)msgHash["Date"]);
				if (actionLastTaken + span <= dateHeader)
				{
					actionLastTaken = dateHeader;
					return true;
				}
				else
				{
					return false;
				}

			}
			else // Count
			{
				freqType = frequency.SelectSingleNode("Count");
				if (MatchCounter % XmlConvert.ToInt32(freqType.InnerText) == 0)
					return true;
				else
					return false;
			}
		}

		private DateTime ConvertRfc2822Date(string dateStr)
		{
			Regex timeRE = new Regex(@"(\d{1,2})\s+(\w{3})\s+(\d{4})\s+(\d{2}):(\d{2}):(\d{2})\s([+-]\d{2})\d{2}");
			// Mon, 26 Nov 2001 11:52:00 -0800 (PST)
			Match m = timeRE.Match(dateStr);
			if (m.Success)
			{
				try
				{
					DateTime dt = new DateTime(Int32.Parse(m.Groups[4].Value),
						(int)monthNameHash[m.Groups[3].Value.ToLower()],
						Int32.Parse(m.Groups[2].Value),
						Int32.Parse(m.Groups[5].Value),
						Int32.Parse(m.Groups[6].Value),
						Int32.Parse(m.Groups[7].Value));
					return dt;
				}
				catch
				{
					if (Config.LogActions) 
					{
						Log.Warning(String.Format("Couldn't convert ({0}) to a valid date. Returning the current date as the date of the message",
							dateStr));
					}
					return DateTime.Now;
				}
			} 
			else
			{
				if (Config.LogActions)
				{
					Log.Warning(String.Format("The \"Date:\" value ({0}) for that email looks nothing at all like an RFC2822 format date. " 
						+ "Returning the current date as the date of the message", dateStr));
				}
				return DateTime.Now;
			}
		}

		private void TakeAction()
		{
			foreach (XmlNode action in actions.SelectNodes(actionTypes))
			{
				switch (action.Name) 
				{
					case "ChangeCursor" :
						break;
					case "Copy" :
						ActionCopy(action);
						break;
					case "Delete" :
						ActionDelete(action);
						break;
					case "Move" :
						ActionMove(action);
						break;
					case "ListMove" :
						ActionListMove(action);
						break;
					case "PlaySound" :
						break;
					default :
						break;
				}
			}
		}

		private void ActionCopy(XmlNode action)
		{

		}

		private void ActionDelete(XmlNode action)
		{

		}

		private void ActionMove(XmlNode action)
		{
			string dst = action.SelectSingleNode("Destination").InnerText;
			string logMsg = String.Format("Action: MOVE\nFilter: {0}\nDestination: {1}\n\nFrom: {2}\nTo: {3}\nSubject: {4}\n",
				this.FilterName, dst, msgHash["From"], msgHash["To"], msgHash["Subject"]);

			try
			{
				MainForm.staticStatusLabel.Text = "Moving Email";
				FilterController.dataSource.MoveEmail(msg, dst);
				if (Config.LogActions)
					Log.Information(logMsg);
			}
			catch (Exception e)
			{
				String msg = "Sherpa was not able to perform the following action:\n\n";
				msg += logMsg + "\n\n";
				msg += e.Message;

				Log.Error(msg);
			}
		}

		private void ActionListMove(XmlNode action)
		{
			string dst = String.Format("{0}/{1}",
				action.SelectSingleNode("MailingListFolder").InnerText,
				matchedAddress.ShortAddress);
			string logMsg = String.Format("Action: MOVE\nFilter: {0}\nDestination: {1}\n\nFrom: {2}\nToCc: {3}\nSubject: {4}\n",
				this.FilterName, dst, msgHash["From"], msgHash["ToCc"], msgHash["Subject"]);
			Log.Information(logMsg);

			MainForm.staticStatusLabel.Text = "Moving Email";
			FilterController.dataSource.MoveEmail(msg, dst);
		}

		#endregion

		#region Testing Methods

		public bool FilterEmail(Email e)
		{
			bool flag = false;

			if (Enabled == false) // Don't run this filter
				return false;
			
			msg = e; // keep a ref to it so the recursive routines don't have to pass it around
			msgHash = e.Hash;
			matchedAddress = null;
			IncrementStatsCounter("TestCounter");

			if (TestCriteria(criteria)) 
			{
				flag = true; // We matched the criteria
				IncrementStatsCounter("MatchCounter");
				LastMatchTime = DateTime.Now;

				if (FrequencyAllowsAction())
				{
					TakeAction();
					flag = true;
				}
				else 
				{
					flag = false;
				}
			}
			msg = null;

			return flag;
		}

		private bool TestCriteria(XmlNode subCriteria)
		{
			XmlNodeList nodeList;

			switch (subCriteria.Name) 
			{
				case "Not" :
					return (! TestCriteria(subCriteria.SelectSingleNode(criteriaTypes)));
				case "And" :
					nodeList = subCriteria.SelectNodes(criteriaTypes);
					foreach (XmlNode node in nodeList)
					{
						if (TestCriteria(node) == false) 
						{
							// Short circuit. End result can't be true. Go ahead and return false now.
							return false;
						}
					}
					return true;
				case "Or" :
					nodeList = subCriteria.SelectNodes(criteriaTypes);
					foreach (XmlNode node in nodeList)
					{
						if (TestCriteria(node) == true)
							return true;
					}
					return false;
				case "All" :
					return true;
				case "HeaderMatch" :
					return TestHeaderKeyMatch(subCriteria, subCriteria.SelectSingleNode("Header").InnerText);
				case "HeaderExists" :
					return TestHeaderExists(subCriteria);
				case "BodyMatch" :
					return TestHeaderKeyMatch(subCriteria, "Body:");
				case "MailingListMatch" :
					return TestMailingListMatch(subCriteria);
				case "ValueCompare" :
					return TestValueCompare(subCriteria);
				case "RelayDomain" :
					return TestRelayDomain(subCriteria);
				case "Bayes" :
					return TestBayes(subCriteria);
				case "ContactMatch" :
					return TestContactMatch(subCriteria);
				default :
					return false;
			}
		}

		private bool TestHeaderKeyMatch(XmlNode subCriteria, string key) 
		{
			if (!msgHash.ContainsKey(key))
				return false;

			string headerVal = (String)msgHash[key];
			foreach (XmlNode node in subCriteria.SelectNodes("Value"))
			{
				Regex re = new Regex(node.InnerText, 
					(node.Attributes.GetNamedItem("Case").Value == "Sensitive") ? RegexOptions.None : RegexOptions.IgnoreCase);
				if (Config.VerboseFilterDebug) 
				{
					//Console.Write("filter = <{0}>, key = <{1}>, value = <{2}>, test = <{3}>, ", FilterName, key, headerVal, node.InnerXml);
				}
				if (re.IsMatch(headerVal)) 
				{
					if (Config.VerboseFilterDebug)
						Log.WriteMessage(String.Format("filter = <{0}>, key = <{1}>, value = <{2}>, test = <{3}>, result = \"TRUE\"", 
							FilterName, key, (headerVal.Length > 128 ? headerVal.Substring(0, 127) : headerVal), node.InnerXml));
					return true;
				} 
				else 
				{
					if (Config.VerboseFilterDebug)
						Log.WriteMessage(String.Format("filter = <{0}>, key = <{1}>, value = <{2}>, test = <{3}>, result = \"False\"", 
							FilterName, key, (headerVal.Length > 128 ? headerVal.Substring(0, 127) : headerVal), node.InnerXml));
				}
			}
			return false;
		}

		private bool TestMailingListMatch(XmlNode subCriteria)
		{
			if (!msgHash.ContainsKey("ToCc"))
				return false;

			ArrayList aList = msg.AddressList(Email.AddressHeaderEnum.ToCc);
			foreach (XmlNode node in subCriteria.SelectNodes("Value"))
			{
				try
				{
					EmailAddress eAddress = new EmailAddress(node.InnerText);
					if (aList.Contains(eAddress)) 
					{
						matchedAddress = eAddress;
						return true;
					}
				}
				catch (EmailAddressException)
				{
					if (Config.LogActions)
						Log.Warning(String.Format("The email address ({0}) specified in the config file is invalid.",
							node.InnerText));
				}
			}

			return false;
		}

		private bool TestHeaderExists(XmlNode subCriteria)
		{
			string header = subCriteria.SelectSingleNode("Header").InnerText;
			if (msgHash.ContainsKey(header))
				return true;
			else
				return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subCriteria"></param>
		/// <returns></returns>
		private bool TestValueCompare(XmlNode subCriteria)
		{
			XmlNode valueNode = subCriteria.SelectSingleNode("Value");
			Int32 testVal = XmlConvert.ToInt32(valueNode.InnerText);
			Regex re = new Regex(subCriteria.SelectSingleNode("Regex").InnerText, 
				(valueNode.Attributes.GetNamedItem("Case").Value == "Sensitive") ? RegexOptions.Singleline :
				RegexOptions.Singleline | RegexOptions.IgnoreCase);
			string header = subCriteria.SelectSingleNode("Header").InnerText;
			string compOp = subCriteria.SelectSingleNode("Comparison").InnerText;
			Match match = re.Match(msgHash[header].ToString());
			if (match.Success) 
			{
				Int32 realVal = XmlConvert.ToInt32(match.Groups[1].Value);
				switch (compOp)
				{
					case "gt" :
						return (realVal > testVal);
					case "lt" :
						return (realVal < testVal);
					case "ge" :
						return (realVal >= testVal);
					case "le" :
						return (realVal <= testVal);
					case "eq" :
						return (realVal == testVal);
					case "ne" :
						return (realVal != testVal);
					default :
						return false;
				}
			}

			return false;
		}

		private bool TestRelayDomain(XmlNode subCriteria)
		{
			Match m;
			// first off, make up a list of the TLDs that are in the
			// emails relay list.
			Regex tldRE = new Regex(@"\.[a-z]{2,4}$", RegexOptions.IgnoreCase);
			
			ArrayList relayServers = msg.RelayList();
			ArrayList relayTLDs = new ArrayList(relayServers.Count);
			foreach (string relayServer in relayServers)
			{
				m = tldRE.Match(relayServer);
				if (m.Success) // this currently is lazy and ignores IP addresses
					relayTLDs.Add(m.Value);
			}

			ArrayList testTLDs = new ArrayList();
			foreach (XmlNode node in subCriteria.SelectNodes("Good"))
				testTLDs.Add(node.InnerText);
			if (testTLDs.Count > 0)
			{ // we have a list of "Good" domains
				foreach (string relayTLD in relayTLDs)
					if (! testTLDs.Contains(relayTLD))
						return true;
			}
			else
			{ // we have a list of "Bad" domains
				foreach (XmlNode node in subCriteria.SelectNodes("Bad"))
					testTLDs.Add(node.InnerText);
				foreach (string relayTLD in relayTLDs)
					if (testTLDs.Contains(relayTLD))
						return true;
			}

			return false;
		}

		private bool TestBayes(XmlNode subCriteria)
		{
			// TODO: if bayes == null, we should shut down this rule.

			Double probability = FilterController.bayes.SpamProbability(msg);
			Int32 threshold = XmlConvert.ToInt32(subCriteria.SelectSingleNode("Value").InnerText);
			if ((probability * 100) > threshold)
				return true;
			else
				return false;
		}

		private bool TestContactMatch(XmlNode subCriteria)
		{
			ArrayList aList; // address list

			// TODO: we need to figure out how to call a method by name so that this
			// can be collapsed into one call instead of a many-lined switch block.
			switch (subCriteria.SelectSingleNode("Header").InnerText) 
			{
				case "From" :
					aList = msg.AddressList(Email.AddressHeaderEnum.From);
					break;
				case "To" :
					aList = msg.AddressList(Email.AddressHeaderEnum.To);
					break;
				case "Cc" :
					aList = msg.AddressList(Email.AddressHeaderEnum.Cc);
					break;
				case "ToCc" :
					aList = msg.AddressList(Email.AddressHeaderEnum.ToCc);
					break;
				default :
					return false;
			}

			foreach (EmailAddress eAddress in aList) 
			{
				if (FilterController.contactHash.ContainsKey(eAddress)) 
					return true;
			}

			return false;
		}

		#endregion
	}
}
