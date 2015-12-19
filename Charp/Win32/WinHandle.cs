using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Utility
{
	/// <summary>
	/// WindowAPI関連クラス
	/// </summary>
	public class WinHandle
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

		[DllImport("USER32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("USER32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPTStr)] System.Text.StringBuilder buff);

		[DllImport("user32.dll")]
		public static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll")]
		public static extern IntPtr SetForegroundWindow(IntPtr ptr);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

		private const int WM_GETTEXT = 0x000D;
		private const int WM_GETTEXTLENGTH = 0x000E;

		/// <summary>
		/// キャプションの取得
		/// </summary>
		/// <param name="hwnd">WindowHandle</param>
		/// <returns>キャプション</returns>
		public static string GetWindowTextRaw(IntPtr hwnd)
		{
			// Allocate correct string length first
			int byteLength = SendMessage(hwnd, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero).ToInt32();
			System.Text.StringBuilder buff = new System.Text.StringBuilder(byteLength + 1);
			SendMessage(hwnd, WM_GETTEXT, byteLength + 1, buff);
			return buff.ToString();
		}

		/// <summary>
		/// 検索タイプ
		/// </summary>
		public enum SearchType
		{
			SameClassName,
			ContainClassName,
			SameCaption,
			ContainCaption
		}

		public static IntPtr FindHandle(int rectWidth, IntPtr parentHandle)
		{
			List<IntPtr> children = new List<IntPtr>(GetChildWindows(parentHandle));
			foreach ( IntPtr child in children )
			{
				Rect rect = new Rect();
				GetWindowRect(child, ref rect);

				if ( rect.Width == rectWidth ) return child;
			}
			return IntPtr.Zero;
		}


		public static IntPtr FindHandle(string windowText, IntPtr parentHandle)
		{
			List<IntPtr> children = new List<IntPtr>(GetChildWindows(parentHandle));
			foreach ( IntPtr child in children )
			{
				string result = GetWindowTextRaw(child);
				if ( result.Equals(windowText) && result.Length != 0 )
				{
					//hitHandleList.Add(child);
					return child;
				}
			}
			return IntPtr.Zero;
		}


		/// <summary>
		/// winhandle検索
		/// </summary>
		/// <param name="searchWord">検索ワード</param>
		/// <param name="parentHandle">検索先の親ハンドル</param>
		/// <param name="type">検索タイプ</param>
		/// <returns>子ハンドル</returns>
		public static IntPtr FindHandle(string searchWord, IntPtr parentHandle, SearchType type)
		{
			List<IntPtr> children = new List<IntPtr>(GetChildWindows(parentHandle));

			foreach (IntPtr child in children)
			{
				//同一のキャプション
				if (type == SearchType.SameCaption)
				{
					string result = GetWindowTextRaw(child);
					if (result.Equals(searchWord) && result.Length != 0) return child;
				}

				//キャプション名を含む
				if (type == SearchType.ContainCaption)
				{
					string result = GetWindowTextRaw(child);
					if (result.Contains(searchWord) && result.Length != 0) return child;
				}

				//同一のクラス名
				if (type == SearchType.SameClassName)
				{
					int nRet;
					StringBuilder ClassName = new StringBuilder(256);
					nRet = GetClassName(child, ClassName, ClassName.Capacity);
					if (nRet != 0)
					{
						string className = ClassName.ToString();
						if (className.Equals(searchWord) && className.Length != 0) return child;
					}
				}

				//クラス名を含む
				if (type == SearchType.ContainClassName)
				{
					int nRet;
					StringBuilder ClassName = new StringBuilder(256);
					nRet = GetClassName(child, ClassName, ClassName.Capacity);
					if (nRet != 0)
					{
						string className = ClassName.ToString();
						if (className.Contains(searchWord) && className.Length != 0) return child;
					}
				}
			}
			return IntPtr.Zero;
		}

		public static List<string> GetWindoText(IntPtr parentHandle)
		{
			List<IntPtr> child = GetChildWindows(parentHandle);

			List<string> list = new List<string>();
			foreach (IntPtr ch in child)
			{
				string result = GetWindowTextRaw(ch);
				list.Add(result);
			}
			return list;
		}

		public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

		public static List<IntPtr> GetChildWindows(IntPtr parent)
		{
			List<IntPtr> result = new List<IntPtr>();
			GCHandle listHandle = GCHandle.Alloc(result);
			try
			{
				EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
				EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
			}
			finally
			{
				if (listHandle.IsAllocated)
					listHandle.Free();
			}
			return result;
		}

		private static bool EnumWindow(IntPtr handle, IntPtr pointer)
		{
			GCHandle gch = GCHandle.FromIntPtr(pointer);
			List<IntPtr> list = gch.Target as List<IntPtr>;
			if (list == null)
			{
				throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
			}
			list.Add(handle);
			//  You can modify this to check to see if you want to cancel the operation, then return a null here
			return true;
		}

		/// <summary>
		/// CodeReflection.ScreenCapturingDemo
		/// http://www.codeproject.com/Articles/9741/Screen-Captures-Window-Captures-and-Window-Icon-Ca
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>

		public struct Rect
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public int Width
			{
				get
				{
					return right - left;
				}
			}

			public int Height
			{
				get
				{
					return bottom - top;
				}
			}
		}

		public enum TernaryRasterOperations
		{
			SRCCOPY = 0x00CC0020, /* dest = source                   */
			SRCPAINT = 0x00EE0086, /* dest = source OR dest           */
			SRCAND = 0x008800C6, /* dest = source AND dest          */
			SRCINVERT = 0x00660046, /* dest = source XOR dest          */
			SRCERASE = 0x00440328, /* dest = source AND (NOT dest )   */
			NOTSRCCOPY = 0x00330008, /* dest = (NOT source)             */
			NOTSRCERASE = 0x001100A6, /* dest = (NOT src) AND (NOT dest) */
			MERGECOPY = 0x00C000CA, /* dest = (source AND pattern)     */
			MERGEPAINT = 0x00BB0226, /* dest = (NOT source) OR dest     */
			PATCOPY = 0x00F00021, /* dest = pattern                  */
			PATPAINT = 0x00FB0A09, /* dest = DPSnoo                   */
			PATINVERT = 0x005A0049, /* dest = pattern XOR dest         */
			DSTINVERT = 0x00550009, /* dest = (NOT dest)               */
			BLACKNESS = 0x00000042, /* dest = BLACK                    */
			WHITENESS = 0x00FF0062 /* dest = WHITE                    */
		}

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

		[DllImport("user32.dll")]
		public static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("user32.dll")]
		public static extern IntPtr GetWindowDC(IntPtr hwnd);

		[DllImport("gdi32.dll")]
		public static extern bool BitBlt(IntPtr hdcDst, int xDst, int yDst, int cx, int cy, IntPtr hdcSrc, int xSrc, int ySrc, int ulRop);

		public static Bitmap GetWindowCaptureAsBitmap(IntPtr handle)
		{
			IntPtr hWnd = handle;
			Rect rc = new Rect();
			if (!GetWindowRect(hWnd, ref rc))
				return null;

			// create a bitmap from the visible clipping bounds of the graphics object from the window
			Bitmap bitmap = new Bitmap(rc.Width, rc.Height);

			// create a graphics object from the bitmap
			Graphics gfxBitmap = Graphics.FromImage(bitmap);

			// get a device context for the bitmap
			IntPtr hdcBitmap = gfxBitmap.GetHdc();

			// get a device context for the window
			IntPtr hdcWindow = GetWindowDC(hWnd);

			// bitblt the window to the bitmap
			BitBlt(hdcBitmap, 0, 0, rc.Width, rc.Height, hdcWindow, 0, 0, (int)TernaryRasterOperations.SRCCOPY);

			// release the bitmap's device context
			gfxBitmap.ReleaseHdc(hdcBitmap);

			ReleaseDC(hWnd, hdcWindow);

			// dispose of the bitmap's graphics object
			gfxBitmap.Dispose();

			// return the bitmap of the window
			return bitmap;
		}
	}
}