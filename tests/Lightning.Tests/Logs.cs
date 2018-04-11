using Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace Lightning.Tests
{

    public class XUnitLogProvider : ILoggerProvider
    {
        ITestOutputHelper _Helper;
        public XUnitLogProvider(ITestOutputHelper helper)
        {
            _Helper = helper;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLog(_Helper) { Name = categoryName };
        }

        public void Dispose()
        {

        }
    }

	public class XUnitLogProviderFactory : LogProviderFactory
	{
		ILoggerProvider output;
		public XUnitLogProviderFactory(ILoggerProvider output)
		{
			this.output = output;
		}
		public override ILoggerProvider Create()
		{
			return output;
		}

		public override void Dispose()
		{
			
		}
	}

	public class XUnitLog : ILogger, IDisposable
    {
        ITestOutputHelper _Helper;
        public XUnitLog(ITestOutputHelper helper)
        {
            _Helper = helper;
        }

        public string Name
        {
            get; set;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {

        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(formatter(state, exception));
            if (exception != null)
            {
                builder.AppendLine();
                builder.Append(exception.ToString());
            }
            LogInformation(builder.ToString());
        }

        public void LogInformation(string msg)
        {
            if (msg != null)
                try
                {
                    _Helper.WriteLine(DateTimeOffset.UtcNow + " :" + Name + ":   " + msg);
                }
                catch { }
        }
    }
    public class Logs
    {
        public static ILogger Tester
        {
            get; set;
        }
        public static XUnitLogProvider LogProvider
        {
            get;
            set;
        }
    }
}
