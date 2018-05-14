using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Common.CLightning;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lightning.Tests
{
	public class AlicePaysBobViaCarol
	{
		Tester Tester;
		ActorTester Alice;
		ActorTester Bob;
		ActorTester Carol;

		[GlobalSetup]
		public void Setup()
		{
			SetupAsync().GetAwaiter().GetResult();
		}

		private async Task SetupAsync()
		{
			Tester = Tester.Create();
			Alice = Tester.CreateActor("Alice");
			Bob = Tester.CreateActor("Bob");
			Carol = Tester.CreateActor("Carol");
			Tester.Start();

			await Tester.ConnectPeers(Alice, Bob, Carol);
			await Tester.CreateChannel(Alice, Carol);
			await Tester.CreateChannel(Carol, Bob);
			await Alice.WaitRouteTo(Bob);
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			Tester.Dispose();
		}

		[Benchmark]
		public async Task RunAlicePayBobViaCarol()
		{
			await RunAlicePayBobViaCarol(1);
		}

		private Task RunAlicePayBobViaCarol(int concurrent)
		{
			return Task.WhenAll(Enumerable.Range(0, concurrent)
				.Select(async _ =>
				{
					var invoice = await Bob.RPC.CreateInvoice(LightMoney.Satoshis(1000));
					await Alice.RPC.SendAsync(invoice.BOLT11);
				}));
		}
		
	}
}
