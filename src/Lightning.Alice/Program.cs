using Common;
using Common.CLightning;
using Common.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console.Internal;
using NBitcoin.RPC;
using StandardConfiguration;
using System;

namespace Lightning.Alice
{
	class Program
	{
		static void Main(string[] args)
		{
			new AliceRunner().Run(args);
		}
	}

	public class AliceRunner : HostRunner<AliceStartup>
	{
		public override StandardConfiguration.DefaultConfiguration CreateDefaultConfiguration()
		{
			return new DefaultConfiguration();
		}
	}

	public class AliceStartup
	{
		private IConfiguration _Configuration;

		public AliceStartup(IConfiguration configuration)
		{
			_Configuration = configuration;
		}
		public void ConfigureServices(IServiceCollection services)
		{
			StartupDefault.ConfigureServices(_Configuration, services);
		}

		public void Configure(
		IApplicationBuilder app,
		IHostingEnvironment env,
		ILoggerFactory loggerFactory)
		{
			StartupDefault.Configure(app, env, loggerFactory);
			Logs.Configure(loggerFactory);
		}
	}
}
