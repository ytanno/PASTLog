using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabImg
{
	/// <summary>
	/// 3Dデータの平滑化クラス
	/// </summary>
	public static class Smoothing3D
	{
		/// <summary>
		/// <para>ラプラシアンスムージング</para>
		/// <para>裏返った場合の処理は未実装 誰かやってください</para>
		/// </summary>
		/// <param name="srcTriangleList">3dを構築する三角形リスト</param>
		/// <param name="dt">更新量の補正値  (x,y,z)更新値= 更新量 * dt + 現在値 </param>
		/// <returns>スムージング後の三角形リスト</returns>
		public static List<Triangle> Laplacian(List<Triangle> srcTriangleList, float dt)
		{
			//自身の点をKeyにして隣接する点のリストを返すDictonary
			Dictionary<PVector, List<PVector>> neighborDic = new Dictionary<PVector, List<PVector>>();

			//三角形リストから点と隣接する点を求める
			foreach (var triagnle in srcTriangleList)
			{
				//三角形の点を呼び出す
				foreach (PVector point in triagnle.Vertics)
				{
					//登録されていない点の時
					if (!neighborDic.ContainsKey(point))
					{
						neighborDic[point] = new List<PVector>();

						//隣接する点を求める
						//比較用の三角形を呼び出す
						foreach (var compTriangle in srcTriangleList)
						{
							//比較用の三角形の中に検索している点があった場合
							if (compTriangle.Vertics.Count(vector => vector.Equals(point)) == 1)
							{
								//検索している点以外を三角形から取り出す
								List<PVector> tempList = compTriangle.Vertics.Where(x => !x.Equals(point)).Select(x => x).ToList();

								foreach (PVector newPoint in tempList)
								{
									//以前に登録していなければ加える
									if (!neighborDic[point].Contains(newPoint)) neighborDic[point].Add(newPoint);
								}
							}
						}
					}
				}
			}


			//三角形リストを更新する
			List<Triangle> tempTriList = new List<Triangle>(srcTriangleList);
			foreach (var key in neighborDic.Keys)
			{
				//ラプラシアンスムージングの計算
				List<PVector> p = neighborDic[key];
				float updateX = 0.0f;
				float updateY = 0.0f;
				float updateZ = 0.0f;
				foreach (var neighbor in p)
				{
					updateX += neighbor.X - key.X;
					updateY += neighbor.Y - key.Y;
					updateZ += neighbor.Z - key.Z;
				}
				updateX /= p.Count;
				updateY /= p.Count;
				updateZ /= p.Count;


				//裏返った時用の補正
				//多分違う
				//if (updateX * key.X < 0) updateX *= -1;
				//if (updateY * key.Y < 0) updateY *= -1;
				//if (updateZ * key.Z < 0) updateZ *= -1;
				
				//各三角形の座標更新
				for (int i = 0; i < tempTriList.Count; i++)
				{
					var trianglePoints = tempTriList[i].Vertics;
					PVector[] tempVec = new PVector[3];

					//更新前の点を含んでいる三角形を見つける
					for (int j = 0; j < trianglePoints.Length; j++)
					{
						if (trianglePoints[j].Equals(key))
						{
							PVector uVector = new PVector(updateX * dt + key.X, updateY * dt + key.Y, updateZ * dt + key.Z);
							if (j == 0)
							{
								tempTriList[i].Vertics[0] = uVector;
								tempTriList[i].V1 = uVector;
							}
							if (j == 1)
							{
								tempTriList[i].Vertics[1] = uVector;
								tempTriList[i].V2 = uVector;
							}
							if (j == 2)
							{
								tempTriList[i].Vertics[2] = uVector;
								tempTriList[i].V3 = uVector;
							}
						}
					}
				}
			}
			return tempTriList;
		}
	}
}
