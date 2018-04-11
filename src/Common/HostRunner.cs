using Microsoft.Extensions.DependencyInjection;
using Common.CLightning;
using Common.Configuration;
using Common.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console.Internal;
using NBitcoin.RPC;
using StandardConfiguration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
	public class ConsoleLogProviderFactory : LogProviderFactory
	{
		ConsoleLoggerProcessor processor = new ConsoleLoggerProcessor();
		public override ILoggerProvider Create()
		{
			return new CustomConsoleLogProvider(processor);
		}
		public override void Dispose()
		{
			if(processor != null)
				processor.Dispose();
		}
	}
	public abstract class LogProviderFactory : IDisposable
	{
		public abstract ILoggerProvider Create();
		public abstract void Dispose();
	}
	public abstract class HostRunner<TStartup> : IDisposable
		where TStartup : class
	{
		public void Run(string[] args)
		{
			RunAsync(args).GetAwaiter().GetResult();
		}

		public LogProviderFactory LogProviderFactory
		{
			get; set;
		}

		public async Task RunAsync(string[] args)
		{
			ServicePointManager.DefaultConnectionLimit = 100;
			IWebHost host = null;
			var processor = new ConsoleLoggerProcessor();

			var logProviderFactory = LogProviderFactory ?? new ConsoleLogProviderFactory();
			var loggerFactory = new LoggerFactory();
			loggerFactory.AddProvider(logProviderFactory.Create());
			var logger = loggerFactory.CreateLogger("Configuration");


			try
			{
				Common.Logging.CommonLogs.Configure(loggerFactory);
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
					   l.AddProvider(logProviderFactory.Create());
				   })
				   .UseStartup<TStartup>()
					.Build();
				Host = host;
				await host.StartAsync();
				var urls = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
				foreach(var url in urls)
				{
					logger.LogInformation("Listening on " + url);
				}
				await host.WaitForShutdownAsync();
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
				logProviderFactory.Dispose();
			}
		}

		public IWebHost Host
		{
			get; set;
		}

		private CLightningRPCClient _RPC;
		public CLightningRPCClient RPC
		{
			get
			{
				if(_RPC == null)
					_RPC = Host.Services.GetRequiredService<CLightningRPCClient>();
				return _RPC;
			}
		}

		private RPCClient _BitcoinRPC;
		public RPCClient BitcoinRPC
		{
			get
			{
				if(_BitcoinRPC == null)
					_BitcoinRPC = Host.Services.GetRequiredService<RPCClient>();
				return _BitcoinRPC;
			}
		}

		public abstract DefaultConfiguration CreateDefaultConfiguration();

		public void Dispose()
		{
			Host.StopAsync().GetAwaiter().GetResult();
			Host.Dispose();
		}
	}
}
