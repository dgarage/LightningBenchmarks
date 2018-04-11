using Common.CLightning;
using Lightning.Alice;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lightning.Tests
{
	public interface IActorTester
	{
		string P2PHost
		{
			get;
		}
		RPCClient BitcoinRPC
		{
			get;
		}
		CLightningRPCClient RPC
		{
			get;
		}
		void Start();
	}
	public class ActorTester<TRunner, TStartup> : IActorTester, IDisposable
			where TRunner : Common.HostRunner<TStartup>, new()
			where TStartup : class
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

		private static object GetVariable(string defaultValue, string variableName)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			return string.IsNullOrEmpty(value) ? defaultValue : value;
		}

		List<IDisposable> leases = new List<IDisposable>();

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
			Runner = new TRunner();
			if(Logs.LogProvider != null)
			{
				Runner.LogProviderFactory = new XUnitLogProviderFactory(Logs.LogProvider);
			}
			Running = Runner.RunAsync(parameters);
			leases.Add(Runner);
		}

		Task Running;

		public TRunner Runner
		{
			get; set;
		}

		public RPCClient BitcoinRPC => Runner.BitcoinRPC;

		public CLightningRPCClient RPC => Runner.RPC;

		public void Dispose()
		{
			foreach(var lease in leases)
			{
				lease.Dispose();
			}
			if(Running != null)
				Running.Wait();
		}
	}
}
