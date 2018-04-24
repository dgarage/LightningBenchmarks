using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Running;
using Common.CLightning;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lightning.Tests
{
	public class AlicePaysBob
	{
		Tester Tester;
		ActorTester Alice;
		ActorTester Bob;

		[GlobalSetup]
		public void Setup()
		{
			Tester = Tester.Create();
			Alice = Tester.CreateActor("Alice");
			Bob = Tester.CreateActor("Bob");
			Tester.Start();
			Tester.CreateChannel(Alice, Bob).GetAwaiter().GetResult();
		}


		[GlobalCleanup]
		public void Cleanup()
		{
			Tester.Dispose();
		}

		[Benchmark(Baseline = true)]
		public async Task RunAlicePayBob()
		{
			await RunAlicePayBobCore(1);
		}

		[Benchmark]
		public async Task RunAlicePayBob5x()
		{
			await RunAlicePayBobCore(5);
		}

		[Benchmark]
		public async Task RunAlicePayBob10x()
		{
			await RunAlicePayBobCore(10);
		}

		[Benchmark]
		public async Task RunAlicePayBob15x()
		{
			await RunAlicePayBobCore(15);
		}

		private Task RunAlicePayBobCore(int concurrent)
		{
			return Task.WhenAll(Enumerable.Range(0, concurrent)
				.Select(async _ =>
				{
					var invoice = await Bob.RPC.CreateInvoice(LightMoney.Satoshis(100));
					await Alice.RPC.SendAsync(invoice.BOLT11);
				}));
		}


		//[Benchmark]
		public async Task RunCreateInvoice()
		{
			await Bob.RPC.CreateInvoice(LightMoney.Satoshis(100));
		}
	}
}
