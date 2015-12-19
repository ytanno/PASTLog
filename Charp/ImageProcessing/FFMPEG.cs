using System;

using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ytVideoEditor
{
	public static class FFMPEG
	{
		public static void PngToMovie(string imgDir, float rate, string moviePath)
		{
			var saveVideoPath = moviePath;
			//if ( File.Exists(saveVideoPath) ) File.Delete(saveVideoPath);

			CheckAndKillProcess("ffmpeg");

			Process ffmpeg = new Process();
			ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
			//ffmpeg.StartInfo.Arguments = " -r " + rate + " -i " + path + " -vcodec mjpeg video.avi";
			//ffmpeg.StartInfo.Arguments = " -r " + rate + " -f image2 -i " + jpgDir + @"%06d.png " + saveVideoPath;

			//qscale:v を設定しないと画質が落ちる
			//ffmpeg.StartInfo.Arguments = " -r " + rate.ToString() + " -i " + imgDir + @"%06d.png -vcodec libx264 -qscale:v 0 " + saveVideoPath;
			ffmpeg.StartInfo.Arguments = " -y -r " + rate.ToString() + " -i " + imgDir + @"%06d.png -an -vcodec libx264 -pix_fmt yuv420p  " + saveVideoPath;
			ffmpeg.StartInfo.CreateNoWindow = true;
			ffmpeg.StartInfo.UseShellExecute = false;
			ffmpeg.Start();
			ffmpeg.WaitForExit();
		}

		public static float GetFrameRate(string moviePath)
		{
			float rate = -1.0f;
			CheckAndKillProcess("ffmpeg");
			Process ffmpeg = new Process();

			ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
			ffmpeg.StartInfo.Arguments = " -i " + moviePath;
			ffmpeg.StartInfo.CreateNoWindow = true;
			ffmpeg.StartInfo.UseShellExecute = false;
			ffmpeg.StartInfo.RedirectStandardInput = false;
			ffmpeg.StartInfo.RedirectStandardOutput = true;
			ffmpeg.StartInfo.RedirectStandardError = true;
			ffmpeg.Start();
			//output = ffmpeg.StandardOutput.ReadToEnd();
			var error = ffmpeg.StandardError.ReadToEnd();
			ffmpeg.WaitForExit();

			//Console.WriteLine(output);
			ffmpeg.Close();

			var rV = error.Split(',')
				.FirstOrDefault(x => x.Contains("fps")).Replace("fps", "").Trim();

			if ( rV != null )
			{
				float.TryParse(rV, out rate);
			}

			return rate;
		}

		public static void MergeSoundAndMovie(string soundPath, string moviePath, string completeMoviePath)
		{
			//if ( File.Exists(completeMoviePath) ) File.Delete(completeMoviePath);
			CheckAndKillProcess("ffmpeg");
			Process ffmpeg = new Process();
			ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
			ffmpeg.StartInfo.Arguments = " -y -i " + moviePath + " -i " + soundPath + " -vcodec copy -acodec copy " + completeMoviePath;
			ffmpeg.StartInfo.CreateNoWindow = true;
			ffmpeg.StartInfo.UseShellExecute = false;
			ffmpeg.Start();
			ffmpeg.WaitForExit();
		}

		public static void DivSound(string moviePath, string soundPath)
		{
			//if ( File.Exists(soundPath) ) File.Delete(soundPath);
			CheckAndKillProcess("ffmpeg");
			Process ffmpeg = new Process();
			ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
			ffmpeg.StartInfo.Arguments = " -y -i " + moviePath + " -acodec copy  -map 0:1 " + soundPath;
			ffmpeg.StartInfo.CreateNoWindow = true;
			ffmpeg.StartInfo.UseShellExecute = false;
			ffmpeg.Start();
			ffmpeg.WaitForExit();
		}

		public static void CopyStopMotionFile(string fixDir, string stDir, float stParam)
		{
			if ( !Directory.Exists(stDir) ) Directory.CreateDirectory(stDir);
			else
			{
				foreach ( var fi in new DirectoryInfo(stDir).GetFiles() ) fi.Delete();
			}

			var index = 1;
			foreach ( var fi in new DirectoryInfo(fixDir)
				.GetFiles().Where((x, i) => i % (int)stParam == 0).Select(x => x.FullName) )
			{
				var indexStr = string.Format("{0:D6}", index);
				var savePath = stDir + indexStr + ".png";
				File.Copy(fi, savePath);
				index++;
			}
		}

		public static void DivMovie(string srcMoviePath, string notSoundMoviePath)
		{
			//if ( File.Exists(notSoundMoviePath) ) File.Delete(notSoundMoviePath);
			CheckAndKillProcess("ffmpeg");
			Process ffmpeg = new Process();
			ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
			ffmpeg.StartInfo.Arguments = " -y -i " + srcMoviePath + " -vcodec copy -map 0:0 " + notSoundMoviePath;
			ffmpeg.StartInfo.CreateNoWindow = true;
			ffmpeg.StartInfo.UseShellExecute = false;
			ffmpeg.Start();
			ffmpeg.WaitForExit();
		}

		/*
		public static void UpdateBitRate(string movieName, string bitRate)
		{
			var saveVideoPath = Environment.CurrentDirectory + @"\" + movieName;
			if ( File.Exists(saveVideoPath) )
			{
				Process ffmpeg = new Process();
				ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
				ffmpeg.StartInfo.Arguments = " -i " + saveVideoPath + " -b:v " + bitRate + " test.mp4";
				ffmpeg.StartInfo.CreateNoWindow = true;
				ffmpeg.StartInfo.UseShellExecute = false;
				ffmpeg.Start();
			}
		}
		*/

		private static void CheckAndKillProcess(string processName)
		{
			foreach ( var p in Process.GetProcesses() )
			{
				if ( p.ProcessName == processName )
				{
					p.Kill();
				}
			}
		}

		public static void movieToPng(string moviePath, float rate, string saveDir)
		{
			//jpgだと画質が落ちる

			if ( !Directory.Exists(saveDir) ) Directory.CreateDirectory(saveDir);
			else
			{
				foreach ( var fi in new DirectoryInfo(saveDir).GetFiles() ) fi.Delete();
			}
			CheckAndKillProcess("ffmpeg");
			Process ffmpeg = new Process();
			ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
			//ffmpeg -i 元動画.avi -ss 144 -t 148 -r 24 -f image2 %06d.jpg
			ffmpeg.StartInfo.Arguments = " -r " + rate.ToString() + " -i " + moviePath + " -f image2 " + saveDir + "%06d.png";
			ffmpeg.StartInfo.CreateNoWindow = true;
			ffmpeg.StartInfo.UseShellExecute = false;
			ffmpeg.Start();

			ffmpeg.WaitForExit();
		}
	}
}