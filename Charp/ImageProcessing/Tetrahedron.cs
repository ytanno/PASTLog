using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabImg
{
	/// <summary>
	/// 三角錐のデータクラス
	/// </summary>
	public class Tetrahedron
	{
		/// <summary>
		/// 座標 1
		/// </summary>
		public PVector P1 { get; private set; }

		/// <summary>
		/// 座標 2
		/// </summary>
		public PVector P2 { get; private set; }

		/// <summary>
		/// 座標 3
		/// </summary>
		public PVector P3 { get; private set; }

		/// <summary>
		/// 座標 4
		/// </summary>
		public PVector P4 { get; private set; }


		/// <summary>
		/// 4点を含んだ配列
		/// </summary>
		public PVector[] Vertices { get; set; }

		/// <summary>
		/// 外接円の中心
		/// </summary>
		public PVector O { get; private set; }	

		/// <summary>
		/// 外接円の半径
		/// </summary>
		public float R { get; private set; } 	

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="p1">座標 1</param>
		/// <param name="p2">座標 2</param>
		/// <param name="p3">座標 3</param>
		/// <param name="p4">座標 4</param>
		public Tetrahedron(PVector p1, PVector p2, PVector p3, PVector p4)
		{
			Vertices = new PVector[4];

			P1 = p1;
			Vertices[0] = p1;

			P2 = p2;
			Vertices[1] = p2;

			P3 = p3;
			Vertices[2] = p3;

			P4 = p4;
			Vertices[3] = p4;

			getCenterCircumcircle();
		}



		/// <summary>
		/// 三角錐の四点から外接円の半径と中心を求める
		/// 参考URL　http://www.openprocessing.org/sketch/31295
		/// </summary>
		private void getCenterCircumcircle()
		{
			//関数内で配列の値を直接操作している

			//P1を始点にして平行移動
			double[,] A = new double[,] {
			{P2.X - P1.X, P2.Y-P1.Y, P2.Z-P1.Z},
			{P3.X - P1.X, P3.Y-P1.Y, P3.Z-P1.Z},
			{P4.X - P1.X, P4.Y-P1.Y, P4.Z-P1.Z}
		};
			//未理解
			double[] b = new double[]{
			0.5 * (P2.X*P2.X - P1.X*P1.X + P2.Y*P2.Y - P1.Y*P1.Y + P2.Z*P2.Z - P1.Z*P1.Z),
			0.5 * (P3.X*P3.X - P1.X*P1.X + P3.Y*P3.Y - P1.Y*P1.Y + P3.Z*P3.Z - P1.Z*P1.Z),
			0.5 * (P4.X*P4.X - P1.X*P1.X + P4.Y*P4.Y - P1.Y*P1.Y + P4.Z*P4.Z - P1.Z*P1.Z)
		};

			//外接円の中心
			//方程式の解
			double[] x = new double[3];
			if (gauss(A, b, x) == 0)
			{
				O = null;
				R = -1;
			}
			else
			{
				O = new PVector((float)x[0], (float)x[1], (float)x[2]);
				R = CommonUtility.GetDist(O, P1);
			}
		}

		/// <summary>
		/// ガウス消去法
		/// </summary>
		/// <param name="a">分解する方の配列</param>
		/// <param name="b">解</param>
		/// <param name="x">連立方程式のX部分</param>
		/// <returns></returns>
		private double gauss(double[,] a, double[] b, double[] x)
		{
			int n = a.GetLength(0);
			int[] ip = new int[n];
			double det = lu(a, ip);

			if (det != 0) { solve(a, b, ip, x); }
			return det;
		}

		/// <summary>
		/// 対角上の要素を判定する
		/// </summary>
		/// <param name="a"></param>
		/// <param name="ip"></param>
		/// <returns></returns>
		private double lu(double[,] a, int[] ip)
		{
			int n = a.GetLength(0);
			double[] weight = new double[n];

			for (int k = 0; k < n; k++)
			{
				ip[k] = k;
				double u = 0;

				//行の部分ピボテッィング
				//行中の絶対値の最大を求める
				for (int j = 0; j < n; j++)
				{
					double t = Math.Abs(a[k, j]);
					if (t > u) u = t;
				}
				if (u == 0) return 0;
				weight[k] = 1 / u;
			}


			double det = 1;
			for (int k = 0; k < n; k++)
			{
				double u = -1;
				int m = 0;

				//上三角行列内で絶対値/ weight が最大になる行と値を求める
				for (int i = k; i < n; i++)
				{
					int ii = ip[i];
					double t = Math.Abs(a[ii, k]) * weight[ii];
					if (t > u) { u = t; m = i; }
				}

				int ik = ip[m];

				//行と列数が同じではないとき
				if (m != k)
				{
					//行番号を入れ替える
					ip[m] = ip[k]; ip[k] = ik;
					det = -det;
				}
				u = a[ik, k]; det *= u;
				if (u == 0) return 0;


				//v1 v2 v3
				//0  v4 v5
				//0  0  v6 
				//の形式に直す。
				//0の部分は配列内では0になっていないが
				//計算上では呼び出していないため問題ない

				for (int i = k + 1; i < n; i++)
				{
					int ii = ip[i]; 
					double t = (a[ii, k] /= u);
					for (int j = k + 1; j < n; j++)
					{
						//a[i][k] -a[k][k](a[i][k] / a[k][k])
						//http://www.kata-lab.itc.u-tokyo.ac.jp/OpenLecture/SP20110118.pdf の
						//外積形式ガウス法参考
						a[ii, j] -= t * a[ik, j];
					}
				}
			}
			return det;
		}


		/// <summary>
		/// LU分解の解を解く
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="ip"></param>
		/// <param name="x"></param>
		private void solve(double[,] a, double[] b, int[] ip, double[] x)
		{
			int n = a.GetLength(0);

			//解の初期化 3x3の時
			// x = b1
			// y = b2 - a21 * b1
			// z = b3 - a31 * b1 - a32 * b2
			for (int i = 0; i < n; i++)
			{
				int ii = ip[i]; double t = b[ii];
				for (int j = 0; j < i; j++) t -= a[ii, j] * x[j];
				x[i] = t;
			}

			//行移動がない場合 3x3 の時
			//下記のような方程式の解（x y z)を求めている
			//v1 v2 v3    x     b1 
			//0  v4 v5  * y  =  b2 - a21 * b1
			//0  0  v6    z  =  b3 - a31 * b1 - a32 * b2 
			for (int i = n - 1; i >= 0; i--)
			{
				double t = x[i]; int ii = ip[i];
				for (int j = i + 1; j < n; j++) t -= a[ii, j] * x[j];
				x[i] = t / a[ii, i];
			}
		}
	}
}
