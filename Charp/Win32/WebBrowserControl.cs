using System;

using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Utility
{
	/// <summary>
	/// ブラウザ系列
	/// </summary>
	public  class WebBrowserControl
	{
		public WebBrowser Wb;

		public WebBrowserControl(WebBrowser web)
		{
			Wb = web;
		}

		public void ClickTag(string tagName, string searchAttr, string searchMethodName, int index)
		{
			var co = 0;
			foreach ( HtmlElement h in Wb.Document.GetElementsByTagName(tagName) )
			{
				if ( h.OuterHtml.Contains(searchAttr) && h.OuterHtml.Contains(searchMethodName) )
				{
					if ( co == index )
					{
						h.InvokeMember("click");
						break;
					}
					co++;
				}
			}
		}

		public void ClickAtagInnerText(string searchInnerText, int frameIndex)
		{
			//var ok = false;
			HtmlElement hit = SearchATagInnerText(searchInnerText, frameIndex);
			if ( hit != null )
			{
				//ok = true;
				Click(hit);
			}
			//return ok;
		}

		public int CountInnerText(string tagName, string innerText)
		{
			var count = 0;
			foreach ( HtmlElement hc in Wb.Document.GetElementsByTagName(tagName) )
			{
				if ( hc.InnerText != null )
				{
					if ( hc.InnerText.Contains(innerText) ) count++;
				}
			}

			return count;
		}

		public void ClickAtagInnerText(List<string> searchTexts, int frameIndex, int index)
		{
			HtmlDocument doc = Wb.Document;
			//例外あり
			if ( frameIndex > -1 ) doc = Wb.Document.Window.Frames[frameIndex].Document;

			int c = 0;
			foreach ( HtmlElement aTag in doc.GetElementsByTagName("A") )
			{
				////Console.WriteLine(c);
				////Console.WriteLine(aTag.InnerText);
				if ( aTag.InnerText != null )
				{
					var ok = false;

					foreach ( var searchText in searchTexts )
					{
						if ( aTag.InnerText.Contains(searchText) )
						{
							ok = true;
							break;
						}
					}

					if ( ok )
					{
						if ( c == index )
						{
							aTag.InvokeMember("click");
							break;
						}
						c++;
					}
				}
			}
		}

		public void SelectBox(string id, string attrsValue)
		{
			Wb.Document.GetElementById(id).SetAttribute("value", attrsValue);
		}



		public  void ClickAtag( int frameIndex, int aTagIndex)
		{
			if ( frameIndex == -1 )
			{
				var aTags = Wb.Document.Body.GetElementsByTagName("a");
				Click(aTags[aTagIndex]);
			}
			else
			{
				var frames = Wb.Document.Window.Frames;
				var aTags = frames[frameIndex].Document.GetElementsByTagName("a");
				Click(aTags[aTagIndex]);
			}
		}

		public  void InputInnerText( string searchName, int index, string inputContent, int frameIndex)
		{
			HtmlElementCollection hc = SearchName(searchName, frameIndex);
			if ( hc.Count > index )
			{
				hc[index].InnerText = inputContent;
			}
		}

		public  void InputValue( string searchName, int index, string inputContent, int frameIndex)
		{
			HtmlElementCollection hc = SearchName(searchName, frameIndex);
			if ( hc.Count > index )
			{
				hc[index].SetAttribute("value", inputContent);
			}
		}

		public  HtmlElementCollection SearchName( string searchName, int frameIndex)
		{
			HtmlElementCollection hc = null;

			if ( frameIndex == -1 )
			{
				hc = Wb.Document.All.GetElementsByName(searchName);
			}
			else
			{
				hc = Wb.Document.Window.Frames[frameIndex].Document.All.GetElementsByName(searchName);
			}

			return hc;
		}

		public  void ClickName( string name, int index, int frameIndex)
		{
			HtmlElementCollection hc = SearchName( name, frameIndex);
			if ( hc.Count > index )
			{
				Click(hc[index]);
			}
		}

		private HtmlElement SearchATagInnerText(string name, int frameIndex)
		{
			HtmlDocument doc = Wb.Document;
			//例外あり
			if (frameIndex > -1) doc = Wb.Document.Window.Frames[frameIndex].Document;

			int c = 0;
			foreach (HtmlElement aTag in doc.GetElementsByTagName("A"))
			{
				////Console.WriteLine(c);
				////Console.WriteLine(aTag.InnerText);
				if (aTag.InnerText != null)
				{
					if (aTag.InnerText.Trim() == name) return aTag;
				}
				c++;
			}
			return null;
		}


	

		private  void Click(HtmlElement h)
		{
			//Console.WriteLine("Click {0}", h.OuterHtml);
			h.InvokeMember("click");
		}

		public  void ClickID( string idName)
		{
			var cID = Wb.Document.GetElementById(idName);
			Click(cID);
		}

		public  void SubmitForm0( string formName, int frameIndex)
		{
			if ( frameIndex == -1 )
			{
				//Console.WriteLine("Submit");
				var forms = Wb.Document.All.GetElementsByName(formName);
				forms[0].InvokeMember("submit");
			}
			else
			{
				var forms = Wb.Document.Window.Frames[frameIndex].Document.All.GetElementsByName(formName);
				forms[0].InvokeMember("submit");
			}
		}

		public async Task NavigateAsync(CancellationToken ct, Action startNavigation, int timeout = Timeout.Infinite)
		{
			const int AJAX_DELAY = 1000;
			var onloadTcs = new TaskCompletionSource<bool>();
			EventHandler onloadEventHandler = null;

			WebBrowserDocumentCompletedEventHandler documentCompletedHandler = delegate
			{
				// DocumentCompleted may be called several time for the same page,
				// beacuse of frames
				if ( onloadEventHandler != null || onloadTcs == null || onloadTcs.Task.IsCompleted )
					return;


				// handle DOM onload event to make sure the document is fully loaded
				onloadEventHandler = (s, e) => onloadTcs.TrySetResult(true);


				Wb.Document.Window.AttachEventHandler("onload", onloadEventHandler);
			};

			using ( var cts = CancellationTokenSource.CreateLinkedTokenSource(ct) )
			{
				if ( timeout != Timeout.Infinite )
					cts.CancelAfter(Timeout.Infinite);

				using ( cts.Token.Register(() => onloadTcs.TrySetCanceled(), useSynchronizationContext : true) )
				{
					Wb.DocumentCompleted += documentCompletedHandler;
					try
					{
						startNavigation();
						// wait for DOM onload, throw if cancelled
						await onloadTcs.Task;
						ct.ThrowIfCancellationRequested();
						// let AJAX code run, throw if cancelled
						await Task.Delay(AJAX_DELAY, ct);
					}
					finally
					{
						Wb.DocumentCompleted -= documentCompletedHandler;
						if ( onloadEventHandler != null )
						{
							Wb.Document.Window.DetachEventHandler("onload", onloadEventHandler);
						}

					}
				}
			}
		}


	}
}