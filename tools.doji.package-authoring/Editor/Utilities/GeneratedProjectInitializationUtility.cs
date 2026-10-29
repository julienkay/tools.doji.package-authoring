using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Doji.PackageAuthoring.Utilities {
    /// <summary>
    /// Prepares generated projects for their initial commit using Unity's own package resolution and asset import.
    /// </summary>
    internal static class GeneratedProjectInitializationUtility {
        private const int TimeoutMilliseconds = 600000;

        /// <summary>
        /// Completes a headless first import independently of the generated project's auto-open option.
        /// Failure leaves the generated files available and prevents the caller from creating an incomplete commit.
        /// </summary>
        internal static void Initialize(string projectPath) {
            projectPath = Path.GetFullPath(projectPath);
            if (File.Exists(Path.Combine(projectPath, "Temp", "UnityLockfile"))) {
                throw new InvalidOperationException($"Close the generated project before initializing it: {projectPath}");
            }

            string executable = UnityEditorLauncherUtility.ResolveCurrentEditorExecutablePath();
            if (!File.Exists(executable)) {
                throw new FileNotFoundException("Could not locate Unity Editor for project initialization.", executable);
            }

            string logDirectory = Path.Combine(projectPath, ".utmp", "PackageAuthoring");
            Directory.CreateDirectory(logDirectory);
            string logPath = Path.Combine(logDirectory, $"initialize-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
            Debug.Log($"Initializing generated project before its first commit. This may take several minutes. Log: {logPath}");

            using Process process = new() {
                StartInfo = new ProcessStartInfo {
                    FileName = executable,
                    Arguments = $"-batchmode -nographics -quit -projectPath {Quote(projectPath)} -logFile {Quote(logPath)}",
                    WorkingDirectory = projectPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            Stopwatch stopwatch = Stopwatch.StartNew();
            try {
                while (!process.WaitForExit(250)) {
                    if (stopwatch.ElapsedMilliseconds >= TimeoutMilliseconds) {
                        throw new TimeoutException($"Generated project initialization timed out. No initial commit was created. Log: {logPath}");
                    }

                    if (!Application.isBatchMode && EditorUtility.DisplayCancelableProgressBar(
                            "Preparing initial commit",
                            $"Unity is resolving packages and importing the generated project ({stopwatch.Elapsed:mm\\:ss}).",
                            0f)) {
                        throw new OperationCanceledException($"Project initialization was canceled. No initial commit was created. Log: {logPath}");
                    }
                }

                if (process.ExitCode != 0) {
                    throw new InvalidOperationException(
                        $"Generated project initialization failed (Unity exit code {process.ExitCode}). No initial commit was created. Log: {logPath}");
                }

                string lockPath = Path.Combine(projectPath, "Packages", "packages-lock.json");
                if (!File.Exists(lockPath) || JObject.Parse(File.ReadAllText(lockPath))["dependencies"] is not JObject) {
                    throw new InvalidOperationException(
                        $"Unity did not produce a valid package lockfile. No initial commit was created. Log: {logPath}");
                }
            }
            finally {
                if (!process.HasExited) {
                    process.Kill();
                    process.WaitForExit(5000);
                }

                if (!Application.isBatchMode) {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private static string Quote(string value) {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }
    }
}
