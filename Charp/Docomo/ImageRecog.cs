using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using System.Net;
using System.Web;



namespace DocomoWinForm
{
	public partial class Form1: Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		CvCapture _Capture;
		DocomoImageRecogBook _dr;
		bool _Start = false;
		bool _bookCheck = false;


		private void Form1_Load(object sender, EventArgs e)
		{
			Init();
		}


		public void Init()
		{
			_Capture = Cv.CreateCameraCapture(0);
			double w = 640, h = 480;
			Cv.SetCaptureProperty(_Capture, CaptureProperty.FrameWidth, w);
			Cv.SetCaptureProperty(_Capture, CaptureProperty.FrameHeight, h);

			var apiKey = @"";
			_dr = new DocomoImageRecogBook(apiKey);

		}


		private void WebCamButton_Click(object sender, EventArgs e)
		{
			if(WebCamButton.Text == "Start")
			{
				WebCamButton.Text = "End";
				_Start = true;
				Start();
			}
			else
			{
				WebCamButton.Text = "Start";
				_Start = false;
				End();
			}
		}





		public void Start()
		{
			Init();
			if ( pictureBox1.Image != null ) pictureBox1.Image.Dispose();

			Task.Factory.StartNew(() =>
			{
				while ( _Start )
				{
					var frame = new IplImage();
					frame = Cv.QueryFrame(_Capture);
					var bmp = frame.ToBitmap();
					Invoke((MethodInvoker)(() => {
						if ( pictureBox1.Image != null ) pictureBox1.Image.Dispose();	
						pictureBox1.Image = bmp;
					}));
						
				
					if ( _bookCheck )
					{
						_bookCheck = false;
						Invoke((MethodInvoker)(() => 
						{
							var d = DateTime.Now;
							var text = _dr.GetBookTitile(bmp);
							var sub = DateTime.Now.Subtract(d).TotalSeconds;
							bookTitleLabel.Text = text + "  Time is " + sub + " sec ";
							WebCamButton.Text = "Start";
						}));
						
						_Start = false;
						frame.Dispose();
						End();
						break;
					}
					frame.Dispose();
				}
			});
	
		}

		public void End()
		{
			_Capture.Dispose();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			_bookCheck = true;
		}


	}






	public class DocomoImageRecogBook
	{
		const string BaseURL = @"https://api.apigw.smt.docomo.ne.jp/imageRecognition/v1/recognize";
		public string RequestURL;

		/// <summary>
		/// APIの詳細
		///https://dev.smt.docomo.ne.jp/?p=docs.api.page&api_docs_id=105#tag01
		/// </summary>
		/// <param name="apiKey"></param>
		public DocomoImageRecogBook(string apiKey)
		{
			RequestURL = BaseURL + @"?APIKEY=" + apiKey + @"&recog=book" + @"&numOfCandidates=2";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="imagePath"></param>
		/// <returns>bookTime</returns>
		public string GetBookTitile(string imagePath)
		{
			//文字コードを指定する
			var enc = Encoding.GetEncoding("UTF-8");

			// パラメタのエンコード・構築
			//var postData = "body=" + Uri.EscapeDataString(messageTextBox.Text);

			using ( var bmp = new Bitmap(imagePath) )
			using ( var mms = new MemoryStream() )
			{
				bmp.Save(mms, System.Drawing.Imaging.ImageFormat.Jpeg);
				var postDataBytes = mms.GetBuffer();

				// WebRequest作成
				var req = WebRequest.Create(RequestURL);
				req.Method = "POST";
				req.ContentType = @"application/octet-stream";
				// POSTデータ長を指定
				req.ContentLength = postDataBytes.Length;
				//req.Headers.Add(string.Format("X-ChatWorkToken: {0}", apiKey));

				// データをPOST送信するためのStreamを取得
				var reqStream = req.GetRequestStream();
				// 送信するデータを書き込む
				reqStream.Write(postDataBytes, 0, postDataBytes.Length);
				reqStream.Close();

				// サーバーからの応答を受信する
				var res = req.GetResponse();
				// 応答データを受信するためのStreamを取得
				var resStream = res.GetResponseStream();

				// 受信して表示

				var bookTitle = "";
				using ( var sr = new StreamReader(resStream, enc) )
				{
					// 結果受信
					var resMess = sr.ReadToEnd();
					bookTitle = ExtractBookTitle(resMess);
				}

				return bookTitle;
			}
		}


		public string GetBookTitile(Bitmap bmp)
		{
			//文字コードを指定する
			var enc = Encoding.GetEncoding("UTF-8");

			// パラメタのエンコード・構築
			//var postData = "body=" + Uri.EscapeDataString(messageTextBox.Text);

			using ( var mms = new MemoryStream() )
			{
				bmp.Save(mms, System.Drawing.Imaging.ImageFormat.Bmp);
				var postDataBytes = mms.GetBuffer();

				// WebRequest作成
				var req = WebRequest.Create(RequestURL);
				req.Method = "POST";
				req.ContentType = @"application/octet-stream";
				// POSTデータ長を指定
				req.ContentLength = postDataBytes.Length;
				//req.Headers.Add(string.Format("X-ChatWorkToken: {0}", apiKey));

				// データをPOST送信するためのStreamを取得
				var reqStream = req.GetRequestStream();
				// 送信するデータを書き込む
				reqStream.Write(postDataBytes, 0, postDataBytes.Length);
				reqStream.Close();

				// サーバーからの応答を受信する
				var res = req.GetResponse();
				// 応答データを受信するためのStreamを取得
				var resStream = res.GetResponseStream();

				// 受信して表示

				var bookTitle = "";
				using ( var sr = new StreamReader(resStream, enc) )
				{
					// 結果受信
					var resMess = sr.ReadToEnd();
					bookTitle = ExtractBookTitle(resMess);
				}

				return bookTitle;
			}
		}


		private string ExtractBookTitle(string res)
		{
			var dst = "";
			var sp = res.Split(':');
			var check = false;
			foreach ( var line in sp )
			{
				if ( check )
				{
					check = false;
					dst = line.Replace(",\"releaseDate\"", "").Replace("\"", "");
				}

				if ( line.Contains("itemName") )
				{
					check = true;
				}
			}
			return dst;
		}
	}
}
