using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Running;
using Common.CLightning;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lightning.Tests
{
	public class Benchmarks
	{
		//[Params(1, 3, 5)]
		public int AliceCount
		{
			get; set;
		} = 5;

		[Params(1, 3, 5, 7)]
		public int Concurrency
		{
			get; set;
		} = 1;


		//[Params(1, 3, 5)]
		public int CarolsCount
		{
			get; set;
		} = 1;

		Tester Tester;
		ActorTester Alice;
		ActorTester Bob;
		ActorTester[] Carols;
		ActorTester[] Alices;


		#region AlicePayBob
		[GlobalSetup(Target = nameof(RunAlicePaysBob))]
		public void SetupRunAlicesPayBob()
		{
			Tester = Tester.Create();
			Alice = Tester.CreateActor("Alice");
			Bob = Tester.CreateActor("Bob");
			Tester.Start();
			Tester.CreateChannel(Alice, Bob).GetAwaiter().GetResult();
		}
		[Benchmark]
		public async Task RunAlicePaysBob()
		{
			await Task.WhenAll(Enumerable.Range(0, Concurrency)
				.Select(async _ =>
				{
					var invoice = await Bob.RPC.CreateInvoice(LightMoney.Satoshis(100));
					await Alice.RPC.SendAsync(invoice.BOLT11);
				}));
		}
		#endregion

		#region AlicePaysBobViaCarol
		[GlobalSetup(Target = nameof(RunAlicePaysBobViaCarol))]
		public void SetupRunAlicePaysBobViaCarol()
		{
			Tester = Tester.Create();
			Alice = Tester.CreateActor("Alice");
			Bob = Tester.CreateActor("Bob");
			Carols = Enumerable.Range(0, CarolsCount).Select(i=> Tester.CreateActor($"Carol{i}")).ToArray();
			Tester.Start();


			Tester.ConnectPeers(Carols.Concat(new[] { Alice, Bob }).ToArray()).GetAwaiter().GetResult();

			ActorTester previousCarol = null;

			foreach(var carol in Carols)
			{
				Tester.CreateChannel(previousCarol ?? Alice, carol).GetAwaiter().GetResult();
				previousCarol = carol;
			}
			Tester.CreateChannel(previousCarol, Bob).GetAwaiter().GetResult();
			Alice.WaitRouteTo(Bob).GetAwaiter().GetResult();
		}
		//[Benchmark]
		public async Task RunAlicePaysBobViaCarol()
		{
			await Task.WhenAll(Enumerable.Range(0, Concurrency)
				.Select(async _ =>
				{
					var invoice = await Bob.RPC.CreateInvoice(LightMoney.Satoshis(1000));
					await Alice.RPC.SendAsync(invoice.BOLT11);
				}));
		}
		#endregion

		#region AlicesPayBob
		[GlobalSetup(Target = nameof(RunAlicesPayBob))]
		public void SetupAlicesPayBob()
		{
			Tester = Tester.Create();
			Bob = Tester.CreateActor("Bob");
			Alices = new ActorTester[AliceCount];
			for(int i = 0; i < Alices.Length; i++)
			{
				Alices[i] = Tester.CreateActor("Alice" + i);
			}
			Tester.Start();

			foreach(var alice in Alices)
			{
				Tester.ConnectPeers(alice, Bob).GetAwaiter().GetResult();
				Tester.CreateChannel(alice, Bob).GetAwaiter().GetResult();
			}
			Alices[Alices.Length - 1].WaitRouteTo(Bob).GetAwaiter().GetResult();
		}

		//[Benchmark]
		public async Task RunAlicesPayBob()
		{
			await Task.WhenAll(Enumerable.Range(0, Concurrency)
				.Select(async i =>
				{
					var alice = Alices[i];
					var invoice = await Bob.RPC.CreateInvoice(LightMoney.Satoshis(1000));
					await alice.RPC.SendAsync(invoice.BOLT11);
				}));
		}
		#endregion

		[GlobalCleanup]
		public void Cleanup()
		{
			Tester.Dispose();
		}
	}
}
