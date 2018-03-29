using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Logging
{
	public class CommonLogs
	{
		static CommonLogs()
		{
			Configure(new FuncLoggerFactory(n => NullLogger.Instance));
		}
		public static void Configure(ILoggerFactory factory)
		{
			Configuration = factory.CreateLogger("Configuration");
		}
		public static ILogger Configuration
		{
			get; set;
		}
	}
}
