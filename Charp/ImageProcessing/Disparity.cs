using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;//Dll読み込みのために必要

using ShoNS.Array;
using ShoNS.MathFunc;

using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace LabImg
{
	/// <summary>
	/// 視差を求めるクラス
	/// </summary>
	public unsafe static class Disparity
	{
		/// <summary>
		/// CDPの呼び出し
		/// </summary>
		/// <param name="inImgData">入力画像のImageDataのポインタ</param>
		/// <param name="inImgHeight">入力画像の高さ</param>
		/// <param name="inImgWidth">入力画像の幅</param>
		/// <param name="inImgWidthStep">入力画像のWidthStep</param>
		/// <param name="refImgData">参照画像のImageDataのポインタ</param>
		/// <param name="refImgHeight">参照画像の高さ</param>
		/// <param name="refImgWidth">参照画像の幅</param>
		/// <param name="refImgWidthStep">参照画像のWidthStep</param>
		/// <param name="disparityX">X方向の視差配列の先頭アドレス（出力用)</param>
		/// <param name="disparityY">Y方向の視差配列の先頭アドレス(出力用)</param>
		[DllImport("cvTwoDCDPLibrary.dll")]
		public static extern void twoDCDP(byte* inImgData, int inImgHeight, int inImgWidth, int inImgWidthStep,
										  byte* refImgData, int refImgHeight, int refImgWidth, int refImgWidthStep,
										  int* disparityX, int* disparityY);

		/// <summary>
		///グレイ画像の曲率を求める
		///opt==0,平均曲率(mean curvature) 
		///opt==1,ガウス曲率（Gaussian Curvature)
		///opt==2,Kitchen-Rosenfeld operator
		///opt==3,Zuniga-Haralick operator
		///opt==4,Harris operator
		///opt==5,MORAVEC-SUZAN (threはSUZAN作用素で用いる）
		///opt==6,rerative SIFT operator 
		/// </summary>
		/// <param name="img"></param>
		/// <param name="opt"></param>
		/// <returns></returns>
		public static CvMat imageCurvature(IplImage img,int opt)
		{
			CvMat Cx = new CvMat(img.Height, img.Width, MatrixType.F32C1);
			CvMat G2 = Disparity.imageGradient2(img);
			CvMat G1 = Disparity.imageGradient(img,0);
			if (opt == 0)
			{
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 0; x < img.Width; x++)
					{
						CvScalar fx = G1.Get2D(y, x);
						CvScalar fxx = G2.Get2D(y, x);
						double h = (fxx.Val0 + fxx.Val1 + (fxx.Val0 * fx.Val1 * fx.Val1) - (2.0f * fxx.Val2 * fx.Val0 * fx.Val1) + (fxx.Val1 * fx.Val0 * fx.Val0)) / (2.0f * (1.0f + fx.Val0 * fx.Val0 + fx.Val1 * fx.Val1));
						Cx.Set2D(y, x, new CvScalar(h));
					}
				}
			}
			else if (opt == 1)
			{
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 0; x < img.Width; x++)
					{
						CvScalar fx = G1.Get2D(y, x);
						CvScalar fxx = G2.Get2D(y, x);
						double K=(fxx.Val0*fxx.Val1 - fxx.Val2*fxx.Val2 ) / (1.0+fx.Val0*fx.Val0 + fx.Val1*fx.Val1);
						Cx.Set2D(y, x, new CvScalar(K));
					}
				}
			}
			else if (opt == 2)
			{
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 0; x < img.Width; x++)
					{
						CvScalar fx = G1.Get2D(y, x);
						CvScalar fxx = G2.Get2D(y, x);
						double fx2=fx.Val0*fx.Val0;
						double fy2=fx.Val1*fx.Val1; 
						double fxfy=fx.Val0*fx.Val1; 
						double fxxyy=fx2+fy2; 
//                        double fxxyy2=fxxyy*Math.Sqrt(fxxyy);
						double fxx2=fxx.Val0*fy2 + fxx.Val1*fx2 - 2.0*fxx.Val2*fxfy;
						double K=fxx2/fxxyy;
						Cx.Set2D(y, x, new CvScalar(K));
					}
				}
				//    fx2=fx.^2;  fy2=fy.^2; fxfy=fx.*fy; fxxyy=fx2+fy2; fxxyy2=fxxyy.*sqrt(fxxyy);
				//    fxx2=fxx.*fy2 + fyy.*fx2 - 2*fxy.*fxfy;
				//    K=fxx2./fxxyy;
				//    H=fxx2./fxxyy2;
			}
			else if (opt == 3)
			{
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 0; x < img.Width; x++)
					{
						CvScalar fx = G1.Get2D(y, x);
						CvScalar fxx = G2.Get2D(y, x);
						double fx2 = fx.Val0 * fx.Val0;
						double fy2 = fx.Val1 * fx.Val1;
						double fxfy = fx.Val0 * fx.Val1;
						double fxxyy = fx2 + fy2;
						double fxxyy2 = fxxyy * Math.Sqrt(fxxyy);
						double fxx2 = fxx.Val0 * fy2 + fxx.Val1 * fx2 - 2.0 * fxx.Val2 * fxfy;
						double H = fxx2 / fxxyy2;
						Cx.Set2D(y, x, new CvScalar(H));
					}
				}
 
			}
			return Cx;
		}
		/// <summary>
		/// 一階微分画像を作成（入力は、グレースケール) 
		/// </summary>
		/// <param name="img"></param>
		/// <param name="opt">opt=0 GX,GY,opt=1　Gain,Phase</param>
		/// <returns>>X方向微分、Y方向微分、振幅、位相（度）を４ｃｈCvMatで戻す</returns>
		public static CvMat imageGradient(IplImage img, int opt)
		{
			unsafe
			{
				CvMat GXYGP = new CvMat(img.Height, img.Width, MatrixType.F32C2);
				double gx, gy, gn, ph;
				double[,] GX = new double[img.Height, img.Width];
				double[,] GY = new double[img.Height, img.Width];

				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 1; x < (img.Width - 1); x++)
					{
						int xm = x - 1;
						int xp = x + 1;
						gx = (double)(img[y, xp].Val0 - img[y, xm].Val0) / 2.0;
						GX[y,x]=gx;
					}
					GX[y,0]=0.0;
					GX[y,img.Width - 1]=0.0;
				}
				for (int x = 0; x < img.Width; x++)
				{
					for (int y = 2; y < (img.Height - 1); y++)
					{
						int ym = y - 1;
						int yp = y + 1;
						gy = (double)(img[yp, x].Val0 - img[ym, x].Val0) / 2.0;
						GY[y, x]=gy;
					}
					GY[0, x]=0.0;
					GY[img.Height - 1, x]=0.0;
				}
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 0; x < img.Width; x++)
					{
						//                        int offset = (FG.Step * y)/8 +  x*2;
						gx = GX[y, x];
						gy = GY[y, x];
						if (opt == 0)
						{
							GXYGP.Set2D(y, x, new CvScalar(gx, gy));
						}
						else
						{
							gn = Math.Sqrt(gx * gx + gy * gy);
							ph = Math.Atan2(gy, gx) * 180.0 / 3.14159;
							GXYGP.Set2D(y, x, new CvScalar(gn, ph));
						}
					}
				}
				return GXYGP;
			}
		}
		/// <summary>
		/// 一階微分画像を作成（入力は、グレースケール)
		/// </summary>
		/// <param name="img"></param>
		/// <param name="GX">X方向微分</param>
		/// <param name="GY">Y方向微分</param>
		/// <param name="GN">振幅</param>
		/// <param name="PH">位相（摂</param>

		public static void imageGradient(IplImage img, out CvMat GX, out CvMat GY, out CvMat GN, out CvMat PH)
		{
			//CvMatの読み込み
			//		CvScalar scalar = GX.Get2D(y, x);
			//		Console.WriteLine(scalar.Val0 + " " + scalar.Val1 + " " + scalar.Val2);
			//書き込み
			//     CvScalar scalar;
			//     Scalar.
			//		GX.Set2D(y, x, scalar);
			unsafe
			{
				GX = new CvMat(img.Height, img.Width, MatrixType.F32C1);
				GY = new CvMat(img.Height, img.Width, MatrixType.F32C1);
				GN = new CvMat(img.Height, img.Width, MatrixType.F32C1);
				PH = new CvMat(img.Height, img.Width, MatrixType.F32C1);
				double gx, gy, gn, ph;
				//                float* ptrFG = (float*)FG.Data;
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 1; x < (img.Width - 1); x++)
					{
						int xm = x - 1;
						int xp = x + 1;
						gx = (double)(img[y, xp].Val0 - img[y, xm].Val0) / 2.0;
						GX.Set2D(y, x, new CvScalar(gx));
					}
					GX.Set2D(y, 0, new CvScalar(0.0));
					GX.Set2D(y, img.Width - 1, new CvScalar(0.0));
				}
				for (int x = 0; x < img.Width; x++)
				{
					for (int y = 2; y < (img.Height - 1); y++)
					{
						int ym = y - 1;
						int yp = y + 1;
						gy = (double)(img[yp, x].Val0 - img[ym, x].Val0) / 2.0;
						GY.Set2D(y, x, new CvScalar(gy));
					}
					GY.Set2D(0, x, new CvScalar(0.0));
					GY.Set2D(img.Height - 1, x, new CvScalar(0.0));
				}
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 0; x < img.Width; x++)
					{
						//                        int offset = (FG.Step * y)/8 +  x*2;
						gx = GX.Get2D(y, x).Val0;
						gy = GY.Get2D(y, x).Val0;
						gn = Math.Sqrt(gx * gx + gy * gy);
						ph = Math.Atan2(gy, gx) * 180.0 / 3.14159;
						GN.Set2D(y, x, new CvScalar(gn));
						PH.Set2D(y, x, new CvScalar(ph));
					}
				}
				return;
			}
		}
		/// <summary>
		/// 二階微分の計算（入力は、グレースケール) 
		/// </summary>
		/// <param name="img"></param>
		/// <returns>>gxx,gyy,gxyを３ｃｈのCvMatで戻す</returns>
		public static CvMat imageGradient2(IplImage img)
		{
			unsafe
			{
				CvMat GLP = new CvMat(img.Height, img.Width, MatrixType.F32C3);
				//                float* ptrFG = (float*)FG.Data;
				double[,] gxx = new double[img.Height, img.Width];
				double[,] gyy = new double[img.Height, img.Width];
				double[,] gxy = new double[img.Height, img.Width];
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 1; x < (img.Width - 1); x++)
					{
						int xm = x - 1;
						int xp = x + 1;
						gxx[y, x] = (double)(img[y, xp].Val0 + img[y, xm].Val0 - 2.0 * img[y, x].Val0);
					}
					gxx[y, 0] = (double)(img[y, 1].Val0 - img[y, 0].Val0);
					gxx[y, img.Width - 1] = (double)(img[y, img.Width - 2].Val0 - img[y, img.Width - 1].Val0);
				}
				for (int x = 0; x < img.Width; x++)
				{
					for (int y = 1; y < (img.Height - 1); y++)
					{
						int ym = y - 1;
						int yp = y + 1;
						gyy[y,x] = (double)(img[yp, x].Val0 + img[ym, x].Val0 - 2.0 * img[y, x].Val0);
					}
					gyy[0,x] = (double)(img[1, x].Val0 - img[0, x].Val0);
					gyy[img.Height-1,x] = (double)(img[img.Height - 2, x].Val0 - img[img.Height - 1, x].Val0);
				}
				for (int y = 1; y < img.Height - 1; y++)
				{
					for (int x = 1; x < img.Width - 1; x++)
					{
						int xm = x - 1;
						int xp = x + 1;
						int ym = y - 1;
						int yp = y + 1;
						gxy[y,x]= (img[yp,xp].Val0 - img[y,xp].Val0 - img[yp,x].Val0 + img[y,x].Val0
								  +img[y, x].Val0 - img[ym, x].Val0 - img[y, xm].Val0 + img[ym, xm].Val0)/2.0f;
					}
				}
				for (int y = 0; y < img.Height; y++)
				{
					for (int x = 0; x < img.Width; x++)
					{
						GLP.Set2D(y, x, new CvScalar(gxx[y,x],gyy[y,x],gxy[y,x]));
					}
				}
				return GLP;
			}
		}
		/// <summary>
		/// 二階微分画像のラプラシアンを作成（入力は、グレースケール) 
		/// </summary>
		/// <param name="img"></param>
		/// <returns>>ラプラシアンを一ｃｈCvMatで戻す</returns>
		public static CvMat imageLanlacian(IplImage img)
		{
			unsafe
			{
				CvMat GLP = new CvMat(img.Height, img.Width, MatrixType.F32C1);
				//                float* ptrFG = (float*)FG.Data;
				double[,] gxx = new double[img.Height, img.Width];
				for (int y = 0; y < img.Height; y++) 
				{
					for (int x = 1; x < (img.Width - 1); x++)
					{
						int xm = x - 1;
						int xp = x + 1;
						gxx[y,x] = (double)(img[y, xp].Val0 + img[y, xm].Val0 - 2.0 * img[y, x].Val0); 
					}
					gxx[y, 0] = (double)(img[y, 1].Val0 - img[y, 0].Val0);
					gxx[y, img.Width - 1] = (double)(img[y, img.Width - 2].Val0 - img[y, img.Width - 1].Val0);
				}
				for (int x = 0; x < img.Width; x++)
				{
					double gyy,gn;
					for (int y = 1; y < (img.Height - 1); y++)
					{
						int ym = y - 1;
						int yp = y + 1;
						gyy = (double)(img[yp, x].Val0 + img[ym, x].Val0 - 2.0 * img[y, x].Val0);
						gn = gxx[y,x] * gxx[y,x] + gyy * gyy;
						GLP.Set2D(y, x, new CvScalar(gn));
					}
					gyy = (double)(img[1,x].Val0 - img[0,x].Val0);
					gn = gxx[0, x] * gxx[0, x] + gyy * gyy;
					GLP.Set2D(0, x, new CvScalar(gn));
					gyy = (double)(img[img.Height - 2,x].Val0 - img[img.Height - 1,x].Val0);
					gn = gxx[img.Height-1, x] * gxx[img.Height-1, x] + gyy * gyy;
					GLP.Set2D(img.Height-1, x, new CvScalar(gn));
				}
				return GLP;
			}
		}
		/// <summary>
		/// ４CｈのCvMatから、exNoで指定したデータを取り出し、１ｃｈのCvMatで戻す
		/// </summary>
		/// <param name="G"></param>
		/// <param name="exNo">０、１，２，３で指定</param>
		/// <returns></returns>
		public static CvMat CvMatExtraction(CvMat G, int exNo)
		{
			CvMat GEx = new CvMat(G.Height, G.Width, MatrixType.F32C1);
			for (int y = 0; y < G.Height; y++)
			{
				for (int x = 0; x < G.Width; x++)
				{
					CvScalar W = G.Get2D(y, x);
					if( exNo==3)  GEx.Set2D(y, x, W.Val3);
					else if (exNo == 2) GEx.Set2D(y, x, W.Val2);
					else if (exNo == 1) GEx.Set2D(y, x, W.Val1);
					else  GEx.Set2D(y, x, W.Val0);
				}
			}
			return GEx;
		}
		/// <summary>
		/// 画像のブロックごとの分散または振幅を求める
		/// </summary>
		/// <param name="rightI"></param>
		/// <param name="halfBlockSize"></param>
		/// <param name="VorPP">’P’で振幅、それ以外は標準偏差</param>
		/// <returns>FloatArrayを返す</returns>
		public static CvMat BlockMatchTexture(IplImage rightI, int halfBlockSize, char VorPP)
		{
			int NX = rightI.Width;
			int NY = rightI.Height;
			int blockSize = 2 * halfBlockSize + 1;
			CvMat var = new CvMat(NY, NX, MatrixType.F32C1);
//			IplImage tmR=new IplImage(blockSize,blockSize,BitDepth.F32,1);
			FloatArray tmR;
			FloatArray RG = kaneUtility.IplImageToFloatArray(rightI);
//            CvRect rect = new CvRect(n, m * 2, 1, 2);
//            baseMat.GetSubRect(out subRect, rect);
//            CvMat subRect;
//            CvRect rect = new CvRect(n, m * 2, 1, 2);
//            baseMat.GetSubRect(out subRect, rect);

//            subRect[0, 0] = mxyMat.Get2D(m, n).Val0 - txy[m].Val0;
//            subRect[1, 0] = mxyMat.Get2D(m, n).Val1 - txy[m].Val1;
//            subRect.Dispose();

			for (int m = 0; m < NY; m++)
			{
				int minr = Math.Max(1, m + 1 - halfBlockSize);
				int maxr = Math.Min(NY, m + 1 + halfBlockSize);

				for (int n = 0; n < NX; n++)
				{
					int minc = Math.Max(1, n + 1 - halfBlockSize);
					int maxc = Math.Min(NX, n + 1 + halfBlockSize);
					tmR = RG.GetSlice(minr - 1, maxr - 1, minc - 1, maxc - 1);
					//double avr=tmR.Mean();
					float xx;
					if (VorPP == 'P' || VorPP == 'p')
					{
						xx = tmR.Max() - tmR.Min();
					}
					else
					{
						xx = tmR.Std();
					}
					var[m, n] = xx;
				}
			}
			return var;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="grayLeft"></param>
		/// <param name="grayRight"></param>
		/// <param name="halfBlockSize"></param>
		/// <param name="disparityRange"></param>
		/// <param name="TopLeftX"></param>
		/// <param name="TopLeftY"></param>
		/// <returns></returns>
		public static float[] HorizontalBlockMatch(IplImage grayLeft, IplImage grayRight, int halfBlockSize, int disparityRange, int TopLeftX,int TopLeftY)
		{
			int NX = grayLeft.Width;
			int NY = grayLeft.Height;
			//IplImage Ddynamic = new IplImage(NX, NY, BitDepth.F32, 1);
			float[] disparityCost=new float[disparityRange+1];
			unsafe
			{
				int disparityRange1 = disparityRange + 1;//０－disparityrange＋１までを探索
				int blockSize = 2 * halfBlockSize + 1;
				int nRowsLeft = NY;

				float finf = float.MaxValue;
//                float[] disparityCost = new float[disparityRange1];
				IplImage tmR = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				IplImage tmL = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				byte* rightPtr = (byte*)grayRight.ImageData;
				byte* tmRPtr = (byte*)tmR.ImageData;
				byte* tmLPtr = (byte*)tmL.ImageData;
				byte* leftPtr = (byte*)grayLeft.ImageData;
  //              float* dynamicPtr = (float*)Ddynamic.ImageData;
				int iwd = tmL.WidthStep;
				int rwd = grayRight.WidthStep;
  //              int dwd = Ddynamic.WidthStep / 4;

				int m = TopLeftY;
				//for (int m = 0; m < nRowsLeft; m++)
				//{

					for (int i = 0; i < disparityRange1; i++) disparityCost[i] = finf;
					int minr = Math.Max(1, m + 1 - halfBlockSize);
					int maxr = Math.Min(nRowsLeft, m + 1 + halfBlockSize);

					int n=TopLeftX;
					//for (int n = 0; n < NX; n++)
					//{
						int minc = Math.Max(1, n + 1 - halfBlockSize);
						int maxc = Math.Min(NX, n + 1 + halfBlockSize);
						int mind = Math.Max(0, 1 - minc);
						int maxd = Math.Min(disparityRange, NX - maxc);

						//                    tmL = leftI.GetSlice(minr - 1, maxr - 1, minc + d - 1, maxc + d - 1);
						//                    tmR = rightI.GetSlice(minr - 1, maxr - 1, minc - 1, maxc - 1);

						for (int y = 0; y < blockSize * blockSize; y++) tmRPtr[y] = 0;

						for (int y = minr - 1; y <= (maxr - 1); y++)
						{
							for (int x = minc - 1; x <= (maxc - 1); x++)
							{
								tmRPtr[(y - minr + 1) * iwd + (x - minc + 1)] = rightPtr[y * rwd + x];
							}
						}


						for (int d = mind; d <= maxd; d++)
						{
							for (int y = 0; y < blockSize * blockSize; y++) tmLPtr[y] = 0;

							for (int y = minr - 1; y <= (maxr - 1); y++)
							{
								for (int x = minc + d - 1; x <= (maxc + d - 1); x++)
								{
									tmLPtr[(y - minr + 1) * iwd + (x - minc - d + 1)] = leftPtr[y * rwd + x];
								}
							}

							int sum = 0;
							for (int y = 0; y < (blockSize * blockSize); y++)
							{
								sum += (int)Math.Abs(tmLPtr[y] - tmRPtr[y]);
							}
							disparityCost[d] = sum;
						}

						float dismin = finf;
						float ix1 = 0;
						for (int i = 0; i < disparityRange1; i++)
						{
							if (disparityCost[i] < dismin)
							{
								ix1 = i;
								dismin = disparityCost[i];
							}
						}

	//                    dynamicPtr[m * dwd + n] = ix1;

						//Ddynamic[m, n] = ix1;
					//}
				//}
				tmL.Dispose();
				tmR.Dispose();
			}

			return disparityCost;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="leftPath"></param>
		/// <param name="rightPath"></param>
		/// <param name="halfBlockSize"></param>
		/// <param name="disparityRange"></param>
		/// <returns></returns>
		public static IplImage KanemotoBlockMatch(string leftPath, string rightPath, int halfBlockSize, int disparityRange)
		{
			using (IplImage grayLeft = new IplImage(leftPath, LoadMode.GrayScale))
			using (IplImage grayRight = new IplImage(rightPath, LoadMode.GrayScale))
			{
				return KanemotoBlockMatch(grayLeft, grayRight,  halfBlockSize, disparityRange);
			}
		}

	



		/// <summary>
		/// MATLABのBlockマッチング
		/// </summary>
		/// <param name="grayLeft"></param>
		/// <param name="grayRight"></param>
		/// <param name="halfBlockSize"></param>
		/// <param name="disparityRange"></param>
		/// <returns></returns>
		public static IplImage KanemotoBlockMatch(IplImage grayLeft, IplImage grayRight, int halfBlockSize, int disparityRange)
		{
			int NX = grayLeft.Width;
			int NY = grayLeft.Height;
			IplImage Ddynamic = new IplImage(NX, NY, BitDepth.F32, 1);
			unsafe
			{
				int disparityRange1 = disparityRange + 1;//０－disparityrange＋１までを探索
				int blockSize = 2 * halfBlockSize + 1;
				int nRowsLeft = NY;

				float finf = float.MaxValue;
				float[] disparityCost = new float[disparityRange1];
				IplImage tmR = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				IplImage tmL = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				byte* rightPtr = (byte*)grayRight.ImageData;
				byte* tmRPtr = (byte*)tmR.ImageData;
				byte* tmLPtr = (byte*)tmL.ImageData;
				byte* leftPtr = (byte*)grayLeft.ImageData;
				float* dynamicPtr = (float*)Ddynamic.ImageData;
				int iwd = tmL.WidthStep;
				int rwd = grayRight.WidthStep;
				int dwd = Ddynamic.WidthStep / 4;


				for (int m = 0; m < nRowsLeft; m++)
				{

					for (int i = 0; i < disparityRange1; i++) disparityCost[i] = finf;
					int minr = Math.Max(1, m + 1 - halfBlockSize);
					int maxr = Math.Min(nRowsLeft, m + 1 + halfBlockSize);

					for (int n = 0; n < NX; n++)
					{
						int minc = Math.Max(1, n + 1 - halfBlockSize);
						int maxc = Math.Min(NX, n + 1 + halfBlockSize);
						int mind = Math.Max(0, 1 - minc);
						int maxd = Math.Min(disparityRange, NX - maxc);

						//                    tmL = leftI.GetSlice(minr - 1, maxr - 1, minc + d - 1, maxc + d - 1);
						//                    tmR = rightI.GetSlice(minr - 1, maxr - 1, minc - 1, maxc - 1);

						for (int y = 0; y < blockSize * blockSize; y++) tmRPtr[y] = 0;

						for (int y = minr - 1; y <= (maxr - 1); y++)
						{
							for (int x = minc - 1; x <= (maxc - 1); x++)
							{
								tmRPtr[(y - minr + 1) * iwd + (x - minc + 1)] = rightPtr[y * rwd + x];
							}
						}


						for (int d = mind; d <= maxd; d++)
						{
							for (int y = 0; y < blockSize * blockSize; y++) tmLPtr[y] = 0;

							for (int y = minr - 1; y <= (maxr - 1); y++)
							{
								for (int x = minc + d - 1; x <= (maxc + d - 1); x++)
								{
									tmLPtr[(y - minr + 1) * iwd + (x - minc - d + 1)] = leftPtr[y * rwd + x];
								}
							}

							int sum = 0;
							for (int y = 0; y < (blockSize * blockSize); y++)
							{
								sum += (int)Math.Abs(tmLPtr[y] - tmRPtr[y]);
							}
							disparityCost[d] = sum;
						}

						float dismin = finf;
						float ix1 = 0;
						for (int i = 0; i < disparityRange1; i++)
						{
							if (disparityCost[i] < dismin)
							{
								ix1 = i;
								dismin = disparityCost[i];
							}
						}

						dynamicPtr[m * dwd  + n] = ix1;
						
						//Ddynamic[m, n] = ix1;
					}
				}
				tmL.Dispose();
				tmR.Dispose();
			}

			return Ddynamic;
		}
		/// <summary>
		/// 制約付きBlockMatch(Stencil
		/// </summary>
		/// <param name="grayLeft"></param>
		/// <param name="grayRight"></param>
		/// <param name="halfBlockSize"></param>
		/// <param name="stencil"></param>
		/// <param name="srcRange"></param>
		/// <returns></returns>
		public static IplImage KanemotoBlockMatch(IplImage grayLeft, IplImage grayRight, int halfBlockSize, CvMat stencil, int srcRange)
		{
			int NX = grayLeft.Width;
			int NY = grayLeft.Height;
			IplImage Ddynamic = new IplImage(NX, NY, BitDepth.F32, 1);
			int[,] disparityRangeL = new int[NY, NX];
			int[,] disparityRangeH = new int[NY, NX];
			for (int y = 0; y < stencil.Height; y++)
			{
				for (int x = 0; x < stencil.Width; x++)
				{
					disparityRangeL[y, x] = (int)stencil[y, x] - srcRange;
					disparityRangeH[y, x] = (int)stencil[y, x] + srcRange;
					if (disparityRangeL[y, x] < 0) disparityRangeL[y, x] = 0;
					//if(disparityRangeH[y,x]>maxSearchRange) disparityRangeH[y,x]=maxSearchRange;
				}
			}

			unsafe
			{
				//                int disparityRange1 = disparityRange + 1;//０－disparityrange＋１までを探索
				int blockSize = 2 * halfBlockSize + 1;
				int nRowsLeft = NY;

				float finf = float.MaxValue;
				//                float[] disparityCost = new float[disparityRange1];
				IplImage tmR = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				IplImage tmL = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				byte* rightPtr = (byte*)grayRight.ImageData;
				byte* tmRPtr = (byte*)tmR.ImageData;
				byte* tmLPtr = (byte*)tmL.ImageData;
				byte* leftPtr = (byte*)grayLeft.ImageData;
				float* dynamicPtr = (float*)Ddynamic.ImageData;
				int iwd = tmL.WidthStep;
				int rwd = grayRight.WidthStep;
				int dwd = Ddynamic.WidthStep / 4;


				for (int m = 0; m < nRowsLeft; m++)
				{
					int minr = Math.Max(1, m + 1 - halfBlockSize);
					int maxr = Math.Min(nRowsLeft, m + 1 + halfBlockSize);

					for (int n = 0; n < NX; n++)
					{
						int minc = Math.Max(1, n + 1 - halfBlockSize);
						int maxc = Math.Min(NX, n + 1 + halfBlockSize);
						int mind = Math.Max(disparityRangeL[m, n], 1 - minc);
						int maxd = Math.Min(disparityRangeH[m, n], NX - maxc);

						//                    tmL = leftI.GetSlice(minr - 1, maxr - 1, minc + d - 1, maxc + d - 1);
						//                    tmR = rightI.GetSlice(minr - 1, maxr - 1, minc - 1, maxc - 1);
						if (mind < maxd)
						{

							for (int y = 0; y < blockSize * blockSize; y++) tmRPtr[y] = 0;

							for (int y = minr - 1; y <= (maxr - 1); y++)
							{
								for (int x = minc - 1; x <= (maxc - 1); x++)
								{
									tmRPtr[(y - minr + 1) * iwd + (x - minc + 1)] = rightPtr[y * rwd + x];
								}
							}
							int md = maxd - mind + 1;
							float[] cp = new float[md];
							for (int i = 0; i < md; i++) cp[i] = finf;

							for (int d = mind; d <= maxd; d++)
							{
								for (int y = 0; y < blockSize * blockSize; y++) tmLPtr[y] = 0;

								for (int y = minr - 1; y <= (maxr - 1); y++)
								{
									for (int x = minc + d - 1; x <= (maxc + d - 1); x++)
									{
										tmLPtr[(y - minr + 1) * iwd + (x - minc - d + 1)] = leftPtr[y * rwd + x];
									}
								}

								int sum = 0;
								for (int y = 0; y < (blockSize * blockSize); y++)
								{
									sum += (int)Math.Abs(tmLPtr[y] - tmRPtr[y]);
								}
								cp[d - mind] = sum;
							}

							float dismin = finf;
							float ix1 = 0;
							for (int i = 0; i < md; i++)
							{
								if (cp[i] < dismin)
								{
									ix1 = i;
									dismin = cp[i];
								}
							}

							dynamicPtr[m * dwd + n] = ix1 + mind;
						}
						else
						{
							dynamicPtr[m * dwd + n] = mind;
						}

						//Ddynamic[m, n] = ix1;
					}
				}
				tmL.Dispose();
				tmR.Dispose();
			}

			return Ddynamic;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="leftPath"></param>
		/// <param name="rightPath"></param>
		/// <param name="halfBlockSize"></param>
		/// <param name="disparityRange"></param>
		/// <returns></returns>
		public static IplImage KanemotoDPmatch(string leftPath, string rightPath, int halfBlockSize, int disparityRange)
		{
			using (IplImage grayLeft = new IplImage(leftPath, LoadMode.GrayScale))
			using (IplImage grayRight = new IplImage(rightPath, LoadMode.GrayScale))
			{
				return KanemotoBlockMatch(grayLeft, grayRight, halfBlockSize, disparityRange);
			}
		}


		/// <summary>
		/// MATLABのDPマッチング
		/// </summary>
		/// <param name="leftI"></param>
		/// <param name="rightI"></param>
		/// <param name="halfBlockSize"></param>
		/// <param name="disparityRange"></param>
		/// <returns></returns>
		public static IplImage KanemotoDPmatch(IplImage leftI, IplImage rightI, int halfBlockSize, int disparityRange)
		{
			unsafe
			{
				int NX = leftI.Width;
				int NY = leftI.Height;
				int disparityRange1 = disparityRange + 1;
				int blockSize = 2 * halfBlockSize + 1;
				int nRowsLeft = NY;
				byte* LeftPtr = (byte*)leftI.ImageData;
				byte* RightPtr = (byte*)rightI.ImageData;
				//FloatArray Ddynamic = new FloatArray(NY, NX);
				IplImage Ddynamic = new IplImage(NX, NY, BitDepth.F32, 1);
				

				IplImage tmR = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				IplImage tmL = new IplImage(blockSize, blockSize, BitDepth.U8, 1);
				byte* tmRPtr = (byte*)tmR.ImageData;
				byte* tmLPtr = (byte*)tmL.ImageData;
				float* dynamicPtr = (float*)Ddynamic.ImageData;
				int iwd = tmL.WidthStep;
				int rwd = rightI.WidthStep;
				int dwd = Ddynamic.WidthStep / 4;

				float finf = 1000.0f;
				float[,] disparityCost = new float[NX, disparityRange1]; ;
				float disparityPenalty = 0.5f;

				for (int m = 0; m < nRowsLeft; m++)
				{
					for (int j = 1; j < NX; j++)
						for (int i = 0; i < disparityRange1; i++) disparityCost[j, i] = finf;
					int minr = Math.Max(1, m + 1 - halfBlockSize);
					int maxr = Math.Min(nRowsLeft, m + 1 + halfBlockSize);

					for (int n = 0; n < NX; n++)
					{
						int minc = Math.Max(1, n + 1 - halfBlockSize);
						int maxc = Math.Min(NX, n + 1 + halfBlockSize);
						int mind = Math.Max(0, 1 - minc);
						int maxd = Math.Min(disparityRange, NX - maxc);
						for (int y = 0; y < blockSize * blockSize; y++) tmRPtr[y] = 0;
						for (int y = minr - 1; y <= (maxr - 1); y++)
						{
							for (int x = minc - 1; x <= (maxc - 1); x++)
							{
								tmRPtr[(y - minr + 1) * iwd + (x - minc + 1)] = RightPtr[y * rwd + x];
							}
						}
						for (int d = mind; d <= maxd; d++)
						{
							//  tmL = leftI.GetSlice(minr - 1, maxr - 1, minc + d - 1, maxc + d - 1);
							//  tmR = rightI.GetSlice(minr - 1, maxr - 1, minc - 1, maxc - 1);
							for (int y = 0; y < blockSize * blockSize; y++) tmLPtr[y] = 0;
							for (int y = minr - 1; y <= (maxr - 1); y++)
							{
								for (int x = minc + d - 1; x <= (maxc + d - 1); x++)
								{
									tmLPtr[(y - minr + 1) * iwd + (x - minc - d + 1)] = LeftPtr[y * rwd + x];
								}
							}

							int sum = 0;
							for (int y = 0; y < (blockSize * blockSize); y++)
							{
								sum += (int)Math.Abs(tmLPtr[y] - tmRPtr[y]);
							}

							disparityCost[n, d] = (float)sum / 255.0f;

						}
					}

					float[,] optimalIndices = new float[NX, disparityRange1];
					float[] cp = new float[disparityRange1];
					int[] minI = new int[disparityRange1 - 2];
					float[] minV = new float[disparityRange1 - 2];

					for (int i = 0; i < disparityRange1; i++) cp[i] = disparityCost[NX - 1, i];
					//FloatArray cp = disparityCost.GetSlice(NX - 1, NX - 1, 0, disparityCost.size1 - 1);


					for (int j = NX - 2; j > -1; j--)
					{
						float cfinf = (NX - j) * finf;
						float[,] cpc = new float[7, disparityRange1 - 2];
						//cpc.FillValue(cfinf);

						cpc[0, 0] = cpc[0, 1] = cfinf;
						for (int i = 2; i < disparityRange1 - 2; i++) cpc[0, i] = cp[i - 2] + (3 * disparityPenalty);
						cpc[1, 0] = cfinf;
						for (int i = 1; i < disparityRange1 - 2; i++) cpc[1, i] = cp[i - 1] + (2 * disparityPenalty);
						for (int i = 0; i < disparityRange1 - 2; i++) cpc[2, i] = cp[i] + disparityPenalty;
						for (int i = 0; i < disparityRange1 - 2; i++) cpc[3, i] = cp[i + 1];
						for (int i = 0; i < disparityRange1 - 2; i++) cpc[4, i] = cp[i + 2] + disparityPenalty;
						cpc[5, disparityRange1 - 3] = cfinf;
						for (int i = 0; i < disparityRange1 - 3; i++) cpc[5, i] = cp[i + 3] + (2 * disparityPenalty);
						cpc[6, disparityRange1 - 3] = cpc[6, disparityRange1 - 4] = cfinf;
						for (int i = 0; i < disparityRange1 - 4; i++) cpc[6, i] = cp[i + 4] + (3 * disparityPenalty);

						//最小の値とIndexを求め、リストに入れる
						for (int k = 0; k < disparityRange1 - 2; k++)
						{
							int minIndex = 0;
							float minValue = float.MaxValue;
							for (int u = 0; u < 7; u++)
							{
								if (cpc[u, k] < minValue)
								{
									minValue = cpc[u, k];
									minIndex = u;
								}
							}
							minI[k] = minIndex;
							minV[k] = minValue;
						}

						cp[0] = cfinf;
						cp[disparityRange1 - 1] = cfinf;
						for (int k = 0; k < disparityRange1 - 2; k++)
						{
							cp[k + 1] = disparityCost[j, k + 1] + minV[k];
						}
						for (int kk = 1; kk < disparityRange1 - 1; kk++)
						{
							optimalIndices[j, kk] = kk + (minI[kk - 1] - 4) + 2;
						}
					}

					float cpmin = float.MaxValue;
					int ix1 = 0;
					for (int i = 0; i < disparityRange1; i++)
					{
						if (cp[i] < cpmin)
						{
							ix1 = i;
							cpmin = cp[i];
						}
					}

					
					//Ddynamic[m, 0] = ix1;
					dynamicPtr[m * dwd + 0] = ix1;

					//for (int k = 0; k < Ddynamic.size1 - 1; k++)
					for (int k = 0; k < Ddynamic.Width - 1; k++)
					{
						//int kkk = Math.Max(0, Math.Min(disparityRange, (int)Math.Round(Ddynamic[m, k])));
						int kkk = Math.Max(0, Math.Min(disparityRange, (int)Math.Round(dynamicPtr[m * dwd + k])));
						
						//Ddynamic[m, k + 1] = optimalIndices[k, kkk] - 1;
						dynamicPtr[m * dwd + k + 1] = optimalIndices[k, kkk] - 1; ;
					}
				}
				return Ddynamic;
			}
		}


		/// <summary>
		/// OpenCV版ブロックマッチ、、戻り値はU8,1ch
		/// 視差は16倍されている（距離変換後16倍する必要がある)
		/// </summary>
		/// <param name="leftPath"></param>
		/// <param name="rightPath"></param>
		/// <param name="bmsize"></param>
		/// <param name="srcRange"></param>
		/// <returns></returns>
		public static IplImage BlockMatch(string leftPath, string rightPath, int bmsize, int srcRange)
		{
			using(IplImage grayLeft = new IplImage(leftPath, LoadMode.GrayScale))
			using (IplImage grayRight = new IplImage(rightPath, LoadMode.GrayScale))
			{
				return BlockMatch(grayLeft, grayRight, bmsize, srcRange);
			}
		}

		/// <summary>
		/// OpenCV版ブロックマッチ、戻り値はU8,1ch
		/// 視差は16倍されている（距離変換後16倍する必要がある)
		/// </summary>
		/// <param name="grayLeft"></param>
		/// <param name="grayRight"></param>
		/// <param name="bmsize">ブロックマッチングサイズ（2*bmsiz+1が実際のサイズ）</param>
		/// <param name="srcRange">探索範囲の最大値（零から最大値までを探索）</param>
		/// <returns></returns>
		/// 
		///
		public static IplImage BlockMatch(IplImage grayLeft, IplImage grayRight, int bmsize, int srcRange)
		{
			CvStereoBMState stateBM = new CvStereoBMState(StereoBMPreset.Basic, srcRange);
			stateBM.SADWindowSize = bmsize * 2 + 1;
			IplImage dispBM = new IplImage(grayLeft.Size, BitDepth.S16, 1);
			Cv.FindStereoCorrespondenceBM(grayLeft, grayRight, dispBM, stateBM);


			//dst = kaneUtility.IplImageToFloatArray(dispBM); //下記とほぼ同じだが、零の扱いが異なるので、要確認
			IplImage dstBM = new IplImage(grayLeft.Size, BitDepth.U8, 1);
			Cv.ConvertScale(dispBM, dstBM);
			IplImage dst = new IplImage(grayLeft.Size, BitDepth.U8, 1);
			dstBM.Copy(dst);
			dstBM.Dispose();
			dispBM.Dispose();
			return dst;
		}


		
		/// <summary>
		/// OpenCVのDPマッチング関数、戻り値はU8,1ch
		/// 視差は16倍されている（距離変換後16倍する必要がある)
		/// </summary>
		/// <param name="leftFilePath"></param>
		/// <param name="rightFlePath"></param>
		/// <param name="bmsize"></param>
		/// <param name="srcRange"></param>
		/// <returns></returns>
		public static IplImage DPMatch(string leftFilePath, string rightFlePath, int bmsize, int srcRange)
		{
			using (Mat leftMat = new Mat(leftFilePath, LoadMode.GrayScale))
			using (Mat rightMat = new Mat(rightFlePath, LoadMode.GrayScale))
			using (Mat disp = new Mat())
			{
				StereoSGBM sgbm = new StereoSGBM(0, 16, 5, 1, 1, 32, 3, 10, 3, 16, true);
				sgbm.SADWindowSize = 2 * bmsize + 1;
				sgbm.NumberOfDisparities = srcRange;
				sgbm.FindCorrespondence(leftMat, rightMat, disp);
				IplImage dst = new IplImage(leftMat.Cols, leftMat.Rows, BitDepth.U8, 1);
				Cv.Convert(disp.ToCvMat(), dst);
				return dst;
			}
		}



		/// <summary>
		/// OpenCV GraphCut、戻り値はU8,1ch
		///  視差は16倍されている（距離変換後16倍する必要がある)
		/// </summary>
		/// <param name="leftPath"></param>
		/// <param name="rightPath"></param>
		/// <param name="bmsize"></param>
		/// <param name="searchRange"></param>
		/// <returns></returns>
		public static IplImage GraphCut(string leftPath, string rightPath, int bmsize, int searchRange)
		{
			using (IplImage grayLeft = new IplImage(leftPath, LoadMode.GrayScale))
			using (IplImage grayRight = new IplImage(rightPath, LoadMode.GrayScale))
			{
				return GraphCut(grayLeft, grayRight, bmsize, searchRange);
			}
		}

		/// <summary>
		/// OpenCV GraphCut、戻り値はU8,1ch
		///  視差は16倍されている（距離変換後16倍する必要がある)
		/// </summary>
		/// <param name="grayLeft"></param>
		/// <param name="grayRight"></param>
		/// <param name="bmsize"></param>
		/// <param name="searchRange"></param>
		/// <returns></returns>
		public static IplImage GraphCut(IplImage grayLeft, IplImage grayRight, int bmsize, int searchRange)
		{
			IplImage result = new IplImage(grayLeft.Size, BitDepth.U8, 1);
			using (IplImage dispLeft = new IplImage(grayLeft.Size, BitDepth.S16, 1))
			using (IplImage dispRight = new IplImage(grayLeft.Size, BitDepth.S16, 1))
			using (IplImage dstGC = new IplImage(grayLeft.Size, BitDepth.U8, 1))
			{
				using (CvStereoGCState stateGC = new CvStereoGCState(searchRange, 2))
				{
					Cv.FindStereoCorrespondenceGC(grayLeft, grayRight, dispLeft, dispRight, stateGC, false);
					Cv.ConvertScale(dispLeft, dstGC, -16);
					dstGC.Copy(result);
				}
			}
			return result;
		}
	}
}
