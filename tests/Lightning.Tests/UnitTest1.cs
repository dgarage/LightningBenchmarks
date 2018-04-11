using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Lightning.Tests
{
	public class UnitTest1
	{
		public UnitTest1(ITestOutputHelper helper)
		{
			if(helper != null)
			{
				Logs.Tester = new XUnitLog(helper) { Name = "Tests" };
				Logs.LogProvider = new XUnitLogProvider(helper);
			}
			else
			{
				Logs.Tester = NullLogger.Instance;
			}
		}
		[Fact]
		public void Test1()
		{
			using(var tester = Tester.Create())
			{
				tester.Start();
			}
		}
	}
}
