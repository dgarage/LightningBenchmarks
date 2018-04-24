using CommandLine;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Common
{
	public class DefaultConfigurationBase : StandardConfiguration.DefaultConfiguration
	{
		public string Actor
		{
			get;
			set;
		}
		public int DefaultPort
		{
			get;
		} = 0; // unused
		public override string EnvironmentVariablePrefix => $"LIGHTNINGBENCH_{Actor.ToUpperInvariant()}_";

		protected override CommandLineApplication CreateCommandLineApplicationCore()
		{
			CommandLineApplication app = new CommandLineApplication(true)
			{
				FullName = $"{Actor}",
				Name = Actor
			};
			app.HelpOption("-? | -h | --help");
			app.Option("--clightning", "Connection string to a clightning instance (eg. tcp://server/ or /root/.lightning/lightning-rpc)", CommandOptionType.SingleValue);
			app.Option($"--rpcuser", $"The RPC user (default: empty)", CommandOptionType.SingleValue);
			app.Option($"--rpcpassword", $"The RPC password (default: empty)", CommandOptionType.SingleValue);
			app.Option($"--rpccookiefile", $"The RPC cookiefile (default: {RPCClient.GetDefaultCookieFilePath(Network.RegTest)})", CommandOptionType.SingleValue);
			app.Option($"--rpcurl", $"The RPC server url (default: http://127.0.0.1:{Network.RegTest.RPCPort}/)", CommandOptionType.SingleValue);
			return app;
		}


		protected override string GetDefaultConfigurationFile(IConfiguration conf)
		{
			var dataDir = conf["datadir"];
			if(dataDir == null)
				dataDir = GetDefaultDataDir(conf);
			return Path.Combine(dataDir, $"{Actor.ToLowerInvariant()}.conf");
		}

		protected override string GetDefaultConfigurationFileTemplate(IConfiguration conf)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine("#clightning=/root/.lightning/lightning-rpc");
			return builder.ToString();
		}

		protected override string GetDefaultDataDir(IConfiguration conf)
		{
			return StandardConfiguration.DefaultDataDirectory.GetDirectory("LightningBenchmarks", Actor);
		}

		protected override IPEndPoint GetDefaultEndpoint(IConfiguration conf)
		{
			return new IPEndPoint(IPAddress.Parse("127.0.0.1"), DefaultPort);
		}

	}
}
