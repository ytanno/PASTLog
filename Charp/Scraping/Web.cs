using Sgml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Utility
{
	public static class Web
	{
		public static XDocument GetXML(string url)
		{
			XDocument xml = new XDocument();
			using (var stream = new WebClient().OpenRead(url))
			using (var sr = new StreamReader(stream, Encoding.UTF8))
			{
				using (var sgmlReader = new SgmlReader { DocType = "HTML", CaseFolding = CaseFolding.ToLower })
				{
					sgmlReader.InputStream = sr;
					xml = XDocument.Load(sgmlReader);
				}
			}
			return xml;
		}
	}
}