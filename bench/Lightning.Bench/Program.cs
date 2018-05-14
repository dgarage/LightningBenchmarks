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
			if(args[0] == "alice-pays-bob")
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


			if(args[0] == "alice-pays-bob-via-carol")
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


			if(args[0] == "alices-pay-bob")
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

	}
}
