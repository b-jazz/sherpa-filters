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
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Sherpa.DataSource
{
	// TODO: we use mixed names for the email containers, sometimes calling them mailboxes
	// and other times calling them folders. We should straighten this out.

	/// <summary>
	/// Summary description for WebDAV.
	/// </summary>
	class WebDAV : DataSource
	{
		private Random random;
		private Hashtable knownUris;

		public WebDAV()
		{
			random = new Random();
			knownUris = new Hashtable();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		override public Hashtable Contacts(string path)
		{
			WebResponse xmlResponse;
			Hashtable ht = new Hashtable(100, 0.5F, 
				new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer());
			string xmlRequest;
			Uri uri;
			XmlDocument xmldoc;

			uri = new Uri(Config.BaseUri, path, false);
			WebRequest request = WebRequest.Create(uri);
			request.Method = "PROPFIND";
			WebHeaderCollection whc = new WebHeaderCollection();
			whc.Add("Depth", "1");
			request.Headers = whc;
			request.ContentType = "text/xml";
			request.Credentials = System.Net.CredentialCache.DefaultCredentials;

			xmlRequest =  "<?xml version=\"1.0\"?>";
			xmlRequest += "<a:propfind xmlns:a=\"DAV:\" xmlns:d=\"urn:schemas:contacts:\">";
			xmlRequest += "<a:prop><d:email1/><d:email2/><d:email3/></a:prop></a:propfind>";
    
			// TODO: all sorts of exception handling needs to happen around here.

			request.ContentLength = xmlRequest.Length;
			Stream rStream = request.GetRequestStream();
			StreamWriter stream = new StreamWriter(rStream, Encoding.ASCII);
			stream.Write(xmlRequest);
			stream.Close();

			try
			{
				xmlResponse = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				string msg;

				msg = String.Format("When trying to access \"{0}\", the server returned the following error:\n\n{1}",
					e.Response.ResponseUri.ToString(), e.Message);
				MessageBox.Show(msg, "Server Error");
				return ht;
			}

			Stream streamrs = xmlResponse.GetResponseStream();
			XmlTextReader reader = new XmlTextReader(streamrs);
			xmldoc = new XmlDocument();
			xmldoc.Load(reader);

			foreach (string tag in new String[] {"d:email1", "d:email2", "d:email3"}) 
			{
				foreach (XmlNode node in xmldoc.GetElementsByTagName(tag))
				{
					if (node.InnerText != "") 
					{
						//Console.WriteLine(XmlConvert.DecodeName(node.InnerText));
						try 
						{
							ht.Add(new EmailAddress(XmlConvert.DecodeName(node.InnerText)), null);
						}
						catch (EmailAddressException)
						{
							if (Config.LogActions) 
							{
								Log.Warning(String.Format("Unable to parse ({0}) as a valid email address. Ignoring.",
									XmlConvert.DecodeName(node.InnerText)));
							}
						}
						catch (ArgumentException)
						{
							if (Config.LogActions)
							{
								Log.Information(String.Format("We've already seen the email address ({0}). No big deal.",
									XmlConvert.DecodeName(node.InnerText)));
							}
						}
					}
				}
			}

			return ht;
		}


		override public ArrayList NewEmail()
		{
			return MailboxEmail(Config.DefaultMailbox);
		}

		override public ArrayList MailboxEmail(string relPath)
		{
			XmlDocument xmldoc;
			Uri uri;
			String req;
			ArrayList newList = new ArrayList();

			// TODO: we should remove the dependancy of the data source needing to know
			// about the configuration object.
			uri = new Uri(Config.BaseUri, relPath);
			WebRequest request = WebRequest.Create(uri);
			request.Method = "SEARCH";
			WebHeaderCollection whc = new WebHeaderCollection();
			whc.Add("Depth", "1");
			request.Headers = whc;
			request.ContentType = "text/xml";
			request.Credentials = System.Net.CredentialCache.DefaultCredentials;

			req =  "<?xml version=\"1.0\"?>";
			req += "<a:searchrequest xmlns:a=\"DAV:\">";
			req += "<a:sql>";
			req += String.Format("  select \"DAV:href\" from scope('shallow traversal of \"{0}\"')", uri.AbsoluteUri);
			req += "  where \"DAV:ishidden\"=false";
			req += "  order by \"urn:schemas:mailheader:date\"";
			req += "</a:sql>";
			req += "</a:searchrequest>";

			request.ContentLength = req.Length;
			Stream rStream = request.GetRequestStream();
			StreamWriter stream = new StreamWriter(rStream, Encoding.ASCII);
			stream.Write(req);
			stream.Close();

			WebResponse response = (HttpWebResponse)request.GetResponse();
			Stream streamrs = response.GetResponseStream();
			XmlTextReader reader = new XmlTextReader(streamrs);
			xmldoc = new XmlDocument();
			xmldoc.Load(reader);

			//			XmlNodeList nList = xmldoc.GetElementsByTagName("a:prop");
			//			Console.WriteLine(nList.Count);

			foreach (XmlNode node in xmldoc.DocumentElement.GetElementsByTagName("a:prop"))
			{
				// read it inside out... create a uri, create an email object, and add to end of array
				newList.Add(new Email(new Uri(node.InnerText, true)));
			}

			return newList;
		}

		override public Stream EmailStream(Email callingObj)
		{
			Uri uri = callingObj.Uri;

			WebRequest request = WebRequest.Create(uri);
			request.Method = "GET";
			WebHeaderCollection whc = new WebHeaderCollection();
			whc.Add("Depth", "1");
			whc.Add("Translate", "F");
			request.Headers = whc;
			request.Credentials = System.Net.CredentialCache.DefaultCredentials;

			WebResponse response = (HttpWebResponse)request.GetResponse();
			Stream streamrs = response.GetResponseStream();
			return streamrs;
		}

		override public bool FolderExists(string path)
		{
			// The problem about making sure there is a folder is the performance hit. There
			// are three ways that I see to handle this.
			// (1) On every move request, connect and do a "HEAD" to see if folder is there
			// (2) Save a list of known folders and only connect and do a "HEAD" if you haven't
			//     verified that folder before (the folder could be deleted out from under us)
			// (3) Try to move the email first and if it fails the first time, then check for
			//     the existance and then create/error/prompt depending on user options.
			// For pre-1.0 versions, we'll pick option 2.

			// TODO: TEST: move an email to a known folder, delete the folder, send another mail that
			// would end up in that folder, find the error and write code to handle it.

			HttpWebRequest req;
			HttpWebResponse resp;

			if (path == "")
				return false;

			Uri pathUri = new Uri(Config.BaseUri, path, false); // baseUri is escaped, but user enter path is probably not

			if (knownUris.Contains(pathUri))
				return true;

			req = (HttpWebRequest)WebRequest.Create(pathUri);
			req.Method = "HEAD";
			req.Credentials = System.Net.CredentialCache.DefaultCredentials;

			try 
			{
				resp = (HttpWebResponse)req.GetResponse();
				if (resp.StatusCode == HttpStatusCode.NoContent)
				{
					resp.Close();
					knownUris.Add(pathUri, null);
					return true;
				}
			}
			catch (WebException e)
			{
				if (e.Status == WebExceptionStatus.ProtocolError) 
				{
					Regex re = new Regex(@"\((\d{3})\)");
					Match m = re.Match(e.Message);
					if (m.Success && (m.Groups[1].Value == "404"))
						return false;
				}
				throw;
			}

			// resp.Close();
			return false; // We'll never get here, but the compiler doesn't seem to know that.
		}

		private bool CreateFolder(string path)
		{
			HttpWebRequest req;
			HttpWebResponse resp;

			// TODO: we should throw an exception instead of returning a Boolean.

			// TEST: what happens if Mailboxes/foo exists and we try to move a message
			// to Mailboxes/FOO. We'll cache that .../foo exists but will think that 
			// .../FOO doesn't. When we go to create the uppercase name, the create
			// will fail, but we should return true since the appropriate folder really
			// does exist.

			// TODO: Loop through the segments of the path and make sure that
			// all the parent paths exists before you start creating.
			// When creating http://server/EXISTS/EXISTS/NOSUCHDIR/NOSUCHDIR
			// check for http://server/EXISTS, then http://server/EXISTS/EXISTS,
			// then http://server/EXISTS/EXISTS/NOSUCHDIR, and then create
			// that path through to the end.

			Uri pathUri = new Uri(Config.BaseUri, path, true);
			string[] segs = path.Split(Path.DirectorySeparatorChar);
			for (Int32 i = 0; i < (segs.Length - 1); i++)
			{
				// create the path to examine

				// CAREFUL: http://server/exchange/ will not appear to exist (due to permissions
				// I presume).
			}

			req = (HttpWebRequest)WebRequest.Create(pathUri);
			req.Method = "MKCOL";
			req.Credentials = System.Net.CredentialCache.DefaultCredentials;

			try 
			{
				resp = (HttpWebResponse)req.GetResponse();
				if (resp.StatusCode == HttpStatusCode.Created)
				{
					resp.Close();
					return true;
				}
			}
			catch (WebException e)
			{
				if (e.Status == WebExceptionStatus.ProtocolError) 
				{
					Regex re = new Regex(@"\((\d{3})\)");
					Match m = re.Match(e.Message);
					if (m.Success && (m.Groups[1].Value == "404"))
						return false;
				}
				throw;
			}

			// resp.Close();
			return false; // We'll never get here, but the compiler doesn't know that.
		}

		override public bool MoveEmail(Email msg, string path)
		{
			string[] segments = msg.Uri.Segments;
			HttpWebRequest req;
			HttpWebResponse resp;
			HttpStatusCode status;
			Int32 attempts = 0;

			// TODO: We need to make 100% sure that the folder exists before we try to move
			// something into it.

			if (FolderExists(path) == false) 
			{
				// TODO:(v2.0) check options
				// TODO:(v2.0) make it an option to move something to a folder that doesn't exist.

				if (CreateFolder(path))
					return true;
				else
					return false;
			}

			do 
			{
				// If it takes more than 10 randomly number filenames to find a slot
				// to move the file into, we've got some big problems.
				if (++attempts > 10)
					return false;

				string filename = RandomizeFilename(segments[segments.Length-1]);

				Uri destUri = new Uri(Config.BaseUri, String.Format("{0}/{1}", path, filename), true);

				req = (HttpWebRequest)WebRequest.Create(msg.Uri);
				req.Method = "MOVE";
				WebHeaderCollection whc = new WebHeaderCollection();
				whc.Add("Destination", destUri.AbsolutePath);
				whc.Add("Overwrite", "F");
				req.Headers = whc;
				req.Credentials = System.Net.CredentialCache.DefaultCredentials;

				try 
				{
					resp = (HttpWebResponse)req.GetResponse();
					status = resp.StatusCode;
				}
				catch (WebException e)
				{
					// This is hideous. GetResponse() will raise an exception if there is
					// an error. But the exception doesn't contain enough information in it so I
					// have to parse the exception message to find out if there was a conflict. Yuck.
					status = HttpStatusCode.Unused; // just set it to something to quiet the compiler
					bool rethrow = false;
					switch (e.Status)
					{
						case WebExceptionStatus.ProtocolError :
							Regex re = new Regex(@"\((\d{3})\)");
							Match m = re.Match(e.Message);
							if ((m.Success) && (m.Groups[1].Value == "409"))
								status = HttpStatusCode.Conflict;
							else
								rethrow = true;
							break;
						default :
							rethrow = true;
							break;
					}
					if (rethrow)
						throw;
				}
			} while (status == HttpStatusCode.Conflict);

			// TODO: somehow we'll have to figure out if we really had a successful MOVE
			// command, but that is hard since some code paths through the exception won't
			// set the status code correctly.

//			if (response.StatusCode == HttpStatusCode.Created)
//				return true;
//			else 
//			{
//				MessageBox.Show("MoveEmail() Error: " + response.StatusCode.ToString());
//				return false;
//			}

			return true;
		}

		private string RandomizeFilename(string fn)
		{
			Regex re = new Regex(@"(.*)(H\d{4})?.EML");
			Match match = re.Match(fn);
			if (match.Success) 
				return String.Format("{0}H{1}.EML", match.Groups[1].Value, random.Next(1000, 9999));
			else
			{
				Log.Warning(String.Format("Unrecognized file extension in file name ({0}).", fn));
				return fn;
			}
		}

	}
}
