using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace StockPilot.Web.Auth
{
	public class TwitterEmailRetriever
	{
		internal class TwitterVerifyCredentialsDto
		{
			public string Name { get; set; }

			public string Email { get; set; }
		}

		public string Get(string oauthToken, string oauthTokenSecret, string oauthConsumerKey, string oauthConsumerSecret)
		{
			try
			{
				string oauth_version = "1.0";
				string oauth_signature_method = "HMAC-SHA1";
				string oauthNonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
				string oauthTimestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
				string resourceUrl = "https://api.twitter.com/1.1/account/verify_credentials.json";
				string requestQuery = "include_email=true";
				string baseString = $"oauth_consumer_key={oauthConsumerKey}&oauth_nonce={oauthNonce}&oauth_signature_method={oauth_signature_method}&oauth_timestamp={oauthTimestamp}&oauth_token={oauthToken}&oauth_version={oauth_version}";
				baseString = string.Concat("GET&", Uri.EscapeDataString(resourceUrl) + "&" + Uri.EscapeDataString(requestQuery), "%26", Uri.EscapeDataString(baseString));
				string compositeKey = Uri.EscapeDataString(oauthConsumerSecret) + "&" + Uri.EscapeDataString(oauthTokenSecret);
				string oauthSignature;
				using (HMACSHA1 hasher = new HMACSHA1(Encoding.ASCII.GetBytes(compositeKey)))
				{
					oauthSignature = Convert.ToBase64String(hasher.ComputeHash(Encoding.ASCII.GetBytes(baseString)));
				}
				string authHeader = $"OAuth oauth_consumer_key=\"{Uri.EscapeDataString(oauthConsumerKey)}\", oauth_nonce=\"{Uri.EscapeDataString(oauthNonce)}\", oauth_signature=\"{Uri.EscapeDataString(oauthSignature)}\", oauth_signature_method=\"{Uri.EscapeDataString(oauth_signature_method)}\", oauth_timestamp=\"{Uri.EscapeDataString(oauthTimestamp)}\", oauth_token=\"{Uri.EscapeDataString(oauthToken)}\", oauth_version=\"{Uri.EscapeDataString(oauth_version)}\"";
				ServicePointManager.Expect100Continue = false;
				resourceUrl += "?include_email=true";
				HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(resourceUrl);
				obj.Headers.Add("Authorization", authHeader);
				obj.Method = "GET";
				Stream responseStream = obj.GetResponse().GetResponseStream();
				if (responseStream == null)
				{
					return string.Empty;
				}
				return JsonConvert.DeserializeObject<TwitterVerifyCredentialsDto>(new StreamReader(responseStream).ReadToEnd()).Email;
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}
	}
}
