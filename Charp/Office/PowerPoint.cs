using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System.Diagnostics;

namespace PPTForm
{
	public class PowerPoint
	{
		private Microsoft.Office.Interop.PowerPoint.Application _app;
		private Presentation _ppt;
		private int _nowPage = 1;
		private Voice _cv;

		public PowerPoint(string filePath)
		{
			_cv = new Voice();
			_app = new Microsoft.Office.Interop.PowerPoint.Application();
			_app.Visible = MsoTriState.msoTrue;
			_ppt = _app.Presentations.Open(filePath);
		}

		public void Run()
		{
			_ppt.SlideShowSettings.Run();

			var page = _ppt.Slides.Count;

			while ( true )
			{
				if ( _nowPage > page ) break;
				_ppt.SlideShowWindow.View.GotoSlide(_nowPage, MsoTriState.msoFalse);
				//Thread.Sleep(5000);
				var note = _ppt.Slides[_nowPage].NotesPage.Shapes.Placeholders[2].TextFrame.TextRange.Text;
				_cv.Speak(note);
				_nowPage++;
			}

			_ppt.Close();
			_app.Quit();
			KillMyProcess();
		}

		public static void KillMyProcess()
		{
			var process = Process.GetProcesses();
			foreach ( var p in process )
			{
				if ( p.ProcessName == "POWERPNT" )
				{
					p.CloseMainWindow();
				}
			}
		}
	}
}