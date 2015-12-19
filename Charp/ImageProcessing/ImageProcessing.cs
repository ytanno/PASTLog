using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Drawing;

using OpenCvSharp.CPlusPlus;

namespace ytVideoEditor
{
	public static class ImageProcessing
	{


		public static void TransImg(string srcDir, string dstDir, bool multiThread, Color filterColor)
		{
			var backImgPath =
				new DirectoryInfo(srcDir).GetFiles().Where(x => x.FullName.EndsWith(".png")).Select(x => x.FullName).First();
			if ( !Directory.Exists(dstDir) ) Directory.CreateDirectory(dstDir);
			else foreach ( var fi in new DirectoryInfo(dstDir).GetFiles() ) fi.Delete();

			using ( var bgImg = new IplImage(backImgPath) )
			{
				var fs = Directory.EnumerateFiles(srcDir);
				if ( multiThread )
				{
					Parallel.ForEach(fs, f =>
					{
						TransMain(bgImg, f, dstDir, filterColor);
					});
				}
				else
				{
					foreach ( var f in fs ) TransMain(bgImg, f, dstDir, filterColor);
				}
			}
		}



		public enum CheckColorType { R, G, B };

		private static void TransMain(IplImage bgImg,
			string filePath, string folderName, Color filterColor)
		{

			var checkColorType = CheckColorType.R;

			if ( filterColor.R > filterColor.G && filterColor.R > filterColor.B ) checkColorType = CheckColorType.R;
			if ( filterColor.G > filterColor.B && filterColor.G > filterColor.R ) checkColorType = CheckColorType.G;
			if ( filterColor.B > filterColor.R && filterColor.B > filterColor.G ) checkColorType = CheckColorType.B;

			unsafe
			{
				byte* bgP = (byte*)bgImg.ImageData;
				var updateBlock = new bool[bgImg.Height, bgImg.Width];

				using ( var src = new IplImage(filePath) )
				using ( var black = new IplImage(src.Size, BitDepth.U8, 3) )
				using ( var edge = new IplImage(src.Size, BitDepth.U8, 1) )
				using ( var dst = new IplImage(src.Size, BitDepth.U8, 3) )
				{

					/*
					black.Set(new CvScalar(0, 0, 0));
					src.Canny(edge, 10, 100);

					var lineList = new List<CvLineSegmentPoint>();
					lineList.AddRange(GetMyLines(10, 100, 20, edge));
					lineList.AddRange(GetMyLines(50, 100, 20, edge));
					lineList.AddRange(GetMyLines(50, 100, 30, edge));

					foreach ( var r in GetRectList(lineList) )
					{
						black.Rectangle(r, CvColor.White, -1, LineType.AntiAlias, 0);
					}
					*/



					byte* srcP = (byte*)src.ImageData;
					byte* dstP = (byte*)dst.ImageData;
					byte* blackP = (byte*)black.ImageData;

					for ( int y = 0; y < src.Height; y++ )
					{
						for ( int x = 0; x < src.Width; x++ )
						{
							var searchFlag = false;
							var topIndex = y * src.WidthStep + x * 3 + 0;

							var nowColor = Color.FromArgb((int)srcP[topIndex + 2],
														  (int)srcP[topIndex + 1],
														  (int)srcP[topIndex + 0]);
							

							searchFlag = CheckColor(checkColorType, filterColor, nowColor);



							if ( searchFlag )//&& blackP[topIndex] == 255)
							{
								var blockSize = 16;
								for ( int yy = y - blockSize / 4; yy <= y + blockSize / 4; yy++ )
								{
									for ( int xx = x - blockSize; xx <= x + blockSize; xx++ )
									{
										if ( xx < src.Width && yy < src.Height && xx >= 0 && yy >= 0
											&& !updateBlock[yy, xx] )
										{
											for ( int c = 0; c < 3; c++ )
											{
												var index = yy * dst.WidthStep + xx * 3 + c;
												//dstP[index] = bgP[index];
												dstP[index] = srcP[index];
											}
											updateBlock[yy, xx] = true;

										}
									}
								}
							}
							
							if ( !updateBlock[y, x] )
							{
								for ( int c = 0; c < 3; c++ )
								{
									var index = y * dst.WidthStep + x * 3 + c;
									//dstP[index] = srcP[index];
									dstP[index] = bgP[index];
								}
							}
						}

					}

					dst.SaveImage(folderName + Path.GetFileName(filePath));
				}
			}
		}


		public static List<CvLineSegmentPoint> GetMyLines(int th, int p1, int p2, IplImage edge)
		{
			var lineList = new List<CvLineSegmentPoint>();
			using(CvMemStorage storage = new CvMemStorage())
			using ( CvSeq lines = Cv.HoughLines2(edge, storage, HoughLinesMethod.Probabilistic, 1, Math.PI / 180, th, p1, p2) )
			{
				for ( int i = 0; i < lines.Total; i++ )
				{
					CvLineSegmentPoint elem = lines.GetSeqElem<CvLineSegmentPoint>(i).Value;

					var xsp = Math.Abs(elem.P1.X - elem.P2.X);
					var ysp = Math.Abs(elem.P1.Y - elem.P2.Y);
					if ( ysp > xsp && ( elem.P1.Y < 100 || elem.P2.Y < 100 ) )
					{
						//black.Line(elem.P1, elem.P2, CvColor.White, 1, LineType.AntiAlias, 0);
						lineList.Add(elem);
					}
				}
			}

			return lineList;
		}



		public static bool CheckColor(CheckColorType cct, Color filterColor, Color now)
		{
			var hit = false;
		
			var sp = 60;
			var sp2 = 60;

			//if ( now.B > 200 && now.R > 200 && now.B > 200 ) hit = true;
			//if ( now.B < 30 && now.R < 30 && now.B < 30 ) hit = true;


			if(cct == CheckColorType.R)
			{
				if ( 
					 now.B < filterColor.B + sp
					 && now.G < filterColor.G + sp
					 && now.R > 100
					)
				{
					hit = true;
				}

			}

			else if(cct == CheckColorType.G)
			{
				if ( 
					now.B < filterColor.B + sp
					&& now.R < filterColor.R + sp
					&& now.G > 100
					)
				{
					hit = true;
				}
			}


			else
			{
				if ( 
					now.R < filterColor.R + sp
					&& now.G < filterColor.G + sp
					&& now.B > 100
					)
				{
					hit = true;
				}
			}

			



			return hit;
		}



		public static List<CvRect> GetRectList(List<CvLineSegmentPoint> ls)
		{
			var rectList = new List<CvRect>();
			for ( int i = 0; i < ls.Count; i++ )
			{
				for ( int j = i + 1; j < ls.Count; j++ )
				{
					var x1 = ls[i].P1.X;
					var x2 = ls[j].P1.X;
					var spX = Math.Abs(x2 - x1);
					if ( spX > 5 && spX < 100 )
					{
						rectList.Add(GetRect(ls[i], ls[j]));
					}
				}
			}
			return rectList;
		}

		public static CvRect GetRect(CvLineSegmentPoint line1, CvLineSegmentPoint line2)
		{
			var xList = new List<int>();
			var yList = new List<int>();

			xList.Add(line1.P1.X);
			xList.Add(line1.P2.X);
			xList.Add(line2.P1.X);
			xList.Add(line2.P2.X);

			yList.Add(line1.P1.Y);
			yList.Add(line1.P2.Y);
			yList.Add(line2.P1.Y);
			yList.Add(line2.P2.Y);

			var yMin = 0;
			if ( yList.Min() > 100 ) yMin = yList.Min();

			var r = new CvRect(xList.Min(), yMin, xList.Max() - xList.Min(), yList.Max() - yMin);
			return r;
		}


		private void GetLine(IplImage src, int pd, int xd, int yd, int bv, int af, int dl)
		{
			using (var srcGrayLine = new IplImage(src.Size, BitDepth.U8, 1))
			using (var cp = new IplImage(src.Size, BitDepth.U8, 3))
			{
				Cv.CvtColor(src, srcGrayLine, ColorConversion.BgrToGray);

				srcGrayLine.Threshold(srcGrayLine, bv, 255, ThresholdType.BinaryInv);
				//srcGrayLine.Canny(srcGrayLine, 120, 140);
				//Cv.ShowImage("srcGrayLine", srcGrayLine);

				if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
				pictureBox1.Image = srcGrayLine.ToBitmap();

				src.Copy(cp);
				var blobs = new CvBlobs();

				var v = blobs.Label(srcGrayLine);

				blobs.FilterByArea(af, int.MaxValue);

				var list = new List<Point>();
				var rmList = new List<int>();
				var beforeX = src.Size.Width * 2;
				var beforeY = src.Size.Height * 2;

				var pList = new List<Point>();
				var ppList = new List<Point>();
				var tmpList = new List<Point>();

				foreach (var blob in blobs.OrderBy(x => x.Value.Centroid.Y))
				{
					var cx = (int)blob.Value.Centroid.X;
					var cy = (int)blob.Value.Centroid.Y;

					var check = true;
					foreach (var p in pList)
					{
						var dist = Math.Sqrt(Math.Pow(cx - p.X, 2) + Math.Pow(cy - p.Y, 2));
						if (dist < pd)
						{
							check = false;
							break;
						}
					}
					if (check) pList.Add(new Point(cx, cy));
				}

				foreach (var p in pList)
				{
					var check = true;
					foreach (var pp in ppList)
					{
						if (Math.Abs(pp.X - p.X) < xd && //Math.Abs(pp.Y - p.Y) < 5)
							Math.Abs(pp.Y - p.Y) < yd)
						{
							check = false;
							break;
						}
					}
					if (check) ppList.Add(p);
				}


				var lineSize = new CvSize(20, 2);


				var crList = new List<MyCircle>();


				foreach (var pp in ppList)
				{
					var size = GetLineSize(srcGrayLine, pp, lineSize);

					if (size.Width > dl)
					{
						//pppList.Add(pp);
						crList.Add(new MyCircle(pp, new CvScalar(0, 0, 255)));

						if (size.Height > 3)
						{
							crList.Add(new MyCircle(new Point(pp.X, pp.Y + 2), new CvScalar(255, 0, 0)));
						}

					}
				}

				var dst = "";
				foreach (var p in crList.OrderByDescending(x => x.P.Y))
				{
					cp.Circle(new CvPoint(p.P.X, p.P.Y), 3, p.Color);
					var num = (int)(p.P.X / 31) + 1;
					if (num > 9) num = 0;
					dst += num.ToString();
				}

				resultLabel.Text = "result " + dst;

				if (pictureBox2.Image != null) pictureBox2.Image.Dispose();
				pictureBox2.Image = cp.ToBitmap();

				//Cv.ShowImage("test", cp);
			}
		}

		public CvSize GetLineSize(IplImage src, Point p, CvSize lineSize)
		{
			CvSize dst = new CvSize(0, 0);
			var searchWidth = lineSize.Width / 2;
			var searchHeight = lineSize.Height;

			bool start = false;
			Point s = new Point(0, 0);
			Point e = new Point(0, 0);

			List<Point> pList = new List<Point>();

			int missLimit = 20;
			int missCount = 0;

			for (int y = p.Y; y < p.Y + searchHeight * 4; y++)
			{
				for (int x = p.X - searchWidth; x < p.X + searchWidth; x++)
				{
					var yI = y;
					var xI = x;
					if (y < 0) yI = 0;
					if (x < 0) xI = 0;
					if (y >= src.Height) yI = src.Height - 1;
					if (x >= src.Width) xI = src.Width - 1;

					if (start) pList.Add(new Point(x, y));
					if (src[yI, xI] == 255 && !start)
					{
						//s = new Point(xI, yI);
						start = true;
					}

					if (start && src[yI, xI] == 0) missCount++;

					if (missLimit < missCount)
					{
						dst = new CvSize(pList.Max(v => v.X) - pList.Min(v => v.X),
										pList.Max(v => v.Y) - pList.Min(v => v.Y));

						return dst;
					}
				}
			}

			return dst;
		}


		private void CutImg(int pd, int xd, int yd, int bv, int af, int dl)
		{
			foreach (var blob in _blobs)
			{
				var cutRect = blob.Value.Rect;
				cutRect.Height += 20;

				using (var cpImg = new IplImage(_colorImg.Size, BitDepth.U8, 3))
				using (var cutImg = new IplImage(new CvSize(blob.Value.Rect.Width,
							blob.Value.Rect.Height + 20), BitDepth.U8, 3))
				{
					_colorImg.Copy(cpImg);

					//画像の切り取り
					cpImg.SetROI(cutRect);
					Cv.Copy(cpImg, cutImg);
					cpImg.ResetROI();
					//Cv.ShowImage("tes1", cutImg);

					using (var binary = new IplImage(cutImg.Size, BitDepth.U8, 1))
					using (var re = new IplImage(cutImg.Size, BitDepth.U8, 3))
					{
						Cv.CvtColor(cutImg, binary, ColorConversion.BgrToGray);

						binary.Threshold(binary, 80, 255, ThresholdType.Binary);
						binary.Canny(binary, 20, 40);

						Cv.CvtColor(binary, re, ColorConversion.GrayToBgr);

						var blobs = new CvBlobs();
						var v = blobs.Label(binary);
						blobs.FilterByArea(10, int.MaxValue);

						var minX = blobs.Select(x => (int)x.Value.Rect.Left).ToList().Min();
						var maxX = blobs.Select(x => (int)x.Value.Rect.Right).ToList().Max();
						var minY = blobs.Select(x => (int)x.Value.Rect.Top).ToList().Min();
						var maxY = cutImg.Height;

						var reSize = new CvSize(maxX - minX, maxY - minY);
						var reRect = new CvRect(minX, minY, maxX - minX, maxY - minY);

						using (var reCut = new IplImage(reSize, BitDepth.U8, 3))
						{
							cutImg.SetROI(reRect);
							Cv.Copy(cutImg, reCut);
							cutImg.ResetROI();

							GetLine(reCut, pd, xd, yd, bv, af, dl);
						}
					}

					break;
				}
			}
		}


		private void SetBinary(int value)
		{
			using (var binary = new IplImage(_src.Size, BitDepth.U8, 1))
			{
				_src.Threshold(binary, value, 255, ThresholdType.Binary);
				//_src.Canny(binary, 10, 30);
				if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
				pictureBox1.Image = binary.ToBitmap();

				if (pictureBox2.Image != null) pictureBox2.Image.Dispose();

				_blobs = new CvBlobs();

				//領域計算
				var v = _blobs.Label(binary);

				//面積フィルター
				_blobs.FilterByArea(int.Parse(textBox1.Text), int.MaxValue);

				using (var dst = new IplImage(_colorImg.Size, BitDepth.U8, 3))
				{
					_colorImg.Copy(dst);
					_blobs.RenderBlobs(_colorImg, dst);
					pictureBox2.Image = dst.ToBitmap();
				}
			}
		}

		private void GetEyeLine()
		{
			const double Scale = 1.1;
			const double ScaleFactor = 2.5;
			const int MinNeighbors = 2;

			//string fPath = Environment.CurrentDirectory + @"\haarcascade_righteye_2splits.xml";

			foreach (var fi in new DirectoryInfo(Environment.CurrentDirectory).GetFiles().Where(x => x.FullName.Contains("eye")))
			{
				using (IplImage img = new IplImage(_filePath, LoadMode.Color))
				using (IplImage gray = new IplImage(_filePath, LoadMode.GrayScale))
				using (IplImage smallImg = new IplImage(new CvSize(Cv.Round(img.Width / Scale), Cv.Round(img.Height / Scale)), BitDepth.U8, 1))
				using (CvHaarClassifierCascade cascade =
					CvHaarClassifierCascade.FromFile(fi.FullName))
				using (CvMemStorage storage = new CvMemStorage())
				{
					Cv.Resize(gray, smallImg, Interpolation.Linear);
					Cv.EqualizeHist(smallImg, smallImg);


					storage.Clear();


					//目の検出
					CvSeq<CvAvgComp> eyes = Cv.HaarDetectObjects(smallImg,
						cascade, storage, ScaleFactor, MinNeighbors, HaarDetectionType.Zero, new CvSize(30, 30));


					foreach (var eye in eyes)
					{
						Cv.Rectangle(img, eye.Value.Rect, new CvScalar(0, 255, 255));
					}

					if (eyes.Count() > 0)
						pictureBox1.Image = (Bitmap)img.ToBitmap().Clone();

				}
			}

	}
}
