using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class BaseClient
    {
		static HttpClient _Client = new HttpClient();
		public BaseClient(Uri uri)
		{
			if(uri == null)
				throw new ArgumentNullException(nameof(uri));
			_Uri = uri;
		}


		private readonly Uri _Uri;
		public Uri Uri
		{
			get
			{
				return _Uri;
			}
		}
		protected Uri GetFullUri(string partialUrl)
		{
			var uri = _Uri.AbsoluteUri;
			if(!uri.EndsWith("/", StringComparison.InvariantCultureIgnoreCase))
				uri += "/";
			return new Uri(uri + partialUrl);
		}

		protected HttpRequestMessage CreateMessage(HttpMethod method, string path)
		{
			var uri = GetFullUri(path);
			var request = new HttpRequestMessage(method, uri);
			return request;
		}

		protected async Task<T> Send<T>(HttpMethod method, string path, CancellationToken cancellation = default(CancellationToken))
		{
			var request = CreateMessage(method, path);
			var message = await _Client.SendAsync(request, cancellation);
			message.EnsureSuccessStatusCode();
			var content = await message.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(content);
		}
		protected async Task Send(HttpMethod method, string path, CancellationToken cancellation = default(CancellationToken))
		{
			var request = CreateMessage(method, path);
			var message = await _Client.SendAsync(request, cancellation);
			message.EnsureSuccessStatusCode();
		}
	}
}
