using Common.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lightning.Alice
{
	public class Logs
	{
		static Logs()
		{
			Configure(new FuncLoggerFactory(n => NullLogger.Instance));
		}
		public static void Configure(ILoggerFactory factory)
		{
			Alice = factory.CreateLogger("Alice");
		}
		public static ILogger Alice
		{
			get; set;
		}
	}
}
