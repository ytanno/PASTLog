using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabImg
{
	/// <summary>
	/// 3次元データのクラス
	/// </summary>
	public class PVector
	{
		/// <summary>
		/// X座標
		/// </summary>
		public float X { get; set; }

		/// <summary>
		/// Y座標
		/// </summary>
		public float Y { get; set; }

		/// <summary>
		/// Z座標
		/// </summary>
		public float Z { get; set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="x">X座標</param>
		/// <param name="y">Y座標</param>
		/// <param name="z">Z座標</param>
		public PVector(float x, float y, float z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		/// <summary>
		/// 自身の3点と引数の3点の外積を返す
		/// </summary>
		/// <param name="src">公式のbの方</param>
		/// <returns>自身と引数の外積</returns>
		public PVector Cross(PVector src)
		{
			float crossX = this.Y * src.Z - this.Z * src.Y;
			float crossY = this.Z * src.X - this.X * src.Z;
			float crossZ = this.X * src.Y - this.Y * src.X;

			return new PVector(crossX, crossY, crossZ);
		}

		/// <summary>
		/// 自身の3点と引数の内積を返す
		/// </summary>
		/// <param name="src">公式のbの方</param>
		/// <returns>内積</returns>
		public float Dot(PVector src)
		{
			return X * (src.X) + Y * src.Y + Z * src.Z;
		}

		/// <summary>
		/// 自身のXYZ座標を引数のXYZそれぞれ減算した結果
		/// </summary>
		/// <param name="src">減算する3点</param>
		/// <returns>減算されたXYZ座標</returns>
		public PVector Sub(PVector src)
		{
			PVector dst = new PVector(this.X - src.X, this.Y - src.Y, this.Z - src.Z);
			return dst;
		}


		/// <summary>
		/// 正規化した値を返す
		/// </summary>
		/// <returns>正規化した値</returns>
		public PVector GetNormalization()
		{
			double length = Math.Sqrt((X * X) + (Y * Y) + (Z * Z));


			PVector dst = new PVector(this.X / (float)length,
									  this.Y / (float)length,
									  this.Z / (float)length);

			return dst;	
		}

		/// <summary>
		/// 自身と引数が同じかどうか判定する
		/// </summary>
		/// <param name="src">比較する方のPVector</param>
		/// <returns>true or false</returns>
		public bool Equals(PVector src)
		{
			if (this.X == src.X && this.Y == src.Y && this.Z == src.Z) return true;
			else return false;
		}


	}
}
