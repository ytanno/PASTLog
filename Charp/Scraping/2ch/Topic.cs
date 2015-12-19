using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Xml.Linq;
using Sgml;

namespace _2ch
{
	public class Topic
	{
		public string URL;
		public string Title;
		public DateTime UpdateTime;

		public List<Content> Contents { private set; get;}
		public List<Content> FilteredContents { private set; get;}

		public Dictionary<MecabDataFormat, int> FilterContentsWordDic { private set; get; }


		public Topic(string url, string title, DateTime updateTime, DateTime timeFilter, Mecab mecab)
		{
			URL = url;
			Title = title;
			UpdateTime = updateTime;
			Contents = new List<Content>();
			FilteredContents = new List<Content>();

			GetContents(mecab);

			FilterTimeContents(timeFilter);

			if (mecab != null)
			{
				SetFilterContetnsWordDic(FilteredContents);
			}
		}


		//public Topic() { }


		private void SetFilterContetnsWordDic(List<Content> contents)
		{
			FilterContentsWordDic = new Dictionary<MecabDataFormat, int>();

			for (int i = 0; i < contents.Count; i++)
			{
				foreach (var key in contents[i].WordDic.Keys)
				{
					if (FilterContentsWordDic.Keys.Where(x => x.Word.Equals(key.Word)).Count() == 0)
					{
						int count = contents[i].WordDic[key];
						for (int j = i + 1; j < contents.Count; j++)
						{
							var key2  = contents[j].WordDic.Keys.Where(x => x.Word.Equals(key.Word)).FirstOrDefault();
							if (key2 != null) count += contents[j].WordDic[key2];
						}
						FilterContentsWordDic[key] = count;
					}
				}
			}

			FilterContentsWordDic = FilterContentsWordDic.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
			
		}




		/*
		public Dictionary<MecabDataFormat, int> SetFilterContetnsWordDic(List<Content> contents)
		{
			Dictionary<MecabDataFormat, int> test = new Dictionary<MecabDataFormat, int>();

			for (int i = 0; i < contents.Count - 1; i++)
			{
				foreach (var key in contents[i].WordDic.Keys)
				{
					if (test.Keys.Where(x => x.Word.Equals(key.Word)).Count() == 0)
					{
						int count = contents[i].WordDic[key];
						for (int j = i + 1; j < contents.Count; j++)
						{
							int count2 = contents[j].WordDic.Keys.Where(x => x.Word.Equals(key.Word)).Count();
							count += count2;
						}
						test[key] = count;
					}
				}
			}

			test = test.OrderByDescending(x => x.Value).ToDictionary(x=>x.Key, y =>y.Value);
			return test;
		}
		 */ 



	
		public Topic(string filePath, int skipIndex)
		{
			var words = File.ReadAllLines(filePath).Skip(skipIndex).First().Split(',');

			UpdateTime = DateTime.Parse(words[0]);
			URL = words[1];
			Title = words[2];
			Contents = new List<Content>();
		}


	
		public void Save(string filePath)
		{
			Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
			using (StreamWriter sw = new StreamWriter(filePath, true, sjisEnc))
			{
				sw.WriteLine("{0},{1},{2}", UpdateTime, URL, Title);
			}
		}


		private void GetContents(Mecab mecab)
		{
			Contents.Clear();
			XDocument xml;

			try
			{

				using (SgmlReader sgml = new SgmlReader() { Href = this.URL })
				{
					xml = XDocument.Load(sgml);
				}
				var ns = xml.Root.Name.Namespace;


				var temp1 = xml.Descendants(ns + "dl").FirstOrDefault();
				var temp2 = xml.Descendants(ns + "dl").FirstOrDefault();


				System.Text.RegularExpressions.Regex dateRegex = new System.Text.RegularExpressions.Regex(
													@"\d\d\d\d/\d\d/\d\d\W+\w+\W+\s+\d\d:\d\d:\d\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);


				string nowYear = DateTime.Now.Year.ToString() +@"/";
				if (temp1 != null && temp2 != null)
				{
					var infoList = temp1.Descendants(ns + "dt").ToList();
					var comments = temp2.Descendants(ns + "dd").ToList();


					for (int i = 0; i < infoList.Count(); i++)
					{
						var info = infoList[i];
						var comment = comments[i];

						if (info != null && comment != null)
						{
							string userName = info.Descendants(ns + "b").First().Value;
							var splitID = info.Value.Split("ID:".ToArray(), StringSplitOptions.RemoveEmptyEntries);
							string id = splitID[splitID.Length - 1];
							var m = dateRegex.Match(info.Value);

							if (m.Length > 0)
							{
								DateTime writeTime = DateTime.Parse(m.Value);
								Content ct = new Content(id, userName, comment.Value, writeTime, mecab);
								Contents.Add(ct);
							}

						}

					}

				}
			}
			catch (Exception e)
			{
				if (!e.Message.Contains("タイムアウト"))
				{
					Console.WriteLine(e);
					throw new Exception("error");
				}
			}
		}

		private void FilterTimeContents(DateTime time)
		{
			FilteredContents =  Contents.Where(x => x.WriteTime.Subtract(time).TotalMinutes > 0).Select(x => x).ToList();
		}
	}

	public class Content
	{
		public DateTime WriteTime;
		public string UserName;
		public string Id;
		public string Comment;


		//public Dictionary<string, int> WordDic;
		public Dictionary<MecabDataFormat, int> WordDic;


		public Content(string id, string userName, string comment, DateTime writeTime, Mecab mecab)
		{
			WriteTime = writeTime;
			UserName = userName;
			Comment = comment.Replace("\r", "").Replace("\n", "");
			Id = id;


			if (mecab != null)
			{
				List<MecabDataFormat> mecabList = mecab.Input(Comment);
				MecabToWordDic(mecabList);
			}
		}


		private void MecabToWordDic(List<MecabDataFormat> mecabList)
		{
			var dic = 	mecabList.Where(x=>!x.Word.Equals("EOS"))
								 .GroupBy(x => x.Word).Select(x => new
			{
				key = x,
				value = x.Count()
			});


			WordDic = new Dictionary<MecabDataFormat, int>();
			foreach (var v in dic)
			{
				WordDic[v.key.First()] = v.value;
			}

		}


		/*
		public Content(string filePath, int skipIndex)
		{
			var words = File.ReadAllLines(filePath).Skip(skipIndex).First().Split(',');

			//未実装
			WriteTime = new DateTime();
			UserName = words[1];
			Comment = words[2];
		}
		 */ 


		
		public void Save(string topicTitle, string filePath)
		{
			Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
			using (StreamWriter sw = new StreamWriter(filePath, true, sjisEnc))
			{
				sw.WriteLine("{0},{1},{2},{3}", WriteTime, topicTitle, UserName, Comment);
			}
		}
	

	}


}
