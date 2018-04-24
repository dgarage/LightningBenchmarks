using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Common.CLightning;
using Lightning.Alice;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lightning.Tests
{
	public class AlicePaysBobViaCarol
	{
		Tester Tester;
		ActorTester<AliceRunner, AliceStartup> Alice;
		ActorTester<AliceRunner, AliceStartup> Bob;
		ActorTester<AliceRunner, AliceStartup> Carol;

		[GlobalSetup]
		public void Setup()
		{
			SetupAsync().GetAwaiter().GetResult();
		}

		private async Task SetupAsync()
		{
			Tester = Tester.Create();
			Alice = Tester.CreateActor<Lightning.Alice.AliceRunner, Lightning.Alice.AliceStartup>("Alice");
			Bob = Tester.CreateActor<Lightning.Alice.AliceRunner, Lightning.Alice.AliceStartup>("Bob");
			Carol = Tester.CreateActor<Lightning.Alice.AliceRunner, Lightning.Alice.AliceStartup>("Carol");
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

		[Benchmark(Baseline = true)]
		public async Task RunAlicePayBobViaCarol()
		{
			await RunAlicePayBobViaCarol(1);
		}

		[Benchmark]
		public async Task RunAlicePayBobViaCarol5x()
		{
			await RunAlicePayBobViaCarol(5);
		}

		[Benchmark]
		public async Task RunAlicePayBobViaCarol10x()
		{
			await RunAlicePayBobViaCarol(10);
		}

		[Benchmark]
		public async Task RunAlicePayBobViaCarol15x()
		{
			await RunAlicePayBobViaCarol(15);
		}

		private Task RunAlicePayBobViaCarol(int concurrent)
		{
			return Task.WhenAll(Enumerable.Range(0, concurrent)
				.Select(async _ =>
				{
					var invoice = await Bob.Runner.RPC.CreateInvoice(LightMoney.Satoshis(1000));
					await Alice.Runner.RPC.SendAsync(invoice.BOLT11);
				}));
		}
		
	}
}
