using Common;
using Common.CLightning;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin.RPC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lightning.Tests
{
	public class ActorTester
	{
		public int Port
		{
			get; set;
		}
		public string Directory
		{
			get; set;
		}
		public string RPCUser
		{
			get;
			set;
		}
		public string RPCPassword
		{
			get;
			private set;
		}
		public string RPCURL
		{
			get;
			private set;
		}
		public string CLightning
		{
			get;
			private set;
		}

		public string P2PHost
		{
			get; set;
		}

		public ActorTester(string baseDirectory, string name, int lightningPort)
		{
			RPCUser = Utils.GetVariable("TESTS_RPCUSER", "ceiwHEbqWI83");
			RPCPassword = Utils.GetVariable("TESTS_RPCPASSWORD", "DwubwWsoo3");
			RPCURL = Utils.GetVariable("TESTS_RPCURL", "http://127.0.0.1:24735/");
			CLightning = Utils.GetVariable("TESTS_CLIGHTNING", $"tcp://127.0.0.1:{lightningPort}/");
			Directory = Path.Combine(baseDirectory, name);
			P2PHost = name;
			Port = Utils.FreeTcpPort();
		}

		public async Task WaitRouteTo(ActorTester destination)
		{
			var info = await destination.RPC.GetInfoAsync();
			int blockToMine = 6;
			while(true)
			{
				var route = await RPC.GetRouteAsync(info.Id, LightMoney.Satoshis(100), 0.0);
				if(route != null)
					break;
				await Task.Delay(1000);
				BitcoinRPC.Generate(blockToMine);
				blockToMine = 1;
			}
			Console.WriteLine($"// Route from {this.P2PHost} to {destination.P2PHost} can now be used");
		}


		public void Start()
		{
			StringBuilder config = new StringBuilder();
			config.AppendLine($"port={Port}");
			config.AppendLine($"rpcuser={RPCUser}");
			config.AppendLine($"rpcpassword={RPCPassword}");
			config.AppendLine($"rpcurl={RPCURL}");
			config.AppendLine($"clightning={CLightning}");
			Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

			string conf = System.IO.Path.Combine(Directory, "settings.conf");
			System.IO.Directory.CreateDirectory(Directory);
			File.Create(conf).Close();
			File.WriteAllText(conf, config.ToString());
			var parameters = new[] { "--datadir", Directory, "--conf", conf };

			var iconf = new DefaultConfigurationBase() { Actor = P2PHost }.CreateConfiguration(parameters);
			var services = new ServiceCollection();
			StartupDefault.ConfigureServices(iconf, services);
			ServiceProvider = services.BuildServiceProvider();
		}

		public IServiceProvider ServiceProvider
		{
			get; set;
		}

		public RPCClient BitcoinRPC => ServiceProvider.GetRequiredService<RPCClient>();

		public CLightningRPCClient RPC => ServiceProvider.GetRequiredService<CLightningRPCClient>();

		ConcurrentDictionary<int, CLightningRPCClient> _ReuseClients = new ConcurrentDictionary<int, CLightningRPCClient>();
		public CLightningRPCClient GetRPC(int i)
		{
			if(!_ReuseClients.TryGetValue(i, out var client))
			{
				client = new CLightningRPCClient(RPC.Address, RPC.Network);
				client.ReuseSocket = true;
				_ReuseClients.TryAdd(i, client);
			}
			return client;
		}

		public override string ToString()
		{
			return P2PHost;
		}
	}
}
