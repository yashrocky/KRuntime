using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Net.Runtime
{
    public class Executable
    {
        public Executable(string path, string workingDirectory)
        {
            Path = path;
            WorkingDirectory = workingDirectory;
        }

        public bool IsAvailable
        {
            get
            {
                return File.Exists(Path);
            }
        }

        public string WorkingDirectory { get; private set; }

        public string Path { get; private set; }

        public Process Execute(string arguments, params object[] args)
        {
            return Execute(s => { Console.WriteLine(s); return true; },
                           s => { Console.Error.WriteLine(s); return true; },
                           arguments,
                           args);
        }

        public Process Execute(Func<string, bool> onWriteOutput, Func<string, bool> onWriteError, string arguments, params object[] args)
        {
            Process process = CreateProcess(arguments, args);

            process.EnableRaisingEvents = true;

            var errorBuffer = new StringBuilder();
            var outputBuffer = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    if (onWriteOutput(e.Data))
                    {
                        outputBuffer.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    if (onWriteError(e.Data))
                    {
                        errorBuffer.AppendLine(e.Data);
                    }
                }
            };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            return process;
        }

        internal Process CreateProcess(string arguments, object[] args)
        {
            return CreateProcess(String.Format(arguments, args));
        }

        internal Process CreateProcess(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path,
                WorkingDirectory = WorkingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
#if NET45
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                ErrorDialog = false,
#endif
                Arguments = arguments
            };

            var process = new Process()
            {
                StartInfo = psi
            };

            return process;
        }
    }
}