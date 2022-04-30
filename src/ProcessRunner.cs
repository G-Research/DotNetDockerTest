using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DockerTesting
{
    public class ProcessResult
    {
        public TimeSpan Duration { get; }
        public DateTime ExitTime { get; }
        public int ExitCode { get; }

        public ProcessResult(int exitCode, TimeSpan duration, DateTime exitTime)
        {
            Duration = duration;
            ExitCode = exitCode;
            ExitTime = exitTime;
        }
    }

    public enum OutputType
    {
        StdOut,
        StdErr
    }

    public class ProcessRunner
    {
        private readonly TaskLoggingHelper _logger;

        public ProcessRunner(TaskLoggingHelper logger)
        {
            _logger = logger;
        }

        public async Task<ProcessResult> Run(
            string command, 
            string arguments, 
            CancellationToken cancellationToken, 
            bool useShell = false, 
            string workingDirectory = "")
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(0);

            Stopwatch stopwatch = Stopwatch.StartNew();
            var processStartInfo = new ProcessStartInfo(command, arguments)
            {
                UseShellExecute = useShell,
            };

            if (!String.IsNullOrEmpty(workingDirectory))
            {
                processStartInfo.WorkingDirectory = workingDirectory;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException($"Cancellation of {command} has been requested.");
            }

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,

            };

            int exitCode;
            DateTime exitTime;
            try
            {
                // Add to job so we can stop the process tree in a sensible way?
                process.Exited += (object sender, EventArgs eventArgs) =>
                {
                    // It is possible for the event to be raised early!
                    while (!process.HasExited)
                    {
                        Thread.Sleep(50);
                    }

                    semaphore.Release();
                };

                process.Start();

                try
                {
                    await semaphore.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (semaphore.CurrentCount == 0 && !process.HasExited)
                    {
                        _logger.LogError($"Task cancelled - attempting to kill process {process.Id}.");
                        process.Kill();
                    }

                    throw new TaskCanceledException($"Running of command ['{command}'] has been cancelled.");
                }

                process.WaitForExit();
                exitCode = process.ExitCode;
                exitTime = process.ExitTime;
            }
            finally
            {
                process.Dispose();
                stopwatch.Stop();
            }

            // At this point there should be no more data being written to the output...
            return new ProcessResult(exitCode, stopwatch.Elapsed, exitTime);
        }
    }
}