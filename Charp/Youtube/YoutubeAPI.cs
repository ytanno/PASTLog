using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeAPI
{
	internal class Program
	{
		internal class UploadVideo
		{
			[STAThread]
			private static void Main(string[] args)
			{
				//必要な情報
				//https://code.google.com/apis/console‎ で登録
				//Client ID
				//Client Secret

				//Youtubeアカウント
				//accountName
				var clientId = @"";
				var clientSecret = @"";
				var youtubeName = @"dummy";

				Console.WriteLine("YouTube Data API: Upload Video");
				Console.WriteLine("==============================");

				try
				{
					new UploadVideo().Run(clientId, clientSecret, youtubeName).Wait();
				}
				catch (AggregateException ex)
				{
					foreach (var e in ex.InnerExceptions)
					{
						Console.WriteLine("Error: " + e.Message);
					}
				}

				//出力結果
				//https://www.youtube.com/watch?v=yuQ3xsYROBY


				Console.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}

			private async Task Run(string clientId, string clientSecret, string youtubeAccountName)
			{
				//ユーザーからの認証待ち
				UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
						new ClientSecrets
						{
							ClientId = clientId,
							ClientSecret = clientSecret,
						},
						new[] { YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.Youtube },
						youtubeAccountName,
						CancellationToken.None
					);

				//accessTokenの有効期限が切れたら更新する必要がある。

				var youtubeService = new YouTubeService(new BaseClientService.Initializer()
				{
					HttpClientInitializer = credential,
					ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
				});

				var video = new Video();
				video.Snippet = new VideoSnippet();
				video.Snippet.Title = "flower";
				video.Snippet.Description = "sample Description";
				video.Snippet.Tags = new string[] { "tag1", "tag2" };
				video.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
				video.Status = new VideoStatus();
				video.Status.PrivacyStatus = "public";
				var filePath = Environment.CurrentDirectory + @"\video.avi";

				using (var fileStream = new FileStream(filePath, FileMode.Open))
				{
					var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
					videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
					videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;

					await videosInsertRequest.UploadAsync();
				}
			}

			private static void videosInsertRequest_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
			{
				switch (progress.Status)
				{
					case UploadStatus.Uploading:
						Console.WriteLine("{0} bytes sent.", progress.BytesSent);
						break;

					case UploadStatus.Failed:
						Console.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
						break;
				}
			}

			private static void videosInsertRequest_ResponseReceived(Video video)
			{
				Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
			}

			private static void MakeImageToMovie(string filePath)
			{
				Process ffmpeg = new Process();
				ffmpeg.StartInfo.FileName = Environment.CurrentDirectory + @"\ffmpeg.exe";
				ffmpeg.StartInfo.Arguments = " -r 0.2  -i " + filePath + " -vcodec mjpeg video.avi";
				ffmpeg.Start();
			}
		}
	}
}