using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace bmpConverter
{
	public partial class Form1: Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private async void button1_Click(object sender, EventArgs e)
		{
		

			button1.Enabled = false;

			await Task.Run(() => DoWork());
		
			button1.Enabled = true; 
		}


		private void DoWork()
		{

			//KillConsoleHostProcess();

			var result = 1;
			var dstDir = Environment.CurrentDirectory + @"\tmp\";
			if ( !Directory.Exists(dstDir) ) Directory.CreateDirectory(dstDir);
			else foreach ( var fi in new DirectoryInfo(dstDir).GetFiles() ) File.Delete(fi.FullName);

			foreach ( var fi in new DirectoryInfo(Environment.CurrentDirectory + @"\data").GetFiles() )
			{
				ConvertBmp(fi.FullName, dstDir + fi.Name.Replace(".jpg", ".bmp"));
			}

			var fPath = new DirectoryInfo(dstDir).GetFiles().FirstOrDefault();
			if ( fPath != null )
			{
				var lastName = fPath.Name;
				var lastDir = Environment.CurrentDirectory + @"\";
				CopyCommandLine(dstDir, lastDir + lastName);
			}
			else result = -1;
			//foreach ( var fi in new DirectoryInfo(dstDir).GetFiles() ) File.Delete(fi.FullName);
			//KillConsoleHostProcess();

			
		}


		private void KillConsoleHostProcess()
		{
			var ps = Process.GetProcesses();
			foreach(var p in Process.GetProcesses())
			{
				if(p.ProcessName == "conhost")
				{
					p.Kill();
				}

			}



		}






		private void ConvertBmp(string srcPath, string dstPath)
		{
			using ( var bmp = new Bitmap(srcPath) )
			{
				byte[] b = new byte[bmp.Width * bmp.Height * 2];
				var gch = GCHandle.Alloc(b, GCHandleType.Pinned);

				IntPtr scan0 = gch.AddrOfPinnedObject();

				Bitmap dest = new Bitmap(bmp.Width, bmp.Height, 4 * ( ( bmp.Width * 16 + 31 ) / 32 ), PixelFormat.Format16bppRgb565, scan0);

				using ( Graphics g = Graphics.FromImage(dest) )
				{
					g.DrawImage(bmp, 0, 0);
				}

				gch.Free();
				dest.Save(dstPath, ImageFormat.Bmp);
			}
		}

		private void CopyCommandLine(string srcDirPath, string dstFilePath)
		{
			if ( File.Exists(dstFilePath) ) File.Delete(dstFilePath);

			//Processオブジェクトを作成
			System.Diagnostics.Process p = new System.Diagnostics.Process();

			//ComSpec(cmd.exe)のパスを取得して、FileNameプロパティに指定
			p.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardInput = false;
			//ウィンドウを表示しないようにする
			p.StartInfo.CreateNoWindow = true;
			//コマンドラインを指定（"/c"は実行後閉じるために必要）
			p.StartInfo.Arguments = "/c copy /b " + srcDirPath + "*.bmp " + dstFilePath;

			//起動
			p.Start();

			string results = p.StandardOutput.ReadToEnd();

			p.WaitForExit();
			p.Close();

			Console.WriteLine(results);

		}





	}
}
