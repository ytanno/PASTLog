using Codeplex.Data;
using System;

namespace YoutubeOuth2Sample
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			//Auth2.0のサンプル


			//必要情報
			//開発者情報 https://console.developers.google.com/ で登録
			var client_id = @"";
			var client_secret = "";

			string deviceCode, userCode, verificationURL;
			SetDeviceInfo(client_id, out deviceCode, out userCode, out verificationURL);

			Console.WriteLine("{0} にアクセスして{1}を入力して、認証を許可してください。終わったらエンターキーを押してください。", verificationURL, userCode);
			Console.ReadLine();

			string accessToken, refreshToken;
			SetTokenInfo(client_id, client_secret, deviceCode, out accessToken, out refreshToken);

			//accessTokenは有効期限があるので切れたらrefreshTokenでaccessTokenを再発行
			var reAccessToken = Refresh(client_id, client_secret, refreshToken);
		}

		private static string Refresh(string client_id, string client_secret, string refreshToken)
		{
			var reAccessToken = "";

			var url = @"https://accounts.google.com/o/oauth2/token";
			var post = @"client_id=" + client_id + @"&client_secret=" + client_secret + @"&refresh_token=" + refreshToken + @"&grant_type=refresh_token";
			var res = Post(url, post);

			var json = DynamicJson.Parse(res);
			reAccessToken = json["access_token"];

			return reAccessToken;
		}

		private static void SetTokenInfo(string client_id, string client_secret, string deviceCode, out string accessToken, out string refreshToken)
		{
			var url = @"https://accounts.google.com/o/oauth2/token";
			var post = @"client_id=" + client_id + @"&client_secret=" + client_secret + @"&code=" + deviceCode + @"&grant_type=http://oauth.net/grant_type/device/1.0";
			var res = Post(url, post);

			var json = DynamicJson.Parse(res);
			accessToken = json["access_token"];
			refreshToken = json["refresh_token"];
		}

		private static void SetDeviceInfo(string client_id,
			out string deviceCode, out string userCode, out string verificationURL)
		{
			deviceCode = "";
			userCode = "";
			verificationURL = "";

			//利用許可したいサービスのURL
			//詳細 https://developers.google.com/youtube/v3/guides/authentication?hl=ja#devices
			var scope = @"https://www.googleapis.com/auth/youtube";

			var url = @"https://accounts.google.com/o/oauth2/device/code";
			var post = @"client_id=" + client_id + @"&scope=" + scope;

			var res1 = Post(url, post);

			var json = DynamicJson.Parse(res1);
			deviceCode = json["device_code"];
			userCode = json["user_code"];
			verificationURL = json["verification_url"];
		}

		private static string Post(string url, string post)
		{
			var res = "";
			using (var wc = new System.Net.WebClient())
			{
				wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
				res = wc.UploadString(url, post);
			}

			return res;
		}
	}
}