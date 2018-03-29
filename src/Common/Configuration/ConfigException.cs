using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Configuration
{
	public class ConfigException : Exception
	{
		public ConfigException()
		{

		}
		public ConfigException(string message) : base(message)
		{

		}
	}
}
