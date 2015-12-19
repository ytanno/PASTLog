using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using Sgml;
using System.Xml.Linq;
using System.Xml;
using System.IO;


namespace torSample
{
	class Program
	{
		static void Main(string[] args)
		{
			//アクセス元の情報を返すサイト
			var url = @"http://uguisu.skr.jp/cgi-bin/telnet/lecture.cgi";

			var request = (HttpWebRequest)WebRequest.Create(url);

			//privoxy使用。
			request.Proxy = new WebProxy("127.0.0.1:8118");

			XDocument xml;
			using (var response = request.GetResponse())
			using (var sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("Shift_JIS")))
			{
				using (var sgmlReader = new SgmlReader { DocType = "HTML", CaseFolding = CaseFolding.ToLower })
				{
					sgmlReader.InputStream = sr;
					xml = XDocument.Load(sgmlReader);
					var ns = xml.Root.Name.Namespace;
					var myIP = xml.Descendants(ns + "table").Skip(1).First()
								  .Descendants(ns + "td").Skip(10).First().Value.Replace("\n","").Trim();

					Console.WriteLine("Your IP is {0}", myIP);
				}
			}

			Console.ReadLine();
		}
	}
}
