using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace Lightning.Bench
{
	public class BenchmarkConfiguration : ManualConfig
	{
		public BenchmarkConfiguration()
		{
			Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS

			Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
			Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
			Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
			Add(CsvMeasurementsExporter.Default);
			Add(RPlotExporter.Default);

			var job = new Job();
			job.Run.TargetCount = 100;
			job.Run.LaunchCount = 1;
			job.Run.WarmupCount = 0;
			job.Run.InvocationCount = 16;
			Add(job);
		}
	}
}
