using CommandLine;
using Common;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Lightning.Alice
{
    public class DefaultConfiguration : DefaultConfigurationBase
	{

		public override string Actor => "Alice";

		public override int DefaultPort => DefaultPorts.Alice;

		public override string Description => "Alice sends money to Bob";		
	}
}
