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
using System.Text;
using System.Text.RegularExpressions;

namespace Sherpa
{
	/// <summary>
	/// Summary description for Bayes.
	/// </summary>
	[Serializable]
	public class Bayes
	{
		public enum EmailType {spam, nonspam};

		private Hashtable spam;
		private Int32 spamCount;
		private Hashtable nonspam;
		private Int32 nonspamCount;

		private Hashtable probs;

		private	static Regex tokenRE = new Regex(@"([A-Za-z0-9_\-\.\$]+[A-Za-z0-9]+)", RegexOptions.Singleline);

		public static Hashtable TokenizeEmail(Email email)
		{
			Hashtable tokens = new Hashtable(250);
			MatchCollection matches;
			String key;

			matches = tokenRE.Matches(email.RawText);
			foreach (Match m in matches)
			{
				key = m.Groups[1].Value.ToLower();

				if (key.Length > 30) // ignore things that probably aren't good tokens
					continue;

				if (tokens.ContainsKey(key))
					tokens[key] = (Int32)tokens[key] + 1;
				else
					tokens[key] = 1;
			}
			return tokens;
		}

		public Bayes()
		{
			spam = new Hashtable(1000);
			nonspam = new Hashtable(1000);

			Bootstrap();
		}

		/// <summary>
		/// Takes the two corpus token hashes and computes (from scratch) a single hash with probabilities.
		/// </summary>
		private void CreateProbabilities()
		{
			if (probs == null)
				probs = new Hashtable(2000);
			else
				probs.Clear();

			// From http://www.paulgraham.com/spam.html ...
			//
			//(let ((g (* 2 (or (gethash word good) 0)))
			//      (b (or (gethash word bad) 0)))
			//   (unless (< (+ g b) 5)
			//     (max .01
			//          (min .99 (float (/ (min 1 (/ b nbad))
			//                             (+ (min 1 (/ g ngood))   
			//                                (min 1 (/ b nbad)))))))))

			Int32 b, g;

			foreach (Object key in spam.Keys)
			{
				g = (nonspam[key] != null) ? (Int32)nonspam[key] * 2 : 0;
				b = (Int32)spam[key];

				if (g + b > 5) 
				{
					probs[key] = Math.Max(0.01, Math.Min(0.99, (Math.Min(1.0, (Double)b / spamCount) / 
						(Math.Min(1.0, (Double)g / nonspamCount) + (Math.Min(1.0, (Double)b / spamCount))))));
				}
			}

			foreach (Object key in nonspam.Keys)
			{
				if (probs.ContainsKey(key))
					continue;

				g = (Int32)nonspam[key];
				b = (spam[key] != null) ? (Int32)spam[key] : 0;

				if (g + b > 5) 
				{
					probs[key] = Math.Max(0.01, Math.Min(0.99, (Math.Min(1.0, (Double)b / spamCount) / 
						(Math.Min(1.0, (Double)g / nonspamCount) + (Math.Min(1.0, (Double)b / spamCount))))));
				}
			}
		}

		/// <summary>
		/// Update the spam and nonspam hashes and counts and incrementally update the probabilities hash.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="eType"></param>
		private void UpdateProbabilities(Email email, EmailType eType)
		{
			// TODO: if email's token hash is empty, we need to calculate it
			if (eType == EmailType.spam)
				spamCount++;
			else
				nonspamCount++;

			foreach (Object key in email.Tokens.Keys)
			{
				if (eType == EmailType.spam)
				{
					if (spam.ContainsKey(key))
						spam[key] = (Int32)spam[key] + 1;
					else
						spam[key] = 1;
				}
				else
				{
					if (nonspam.ContainsKey(key))
						nonspam[key] = (Int32)nonspam[key] + 1;
					else
						nonspam[key] = 1;
				}
			}
		}

		/// <summary>
		/// Returns the probability that the given email is a piece of spam.
		/// </summary>
		/// <param name="email"></param>
		/// <returns></returns>
		public Double SpamProbability(Email email)
		{
			ArrayList tuples = new ArrayList(200);

			foreach (String token in email.Tokens.Keys)
			{
				if (probs.ContainsKey(token)) 
					tuples.Add(new ProbTuple(token, (Double)probs[token], Math.Abs((Double)probs[token] - 0.5)));
				else
					tuples.Add(new ProbTuple(token, 0.4, 0.1));
			}
		
			tuples.Sort();
			tuples.Reverse(); // TODO: LAZY! get rid of this and grab the range from the end. Sheesh.
			Double prob = BayesCombination(tuples.GetRange(0, 15));
			return prob;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mags">A list of 15 doubles the represent distance from 0.5</param>
		/// <returns></returns>
		private Double BayesCombination(ArrayList tuples)
		{
			Double abc = 1.0;
			Double oneMinus = 1.0;

			foreach (ProbTuple tuple in tuples)
			{
				Console.WriteLine("{0}: {1}", tuple.Token, tuple.Probability);
				abc *= tuple.Probability;
				oneMinus *= (1.0 - tuple.Probability);
			}
			Console.WriteLine("-----------------");

			return (abc / (abc + oneMinus));
		}

		/// <summary>
		/// 
		/// </summary>
		private void Bootstrap()
		{
			if (!(FilterController.dataSource.FolderExists(Config.SpamCorpus) &&
				FilterController.dataSource.FolderExists(Config.NonSpamCorpus)))
			{
				// We don't have valid mailboxes for the Bayes object to continue.
				throw (new Exception("The SpamCorpus and NonSpamCorpus option have not been set to valid Mailbox folders."));
			}
			ArrayList spams = FilterController.dataSource.MailboxEmail(Config.SpamCorpus);
			ArrayList nonspams = FilterController.dataSource.MailboxEmail(Config.NonSpamCorpus);

			if ((spams.Count < 500) || (nonspams.Count < 500))
			{
				throw (new Exception("There are not enough messages in the Spam and NonSpam Corpus Mailboxes to generate a good Bayes filter."));
			}

			foreach (Email email in spams)
			{
				UpdateProbabilities(email, EmailType.spam);
			}
			foreach (Email email in nonspams)
			{
				UpdateProbabilities(email, EmailType.nonspam);
			}
			CreateProbabilities();
		}

		private void DisplayHashtable(Hashtable hash)
		{
			foreach (Object key in hash.Keys)
			{
				if ((((Double)hash[key] > 0.95) || ((Double)hash[key] < 0.05)) &&
					(((Double)hash[key] != 0.99) && ((Double)hash[key] != 0.01)))
					Console.WriteLine("{0}:\t{1}", key.ToString(), hash[key].ToString());
			}
		}

	}
}

