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
		public CommandLineResult Run(string cmd)
		{
			return Run(cmd, true);
		}
		public abstract CommandLineResult Run(string cmd, bool ignoreError);
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
			Console.WriteLine($"// Running {processInfo.FileName} {processInfo.Arguments}");
			StringBuilder builder = new StringBuilder();
			process.OutputDataReceived += (s, r) => 
			{
				Console.WriteLine("// " + r?.Data);
				builder.AppendLine(r?.Data ?? string.Empty);
			};
			process.ErrorDataReceived += (s, r) =>
			{
				Console.WriteLine("// " + r?.Data);
				builder.AppendLine(r?.Data ?? string.Empty);
			};
			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit(30000);
			if(!process.HasExited)
				process.Kill();
			return new CommandLineResult() { ExitCode = process.ExitCode, Output = builder.ToString() };
		}
		public void AssertRun(string cmd)
		{
			var result = Run(cmd, false);
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
		public override CommandLineResult Run(string cmd, bool ignoreError)
		{
			if(ignoreError)
				cmd += " || true";
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
		public override CommandLineResult Run(string cmd, bool ignoreError)
		{
			if(ignoreError)
				cmd += "; $LastExitCode = 0";
			var escapedArgs = cmd.Replace("\"", "\\\"");
			return this.Run(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = $"-Command \"{escapedArgs}\""
			});
		}
	}
}
