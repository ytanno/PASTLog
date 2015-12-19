using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace LabImg
{
	/// <summary>
	/// <para>ドロネー三角分割を求めるクラス </para>
	/// <para>大量の点群を引数にとるとコンストラクタの時点で処理に時間がかかります</para>
	/// </summary>
	public class Delaunay
	{
		/// <summary>
		/// 全点を内包する三角錐の座標データ
		/// </summary>
		public Tetrahedron FirstTetra { private set; get; }

		/// <summary>
		/// ドロネー三角分割で生成された三角錐リスト
		/// </summary>
		public List<Tetrahedron> TetraList{ private set; get;}

		/// <summary>
		/// 三角錐リストから作成された全ての三角形リスト
		/// </summary>
		public List<Triangle> AllTriangleList { private set; get; } 

		/// <summary>
		/// 三角錐リストから作成された外側の三角形リスト
		/// </summary>
		public List<Triangle> OutsideTriangleList { private set; get; } 


		/// <summary>
		/// <para>コンストラクタ</para>
		/// <para>(外接円上に4点以上あるとドロネー三角の劣化が起こるので点の配置は調整してください）</para>
		/// <para>乱数を点に加算減算して位置の調整する等</para>
		/// </summary>
		/// <param name="pVectorList">点のリスト</param>
		public Delaunay(List<PVector> pVectorList)
		{
			//内包する四角錐の座標
			this.FirstTetra = GetFirstTetra(pVectorList);

			//ドロネー分割
			this.TetraList = GetTetraList(pVectorList);

			//三角錐を三角形に直したもの
			this.AllTriangleList = TetraToTriangle(this.TetraList);

			//外部の三角形リスト
			this.OutsideTriangleList = FilterOutSidePoint(this.AllTriangleList);
		}


		/// <summary>
		/// <para>同一の三角形を除去する</para>
		///	<para>内部の三角形を排除することができる</para>
		/// </summary>
		/// <returns>外側の三角形リスト</returns>
		private List<Triangle> FilterOutSidePoint(List<Triangle> allTriangleList)
		{
			List<Triangle> dstList = new List<Triangle>();
			bool[] isSameTriangle = new bool[allTriangleList.Count];
			for (int i = 0; i < allTriangleList.Count - 1; i++)
			{
				for (int j = i + 1; j < allTriangleList.Count; j++)
				{
					if (allTriangleList[i].Equals(allTriangleList[j]))
					{
						isSameTriangle[i] = isSameTriangle[j] = true;
					}
				}
			}

			for (int i = 0; i < isSameTriangle.Length; i++)
			{
				if (!isSameTriangle[i]) dstList.Add(allTriangleList[i]);
			}
			return dstList;
		}




		/// <summary>
		/// 三角錐のリストから外側の三角形のデータだけのリストを返す
		/// </summary>
		/// <returns>三角形のリスト</returns>
		private List<Triangle> TetraToTriangle(List<Tetrahedron> tetraList)
		{
			List<Triangle> triList = new List<Triangle>();

			// 面を求める
			foreach (var tetra in tetraList)
			{
				PVector v1 = tetra.P1;
				PVector v2 = tetra.P2;
				PVector v3 = tetra.P3;
				PVector v4 = tetra.P4;

				Triangle tri1 = new Triangle(v1, v2, v3);
				Triangle tri2 = new Triangle(v1, v3, v4);
				Triangle tri3 = new Triangle(v1, v4, v2);
				Triangle tri4 = new Triangle(v4, v3, v2);

				//面の向きを合わせる
				PVector n;
				n = tri1.GetNormal();
				
				
				if (n.Dot(v1) > n.Dot(v4))　tri1.TurnBack();
				
				n = tri2.GetNormal();
				if (n.Dot(v1) > n.Dot(v2))　tri2.TurnBack();
				
				n = tri3.GetNormal();
				if (n.Dot(v1) > n.Dot(v3))　tri3.TurnBack();
				
				n = tri4.GetNormal();
				if (n.Dot(v2) > n.Dot(v1))　tri4.TurnBack();
				
				triList.Add(tri1);
				triList.Add(tri2);
				triList.Add(tri3);
				triList.Add(tri4);
			}

			return triList;
		}



		/// <summary>
		/// ドロネー三角を計算して、作成した三角錐のリストを返す
		/// </summary>
		/// <param name="pVectorList">全点のリスト</param>
		/// <returns>三角錐リスト</returns>
		private List<Tetrahedron> GetTetraList(List<PVector> pVectorList)
		{
			PVector[] outer = new PVector[] { this.FirstTetra.P1, this.FirstTetra.P2, this.FirstTetra.P3, this.FirstTetra.P4 };

			List<Tetrahedron> tetraList = new List<Tetrahedron>();
			tetraList.Add(this.FirstTetra);

			List<Tetrahedron> tmpTList = new List<Tetrahedron>();
			List<Tetrahedron> newTList = new List<Tetrahedron>();
			List<Tetrahedron> removeTList = new List<Tetrahedron>();

			foreach (var point in pVectorList)
			{
				tmpTList.Clear();
				newTList.Clear();
				removeTList.Clear();

				foreach (var t in tetraList)
				{
					//存在する点が三角錐の外接円に内包される時
					if ((t.O != null) &&
						(t.R > CommonUtility.GetDist(point, t.O)))
					{
						tmpTList.Add(t);
					}
				}

				//存在する点と内包する三角錐の点から新しい三角錐を作る
				foreach (var t1 in tmpTList)
				{
					tetraList.Remove(t1);

					PVector v1 = t1.P1;
					PVector v2 = t1.P2;
					PVector v3 = t1.P3;
					PVector v4 = t1.P4;

					newTList.Add(new Tetrahedron(v1, v2, v3, point));
					newTList.Add(new Tetrahedron(v1, v2, v4, point));
					newTList.Add(new Tetrahedron(v1, v3, v4, point));
					newTList.Add(new Tetrahedron(v2, v3, v4, point));
				}

				//同一の値を検知する
				bool[] isRedundancy = new bool[newTList.Count];
				for (int i = 0; i < newTList.Count - 1; i++)
				{
					for (int j = i + 1; j < newTList.Count; j++)
					{
						if (newTList[i].P1 == newTList[j].P1 &&
							newTList[i].P2 == newTList[j].P2 &&
							newTList[i].P3 == newTList[j].P3 &&
							newTList[i].P4 == newTList[j].P4)
						{
							isRedundancy[i] = isRedundancy[j] = true;
						}
					}
				}
				//同一の値以外のものをリストに追加する
				for (int i = 0; i < newTList.Count; i++)
				{
					if (!isRedundancy[i]) tetraList.Add(newTList[i]);
				}
			}

			//追加した三角錐の座標と最初の三角錐の座標が一致した場合、リストから削除する
			bool isOuter = false;
			for (int i = tetraList.Count - 1; i >= 0; i--)
			{
				isOuter = false;
				foreach (var t4Point in tetraList[i].Vertices)
				{
					foreach (var outerPoint in outer)
					{
						if (t4Point.X == outerPoint.X &&
							t4Point.Y == outerPoint.Y &&
							t4Point.Z == outerPoint.Z)
						{
							isOuter = true;
						}
					}
				}
				if (isOuter)
				{
					tetraList.RemoveAt(i);
				}
			}

			return tetraList;
		}



		/// <summary>
		/// 全てを内包する三角錐の座標を求める
		/// </summary>
		/// <param name="pVectorList">ポイントのXYZのリスト</param>
		/// <returns>内包する三角錐</returns>
		private static Tetrahedron GetFirstTetra(List<PVector> pVectorList)
		{
			float xMin = pVectorList.Min(value => value.X);
			float yMin = pVectorList.Min(value => value.Y);
			float zMin = pVectorList.Min(value => value.Z);

			float xMax = pVectorList.Max(value => value.X);
			float yMax = pVectorList.Max(value => value.Y);
			float zMax = pVectorList.Max(value => value.Z);


			//直方体の座標の差分計算
			float width = xMax - xMin;
			float height = yMax - yMin;
			float depth = zMax - zMin;


			//球の中心座標
			float cX = width / 2 + xMin;
			float cY = height / 2 + yMin;
			float cZ = depth / 2 +  zMin;
			PVector center = new PVector(cX, cY, cZ);

			//半径
			//0.1f はおまけ
			float radius = CommonUtility.GetDist(new PVector(xMax,yMax, zMax), new PVector(xMin, yMin, zMin)) / 2 + 0.1f;

			//三角錐の座標計算
			PVector p1 = new PVector(center.X, center.Y + 3.0f, center.Z);
			PVector p2 = new PVector(center.X + (float)2 * (float)Math.Sqrt(2) * radius, center.Y - radius, center.Z);
			PVector p3 = new PVector(-(float)Math.Sqrt(2) * radius + center.X, -radius + center.Y, (float)Math.Sqrt(6) * radius + center.Z);
			PVector p4 = new PVector(-(float)Math.Sqrt(2) * radius + center.X, -radius + center.Y, -(float)Math.Sqrt(6) * radius + center.Z);

			return  new Tetrahedron(p1, p2, p3, p4);
		}
	}
}
