using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Common.CLightning;
using NBitcoin.RPC;
using System.Diagnostics;
using DockerGenerator;
using System.Runtime.InteropServices;
using Lightning.Bench;

namespace Lightning.Tests
{
	public class Tester : IDisposable
	{
		public static Tester Create([CallerMemberNameAttribute]string scope = null)
		{
			return new Tester(scope);
		}

		string _Directory;
		CommandLineBase cmd;
		public Tester(string scope)
		{
			_Directory = scope;
			cmd = CommandLineFactory.CreateShell();
			cmd.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "lightningbench");
			EnsureCreated(cmd.WorkingDirectory);
		}

		List<IDisposable> leases = new List<IDisposable>();
		List<ActorTester> actors = new List<ActorTester>();

		public void Start()
		{
			EnsureCreated(_Directory);

			if(File.Exists(Path.Combine(cmd.WorkingDirectory, "docker-compose.yml")))
				cmd.Run("docker-compose down --v --remove-orphans");
			cmd.Run("docker kill $(docker ps -f 'name = lightningbench_ *' -q)");
			GenerateDockerCompose(actors.Select(a => a.P2PHost).ToArray());
			cmd.Run("docker-compose down --v --remove-orphans"); // Makes really sure we start clean
			cmd.AssertRun("docker-compose up -d dev");

			foreach(var actor in actors)
			{
				actor.Start();
			}
		}

		private void EnsureCreated(string dir)
		{
			if(Directory.Exists(dir))
				Utils.DeleteDirectory(dir);
			if(!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
		}



		private void GenerateDockerCompose(string[] actors)
		{
			var fragments = FindLocation("docker-fragments");
			var main = Path.Combine(fragments, "main-fragment.yml");
			var actor = File.ReadAllText(Path.Combine(fragments, "actor-fragment.yml"));

			var def = new DockerComposeDefinition("docker-compose.yml", new List<string>());
			def.BuildOutput = Path.Combine(cmd.WorkingDirectory, "docker-compose.yml");
			def.AddFragmentFile(main);
			for(int i = 0; i < actors.Length; i++)
			{
				def.Fragments.Add(actor.Replace("actor0", actors[i])
					 .Replace("24736", (24736 + i).ToString()));
			}
			def.Build();
			Console.WriteLine($"// Generated docker-compose: {def.BuildOutput}");
		}

		private static string FindLocation(string path)
		{
			while(true)
			{
				if(Directory.Exists(path))
					return path;
				path = Path.Combine("..", path);
			}
		}


		public async Task CreateChannel(ActorTester from, ActorTester to)
		{
			var miner = from.BitcoinRPC;
			var toInfo = await to.RPC.GetInfoAsync();
			while(true)
			{
				try
				{
					var skippedStates = new[] { "ONCHAIN", "CHANNELD_SHUTTING_DOWN", "CLOSINGD_SIGEXCHANGE", "CLOSINGD_COMPLETE", "FUNDING_SPEND_SEEN" };
					var channel = (await from.RPC.ListPeersAsync())
								.Where(p => p.Id == toInfo.Id)
								.SelectMany(p => p.Channels)
								.Where(c => !skippedStates.Contains(c.State ?? ""))
								.FirstOrDefault();
					switch(channel?.State)
					{
						case null:
							await WaitLNSynched(miner, to, from);
							var toNodeInfo = new NodeInfo(toInfo.Id, to.P2PHost, toInfo.Port);
							await from.RPC.ConnectAsync(toNodeInfo);

							while(true)
							{
								var funds = await from.RPC.ListFundsAsync();
								if(funds.Outputs.Any(o => o.Status == "unconfirmed"))
								{
									await miner.GenerateAsync(1);
									continue;
								}
								if(!funds.Outputs.Any(o => o.Status == "confirmed"))
								{
									var address = await from.RPC.NewAddressAsync();
									await miner.SendToAddressAsync(address, Money.Coins(49.0m));
									await miner.GenerateAsync(1);
									await WaitLNSynched(miner, to, from);
									continue;
								}
								break;
							}
							await from.RPC.FundChannelAsync(toNodeInfo, Money.Satoshis(16777215));

							break;
						case "CHANNELD_AWAITING_LOCKIN":
							await miner.GenerateAsync(1);
							await WaitLNSynched(miner, to, from);
							break;
						case "CHANNELD_NORMAL":
							Console.WriteLine($"// Channel established: {from} => {to}");
							return;
						default:
							throw new NotSupportedException(channel?.State ?? "");
					}
				}
				catch(RPCException ex) when(ex.RPCCode == RPCErrorCode.RPC_WALLET_INSUFFICIENT_FUNDS)
				{
					await miner.GenerateAsync(101);
				}
			}
		}

		public async Task ConnectPeers(params ActorTester[] peers)
		{
			var peersById = new Dictionary<string, ActorTester>();
			HashSet<string> connected = new HashSet<string>();
			foreach(var peer in peers)
			{
				var peerInfo = await peer.RPC.GetInfoAsync();
				peersById.TryAdd(peerInfo.Id, peer);
				var peerNodeInfo = new NodeInfo(peerInfo.Id, peer.P2PHost, peerInfo.Port);
				foreach(var peer2 in peers)
				{
					if(peer2.P2PHost == peer.P2PHost)
						continue;
					if(connected.Add(string.Join(',', new[] { peer2.P2PHost, peer.P2PHost }.OrderBy(s => s))))
					{
						await peer2.RPC.ConnectAsync(peerNodeInfo);
					}
				}
			}

			foreach(var peer in peers)
			{
				while(true)
				{
					var peerInfo = await peer.RPC.ListPeersAsync();
					var known = peerInfo
						.Where(pi => peersById.ContainsKey(pi.Id))
						.Count();
					if(known == peersById.Count - 1)
						break;
				}
			}
		}

		private async Task WaitLNSynched(RPCClient miner, params ActorTester[] testers)
		{
			while(true)
			{
				var blockCount = await miner.GetBlockCountAsync();
				foreach(var info in testers.Select(t => t.RPC.GetInfoAsync()))
				{
					if((await info).BlockHeight != blockCount)
					{
						await Task.Delay(1000);
						continue;
					}
				}
				break;
			}
		}

		int port = 24736;
		public ActorTester CreateActor(string name)
		{
			var actor = new ActorTester(_Directory, name, port);
			actors.Add(actor);
			port++;
			return actor;
		}

		public void Dispose()
		{
			foreach(var lease in leases)
				lease.Dispose();
			Console.WriteLine("// Tearing down docker-compose...");
			cmd.AssertRun("docker-compose down --v");
			Console.WriteLine("// Docker-compose teared down ");
			File.Delete(Path.Combine(cmd.WorkingDirectory, "docker-compose.yml"));
		}
	}
}
