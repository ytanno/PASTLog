using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace ImageProcessing
{
	static public class Utility
	{

		public static byte[] BitmapToHash(Bitmap CurrentImg)
		{
			BitmapData bmpdata = CurrentImg.LockBits(new Rectangle(0, 0, CurrentImg.Width, CurrentImg.Height), ImageLockMode.ReadWrite, CurrentImg.PixelFormat);
			IntPtr ptr = bmpdata.Scan0;
			int bytes = bmpdata.Stride * CurrentImg.Height;
			byte[] rgbValues = new byte[bytes];
			Marshal.Copy(ptr, rgbValues, 0, bytes);
			byte[] CurrentHash = new MD5CryptoServiceProvider().ComputeHash(rgbValues);
			CurrentImg.UnlockBits(bmpdata);
			return CurrentHash;
		}

		public static byte[] BitmapToByte(Bitmap CurrentImg)
		{
			byte[] dst = new byte[CurrentImg.Width * CurrentImg.Height * 3];

			BitmapPlus bmpP = new BitmapPlus(CurrentImg);
			bmpP.BeginAccess();
			for (int y = 0; y < CurrentImg.Height; y++)
			{
				for (int x = 0; x < CurrentImg.Width; x++)
				{
					Color c = bmpP.GetPixel(x, y);

					dst[y * CurrentImg.Width * 3 + x * 3] = c.R;
					dst[y * CurrentImg.Width * 3 + x * 3 + 1] = c.G;
					dst[y * CurrentImg.Width * 3 + x * 3 + 2] = c.B;
				}
			}
			bmpP.EndAccess();
			return dst;
		}


	}
}
