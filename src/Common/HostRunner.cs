using Common.Configuration;
using Common.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console.Internal;
using StandardConfiguration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Common
{
    public abstract class HostRunner<TStartup> where TStartup : class
    {
		public void Run(string[] args)
		{
			ServicePointManager.DefaultConnectionLimit = 100;
			IWebHost host = null;
			var processor = new ConsoleLoggerProcessor();
			CustomConsoleLogProvider loggerProvider = new CustomConsoleLogProvider(processor);
			var loggerFactory = new LoggerFactory();
			loggerFactory.AddProvider(loggerProvider);
			var logger = loggerFactory.CreateLogger("Configuration");


			try
			{
				var defaultConf = CreateDefaultConfiguration();
				defaultConf.Logger = logger;
				var conf = defaultConf.CreateConfiguration(args);
				if(conf == null)
					return;

				host = new WebHostBuilder()
				   .UseKestrel()
				   .UseContentRoot(Directory.GetCurrentDirectory())
				   .UseConfiguration(conf)
				   .ConfigureLogging(l =>
				   {
					   l.AddFilter("Microsoft", LogLevel.Error);
					   l.AddFilter("Microsoft.AspNetCore.Antiforgery.Internal", LogLevel.Critical);
					   l.AddProvider(new CustomConsoleLogProvider(processor));
				   })
				   .UseStartup<TStartup>()
					.Build();
				host.StartAsync().GetAwaiter().GetResult();
				var urls = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
				foreach(var url in urls)
				{
					logger.LogInformation("Listening on " + url);
				}
				host.WaitForShutdown();
			}
			catch(ConfigException ex)
			{
				if(!string.IsNullOrEmpty(ex.Message))
					CommonLogs.Configuration.LogError(ex.Message);
			}
			finally
			{
				processor.Dispose();
				if(host != null)
					host.Dispose();
				loggerProvider.Dispose();
			}
		}

		public abstract DefaultConfiguration CreateDefaultConfiguration();
    }
}
