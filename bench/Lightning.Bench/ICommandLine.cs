using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Lightning.Bench
{
	public class CommandLineResult
	{
		public int ExitCode
		{
			get; set;
		}
		public string Output
		{
			get; set;
		}
	}
	public class CommandLineException : Exception
	{
		public int ExitCode
		{
			get; set;
		}
		public string Output
		{
			get; set;
		}
		public override string Message => $"Exit code {ExitCode}: {Output}";
	}
	public abstract class CommandLineBase
	{
		public string WorkingDirectory
		{
			get; set;
		}
		public abstract CommandLineResult Run(string cmd);
		protected CommandLineResult Run(ProcessStartInfo processInfo)
		{
			processInfo.WorkingDirectory = WorkingDirectory;
			processInfo.RedirectStandardOutput = true;
			processInfo.UseShellExecute = false;
			processInfo.CreateNoWindow = true;
			var process = new Process()
			{
				StartInfo = processInfo
			};

			StringBuilder builder = new StringBuilder();
			process.OutputDataReceived += (s, r) => builder.AppendLine(r?.Data ?? string.Empty);
			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit(30000);
			if(!process.HasExited)
				process.Kill();
			return new CommandLineResult() { ExitCode = process.ExitCode, Output = builder.ToString() };
		}
		public void AssertRun(string cmd)
		{
			var result = Run(cmd);
			if(result.ExitCode != 0)
			{
				throw new CommandLineException() { ExitCode = result.ExitCode, Output = result.Output };
			}
		}
	}

	public class CommandLineFactory
	{
		public static CommandLineBase CreateShell()
		{
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return new BashCommandLine();
			}
			else
			{
				return new PowershellCommandLine();
			}
		}
	}

	public class BashCommandLine : CommandLineBase
	{
		public override CommandLineResult Run(string cmd)
		{
			var escapedArgs = cmd.Replace("\"", "\\\"");
			return this.Run(new ProcessStartInfo
			{
				FileName = "/bin/bash",
				Arguments = $"-c \"{escapedArgs}\""
			});
		}
	}

	public class PowershellCommandLine : CommandLineBase
	{
		public override CommandLineResult Run(string cmd)
		{
			var escapedArgs = cmd.Replace("\"", "\\\"");
			return this.Run(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = $"-Command \"{escapedArgs}\""
			});
		}
	}
}
