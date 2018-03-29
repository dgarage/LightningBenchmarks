using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Console.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Logging
{
	public class CustomConsoleLogProvider : ILoggerProvider
	{
		ConsoleLoggerProcessor _Processor;
		public CustomConsoleLogProvider(ConsoleLoggerProcessor processor)
		{
			_Processor = processor;
		}
		public ILogger CreateLogger(string categoryName)
		{
			return new CustomConsoleLogger(categoryName, (a, b) => true, false, _Processor);
		}

		public void Dispose()
		{

		}
	}
}
