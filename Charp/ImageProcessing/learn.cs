using OpenCvSharp;
using OpenCvSharp.Blob;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImgToNumber
{
	internal class Program
	{
		/// <summary>
		/// 教師データ格納辞書
		/// </summary>
		private static Dictionary<int, List<IplImage>> _teacherNumDic = new Dictionary<int, List<IplImage>>();

		private static void Main(string[] args)
		{
			Console.WriteLine("Start");

			try
			{
				//教師データの参照フォルダパス
				var teacherPath = Environment.CurrentDirectory + @"\Teacher";

				//教師データの画像の名前変更
				RenameTeacherImgName(teacherPath);

				//教師データ登録
				SetTeacherImg(teacherPath);

				//出力先パス
				var dstPath = Environment.CurrentDirectory + @"\dst\";

				//Testパス
				var testPath = Environment.CurrentDirectory + @"\test\";

				//フォルダ、ファイル整理
				MakeDirOrDeleteFile(dstPath);
				MakeDirOrDeleteFile(testPath);

				//入力ファイル読み込み
				foreach (var srcInfo in new DirectoryInfo(Environment.CurrentDirectory + @"\src").GetFiles())
				{
					//数値出力
					var numbers = ImgToNumbers(srcInfo.FullName);

					//結果出力
					File.Copy(srcInfo.FullName, dstPath + numbers + "_" + srcInfo.Name);
				}

				//教師データのIplImageを開放
				foreach (var key in _teacherNumDic.Keys)
				{
					foreach (var img in _teacherNumDic[key]) img.Dispose();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

			Console.WriteLine("End");
			Console.WriteLine("Press Enter Key");
			Console.ReadLine();
		}

		/// <summary>
		/// フォルダが存在しない場合作成、ある場合は中のファイルを削除
		/// </summary>
		/// <param name="dirPath">フォルダパス</param>
		private static void MakeDirOrDeleteFile(string dirPath)
		{
			if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
			else foreach (var fi in new DirectoryInfo(dirPath).GetFiles()) File.Delete(fi.FullName);
		}

		/// <summary>
		/// 教師データのファイル名を全て変換
		/// </summary>
		/// <param name="teacherDirPath">教師データフォルダパス</param>

		private static void RenameTeacherImgName(string teacherDirPath)
		{
			foreach (var numInfo in new DirectoryInfo(teacherDirPath).GetDirectories())
			{
				var dirInfo = new DirectoryInfo(numInfo.FullName);

				for (int i = 0; i < dirInfo.GetFiles().Length; i++)
				{
					var reName = String.Format("{0:D4}", i) + @".png";
					var nowPath = dirInfo.GetFiles().Skip(i).First().FullName;
					var nowName = dirInfo.GetFiles().Skip(i).First().Name;
					File.Move(nowPath, nowPath.Replace(nowName, reName));
				}
			}
		}

		/// <summary>
		/// 教師データの登録
		/// </summary>
		/// <param name="teacherDirPath">教師データフォルダパス</param>
		private static void SetTeacherImg(string teacherDirPath)
		{
			foreach (var numInfo in new DirectoryInfo(teacherDirPath).GetDirectories())
			{
				var num = int.Parse(numInfo.Name);
				var registList = new List<IplImage>();
				foreach (var imgInfo in new DirectoryInfo(numInfo.FullName).GetFiles())
				{
					var grayImg = new IplImage(imgInfo.FullName, LoadMode.GrayScale);
					registList.Add(grayImg);
				}
				_teacherNumDic[num] = registList;
			}
		}

		/// <summary>
		/// 画像から４つの数値を読む
		/// </summary>
		/// <param name="imgPath">画像ファイルパス</param>
		/// <returns>4桁の数字</returns>
		private static string ImgToNumbers(string imgPath)
		{
			//出力結果
			int dstValue = 0;

			using (IplImage grayImg = new IplImage(imgPath, LoadMode.GrayScale))
			{
				//切り取りサイズ
				CvSize cutSize = new CvSize(grayImg.Width / 4, grayImg.Height);

				int shift = 1000;

				//横に四等分する
				for (int i = 0; i < 4; i++)
				{
					//画像の切り取り
					IplImage widthCutImg = new IplImage(cutSize, grayImg.Depth, grayImg.NChannels);
					var cutRect = new CvRect(i * 50, 0, 50, 60);
					grayImg.SetROI(cutRect);
					Cv.Copy(grayImg, widthCutImg);
					grayImg.ResetROI();

					//領域計算を行い、さらに切り取る(数字部分のみの切り取り)
					using (var cutImg = CutRectImg(widthCutImg))
					using (var resizeImg = new IplImage(28, 28, BitDepth.U8, 1))
					{
						//ごま塩ノイズ除去
						cutImg.Smooth(cutImg, SmoothType.Median);

						//画像サイズ調整
						cutImg.Resize(resizeImg);

						//ごま塩ノイズ除去
						resizeImg.Smooth(resizeImg, SmoothType.Median);

						//一度ファイルに保存
						var tempPath = Environment.CurrentDirectory + @"\temp.png";
						using (var bmp = BitmapConverter.ToBitmap(resizeImg))
						{
							bmp.Save(tempPath);
						}

						//再読み込み
						//teacherのデータを状況を同じにするため
						using (IplImage tempImg = new IplImage(tempPath, LoadMode.GrayScale))
						{
							//一文字ごとの認識
							var num = DetectNumber(tempImg) * shift;
							dstValue += num;
							shift /= 10;
						}
					}
					widthCutImg.Dispose();
				}

				File.Delete(Environment.CurrentDirectory + @"\temp.png");

				return String.Format("{0:D4}", dstValue);
			}
		}

		/// <summary>
		/// 領域計算後、特定の面積内の空間を切り取る
		/// </summary>
		/// <param name="src">画像データ</param>
		/// <returns>切り取られた画像データ</returns>
		private static IplImage CutRectImg(IplImage src)
		{
			//ごま塩ノイズ除去
			src.Smooth(src, SmoothType.Median);

			//2値化
			Cv.Threshold(src, src, 7, 255, ThresholdType.Binary);

			//ごま塩ノイズ除去
			src.Smooth(src, SmoothType.Median);

			//切り取る空間の初期値
			var minX = 0;
			var minY = 0;
			var maxX = 50;
			var maxY = 60;

			var blobs = new CvBlobs();

			//上10pixcelを切り抜き
			//根拠はないがこのパラメータでうまくいっている
			var frameImg = new IplImage(50, 50, BitDepth.U8, 1);
			src.SetROI(new CvRect(0, 10, 50, 50));
			src.Copy(frameImg);
			src.ResetROI();

			using (var colorIpl = new IplImage(frameImg.Width, frameImg.Height, BitDepth.U8, 3))
			using (var edge = new IplImage(frameImg.Size, BitDepth.U8, 1))
			{
				//エッジ抽出
				frameImg.Canny(edge, 10, 30);

				//領域計算
				var v = blobs.Label(edge);

				//面積フィルター
				blobs.FilterByArea(32, 250);

				//領域抽出したデータの保存
				//var testPath = Environment.CurrentDirectory + @"\area.png";
				//edge.CvtColor(colorIpl, ColorConversion.GrayToBgr);
				//blobs.RenderBlobs(colorIpl, colorIpl);
				//colorIpl.SaveImage(testPath);

				//面積の出力
				//foreach (var vv in blobs)
				//Console.WriteLine("{0}", vv.Value.Area);

				//面積の最大領域の検出
				if (blobs.Count > 0)
				{
					minX = blobs.Select(x => (int)x.Value.Rect.Left).ToList().Min();
					maxX = blobs.Select(x => (int)x.Value.Rect.Right).ToList().Max();
					minY = blobs.Select(x => (int)x.Value.Rect.Top).ToList().Min();
					maxY = blobs.Select(x => (int)x.Value.Rect.Bottom).ToList().Max();
				}
			}

			//数字と思われる空間で切り取る
			IplImage cutImg = new IplImage(new CvSize(maxX - minX, maxY - minY), frameImg.Depth, frameImg.NChannels);
			Cv.SetImageROI(frameImg, new CvRect(minX, minY, maxX - minX, maxY - minY));
			cutImg = Cv.CloneImage(frameImg);
			Cv.ResetImageROI(frameImg);

			//開放
			frameImg.Dispose();
			src.Dispose();

			return cutImg;
		}

		/// <summary>
		/// 画像から1つの数値を検出する
		/// </summary>
		private static int DetectNumber(IplImage checkImg)
		{
			//テストデータ保存先
			var testPath = Environment.CurrentDirectory + @"\test\";

			//ファイルカウント
			var fc = new DirectoryInfo(testPath).GetFiles().Length;

			/*
				//多数決
				//比較回数
				int majorityNum = 20;

				//ランダムシャッフル用のindex
				var shuffleIndexArray = new int[majorityNum];
				for (int i = 0; i < majorityNum; i++) shuffleIndexArray[i] = i;
				shuffleIndexArray = shuffleIndexArray.OrderBy(i => Guid.NewGuid()).ToArray();

				//多数決用のDictonary
				var majorityDic = new Dictionary<int, int>();
				foreach (var key in _teacherNumDic.Keys) majorityDic[key] = 0;
				for (int i = 0; i < majorityNum; i++)
				{
					var index = shuffleIndexArray[i];
					//差分登録
					var subDic = new Dictionary<int, int>();
					foreach (var key in _teacherNumDic.Keys)
					{
						subDic[key] = SubImg(checkImg, _teacherNumDic[key][index]);
					}
					//差分が最小の数値を求める
					var minKey = subDic.Where(x => Math.Abs(x.Value) == subDic.Min(y => Math.Abs(y.Value))).Select(x => x.Key).First();
					//多数決のカウント更新
					majorityDic[minKey]++;
				}
				var dstKey = majorityDic.Where(x => x.Value == majorityDic.Max(y => y.Value)).Select(x => x.Key).First();
				checkImg.SaveImage(testPath + dstKey + @"_" + fc + @".png");
				return dstKey;
			*/

			//総当り検索
			var v = int.MaxValue;
			var minKey = 0;
			var minIndex = 0;
			foreach (var key in _teacherNumDic.Keys)
			{
				for (int i = 0; i < _teacherNumDic[key].Count; i++)
				{
					//差分計算
					var test = SubImg(checkImg, _teacherNumDic[key][i]);
					if (test < v)
					{
						v = test;
						minKey = key;
						minIndex = i;
					}
				}
			}

			//テストデータの保存
			checkImg.SaveImage(testPath + minKey + @"_" + String.Format("{0:D4}", minIndex) + @"_" + fc + @".png");
			return minKey;
		}

		/// <summary>
		/// 1byteごとの差分計算
		/// </summary>
		/// <param name="param1">画像データ</param>
		/// <param name="param2">画像データ</param>
		/// <returns>差分値の合計</returns>
		private static int SubImg(IplImage param1, IplImage param2)
		{
			double subDst = 0;
			for (int y = 0; y < param1.Height; y++)
			{
				for (int x = 0; x < param1.Width; x++)
				{
					//Console.WriteLine("{0}      {1} ", param1[y, x].Val0, param2[y, x].Val0);
					subDst += Math.Abs(param1[x, y].Val0 - param2[x, y].Val0);
				}
			}
			return (int)subDst;
		}
	}
}