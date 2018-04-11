using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Common.Configuration;
using System.IO;
using Common.CLightning;
using Common.Logging;
using NBitcoin;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class StartupDefault
    {
		public static void ConfigureServices(IConfiguration conf, IServiceCollection services)
		{
			services.AddMvcCore()
			.AddJsonFormatters()
			.AddAuthorization()
			.AddFormatterMappings();

			var clightning = conf.GetOrDefault("clightning", GetDefaultLightningDirectory());
			if(!LightningConnectionString.TryParse(clightning, out var connectionString))
			{
				throw new ConfigException("Invalid clightning parameter (expected path to lightning-rpc or url)");
			}

			var args = RPCArgs.Parse(conf, Network.RegTest);
			var btcrpc = args.ConfigureRPCClient(Network.RegTest);
			services.AddSingleton(btcrpc);
			RPCArgs.TestRPCAsync(Network.RegTest, btcrpc, default(CancellationToken)).GetAwaiter().GetResult();

			var rpc = new CLightning.CLightningRPCClient(connectionString.ToUri(true), Network.RegTest);
			services.AddSingleton(rpc);
			services.AddSingleton<ILightningInvoiceClient>(rpc);
			try
			{
				rpc.GetInfoAsync().GetAwaiter().GetResult();
			}
			catch(Exception ex)
			{
				throw new ConfigException($"Lightning connection failed ({ex.Message})");
			}
		}

		static string GetDefaultLightningDirectory()
		{
			var home = Environment.GetEnvironmentVariable("HOME");

			if(string.IsNullOrEmpty(home))
				return "";

			if(!string.IsNullOrEmpty(home))
			{
				return Path.Combine(home, ".lightning");
			}
			return "";
		}

		public static void Configure(
		IApplicationBuilder app,
		IHostingEnvironment env,
		ILoggerFactory loggerFactory)
		{
			if(env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			Common.Logging.CommonLogs.Configure(loggerFactory);
		}
	}
}
