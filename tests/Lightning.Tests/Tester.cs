using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Lightning.Alice;
using NBitcoin;
using Common.CLightning;
using NBitcoin.RPC;

namespace Lightning.Tests
{
	public class Tester : IDisposable
	{
		public static Tester Create([CallerMemberNameAttribute]string scope = null)
		{
			return new Tester(scope);
		}

		string _Directory;
		public Tester(string scope)
		{
			_Directory = scope;
		}

		List<IDisposable> leases = new List<IDisposable>();
		List<IActorTester> actors = new List<IActorTester>();

		public void Start()
		{
			if(Directory.Exists(_Directory))
				Utils.DeleteDirectory(_Directory);
			if(!Directory.Exists(_Directory))
				Directory.CreateDirectory(_Directory);

			foreach(var actor in actors)
			{
				actor.Start();
			}
		}

		public async Task CreateChannel(IActorTester from, IActorTester to)
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

		public async Task ConnectPeers(params ActorTester<AliceRunner, AliceStartup>[] peers)
		{
			var peersById = new Dictionary<string, ActorTester<AliceRunner, AliceStartup>>();
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

		private async Task WaitLNSynched(RPCClient miner, params IActorTester[] testers)
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
		public ActorTester<TRunner, TStartup> CreateActor<TRunner, TStartup>(string name)
			where TRunner : Common.HostRunner<TStartup>, new()
			where TStartup : class
		{
			var actor = new ActorTester<TRunner, TStartup>(_Directory, name, port);
			actors.Add(actor);
			leases.Add(actor);
			port++;
			return actor;
		}

		public void Dispose()
		{
			foreach(var lease in leases)
				lease.Dispose();
		}
	}
}
