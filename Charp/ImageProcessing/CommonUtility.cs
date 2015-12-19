using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShoNS.Array;
using System.Threading.Tasks;
using System.Threading;
using OpenCvSharp;
using System.Drawing;
using System.Windows.Forms;


namespace LabImg
{
	/// <summary>
	/// よく使う関数群をまとめたクラス
	/// </summary>
	public static class CommonUtility
	{

		/// <summary>
		///     指定した精度の数値に切り捨てします。</summary>
		/// <param name="dValue">
		///     丸め対象の倍精度浮動小数点数。</param>
		/// <param name="iDigits">
		///     戻り値の有効桁数の精度。</param>
		/// <returns>
		///     iDigits に等しい精度の数値に切り捨てられた数値。</returns>
		public static double ToRoundDown(double dValue, int iDigits)
		{
			double dCoef = System.Math.Pow(10, iDigits);

			return dValue > 0 ? System.Math.Floor(dValue * dCoef) / dCoef :
								System.Math.Ceiling(dValue * dCoef) / dCoef;
		}


		/// <summary>
		/// BitmapをPictureBox内に収める関数
		/// </summary>
		/// <param name="src">bitmap</param>
		/// <param name="renderBox">描写するPictureBox</param>
		public static void FillPicBox(Bitmap src, PictureBox renderBox)
		{
			using (src)
			{
				if (renderBox.Image != null) renderBox.Image.Dispose();
				var bmp2 = new Bitmap(renderBox.Width, renderBox.Height);
				using (var g = Graphics.FromImage(bmp2))
				{
					if (renderBox.Image != null) renderBox.Image.Dispose();

					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
					g.DrawImage(src, new Rectangle(Point.Empty, bmp2.Size));
					renderBox.Image = bmp2;
				}
			}
		}


		/// <summary>
		/// 2点間の距離を求める
		/// </summary>
		/// <param name="p1">座標　1</param>
		/// <param name="p2">座標　2</param>
		/// <returns>2点間の距離</returns>
		public static float GetDist(PVector p1, PVector p2)
		{
			float width = p1.X - p2.X;
			float height = p1.Y - p2.Y;
			float depth = p1.Z - p2.Z;

			float dist = (float)(Math.Sqrt(Math.Pow(width, 2) +
												Math.Pow(height, 2) +
												Math.Pow(depth, 2)));

			return dist;
		}
		//-----　General Library for Gamma-camera image processing Prepared by Kanenmoto(2013-3-11
		/// <summary>
		/// 未完成の２次元メディアンフィルタ
		/// </summary>
		/// <param name="Din"></param>
		/// <param name="Dout"></param>
		/// <param name="M"></param>
		public static void medianFilter(DoubleArray Din, DoubleArray Dout, int M)
		{
			int ni = Din.size0;
			int nj = Din.size1;

			for (int i = 0; i < ni; i++)
			{
				for (int j = 0; j < nj; j++)
				{
					int ii1 = i - M;
					int ii2 = i + M;
					int jj1 = j - M;
					int jj2 = j + M;
					if (ii1 < 0) ii1 = 0;
					if (ii2 >= ni) ii2 = ni - 1;
					if (jj1 < 0) jj1 = 0;
					if (jj2 >= nj) jj2 = nj - 1;
					DoubleArray D = Din.GetSlice(ii1, ii2, jj1, jj2);
					double Dm = D.Median();
					Dout[i, j] = Dm;
				}
			}
		}
		/// <summary>
		/// 未完成
		/// </summary>
		/// <param name="Din"></param>
		/// <param name="Dout"></param>
		/// <param name="M"></param>
		public static void meanFilter(DoubleArray Din, DoubleArray Dout, int M)
		{
			int ni = Din.size0;
			int nj = Din.size1;
			for (int i = 0; i < ni; i++)
			{
				for (int j = 0; j < nj; j++)
				{
					int ii1 = i - M;
					int ii2 = i + M;
					int jj1 = j - M;
					int jj2 = j + M;
					if (ii1 < 0) ii1 = 0;
					if (ii2 >= ni) ii2 = ni - 1;
					if (jj1 < 0) jj1 = 0;
					if (jj2 >= nj) jj2 = nj - 1;
					DoubleArray D = Din.GetSlice(ii1, ii2, jj1, jj2);
					double Dm = D.Mean();
					Dout[i, j] = Dm;
				}
			}
		}
		/// <summary>
		/// 多角形の面積計算
		/// </summary>
		/// <param name="S">s(i,j)：i=1-3；３次元空間の座標、j=0-np-1　頂点の数</param>
		/// <returns>戻り値は面積</returns>
		public static double PolygonArea(DoubleArray S)
		{
			int np = S.size1;
			double Area = 0;

			for (int i = 2; i < np; i++)
			{
				double s12 = Math.Sqrt(Math.Pow(S[0, 0] - S[0, i - 1], 2) + Math.Pow(S[1, 0] - S[1, i - 1], 2) + Math.Pow(S[2, 0] - S[2, i - 1], 2));
				double s23 = Math.Sqrt(Math.Pow(S[0, i - 1] - S[0, i], 2) + Math.Pow(S[1, i - 1] - S[1, i], 2) + Math.Pow(S[2, i - 1] - S[2, i], 2));
				double s13 = Math.Sqrt(Math.Pow(S[0, 0] - S[0, i], 2) + Math.Pow(S[1, 0] - S[1, i], 2) + Math.Pow(S[2, 0] - S[2, i], 2));
				double ss = (s12 + s23 + s13) / 2;
				Area = Area + Math.Sqrt(ss * (ss - s12) * (ss - s23) * (ss - s13));
			}
			return Area;
		}

	}
}
