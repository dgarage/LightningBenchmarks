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
			while(true)
			{
				try
				{

					var skippedStates = new[] { "ONCHAIN", "CHANNELD_SHUTTING_DOWN", "CLOSINGD_SIGEXCHANGE", "CLOSINGD_COMPLETE", "FUNDING_SPEND_SEEN" };
					var channel = (await from.RPC.ListPeersAsync())
								.SelectMany(p => p.Channels)
								.Where(c => !skippedStates.Contains(c.State ?? ""))
								.FirstOrDefault();
					switch(channel?.State)
					{
						case null:
							await WaitLNSynched(miner, to, from);
							var toInfo = await to.RPC.GetInfoAsync();
							var toNodeInfo = new NodeInfo(toInfo.Id, to.P2PHost, toInfo.Port);
							await from.RPC.ConnectAsync(toNodeInfo);
							var address = await from.RPC.NewAddressAsync();
							await miner.SendToAddressAsync(address, Money.Coins(49.0m));
							miner.Generate(1);
							await WaitLNSynched(miner, to, from);
							int i = 0;
							while(true)
							{
								try
								{
									await Task.Delay(1000);
									await from.RPC.FundChannelAsync(toNodeInfo, Money.Satoshis(16777215));
									break;
								}
								catch when (i < 5) { }
								i++;
							}
							break;
						case "CHANNELD_AWAITING_LOCKIN":
							miner.Generate(1);
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
					miner.Generate(101);
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
