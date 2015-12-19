using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	/// <summary>
	/// Bitmap処理を高速化するためのクラス
	/// http://homepage2.nifty.com/nonnon/SoftSample/CS.NET/SampleBitmapPlus.html
	/// </summary>
	public class BitmapPlus
	{
		/// <summary>
		/// オリジナルのBitmapオブジェクト
		/// </summary>
		private Bitmap _bmp = null;

		/// <summary>
		/// Bitmapに直接アクセスするためのオブジェクト
		/// </summary>
		private BitmapData _img = null;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="original"></param>
		public BitmapPlus(Bitmap original)
		{
			// オリジナルのBitmapオブジェクトを保存
			_bmp = original;
		}

		/// <summary>
		/// Bitmap処理の高速化開始
		/// </summary>
		public void BeginAccess()
		{
			// Bitmapに直接アクセスするためのオブジェクト取得(LockBits)
			_img = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height),
				System.Drawing.Imaging.ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		}

		/// <summary>
		/// Bitmap処理の高速化終了
		/// </summary>
		public void EndAccess()
		{
			if (_img != null)
			{
				// Bitmapに直接アクセスするためのオブジェクト開放(UnlockBits)
				_bmp.UnlockBits(_img);
				_img = null;
			}
		}

		/// <summary>
		/// BitmapのGetPixel同等
		/// </summary>
		/// <param name="x">Ｘ座標</param>
		/// <param name="y">Ｙ座標</param>
		/// <returns>Colorオブジェクト</returns>
		public Color GetPixel(int x, int y)
		{
			if (_img == null)
			{
				// Bitmap処理の高速化を開始していない場合はBitmap標準のGetPixel
				return _bmp.GetPixel(x, y);
			}
			unsafe
			{
				// Bitmap処理の高速化を開始している場合はBitmapメモリへの直接アクセス
				byte* adr = (byte*)_img.Scan0;
				int pos = x * 3 + _img.Stride * y;
				byte b = adr[pos + 0];
				byte g = adr[pos + 1];
				byte r = adr[pos + 2];
				return Color.FromArgb(r, g, b);
			}
		}

		/// <summary>
		/// BitmapのSetPixel同等
		/// </summary>
		/// <param name="x">Ｘ座標</param>
		/// <param name="y">Ｙ座標</param>
		/// <param name="col">Colorオブジェクト</param>
		public void SetPixel(int x, int y, Color col)
		{
			if (_img == null)
			{
				// Bitmap処理の高速化を開始していない場合はBitmap標準のSetPixel
				_bmp.SetPixel(x, y, col);
				return;
			}
			unsafe
			{
				// Bitmap処理の高速化を開始している場合はBitmapメモリへの直接アクセス
				byte* adr = (byte*)_img.Scan0;
				int pos = x * 3 + _img.Stride * y;
				adr[pos + 0] = col.B;
				adr[pos + 1] = col.G;
				adr[pos + 2] = col.R;
			}
		}
	}
}
