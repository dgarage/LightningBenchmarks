using BenchmarkDotNet.Configs;
using System.Linq;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using DockerGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

namespace Lightning.Tests
{
	public class TestProgram
	{
		public static void Main(string[] args)
		{
			if(args[0].StartsWith("generate-alice-pays-bob"))
			{
				Generate(new[] { "Alice", "Bob" });
			}
			else if(args[0] == "alice-pays-bob")
			{
				var o = new AlicePaysBob();
				try
				{
					o.Setup();
					o.RunAlicePayBob().GetAwaiter().GetResult();
				}
				finally
				{
					o.Cleanup();
				}
			}
			else if(args[0] == "bench-alice-pays-bob")
			{
				BenchmarkRunner.Run<AlicePaysBob>(new AllowNonOptimized());
			}

			if(args[0].StartsWith("generate-alice-pays-bob-via-carol"))
			{
				Generate(new[] { "Alice", "Bob", "Carol" });
			}
			else if(args[0] == "alice-pays-bob-via-carol")
			{
				var o = new AlicePaysBobViaCarol();
				try
				{
					o.Setup();
					o.RunAlicePayBobViaCarol().GetAwaiter().GetResult();
					o.Cleanup();
				}
				finally
				{
					o.Cleanup();
				}
			}
			else if(args[0] == "bench-alice-pays-bob-via-carol")
			{
				BenchmarkRunner.Run<AlicePaysBobViaCarol>(new AllowNonOptimized());
			}

			if(args[0].StartsWith("generate-alices-pay-bob"))
			{
				var actors = Enumerable.Range(0, AlicesPayBob.AliceCount)
					.Select(a => "Alice" + a)
					.ToList();
				actors.Add("Bob");
				Generate(actors.ToArray());
			}
			else if(args[0] == "alices-pay-bob")
			{
				var o = new AlicesPayBob();
				try
				{
					o.Setup();
					o.RunAlicesPayBob().GetAwaiter().GetResult();
				}
				finally
				{
					o.Cleanup();
				}
			}
			else if(args[0] == "bench-alices-pay-bob")
			{
				BenchmarkRunner.Run<AlicesPayBob>(new AllowNonOptimized());
			}
		}

		public class AllowNonOptimized : ManualConfig
		{
			public AllowNonOptimized()
			{
				Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS

				Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
				Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
				Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
				Add(CsvMeasurementsExporter.Default);
				Add(RPlotExporter.Default);

				var job = new Job();
				job.Run.TargetCount = 5;
				job.Run.LaunchCount = 1;
				job.Run.WarmupCount = 0;
				Add(job);
			}
		}

		private static void Generate(string[] actors)
		{
			var fragments = FindLocation("docker-fragments");
			var main = Path.Combine(fragments, "main-fragment.yml");
			var actor = File.ReadAllText(Path.Combine(fragments, "actor-fragment.yml"));

			var def = new DockerComposeDefinition("docker-compose.yml", new List<string>());
			def.BuildOutput = Path.Combine(Directory.GetParent(fragments).FullName, "docker-compose.yml");
			def.AddFragmentFile(main);
			for(int i = 0; i < actors.Length; i++)
			{
				def.Fragments.Add(actor.Replace("actor0", actors[i])
					 .Replace("24736", (24736 + i).ToString()));
			}
			def.Build();
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
	}
}
