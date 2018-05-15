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
using Lightning.Bench;

namespace Lightning.Tests
{
	public class TestProgram
	{
		public static void Main(string[] args)
		{
			BenchmarkRunner.Run<Benchmarks>(new BenchmarkConfiguration());
		}
	}
}
