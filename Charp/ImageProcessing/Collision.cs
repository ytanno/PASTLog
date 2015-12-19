using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabImg
{
	/// <summary>
	/// 衝突判定クラス
	/// </summary>
	public static class Collision
	{
		/// <summary>
		/// <para>無限平面と線分p1p2の衝突判定</para>
		/// </summary>
		/// <param name="p1">座標 1</param>
		/// <param name="p2">座標 2</param>
		/// <param name="t">三角形(無限平面)</param>
		/// <returns>交差した時はTrue</returns>
		public static bool JudgeInfinitePlane(PVector p1, PVector p2, Triangle t)
		{

			//参考
			//　http://marupeke296.com/COL_Basic_No3_WhatIsPlaneEquation.html 

			bool collision = false;

			//法線ベクトル
			PVector normal = t.GetNormal();

			//平面上の一点
			PVector p = t.V1;

			//始点と平面のベクトル
			PVector start = new PVector(p1.X - p.X, p1.Y - p.Y, p1.Z - p.Z);

			//終点と平面のベクトル
			PVector end = new PVector(p2.X - p.X, p2.Y - p.Y, p2.Z - p.Z);

			float Sn = start.Dot(normal);
			float En = end.Dot(normal);

			//衝突したら
			if (Sn * En < 0) collision = true;

			return collision;
		}


		/// <summary>
		/// <para>線分同士の交差判定</para>
		/// <para>例外時の検証が必要</para>
		/// </summary>
		/// <param name="start0">線分の始点</param>
		/// <param name="end0">線分の終点</param>
		/// <param name="start1">対象の線分の始点</param>
		/// <param name="end1">対象の線分の終点</param>
		/// <returns>交差した時はTrue</returns>
		public static bool LineToLine(PVector start0, PVector end0, PVector start1, PVector end1)
		{
			PVector dummy;
			return LineToLine(start0, end0, start1, end1, out dummy);
		}



		/// <summary>
		/// <para>線分同士の交差判定</para>
		/// <para>例外時の検証が必要</para>
		/// </summary>
		/// <param name="start0">線分の始点</param>
		/// <param name="end0">線分の終点</param>
		/// <param name="start1">対象の線分の始点</param>
		/// <param name="end1">対象の線分の終点</param>
		/// <param name="collisionPoint">交差した点。交差しないときはNUllが入る</param>
		/// <returns>交差した時はTrue</returns>
		public static bool LineToLine(PVector start0, PVector end0, PVector start1, PVector end1, out PVector collisionPoint)
		{
			//参照
			//http://oshiete.goo.ne.jp/qa/74647.html?ans_count_asc=0

			//アルゴリズムの大本は「実例で学ぶゲーム3D数学」の291p-292p

			collisionPoint = null;

			//線分1
			PVector AB = end0.Sub(start0);

			//線分2
			PVector CD = end1.Sub(start1);

			//線分の距離
			float dstAB = CommonUtility.GetDist(start0, end0);
			float dstCD = CommonUtility.GetDist(start1, end1);

			//線分が十分に長いかの検証
			if (dstAB < 0.001) return false;
			if (dstCD < 0.001) return false;


			//平行かどうかの判断
			PVector heikou = AB.Cross(CD);
			if (Math.Abs(heikou.X) < 0.001 &&
				Math.Abs(heikou.Y) < 0.001 &&
				Math.Abs(heikou.Z) < 0.001)
			{
				return false;
			}

			//始点同士のベクトル
			PVector AC = start1.Sub(start0);

			//単位ベクトル
			PVector ABr = AB.GetNormalization();
			PVector CDr = CD.GetNormalization();

			PVector d1xd2 = ABr.Cross(CDr);

			//|| d1 X d2 ||^2 
			float lengthPow2 = d1xd2.X * d1xd2.X + d1xd2.Y * d1xd2.Y + d1xd2.Z * d1xd2.Z;

			float t1 = AC.Cross(CDr).Dot(d1xd2) / lengthPow2;
			float t2 = AC.Cross(ABr).Dot(d1xd2) / lengthPow2;

			//t1　t2がマイナスだと線分の外にでるので衝突してないことになる
			if (t1 < 0) return false;
			if (t2 < 0) return false;

			PVector p = new PVector(start0.X + ABr.X * t1, start0.Y + ABr.Y * t1, start0.Z + ABr.Z * t1);
			PVector q = new PVector(start1.X + CDr.X * t2, start1.Y + CDr.Y * t2, start1.Z + CDr.Z * t2);

			float Ap = CommonUtility.GetDist(start0, p);
			float Cq = CommonUtility.GetDist(start1, q);

			//Aq　Cpが線分AB　CD上にあるか判定する
			if (dstAB < Ap) return false;
			if (dstCD < Cq) return false;

			float dst = CommonUtility.GetDist(p, q);

			//近さの判定
			if (Math.Abs(dst) < 0.001)
			{
				collisionPoint = new PVector((p.X + q.X) / 2.0f, p.Y + q.Y / 2.0f, (p.Z + q.Z) / 2.0f);
				return true;
			}

			return false;
		}





		/// <summary>
		/// <para>TomasMollerの衝突判定</para>
		/// </summary>
		/// <param name="eyePoint">視点の座標</param>
		/// <param name="point">対象の座標</param>
		/// <param name="triagnle">衝突判定対象の三角形</param>
		/// <returns>交差した時はTrue</returns>
		public static bool TomasMoller(PVector eyePoint, PVector point, Triangle triagnle)
		{
			PVector dummy;
			return TomasMoller(eyePoint, point, triagnle, out dummy);
		}


		/// <summary>
		/// <para>TomasMollerの衝突判定</para>
		/// </summary>
		/// <param name="eyePoint">視点の座標</param>
		/// <param name="point">対象の座標</param>
		/// <param name="triagnle">衝突判定対象の三角形</param>
		/// <param name="collisionPoint">交差点を返す。交差しない場合はNULLが入る</param>
		/// <returns>交差した時はTrue</returns>
		public static bool TomasMoller(PVector eyePoint, PVector point, Triangle triagnle, out PVector collisionPoint)
		{
			collisionPoint = null;

			//方向ベクトル（単位ベクトルに正規化）
			PVector dir = point.Sub(eyePoint);
			dir = dir.GetNormalization();


			PVector e1 = triagnle.V2.Sub(triagnle.V1);
			PVector e2 = triagnle.V3.Sub(triagnle.V1);
			PVector pvec = dir.Cross(e2);
			float det = e1.Dot(pvec);

			float u = 0.0f;
			float v = 0.0f;

			PVector qvec;
			PVector tvec;

			if (det > (1e-3))
			{
				tvec = eyePoint.Sub(triagnle.V1);
				u = tvec.Dot(pvec);
				if (u < 0.0f || u > det) return false;

				qvec = tvec.Cross(e1);
				v = dir.Dot(qvec);
				if (v < 0.0 || u + v > det) return false;
			}

			else if (det < -(1e-3))
			{
				tvec = eyePoint.Sub(triagnle.V1);
				u = tvec.Dot(pvec);
				if (u > 0.0 || u < det) return false;

				qvec = tvec.Cross(e1);
				v = dir.Dot(qvec);
				if (v > 0.0 || u + v < det) return false;
			}

			else { return false; }

			float inv_det = 1.0f / det;
			float t = e2.Dot(qvec);
			t *= inv_det;
			u *= inv_det;
			v *= inv_det;

			float px = dir.X * t + eyePoint.X;
			float py = dir.Y * t + eyePoint.Y;
			float pz = dir.Z * t + eyePoint.Z;
			collisionPoint = new PVector(px, py, pz);

			return true;
		}


	}
}
