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
		//[Params(4, 7, 10)]
		public int AliceCount
		{
			get; set;
		} = 5;

		[Params(20, 40, 60, 80)]
		public int Concurrency
		{
			get; set;
		} = 1;

		public int TotalPayments
		{
			get; set;
		} = 100;

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
			Tester.CreateChannels(new[] { Alice }, new[] { Bob }).GetAwaiter().GetResult();
		}
		[Benchmark]
		public async Task RunAlicePaysBob()
		{
			int paymentsLeft = TotalPayments;
			await Task.WhenAll(Enumerable.Range(0, Concurrency)
				.Select(async _ =>
				{
					while(Interlocked.Decrement(ref paymentsLeft) >= 0)
					{
						var invoice = await Bob.GetRPC(_).CreateInvoice(LightMoney.Satoshis(100));
						await Alice.GetRPC(_).SendAsync(invoice.BOLT11);
					}
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
			Carols = Enumerable.Range(0, CarolsCount).Select(i => Tester.CreateActor($"Carol{i}")).ToArray();
			Tester.Start();

			Tester.ConnectPeers(Carols.Concat(new[] { Alice, Bob }).ToArray()).GetAwaiter().GetResult();

			var froms = new[] { Alice }.Concat(Carols).ToArray();
			var tos = Carols.Concat(new[] { Bob }).ToArray();

			Tester.CreateChannels(froms, tos).GetAwaiter().GetResult();
			Alice.WaitRouteTo(Bob).GetAwaiter().GetResult();
		}
		//[Benchmark]
		public async Task RunAlicePaysBobViaCarol()
		{
			int paymentsLeft = TotalPayments;
			await Task.WhenAll(Enumerable.Range(0, Concurrency)
				.Select(async _ =>
				{
					while(Interlocked.Decrement(ref paymentsLeft) >= 0)
					{
						var invoice = await Bob.GetRPC(_).CreateInvoice(LightMoney.Satoshis(100));
						await Alice.GetRPC(_).SendAsync(invoice.BOLT11);
					}
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

			var bobs = Enumerable.Range(0, Alices.Length).Select(_ => Bob).ToArray();
			Task.WaitAll(Alices.Select(a => Tester.ConnectPeers(a, Bob)).ToArray());
			Tester.CreateChannels(Alices, bobs).GetAwaiter().GetResult();
			Task.WaitAll(Alices.Select(a => Tester.ConnectPeers(a, Bob)).ToArray());
			Task.WaitAll(Alices.Select(a => a.WaitRouteTo(Bob)).ToArray());
		}

		//[Benchmark]
		public async Task RunAlicesPayBob()
		{
			int paymentsLeft = TotalPayments;
			await Task.WhenAll(Enumerable.Range(0, Concurrency)
				.SelectMany(_ => Enumerable.Range(0, AliceCount))
				.Select(async _ =>
				{
					while(Interlocked.Decrement(ref paymentsLeft) >= 0)
					{
						var alice = Alices[_];
						var invoice = await Bob.GetRPC(_).CreateInvoice(LightMoney.Satoshis(1000));
						await alice.GetRPC(_).SendAsync(invoice.BOLT11);
					}
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
