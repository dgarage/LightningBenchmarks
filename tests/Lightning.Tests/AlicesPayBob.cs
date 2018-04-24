using BenchmarkDotNet.Attributes;
using Common.CLightning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightning.Tests
{
	public class AlicesPayBob
	{
		public const int AliceCount = 15;
		Tester Tester;
		ActorTester[] Alices = new ActorTester[AliceCount];
		ActorTester Bob;

		[GlobalSetup]
		public void Setup()
		{
			SetupAsync().GetAwaiter().GetResult();
		}

		private async Task SetupAsync()
		{
			Tester = Tester.Create();
			Bob = Tester.CreateActor("Bob");
			for(int i = 0; i < Alices.Length; i++)
			{
				Alices[i] = Tester.CreateActor("Alice" + i);
			}
			Tester.Start();

			foreach(var alice in Alices)
			{
				await Tester.ConnectPeers(alice, Bob);
				await Tester.CreateChannel(alice, Bob);
			}
			await Alices[Alices.Length - 1].WaitRouteTo(Bob);
		}

		[Benchmark(Baseline = true)]
		public async Task RunAlicesPayBob()
		{
			await RunAlicesPayBobCore(1);
		}

		[Benchmark]
		public async Task RunAlicesPayBob5x()
		{
			await RunAlicesPayBobCore(5);
		}

		[Benchmark]
		public async Task RunAlicesPayBob10x()
		{
			await RunAlicesPayBobCore(10);
		}

		[Benchmark]
		public async Task RunAlicesPayBob15x()
		{
			await RunAlicesPayBobCore(15);
		}

		private Task RunAlicesPayBobCore(int aliceCounts)
		{
			return Task.WhenAll(Enumerable.Range(0, aliceCounts)
				.Select(async i =>
				{
					var alice = Alices[i];
					var invoice = await Bob.RPC.CreateInvoice(LightMoney.Satoshis(1000));
					await alice.RPC.SendAsync(invoice.BOLT11);
				}));
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			Tester.Dispose();
		}
	}
}
