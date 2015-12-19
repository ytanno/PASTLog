using OpenCvSharp;
using System;

namespace FillImageArt
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			CvSize subResize = new CvSize(4, 4);
			CvSize mainResize = new CvSize(260, 200);

			using ( IplImage mainColorImg = new IplImage(Environment.CurrentDirectory + @"\inputMain.jpg") )
			using ( IplImage mainColorResize = new IplImage(mainResize, BitDepth.U8, 3) )
			using ( IplImage binaryImg = new IplImage(mainResize, BitDepth.U8, 1) )
			using ( IplImage edgeImg = new IplImage(mainResize, BitDepth.U8, 1) )
			using ( IplImage fillImg = new IplImage(Environment.CurrentDirectory + @"\embed.jpg") )
			using ( IplImage resizeFillImg = new IplImage(subResize.Width, subResize.Height, BitDepth.U8, 3) )
			using ( var dotDst = new IplImage(mainResize, BitDepth.U8, 3) )
			using ( var dotEdgeDst = new IplImage(mainResize, BitDepth.U8, 3) )
			{
				fillImg.Resize(resizeFillImg);
				mainColorImg.Resize(mainColorResize);

				Cv.ShowImage("InputMain", mainColorResize);
				Cv.CvtColor(mainColorResize, binaryImg, ColorConversion.BgrToGray);
				binaryImg.Threshold(binaryImg, 110, 255, ThresholdType.Binary);
				Cv.ShowImage("binaryMain", binaryImg);
				Cv.ShowImage("fillImg", resizeFillImg);
				binaryImg.Canny(edgeImg, 30, 100);
				Cv.ShowImage("edge", edgeImg);

				//fillIt 
				for ( int y = 0; y < binaryImg.Height / subResize.Height; y++)
				{
					for(int x = 0 ; x < binaryImg.Width / subResize.Width; x++)
					{
						var sum = 0.0;
						var cutY = y * subResize.Height;
						var cutX = x * subResize.Width;

						//画素値の平均を求める
						for(int yy = cutY; yy < cutY + subResize.Height; yy++)
						{
							for ( int xx = cutX; xx < cutX + subResize.Width; xx++ )
							{
								sum += binaryImg[yy, xx].Val0;
							}
						}

						if ( sum / ( subResize.Height * subResize.Width ) < 220 )
						{
							for ( int yy = cutY; yy < cutY + subResize.Height; yy++ )
							{
								for ( int xx = cutX; xx < cutX + subResize.Width; xx++ )
								{
									dotDst[yy, xx] = resizeFillImg[yy - cutY, xx - cutX];
								}
							}
						}
						else
						{
							for ( int yy = cutY; yy < cutY + subResize.Height; yy++ )
							{
								for ( int xx = cutX; xx < cutX + subResize.Width; xx++ )
								{
									dotDst[yy, xx] = new CvScalar(255, 255, 255);
								}
							}
						}
					}
				}


				for ( int y = 0; y < binaryImg.Height / subResize.Height; y++ )
				{
					for ( int x = 0; x < binaryImg.Width / subResize.Width; x++ )
					{
						
						var cutY = y * subResize.Height;
						var cutX = x * subResize.Width;
						var check = false;

						//画素値の平均を求める
						for ( int yy = cutY; yy < cutY + subResize.Height; yy++ )
						{
							for ( int xx = cutX; xx < cutX + subResize.Width; xx++ )
							{
								if ( edgeImg[yy, xx].Val0 == 255 )
								{
									check = true;
									break;
								}
							}
						}

						if ( check )
						{
							for ( int yy = cutY; yy < cutY + subResize.Height; yy++ )
							{
								for ( int xx = cutX; xx < cutX + subResize.Width; xx++ )
								{
									dotEdgeDst[yy, xx] = resizeFillImg[yy - cutY, xx - cutX];
								}
							}
						}
						else
						{
							for ( int yy = cutY; yy < cutY + subResize.Height; yy++ )
							{
								for ( int xx = cutX; xx < cutX + subResize.Width; xx++ )
								{
									dotEdgeDst[yy, xx] = new CvScalar(255, 255, 255);
								}
							}
						}
					}
				}




				Cv.ShowImage("dot", dotDst);
				Cv.ShowImage("dotEdge", dotEdgeDst);
				
				Cv.WaitKey();
				Cv.DestroyAllWindows();
			}
		}
	}
}