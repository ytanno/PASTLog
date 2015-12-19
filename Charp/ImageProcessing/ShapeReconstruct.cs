using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ShoNS.Array;
using System.IO;

using OpenCvSharp;

namespace LabImg
{
	/// <summary>
	/// 兼本先生がMatlabで作ったものの変換
	/// </summary>
	public static class ShapeReconstruct
	{

		/// <summary>
		/// <para>対応点からZ座標を求める</para>
		/// <para>画像枚数M 特徴点数N XYZデータ数をPとする</para>
		/// </summary>
		/// <param name="mxyMat">対応点のデータ format[M, N] = 2ch(X,Y)</param>
		/// <param name="f">焦点距離</param>
		/// <param name="ZC">対象の重心の奥行き</param>
		/// <param name="opt">0 = 平行投影(未完成) 1 = 弱透視投影</param>
		/// <param name="Tk">出力用 Tk</param>
		/// <param name="XYZ">出力用 一枚目の画像のXYZ format[P,N] = 1ch </param>
		/// <param name="XYZM">出力用 XYZのミラー</param>
		/// <param name="Rlist">M枚の画像の回転ベクトル format[3,3] = 1ch</param>
		public static void ShapeReconstruction(CvMat mxyMat, float f, float ZC, int opt,
			out CvMat Tk, out CvMat XYZ, out CvMat XYZM, out List<CvMat> Rlist)
		{
			//mxy(2,N,M) M枚の画像で、各画像で　N点の特徴点の（x,y)座標　盾がX軸、水平に右がY軸

			int M = mxyMat.Height;
			int N = mxyMat.Width;

			//中心座標
			CvMat txy = new CvMat(M, 1, MatrixType.F32C2);
			for (int m = 0; m < M; m++)
			{
				//XYの平均をそれぞれ求める
				double centerX = 0;
				double centerY = 0;
				for (int n = 0; n < N; n++)
				{
					centerX += mxyMat.Get2D(m, n).Val0;
					centerY += mxyMat.Get2D(m, n).Val1;
				}
				txy[m] = new CvScalar(centerX / N, centerY / N);
			}

			CvMat baseMat = new CvMat(2 * M, N, MatrixType.F32C1);
			for (int n = 0; n < N; n++)
			{
				for (int m = 0; m < M; m++)
				{
					//SubRectで切り取ったMatは参照渡しになる
					//処理速度は遅い
					CvMat subRect;
					CvRect rect = new CvRect(n, m * 2, 1, 2);
					baseMat.GetSubRect(out subRect, rect);

					subRect[0, 0] = mxyMat.Get2D(m, n).Val0 - txy[m].Val0;
					subRect[1, 0] = mxyMat.Get2D(m, n).Val1 - txy[m].Val1;
					subRect.Dispose();
				}
			}

			CvMat tempW = new CvMat(baseMat.Rows, baseMat.Rows, MatrixType.F32C1);
			CvMat tempU = new CvMat(baseMat.Rows, baseMat.Rows, MatrixType.F32C1);
			CvMat tempV = new CvMat(baseMat.Cols, baseMat.Rows, MatrixType.F32C1);
			baseMat.SVD(tempW, tempU, tempV);

			CvMat U, S, V;
			tempU.GetSubRect(out U, new CvRect(0, 0, 3, tempU.Rows));
			tempV.GetSubRect(out V, new CvRect(0, 0, 3, tempV.Rows));
			tempW.GetSubRect(out S, new CvRect(0, 0, 3, 3));

			float[, , ,] BT = new float[3, 3, 3, 3];


			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					for (int k = 0; k < 3; k++)
					{
						for (int l = 0; l < 3; l++)
						{
							float w = 0;
							for (int m = 0; m < M; m++)
							{
								int m1 = 2 * m;
								int m2 = 2 * m + 1;

								if (opt == 0)
								{
									w = (float)(w + U[m1, i] * U[m1, j] * U[m1, k] * U[m1, l] +
										U[m2, i] * U[m2, j] * U[m2, k] * U[m2, l] +
										0.25f * (U[m1, i] * U[m2, j] + U[m2, i] * U[m1, k] * U[m2, l] +
										U[m2, k] * U[m1, l]));
								}

								else if (opt == 1)
								{
									w = (float)(w + U[m1, i] * U[m1, j] * U[m1, k] * U[m1, l]
										- U[m1, i] * U[m1, j] * U[m2, k] * U[m2, l] - U[m2, i] * U[m2, j] * U[m1, k] * U[m1, l]
										+ U[m2, i] * U[m2, j] * U[m2, k] * U[m2, l]
										+ 0.25f * (U[m1, i] * U[m2, j] * U[m1, k] * U[m2, l] + U[m2, i] * U[m1, j] * U[m1, k] * U[m2, l]
										+ U[m1, i] * U[m2, j] * U[m2, k] * U[m1, l] + U[m2, i] * U[m1, j] * U[m2, k] * U[m1, l]));
								}
							}
							BT[i, j, k, l] = w;
						}
					}
				}
			}

			float r2 = (float)Math.Sqrt(2);
			CvMat B = new CvMat(6, 6, MatrixType.F32C1);
			B.SetZero();

			B[0, 0] = BT[0, 0, 0, 0];
			B[0, 1] = BT[0, 0, 1, 1];
			B[0, 2] = BT[0, 0, 2, 2];
			B[0, 3] = r2 * BT[0, 0, 1, 2];
			B[0, 4] = r2 * BT[0, 0, 2, 0];
			B[0, 5] = r2 * BT[0, 0, 0, 1];

			B[1, 0] = BT[1, 1, 0, 0];
			B[1, 1] = BT[1, 1, 1, 1];
			B[1, 2] = BT[1, 1, 2, 2];
			B[1, 3] = r2 * BT[1, 1, 1, 2];
			B[1, 4] = r2 * BT[1, 1, 2, 0];
			B[1, 5] = r2 * BT[1, 1, 0, 1];

			B[2, 0] = BT[2, 2, 0, 0];
			B[2, 1] = BT[2, 2, 1, 1];
			B[2, 2] = BT[2, 2, 2, 2];
			B[2, 3] = r2 * BT[2, 2, 1, 2];
			B[2, 4] = r2 * BT[2, 2, 2, 0];
			B[2, 5] = r2 * BT[2, 2, 0, 1];

			B[3, 0] = r2 * BT[1, 2, 0, 0];
			B[3, 1] = r2 * BT[1, 2, 1, 1];
			B[3, 2] = r2 * BT[1, 2, 2, 2];
			B[3, 3] = 2 * BT[1, 2, 1, 2];
			B[3, 4] = 2 * BT[1, 2, 2, 0];
			B[3, 5] = 2 * BT[1, 2, 0, 1];

			B[4, 0] = r2 * BT[2, 0, 0, 0];
			B[4, 1] = r2 * BT[2, 0, 1, 1];
			B[4, 2] = r2 * BT[2, 0, 2, 2];
			B[4, 3] = 2 * BT[2, 0, 1, 2];
			B[4, 4] = 2 * BT[2, 0, 2, 0];
			B[4, 5] = 2 * BT[2, 0, 0, 1];

			B[5, 0] = r2 * BT[0, 1, 0, 0];
			B[5, 1] = r2 * BT[0, 1, 1, 1];
			B[5, 2] = r2 * BT[0, 1, 2, 2];
			B[5, 3] = 2 * BT[0, 1, 1, 2];
			B[5, 4] = 2 * BT[0, 1, 2, 0];
			B[5, 5] = 2 * BT[0, 1, 0, 1];

			Tk = new CvMat(3, M, MatrixType.F32C1);
			Tk.SetZero();

			CvMat E = new CvMat(3, 1, MatrixType.F32C1);
			CvMat VV = new CvMat(3, 3, MatrixType.F32C1);

			//要検証
			#region 動作確認をしていないコード
			if (opt == 0)
			{
				CvMat C = new CvMat(6, 1, MatrixType.F32C1);
				C.SetZero();
				C[0, 0] = 1;
				C[1, 0] = 1;
				C[2, 0] = 1;

				CvMat bInv = new CvMat(B.Cols, B.Rows, MatrixType.F32C1);
				B.Inv(bInv);

				CvMat TAU = bInv * C;
				CvMat T = new CvMat(3, 3, MatrixType.F32C1);
				T[0, 0] = TAU[0, 0];
				T[0, 1] = TAU[5, 0] / r2;
				T[0, 2] = TAU[4, 0] / r2;
				T[1, 0] = TAU[5, 0] / r2;
				T[1, 1] = TAU[1, 0];
				T[1, 2] = TAU[3, 0] / r2;
				T[2, 0] = TAU[4, 0] / r2;
				T[2, 1] = TAU[3, 0] / r2;
				T[2, 2] = TAU[2, 0];

				T.EigenVV(VV, E);

				VV = VV.T();

				for (int e = 0; e < 3; e++) if (E[e] < 0) E[e] = 0;

				for (int m = 0; m < M; m++)
				{
					Tk[0, m] = txy[m].Val0;
					Tk[1, m] = txy[m].Val1;
					Tk[2, m] = ZC;
				}

				C.Dispose();
				bInv.Dispose();
				TAU.Dispose();
				T.Dispose();
			}
			#endregion

			if (opt == 1)
			{
				CvMat bVec = new CvMat(B.Rows, B.Cols, MatrixType.F32C1);
				CvMat bValue = new CvMat(B.Rows, 1, MatrixType.F32C1);
				B.EigenVV(bVec, bValue);
				bVec = bVec.T();


				CvMat TAU;
				bVec.GetSubRect(out TAU, new CvRect(bVec.Width - 1, 0, 1, bVec.Height));

				CvMat T = new CvMat(3, 3, MatrixType.F32C1);

				T[0, 0] = TAU[0, 0];
				T[0, 1] = TAU[5, 0] / r2;
				T[0, 2] = TAU[4, 0] / r2;
				T[1, 0] = TAU[5, 0] / r2;
				T[1, 1] = TAU[1, 0];
				T[1, 2] = TAU[3, 0] / r2;
				T[2, 0] = TAU[4, 0] / r2;
				T[2, 1] = TAU[3, 0] / r2;
				T[2, 2] = TAU[2, 0];

				float dd = (float)T.Det();
				if (dd < 0) T = -T;

				T.EigenVV(VV, E);
				VV = VV.T();

				//固有値の大きい順に並び替え（胸像の関係が入れ替わるが、本質的な違いではない）
				//この部分は、論文には記載されていない
				for (int e = 0; e < 3; e++) if (E[e] < 0) E[e] = 0;

				//並進ベクトルの計算
				for (int m = 0; m < M; m++)
				{
					int m1 = 2 * m;
					int m2 = 2 * m + 1;

					CvMat u1, u2;
					U.GetSubRect(out u1, new CvRect(0, m1, U.Width, 1));
					U.GetSubRect(out u2, new CvRect(0, m2, U.Width, 1));

					CvMat w = u1 * T * u1.T() + u2 * T * u2.T();
					Tk[2, m] = f * (float)Math.Sqrt(1 / w[0, 0]); //並進ベクトルのz成分（M枚）
					Tk[0, m] = txy[m].Val0 * Tk[2, m] / f; //並進ベクトルのｘ成分（M枚）
					Tk[1, m] = txy[m].Val1 * Tk[2, m] / f; // 並進ベクトルのy成分（M枚）
				}

				bValue.Dispose();
				bVec.Dispose();
				TAU.Dispose();
				T.Dispose();

			}


			//符号がMatLabの結果と入れ替わり始める
			//UとVVの符号がMatLabと違うため
			CvMat mm = new CvMat(2 * M, 3, MatrixType.F32C1);
			mm.SetZero();
			CvMat mmSubRect, vvSubRect;
			for (int i = 0; i < 3; i++)
			{
				mm.GetSubRect(out mmSubRect, new CvRect(i, 0, 1, mm.Height));
				VV.GetSubRect(out vvSubRect, new CvRect(i, 0, 1, VV.Height));
				((float)Math.Sqrt(E[i]) * U * vvSubRect).Copy(mmSubRect);
			}


			Rlist = new List<CvMat>();
			CvMat w1, w2;
			CvMat w3 = new CvMat(3, 3, MatrixType.F32C1);
			CvMat dummy;
			CvMat tempW2 = new CvMat(w3.Rows, w3.Rows, MatrixType.F32C1);
			CvMat tempU2 = new CvMat(w3.Rows, w3.Rows, MatrixType.F32C1);
			CvMat tempV2 = new CvMat(w3.Cols, w3.Rows, MatrixType.F32C1);
			CvMat temp2 = new CvMat(3, 3, MatrixType.F32C1);
			for (int m = 0; m < M; m++)
			{
				w3.GetSubRect(out w1, new CvRect(0, 0, 1, 3));
				w3.GetSubRect(out w2, new CvRect(1, 0, 1, 3));
				(mm.GetSubRect(out dummy, new CvRect(0, 2 * m, mm.Width, 1)).T()).Copy(w1);
				(mm.GetSubRect(out dummy, new CvRect(0, 2 * m + 1, mm.Width, 1))).T().Copy(w2);

				if (opt == 1) w3 = w3 * Tk[2, m] / f;
				w3.SVD(tempW2, tempU2, tempV2);

				float w4 = (float)(tempV2 * tempU2.T()).Det();
				temp2.Set(0);
				temp2[0, 0] = 1;
				temp2[1, 1] = 1;
				temp2[2, 2] = w4;
				CvMat data = tempU2 * temp2 * tempV2.T();

				//M枚の画像の回転ベクトル
				Rlist.Add(data);
			}


			mm.SetZero();
			for (int m = 0; m < M; m++)
			{
				float w = 0;
				if (opt == 0) w = 1;
				else if (opt == 1) w = f / (float)Tk[2, m];

				CvMat PI = new CvMat(3, 2 * M, MatrixType.F32C1);
				PI.SetZero();
				PI[0, 2 * m] = w;
				PI[1, 2 * m + 1] = w;
				mm = mm + PI.T() * Rlist[m];
			}

			CvMat OM = new CvMat(3, 3, MatrixType.F32C1);
			OM.Set(0);
			OM[0, 0] = -1;
			OM[1, 1] = -1;
			OM[2, 2] = 1;

			CvMat mmInv = new CvMat(mm.Width, mm.Width, MatrixType.F32C1);
			(mm.T() * mm).Inv(mmInv);

			CvMat SA = mmInv * mm.T() * baseMat;
			CvMat SAD = -SA;

			List<CvMat> RDList = new List<CvMat>();

			for (int i = 0; i < M; i++)
			{
				RDList.Add(OM * Rlist[i]);
			}

			CvMat ones = new CvMat(1, N, MatrixType.F32C1);
			ones.Set(1);


			CvMat tkSubRect;
			Tk.GetSubRect(out tkSubRect, new CvRect(0, 0, 1, Tk.Height));

			//Final 3D Position
			XYZ = (ZC / Tk[2, 0]) * (Rlist[0] * SA + Kron(tkSubRect, ones));

			//Mirror Solution
			XYZM = (ZC / Tk[2, 0]) * (RDList[0] * SAD + Kron(tkSubRect, ones));

			#region disposeObject
			tkSubRect.Dispose();
			ones.Dispose();
			SAD.Dispose();
			SA.Dispose();
			mmInv.Dispose();
			OM.Dispose();
			w3.Dispose();
			tempW2.Dispose();
			tempU2.Dispose();
			tempV2.Dispose();
			temp2.Dispose();
			E.Dispose();
			VV.Dispose();
			U.Dispose();
			V.Dispose();
			S.Dispose();
			tempU.Dispose();
			tempV.Dispose();
			tempW.Dispose();
			baseMat.Dispose();
			txy.Dispose();
			#endregion
		}


		/// <summary>
		/// F行列を求める(要検証)
		/// </summary>
		/// <param name="T1">移動行列1</param>
		/// <param name="R1">回転行列1</param>
		/// <param name="T2">移動行列2</param>
		/// <param name="R2">回転行列2</param>
		/// <param name="A">カメラ行列</param>
		/// <param name="F">出力用F行列</param>
		/// <param name="s">出力用F行列の特異値分解のS</param>
		public static void FundamentalMatrix(CvMat T1, CvMat R1,
			CvMat T2, CvMat R2, CvMat A, out CvMat F, out CvMat s)
		{
			CvMat Tx1 = new CvMat(3, 3, MatrixType.F32C1);
			Tx1.SetZero();

			CvMat Tx2 = new CvMat(3, 3, MatrixType.F32C1);
			Tx2.SetZero();

			Tx1[0, 1] = -T1[2, 0];
			Tx1[0, 2] = T1[1, 0];
			Tx1[1, 0] = T1[2, 0];
			Tx1[1, 2] = -T1[0, 0];
			Tx1[2, 0] = -T1[1, 0];
			Tx1[2, 1] = T1[0, 0];

			Tx2[0, 1] = -T2[2, 0];
			Tx2[0, 2] = T2[1, 0];
			Tx2[1, 0] = T2[2, 0];
			Tx2[1, 2] = -T2[0, 0];
			Tx2[2, 0] = -T2[1, 0];
			Tx2[2, 1] = T2[0, 0];

			CvMat Tx = Tx1 - Tx2;
			CvMat Rx = R2 * R1.T();

			CvMat AInv = new CvMat(A.Rows, A.Cols, A.ElemType);
			AInv.SetZero();
			A.Inv(AInv);

			F = new CvMat(3, 3, MatrixType.F32C1);
			F.SetZero();

			F = AInv.T() * Tx * Rx * AInv.T();

			s = new CvMat(F.Rows, F.Cols, MatrixType.F32C1);
			CvMat U = new CvMat(F.Rows, F.Rows, MatrixType.F32C1);
			CvMat V = new CvMat(F.Cols, F.Cols, MatrixType.F32C1);
			F.SVD(s, U, V);

			U.Dispose();
			V.Dispose();
			AInv.Dispose();
			Rx.Dispose();
			Tx.Dispose();
			Tx1.Dispose();
			Tx2.Dispose();
		}



		/// <summary>
		/// Kronecker テンソル積
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <returns></returns>
		private static CvMat Kron(CvMat X, CvMat Y)
		{
			CvMat result = new CvMat(X.Height * Y.Height, X.Width * Y.Width, MatrixType.F32C1);

			for (int x1 = 0; x1 < X.Height; x1++)
			{
				for (int x2 = 0; x2 < X.Width; x2++)
				{
					for (int y1 = 0; y1 < Y.Height; y1++)
					{
						for (int y2 = 0; y2 < Y.Width; y2++)
						{
							result[(x1 + 1) * (y1 + 1) - 1, (x2 + 1) * (y2 + 1) - 1]
								= X[x1, x2] * Y[y1, y2];
						}
					}
				}
			}
			return result;
		}


		/// <summary>
		/// 距離を求める
		/// </summary>
		/// <param name="mxyList">xyデータがN個あるリストがM個あるリスト</param>
		/// <param name="f">焦点距離</param>
		/// <param name="ZC">対象の重心の奥行き</param>
		/// <param name="opt">0 = 平行投影(未完成) 1 = 弱透視投影</param>
		/// <param name="M">画像の枚数</param>
		/// <param name="N">画像1枚ごとのXYのデータ数</param>
		/// <param name="XYZ">出力用XYZ</param>
		/// <param name="XYZM">出力用XYZM</param>
		/// <param name="Rlist">出力用M個のR座標リスト</param>
		/// <param name="Tk">出力用Tk</param>
		public static void ShapeReconstruction(List<List<FloatArray>> mxyList,
			float f, float ZC, int opt, int M, int N,
			out FloatArray XYZ, out FloatArray XYZM, 
			out List<FloatArray> Rlist, out FloatArray Tk)
		{
			//mxy(2,N,M) M枚の画像で、各画像で　N点の特徴点の（x,y)座標　盾がX軸、水平に右がY軸

			//中心座標
			List<FloatArray> txy = new List<FloatArray>();

			for (int i = 0; i < mxyList.Count; i++)
			{
				FloatArray center = new FloatArray(2, 1);

				//XYの平均をそれぞれ求める
				foreach (var mxy in mxyList[i])
				{
					center[0, 0] += mxy[0, 0];
					center[1, 0] += mxy[1, 0];
				}
				center[0, 0] /= mxyList[i].Count;
				center[1, 0] /= mxyList[i].Count;
				txy.Add(center);
			}



			FloatArray W = new FloatArray(2 * M, N);
			for (int j = 0; j < N; j++)
			{
				FloatArray a = new FloatArray(2 * M, 1);
				for (int i = 0; i < M; i++)
				{
					a.SetSlice(mxyList[i][j] - txy[i], i * 2, (i + 1) * 2 - 1, 0, 0);
				}
				W.SetSlice(a, 0, W.size0 - 1, j, j);
			}

			//デバック用
			//Wは大体はあっていた
			//SaveCSV(W);


			//符号が一致していないものがある
			SVDFloat svd = new SVDFloat(W);
			FloatArray U = svd.U.GetSlice(0, svd.U.size0 - 1, 0, 2);
			FloatArray S = svd.D.GetSlice(0, 2, 0, 2);
			FloatArray V = svd.V.GetSlice(0, svd.V.size0 - 1, 0, 2);

			//検証用
			//FloatArray r = svd.U * svd.D * svd.V.T;

			FloatArray temp = svd.U * svd.D * svd.V.T - U * S * V.T;
			float err = temp.Norm();

			float[, , ,] BT = new float[3, 3, 3, 3];


			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					for (int k = 0; k < 3; k++)
					{
						for (int l = 0; l < 3; l++)
						{
							float w = 0;
							for (int m = 0; m < M; m++)
							{
								int m1 = 2 * m;
								int m2 = 2 * m + 1;

								if (opt == 0)
								{
									w = w + U[m1, i] * U[m1, j] * U[m1, k] * U[m1, l] +
										U[m2, i] * U[m2, j] * U[m2, k] * U[m2, l] +
										0.25f * (U[m1, i] * U[m2, j] + U[m2, i] * U[m1, k] * U[m2, l] +
										U[m2, k] * U[m1, l]);
								}

								else if (opt == 1)
								{
									w = w + U[m1, i] * U[m1, j] * U[m1, k] * U[m1, l]
										- U[m1, i] * U[m1, j] * U[m2, k] * U[m2, l] - U[m2, i] * U[m2, j] * U[m1, k] * U[m1, l]
										+ U[m2, i] * U[m2, j] * U[m2, k] * U[m2, l]
										+ 0.25f * (U[m1, i] * U[m2, j] * U[m1, k] * U[m2, l] + U[m2, i] * U[m1, j] * U[m1, k] * U[m2, l]
										+ U[m1, i] * U[m2, j] * U[m2, k] * U[m1, l] + U[m2, i] * U[m1, j] * U[m2, k] * U[m1, l]);
								}
							}
							BT[i, j, k, l] = w;
						}
					}
				}
			}

			float r2 = (float)Math.Sqrt(2);

			FloatArray B = new FloatArray(6, 6);
			B[0, 0] = BT[0, 0, 0, 0];
			B[0, 1] = BT[0, 0, 1, 1];
			B[0, 2] = BT[0, 0, 2, 2];
			B[0, 3] = r2 * BT[0, 0, 1, 2];
			B[0, 4] = r2 * BT[0, 0, 2, 0];
			B[0, 5] = r2 * BT[0, 0, 0, 1];

			B[1, 0] = BT[1, 1, 0, 0];
			B[1, 1] = BT[1, 1, 1, 1];
			B[1, 2] = BT[1, 1, 2, 2];
			B[1, 3] = r2 * BT[1, 1, 1, 2];
			B[1, 4] = r2 * BT[1, 1, 2, 0];
			B[1, 5] = r2 * BT[1, 1, 0, 1];

			B[2, 0] = BT[2, 2, 0, 0];
			B[2, 1] = BT[2, 2, 1, 1];
			B[2, 2] = BT[2, 2, 2, 2];
			B[2, 3] = r2 * BT[2, 2, 1, 2];
			B[2, 4] = r2 * BT[2, 2, 2, 0];
			B[2, 5] = r2 * BT[2, 2, 0, 1];

			B[3, 0] = r2 * BT[1, 2, 0, 0];
			B[3, 1] = r2 * BT[1, 2, 1, 1];
			B[3, 2] = r2 * BT[1, 2, 2, 2];
			B[3, 3] = 2 * BT[1, 2, 1, 2];
			B[3, 4] = 2 * BT[1, 2, 2, 0];
			B[3, 5] = 2 * BT[1, 2, 0, 1];

			B[4, 0] = r2 * BT[2, 0, 0, 0];
			B[4, 1] = r2 * BT[2, 0, 1, 1];
			B[4, 2] = r2 * BT[2, 0, 2, 2];
			B[4, 3] = 2 * BT[2, 0, 1, 2];
			B[4, 4] = 2 * BT[2, 0, 2, 0];
			B[4, 5] = 2 * BT[2, 0, 0, 1];

			B[5, 0] = r2 * BT[0, 1, 0, 0];
			B[5, 1] = r2 * BT[0, 1, 1, 1];
			B[5, 2] = r2 * BT[0, 1, 2, 2];
			B[5, 3] = 2 * BT[0, 1, 1, 2];
			B[5, 4] = 2 * BT[0, 1, 2, 0];
			B[5, 5] = 2 * BT[0, 1, 0, 1];


			Tk = new FloatArray(3, M);
			FloatArray E = null;
			FloatArray VV = null;


			if (opt == 0)
			{
				FloatArray C = new FloatArray(6, 1);
				C[0, 0] = 1;
				C[1, 0] = 1;
				C[2, 0] = 1;

				#region 未検証区間
				//Invの結果が著しくおかしい
				FloatArray test = B.Inv();

				///////////////////////////////////////////
				//Invの値がおかしいため、未検証
				////////////////////////////////////////////////////
				FloatArray TAU = B.Inv() * C;

				FloatArray T = new FloatArray(3, 3);
				T[0, 0] = TAU[0, 0];
				T[0, 1] = TAU[5, 0] / r2;
				T[0, 2] = TAU[4, 0] / r2;
				T[1, 0] = TAU[5, 0] / r2;
				T[1, 1] = TAU[1, 0];
				T[1, 2] = TAU[3, 0] / r2;
				T[2, 0] = TAU[4, 0] / r2;
				T[2, 1] = TAU[3, 0] / r2;
				T[2, 2] = TAU[2, 0];


				EigenFloat eigT = new EigenFloat(T);
				VV = (FloatArray)eigT.V;
				E = (FloatArray)eigT.D;

				for (int i = 0; i < 3; i++)
				{
					if (E[i] < 0) E[i] = 0;
				}
				//固有値の大きい順に並び替え（胸像の関係が入れ替わるが、本質的な違いではない）
				//この部分は、論文には記載されていない

				IntArray I;
				FloatArray ee = E.CopyShallow();
				FloatArray eee = ee.SortDescIndex(0, out I);
				VV = ChangeCol(VV, I);

				for (int i = 0; i < M; i++)
				{
					//PC(2*(i-1)+1,1);%並進ベクトルのｘ成分（M枚）
					Tk[0, i] = txy[0][i, 0];

					//PC(2*(i-1)+2,1);%並進ベクトルのy成分（M枚）
					Tk[1, i] = txy[1][i, 0];

					//並進ベクトルのz成分（M枚）
					Tk[2, i] = ZC;
				}
				#endregion
			}

			else if (opt == 1)
			{
				//Matlabのものと一致しない
				EigenFloat eigB = new EigenFloat(B);
				FloatArray VVb = (FloatArray)eigB.V;
				FloatArray Eb = (FloatArray)eigB.D;
				FloatArray ee = Eb.CopyShallow();

				IntArray I;
				FloatArray eee = ee.SortDescIndex(0, out I);
				E = eee.CopyShallow();

				VVb = ChangeCol(VVb, I);

				//4番目の符号以外大体あってる
				//最少固有値に対応する固有ベクトル（λ=0を選択）
				FloatArray TAU = VVb.GetSlice(0, VVb.size0 - 1, VVb.size1 - 1, VVb.size1 - 1);


				FloatArray T = new FloatArray(3, 3);
				T[0, 0] = TAU[0, 0];
				T[0, 1] = TAU[5, 0] / r2;
				T[0, 2] = TAU[4, 0] / r2;
				T[1, 0] = TAU[5, 0] / r2;
				T[1, 1] = TAU[1, 0];
				T[1, 2] = TAU[3, 0] / r2;
				T[2, 0] = TAU[4, 0] / r2;
				T[2, 1] = TAU[3, 0] / r2;
				T[2, 2] = TAU[2, 0];

				float dd = T.Det();
				if (dd < 0) T = -T;

				EigenFloat eigT = new EigenFloat(T);
				VV = (FloatArray)eigT.V;
				E = (FloatArray)eigT.D;

				for (int i = 0; i < 3; i++)
				{
					if (E[i] < 0) E[i] = 0;
				}

				//固有値の大きい順に並び替え（鏡像の関係が入れ替わるが、本質的な違いではない）
				//この部分は、論文には記載されていない
				ee = E.CopyShallow();
				eee = ee.SortDescIndex(0, out I);
				E = eee.CopyShallow();
				VV = ChangeCol(VV, I);

				//並進ベクトルの計算
				for (int m = 0; m < M; m++)
				{
					int m1 = 2 * m;
					int m2 = 2 * m + 1;

					FloatArray u1 = U.GetSlice(m1, m1, 0, U.size1 - 1);
					FloatArray u2 = U.GetSlice(m2, m2, 0, U.size1 - 1);
					FloatArray w = u1 * T * u1.T + u2 * T * u2.T;
					Tk[2, m] = f * (float)Math.Sqrt(1 / w[0, 0]);
					Tk[0, m] = txy[m][0, 0] * Tk[2, m] / f; //並進ベクトルのｘ成分（M枚）
					Tk[1, m] = txy[m][1, 0] * Tk[2, m] / f; // 並進ベクトルのy成分（M枚）
				}
			}


			//回転行列の計算
			//mmの符号以外は大体あっている
			FloatArray mm = new FloatArray(2 * M, 3);

			for (int i = 0; i < 3; i++)
			{
				FloatArray temp2 = (float)Math.Sqrt(E[i]) * U *
					VV.GetSlice(0, VV.size0 - 1, i, i);

				mm.SetSlice(temp2, 0, mm.size0 - 1, i, i);
			}


			Rlist = new List<FloatArray>();

			for (int m = 0; m < M; m++)
			{
				FloatArray w1 = mm.GetSlice(2 * m, 2 * m, 0, mm.size1 - 1).T;
				FloatArray w2 = mm.GetSlice(2 * m + 1, 2 * m + 1, 0, mm.size1 - 1).T;

				FloatArray w3 = new FloatArray(3, 3);
				if (opt == 0)
				{
					w3.SetSlice(w1, 0, 2, 0, 0);
					w3.SetSlice(w2, 0, 2, 1, 1);
				}
				else if (opt == 1)
				{
					w3.SetSlice(w1, 0, 2, 0, 0);
					w3.SetSlice(w2, 0, 2, 1, 1);
					w3 = w3 * Tk[2, m] / f;
				}

				SVDFloat svd2 = new SVDFloat(w3);
				float w4 = (svd2.V * svd2.U.T).Det();

				FloatArray temp2 = new FloatArray(1, 3);
				temp2.FillValue(1);
				temp2[0, 2] = w4;

				//M枚の画像の回転ベクトル
				Rlist.Add(svd2.U * ArrayUtils.Diag(temp2) * svd2.V.T);
			}


			mm = new FloatArray(2 * M, 3);
			for (int i = 0; i < M; i++)
			{
				float w = 0;
				if (opt == 0) w = 1;
				else if (opt == 1) w = f / Tk[2, i];

				FloatArray PI = new FloatArray(3, 2 * M);
				PI[0, 2 * i] = w;
				PI[1, 2 * i + 1] = w;
				mm = mm + PI.T * Rlist[i];
			}

			FloatArray temp3 = new FloatArray(1, 3);
			temp3.FillValue(-1);
			temp3[0, 2] = 1;
			FloatArray OM = ArrayUtils.Diag(temp3);
			FloatArray SA = (mm.T * mm).Inv() * mm.T * W;
			FloatArray SAD = -SA;

			List<FloatArray> RDList = new List<FloatArray>();

			for (int i = 0; i < M; i++)
			{
				RDList.Add(OM * Rlist[i]);
			}

			FloatArray ones = new FloatArray(1, N);
			ones.FillValue(1);

			//Zの値が左右逆？
			//Final 3D Position
			XYZ = (ZC / Tk[2, 0]) * (Rlist[0] * SA + Kron(Tk.GetSlice(0, Tk.size0 - 1, 0, 0), ones));

			//Mirror Solution
			XYZM = (ZC / Tk[2, 0]) * (RDList[0] * SAD + Kron(Tk.GetSlice(0, Tk.size0 - 1, 0, 0), ones));
		}

		/// <summary>
		/// 配列をCSV形式でデスクトップに保存
		/// デバック用
		/// </summary>
		/// <param name="saveArray">保存したい配列</param>
		private static void SaveCSV(FloatArray saveArray)
		{
			DateTime dt = DateTime.Now;
			using (StreamWriter sw = new StreamWriter(
				Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
				+ @"\" + dt.ToString("HHmmss") + ".csv"))
			{
				for (int i = 0; i < saveArray.size0; i++)
				{
					string saveLine = "";
					for (int j = 0; j < saveArray.size1; j++)
					{
						saveLine += saveArray[i, j] + ",";
					}
					saveLine = saveLine.Remove(saveLine.Length - 1);
					sw.WriteLine(saveLine);
				}
			}

		}



		/// <summary>
		/// 列の入れ替え
		/// </summary>
		/// <param name="src">入れ替える配列</param>
		/// <param name="sortArray">入れ替える順番が入った配列</param>
		/// <returns>入れ替え終わった配列</returns>
		private static FloatArray ChangeCol(FloatArray src, IntArray sortArray)
		{
			FloatArray dst = new FloatArray(src.size0, src.size1);

			for (int i = 0; i < sortArray.Length; i++)
			{
				FloatArray temp = src.GetSlice(0, src.size0 - 1,
												sortArray[i], sortArray[i]);

				dst.SetSlice(temp, 0, dst.size0 - 1, i, i);
			}

			return dst;
		}


		/// <summary>
		/// Kronecker テンソル積
		/// </summary>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <returns></returns>
		private static FloatArray Kron(FloatArray X, FloatArray Y)
		{
			FloatArray result = new FloatArray(X.size0 * Y.size0, X.size1 * Y.size1);

			for (int x1 = 0; x1 < X.size0; x1++)
			{
				for (int x2 = 0; x2 < X.size1; x2++)
				{
					for (int y1 = 0; y1 < Y.size0; y1++)
					{
						for (int y2 = 0; y2 < Y.size1; y2++)
						{
							result[(x1 + 1) * (y1 + 1) - 1, (x2 + 1) * (y2 + 1) - 1]
								= X[x1, x2] * Y[y1, y2];
						}
					}
				}
			}
			return result;
		}


		/// <summary>
		/// F行列を求める
		/// </summary>
		/// <param name="T1">移動行列1</param>
		/// <param name="R1">回転行列1</param>
		/// <param name="T2">移動行列2</param>
		/// <param name="R2">回転行列2</param>
		/// <param name="A">カメラ行列</param>
		/// <param name="F">出力用F行列</param>
		/// <param name="s">出力用F行列の特異値分解のS</param>
		public static void FundamentalMatrix(FloatArray T1, FloatArray R1,
			FloatArray T2, FloatArray R2, FloatArray A, out FloatArray F, out FloatArray s)
		{
			FloatArray Tx1 = new FloatArray(3, 3);
			FloatArray Tx2 = new FloatArray(3, 3);

			Tx1[0, 1] = -T1[2, 0];
			Tx1[0, 2] = T1[1, 0];
			Tx1[1, 0] = T1[2, 0];
			Tx1[1, 2] = -T1[0, 0];
			Tx1[2, 0] = -T1[1, 0];
			Tx1[2, 1] = T1[0, 0];

			Tx2[0, 1] = -T2[2, 0];
			Tx2[0, 2] = T2[1, 0];
			Tx2[1, 0] = T2[2, 0];
			Tx2[1, 2] = -T2[0, 0];
			Tx2[2, 0] = -T2[1, 0];
			Tx2[2, 1] = T2[0, 0];

			FloatArray Tx = Tx1 - Tx2;
			FloatArray Rx = R2 * R1.T;

			F = A.Inv().T * Tx * Rx * A.Inv();
			SVDFloat svd = new SVDFloat(F);

			s = ArrayUtils.Diag(svd.D);
		}
	}
}
