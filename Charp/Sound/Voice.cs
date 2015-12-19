using SpeechLib;

namespace PPTForm
{
	public class Voice
	{
		private SpVoice _cv = null;

		public Voice()
		{
			_cv = new SpVoice();

			//speed
			//_cv.Rate = -5;

			//volume
			//_cv.Volume = 1;
		}

		public void Speak(string text)
		{
			_cv.Speak(text);
		}
	}
}