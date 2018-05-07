using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lightning.Tests
{
    public class Utils
    {
		public static int FreeTcpPort()
		{
			TcpListener l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			int port = ((IPEndPoint)l.LocalEndpoint).Port;
			l.Stop();
			return port;
		}
		public static string GetVariable(string variableName, string defaultValue)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			return string.IsNullOrEmpty(value) ? defaultValue : value;
		}
		public static void DeleteDirectory(string destinationDir)
		{
			const int magicDust = 10;
			for(var gnomes = 1; gnomes <= magicDust; gnomes++)
			{
				try
				{
					Directory.Delete(destinationDir, true);
				}
				catch(DirectoryNotFoundException)
				{
					return;  // good!
				}
				catch(IOException)
				{
					if(gnomes == magicDust)
						throw;
					// System.IO.IOException: The directory is not empty
					System.Diagnostics.Debug.WriteLine("Gnomes prevent deletion of {0}! Applying magic dust, attempt #{1}.", destinationDir, gnomes);

					// see http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true for more magic
					Thread.Sleep(100);
					continue;
				}
				catch(UnauthorizedAccessException)
				{
					if(gnomes == magicDust)
						throw;
					// Wait, maybe another software make us authorized a little later
					System.Diagnostics.Debug.WriteLine("Gnomes prevent deletion of {0}! Applying magic dust, attempt #{1}.", destinationDir, gnomes);

					// see http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true for more magic
					Thread.Sleep(100);
					continue;
				}
				return;
			}
			// depending on your use case, consider throwing an exception here
		}
	}
}
