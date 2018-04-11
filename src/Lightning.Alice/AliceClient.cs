using Common;
using Common.CLightning;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lightning.Alice
{
	public class AliceClient : BaseClient
	{
		public AliceClient(Uri uri) : base(uri)
		{

		}

		public void Fund(NodeInfo nodeInfo)
		{
			FundAsync(nodeInfo).GetAwaiter().GetResult();
		}
		public async Task FundAsync(NodeInfo nodeInfo)
		{
			await Send(HttpMethod.Post, "fund?nodeInfo=" + nodeInfo);
		}
	}
}
