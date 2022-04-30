using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.TestPlatform.Build.Utils;

namespace DockerTesting
{
    public class DockerVSTestTask : Task, ICancelableTask
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private const string vsTestAppName = "vstest.console.dll";

        public string TargetAssembly
        {
            get;
            set;
        }

        public string VSTestSetting
        {
            get;
            set;
        }

        public string[] VSTestTestAdapterPath
        {
            get;
            set;
        }

        public string VSTestFramework
        {
            get;
            set;
        }

        public string VSTestPlatform
        {
            get;
            set;
        }

        public string VSTestTestCaseFilter
        {
            get;
            set;
        }

        public string[] VSTestLogger
        {
            get;
            set;
        }

        public string VSTestListTests
        {
            get;
            set;
        }

        public string VSTestDiag
        {
            get;
            set;
        }

        public string[] VSTestCLIRunSettings
        {
            get;
            set;
        }

        public string VSTestResultsDirectory
        {
            get;
            set;
        }

        public string VSTestVerbosity
        {
            get;
            set;
        }

        public string[] VSTestCollect
        {
            get;
            set;
        }

        public string VSTestBlame
        {
            get;
            set;
        }

        public string VSTestTraceDataCollectorDirectoryPath
        {
            get;
            set;
        }

        public string VSTestNoLogo
        {
            get;
            set;
        }

        [Required]
        public string TestDirectory
        {
            get;
            set;
        }

        [Required]
        public string DockerImage
        {
            get;
            set;
        }

        public bool Debug
        {
            get;
            set;
        }

        public string TestMountDirectory => "/scratch";

        public void WaitForDebugger()
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                Log.LogMessage(MessageImportance.High, $"Current Build Process: {currentProcess.ProcessName}. Pid = {currentProcess.Id}.");
            }

            if (true)
            {
                using (Process currentProcess = Process.GetCurrentProcess())
                {
                    Log.LogMessage($"Debug MSBuild Process {currentProcess.Id}");
                }

                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public override bool Execute()
        {
            if (Debug)
            {
                WaitForDebugger();
            }

            ProcessRunner processRunner = new ProcessRunner(Log);

            try
            {
                var processResult = processRunner.Run("docker", GenerateCommandLineCommands(), _cancellationTokenSource.Token, useShell: false, workingDirectory: "").GetAwaiter().GetResult();


                if (processResult.ExitCode != 0)
                {
                    Log.LogError($"Test execution of {TargetAssembly} has failed. ExitCode: {processResult.ExitCode}");
                    return false;
                }
            }
            catch (Exception exception)
            {
                Log.LogErrorFromException(exception);
                return false;
            }

            return true;
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        protected string GenerateCommandLineCommands()
        {
            string dotnetArg = $"dotnet /usr/share/dotnet/sdk/$DOTNET_SDK_VERSION/{vsTestAppName} {String.Join(" ", CreateArgument())}";

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                TestDirectory = TestDirectory.Replace('\\', '/');
            }
            else
            {
                TestDirectory = System.IO.Path.GetFullPath(TestDirectory);
            }

            string dockerArguments = $@"run --rm -v ""{TestDirectory}:{TestMountDirectory}"" --entrypoint sh {DockerImage} -c ""{dotnetArg}""";
            Log.LogMessage(MessageImportance.Normal, "Calling 'docker' with '{0}'", dockerArguments);
            return dockerArguments;
        }

        internal IEnumerable<string> CreateArgument()
        {
            var allArgs = this.AddArgs();

            // VSTestCLIRunSettings should be last argument in allArgs as vstest.console ignore options after "--"(CLIRunSettings option).
            this.AddCLIRunSettingsArgs(allArgs);

            return allArgs;
        }

        private void AddCLIRunSettingsArgs(List<string> allArgs)
        {
            if (this.VSTestCLIRunSettings != null && this.VSTestCLIRunSettings.Length > 0)
            {
                allArgs.Add("--");
                foreach (var arg in this.VSTestCLIRunSettings)
                {
                    allArgs.Add(ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));
                }
            }
        }

        private List<string> AddArgs()
        {
            var isConsoleLoggerSpecifiedByUser = false;
            var isCollectCodeCoverageEnabled = false;
            var isRunSettingsEnabled = false;
            var allArgs = new List<string>();

            // TODO log arguments in task
            if (!string.IsNullOrEmpty(this.VSTestSetting))
            {
                isRunSettingsEnabled = true;
                allArgs.Add("--settings:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(this.VSTestSetting));
            }

            if (this.VSTestTestAdapterPath != null && this.VSTestTestAdapterPath.Length > 0)
            {
                foreach (var arg in this.VSTestTestAdapterPath)
                {
                    allArgs.Add("--testAdapterPath:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));
                }
            }

            if (!string.IsNullOrEmpty(this.VSTestFramework))
            {
                allArgs.Add("--framework:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(this.VSTestFramework));
            }

            // vstest.console only support x86 and x64 for argument platform
            if (!string.IsNullOrEmpty(this.VSTestPlatform) && !this.VSTestPlatform.Contains("AnyCPU"))
            {
                allArgs.Add("--platform:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(this.VSTestPlatform));
            }

            if (!string.IsNullOrEmpty(this.VSTestTestCaseFilter))
            {
                allArgs.Add("--testCaseFilter:" +
                            ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(this.VSTestTestCaseFilter));
            }

            if (this.VSTestLogger != null && this.VSTestLogger.Length > 0)
            {
                foreach (var arg in this.VSTestLogger)
                {
                    allArgs.Add("--logger:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));

                    if (arg.StartsWith("console", StringComparison.OrdinalIgnoreCase))
                    {
                        isConsoleLoggerSpecifiedByUser = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(this.VSTestResultsDirectory))
            {
                allArgs.Add("--resultsDirectory:" +
                            ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(this.VSTestResultsDirectory));
            }

            if (!string.IsNullOrEmpty(this.VSTestListTests))
            {
                allArgs.Add("--listTests");
            }

            if (!string.IsNullOrEmpty(this.VSTestDiag))
            {
                allArgs.Add("--Diag:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(this.VSTestDiag));
            }

            if (string.IsNullOrEmpty(this.TargetAssembly))
            {
                this.Log.LogError("Test file path cannot be empty or null.");
            }
            else
            {
                allArgs.Add(ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart($"{TestMountDirectory}/{TargetAssembly}"));
            }

            // Console logger was not specified by user, but verbosity was, hence add default console logger with verbosity as specified
            if (!string.IsNullOrWhiteSpace(this.VSTestVerbosity) && !isConsoleLoggerSpecifiedByUser)
            {
                var normalTestLogging = new List<string>() { "n", "normal", "d", "detailed", "diag", "diagnostic" };
                var quietTestLogging = new List<string>() { "q", "quiet" };

                string vsTestVerbosity = "minimal";
                if (normalTestLogging.Contains(this.VSTestVerbosity))
                {
                    vsTestVerbosity = "normal";
                }
                else if (quietTestLogging.Contains(this.VSTestVerbosity))
                {
                    vsTestVerbosity = "quiet";
                }

                allArgs.Add("--logger:Console;Verbosity=" + vsTestVerbosity);
            }

            if (!string.IsNullOrEmpty(this.VSTestBlame))
            {
                allArgs.Add("--Blame");
            }

            if (this.VSTestCollect != null && this.VSTestCollect.Length > 0)
            {
                foreach (var arg in this.VSTestCollect)
                {
                    if (arg.Equals("Code Coverage", StringComparison.OrdinalIgnoreCase))
                    {
                        isCollectCodeCoverageEnabled = true;
                    }

                    allArgs.Add("--collect:" + ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(arg));
                }
            }

            if (isCollectCodeCoverageEnabled || isRunSettingsEnabled)
            {
                // Pass TraceDataCollector path to vstest.console as TestAdapterPath if --collect "Code Coverage"
                // or --settings (User can enable code coverage from runsettings) option given.
                // Not parsing the runsettings for two reason:
                //    1. To keep no knowledge of runsettings structure in VSTestTask.
                //    2. Impact of adding adapter path always is minimal. (worst case: loads additional data collector assembly in datacollector process.)
                // This is required due to currently trace datacollector not ships with dotnet sdk, can be remove once we have
                // go code coverage x-plat.
                if (!string.IsNullOrEmpty(this.VSTestTraceDataCollectorDirectoryPath))
                {
                    allArgs.Add("--testAdapterPath:" +
                                ArgumentEscaper.HandleEscapeSequenceInArgForProcessStart(this
                                    .VSTestTraceDataCollectorDirectoryPath));
                }
                else
                {
                    if (isCollectCodeCoverageEnabled)
                    {
                        // Not showing message in runsettings scenario, because we are not sure that code coverage is enabled.
                        // User might be using older Microsoft.NET.Test.Sdk which don't have CodeCoverage infra.
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(this.VSTestNoLogo))
            {
                allArgs.Add("--nologo");
            }

            return allArgs;
        }

    }
}
