using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sgml;
using System.Xml.Linq;
using System.Web;
using System.IO;

using System.Threading;

namespace _2ch
{
	public static class Utility
	{
		/// <summary>
		/// 2chのデータをスクレイピングする。デフォルトで50個のTopic取得
		/// </summary>
		/// <param name="searchWord">検索ワード</param>
		/// <param name="timeFilter">最新更新時間とTopicのスレッドの書き込み時間のフィルタ</param>
		/// <param name="mecab">mecabがない時はnull</param>
		/// <returns></returns>
		public static List<Topic> GetTopics(string searchWord, DateTime timeFilter, Mecab mecab)
		{
			List<Topic> topicList = new List<Topic>();
			string param = HttpUtility.UrlEncode(searchWord, System.Text.Encoding.GetEncoding("EUC-JP"));
			try
			{
				for (int i = 0; i < 1; i++)
				{
					string url = @"http://find.2ch.net/?STR=" + param + @"&COUNT=50&TYPE=TITLE&BBS=ALL&OFFSET=" + (i * 50).ToString();
					XDocument xml;
					using (SgmlReader sgml = new SgmlReader() { Href = url })
					{
						xml = XDocument.Load(sgml);
					}

					var ns = xml.Root.Name.Namespace;

					var titleAndUrl = xml.Descendants(ns + "dl").Skip(1).First()
							  .Descendants(ns + "dt").Select(x => x.Descendants(ns + "a").FirstOrDefault()).Where(x => x != null).ToList();

					var time = xml.Descendants(ns + "dl").Skip(1).First()
							  .Descendants(ns + "dd").Select(x => x.Descendants(ns + "font").Skip(1).FirstOrDefault()).ToList();


					for (int j = 0; j < time.Count(); j++)
					{
						var tu = titleAndUrl[j];
						var ti = time[j];

						if (tu != null && ti != null)
						{
							string topicUrl = (string)tu.Attribute("href");
							string topicTitle = tu.Value;
							DateTime topicUpdateTime = DateTime.Parse(ti.Value.Replace("最新:", ""));

							Topic topic = new Topic(topicUrl, topicTitle, topicUpdateTime, timeFilter, mecab);

							if (topic.UpdateTime.Subtract(timeFilter).TotalHours > 0)
							{
								topicList.Add(topic);
							}
						}
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
			}

			return topicList;
		}

		/// <summary>
		/// Topicを保存
		/// </summary>
		/// <param name="topicList">データ</param>
		/// <param name="filePath">ファイルパス</param>
		/// <param name="ReCreate">新しく作り直す時はTrue　上書きはfalse</param>
		public static void SaveTopics(List<Topic> topicList, string filePath, bool ReCreate)
		{
			if (ReCreate)
			{
				FileInfo fi = new FileInfo(filePath);
				if (fi.Exists) fi.Delete();
			}

			foreach (var topic in topicList)
			{
				topic.Save(filePath);
			}
		}

	


		
		public static void SaveFilterContents(List<Topic> topicList, string filePath, bool ReCreate)
		{

			if (ReCreate)
			{
				FileInfo fi = new FileInfo(filePath);
				if (fi.Exists) fi.Delete();
			}

			foreach (var topic in topicList)
			{
				foreach (var filterContent in topic.FilteredContents)
				{
					filterContent.Save(topic.Title, filePath);
				}
			}
		}


		/// <summary>
		/// TopicごとのwordCountを全てあわせる
		/// </summary>
		/// <param name="topicList"></param>
		/// <returns></returns>
		public static Dictionary<MecabDataFormat, int> JoinTopicWordCount(List<Topic> topicList)
		{
			Dictionary<MecabDataFormat, int> wordDic = new Dictionary<MecabDataFormat, int>();

			for (int i = 0; i < topicList.Count; i++)
			{
				foreach (var key in topicList[i].FilterContentsWordDic.Keys)
				{
					if (wordDic.Keys.Where(x => x.Word.Equals(key.Word)).Count() == 0)
					{
						int count = topicList[i].FilterContentsWordDic[key];
						for (int j = i + 1; j < topicList.Count; j++)
						{
							var key2 = topicList[j].FilterContentsWordDic.Keys.Where(x => x.Word.Equals(key.Word)).FirstOrDefault();
							if (key2 != null) count += topicList[j].FilterContentsWordDic[key2];
						}
						wordDic[key] = count;
					}
				}
			}

			wordDic = wordDic.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);

			return wordDic;
		}



	}
}
