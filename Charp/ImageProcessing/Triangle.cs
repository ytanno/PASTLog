using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabImg
{
	/// <summary>
	/// 三角形のデータクラス
	/// </summary>
	public class Triangle
	{
		/// <summary>
		/// 三角形の座標 1
		/// </summary>
		public PVector V1 { get; set; }

		/// <summary>
		/// 三角形の座標 2
		/// </summary>
		public PVector V2 { get; set; }

		/// <summary>
		/// 三角形の座標 3
		/// </summary>
		public PVector V3 { get; set; }

		/// <summary>
		/// <para>三角形の座標データが入った配列</para>
		/// <para>データ数は三つ</para>
		/// </summary>
		public PVector[] Vertics { get; set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="v1">三角形の座標 1</param>
		/// <param name="v2">三角形の座標 2</param>
		/// <param name="v3">三角形の座標 3</param>
		public Triangle(PVector v1, PVector v2, PVector v3)
		{
			this.V1 = v1;
			this.V2 = v2;
			this.V3 = v3;

			Vertics = new PVector[3];
			Vertics[0] = v1;
			Vertics[1] = v2;
			Vertics[2] = v3;
		}

		/// <summary>
		/// 法線を求める
		/// 頂点は左回りの順であるとする
		/// </summary>
		/// <returns>法線ベクトル</returns>
		public PVector GetNormal()
		{
			PVector edge1 = new PVector(V2.X - V1.X, V2.Y - V1.Y, V2.Z - V1.Z);
			PVector edge2 = new PVector(V3.X - V1.X, V3.Y - V1.Y, V3.Z - V1.Z);

			// クロス積
			PVector normal = edge1.Cross(edge2);

			//正規化
			return normal.GetNormalization();
		}

		/// <summary>
		/// 面を裏返す（頂点の順序を逆に）
		/// </summary>
		public void TurnBack()
		{
			PVector tmp = this.V3;
			this.V3 = this.V1;
			this.V1 = tmp;

			this.Vertics[0] = this.V1;
			this.Vertics[2] = this.V3;
		}

		/// <summary>
		/// 自身と引数の比較
		/// </summary>
		/// <param name="src">比べたい三角形</param>
		/// <returns>true or false</returns>
		public bool Equals(Triangle src)
		{
			foreach (var my in this.Vertics)
			{
				bool match = false;
				foreach (var t in src.Vertics)
				{
					if (my.Equals(t)) match = true;
				}
				if (!match) return false;
			}
			return true;
		}

		/// <summary>
		/// 自身の面積を求める
		/// </summary>
		/// <returns>面積</returns>
		public float GetArea()
		{
			PVector AB = this.V2.Sub(this.V1);
			PVector AC = this.V3.Sub(this.V1);
			PVector cross = AB.Cross(AC);
			double result = Math.Sqrt(cross.X * cross.X + cross.Y * cross.Y + cross.Z * cross.Z) / 2.0;
			return (float)result;
		}

	}
}
