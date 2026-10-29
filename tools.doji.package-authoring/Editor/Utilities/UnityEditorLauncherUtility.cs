using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Doji.PackageAuthoring.Utilities {
    /// <summary>
    /// Opens generated projects in the same Unity editor version that is currently running the wizard.
    /// </summary>
    internal static class UnityEditorLauncherUtility {
        /// <summary>
        /// Attempts to open the provided project path in the current Unity editor installation.
        /// </summary>
        /// <param name="projectPath">Absolute path to the Unity project that should be opened.</param>
        /// <returns><see langword="true"/> when a launch command was started successfully.</returns>
        public static bool TryOpenProjectInCurrentEditor(string projectPath) {
            if (string.IsNullOrWhiteSpace(projectPath)) {
                Debug.LogError("Cannot open a Unity project because the project path is empty.");
                return false;
            }

            if (IsProjectOpen(projectPath)) {
                Debug.Log($"Project already appears to be open; skipping auto-open: {projectPath}");
                return false;
            }

            return TryOpenProjectWithExecutable(ResolveCurrentEditorExecutablePath(), projectPath);
        }

        /// <summary>
        /// Shares the current editor executable between interactive launch and headless project initialization.
        /// </summary>
        internal static string ResolveCurrentEditorExecutablePath() {
            string applicationPath = EditorApplication.applicationPath;
#if UNITY_EDITOR_OSX
            return ResolveMacEditorExecutablePath(applicationPath);
#else
            return File.Exists(applicationPath) ? applicationPath : ResolveEditorExecutablePath();
#endif
        }

        /// <summary>
        /// Starts the Unity editor executable directly with a <c>-projectPath</c> argument.
        /// </summary>
        private static bool TryOpenProjectWithExecutable(string editorExecutablePath, string projectPath) {
            if (string.IsNullOrWhiteSpace(editorExecutablePath) || !File.Exists(editorExecutablePath)) {
                Debug.LogError("Could not locate Unity Editor executable to open new project.");
                return false;
            }

            Process.Start(new ProcessStartInfo {
                FileName = editorExecutablePath,
                Arguments = $"-projectPath {Quote(projectPath)}",
                UseShellExecute = false
            });

            Debug.Log($"Opening project in Unity: {projectPath}");
            return true;
        }

        /// <summary>
        /// Uses Unity's per-project lock file as a best-effort signal that the target project is already open.
        /// </summary>
        private static bool IsProjectOpen(string projectPath) {
            string lockFilePath = Path.Combine(projectPath, "Temp", "UnityLockfile");
            return File.Exists(lockFilePath);
        }

        /// <summary>
        /// Resolves the Unity editor executable when the platform does not expose a directly launchable path by default.
        /// </summary>
        /// <remarks>
        /// Windows uses <see cref="EditorApplication.applicationPath"/> directly. macOS launches the outer
        /// <c>.app</c> bundle via <c>open</c>. This fallback is kept only for other editor platforms where Unity
        /// may expose an installation contents path but not the final executable path.
        /// </remarks>
        /// <returns>
        /// The absolute path to the Unity editor executable, or <see cref="string.Empty"/> when it cannot be resolved.
        /// </returns>
        private static string ResolveEditorExecutablePath() {
            // Unity's contents path points at the installation's internal data root rather than the launchable app path.
            string contentsPath = EditorApplication.applicationContentsPath;
            if (Directory.Exists(contentsPath)) {
                string linuxExecutablePath = Path.Combine(contentsPath, "Linux", "Unity");
                if (File.Exists(linuxExecutablePath)) {
                    return linuxExecutablePath;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Resolves the real Unity executable inside a macOS app bundle or contents directory.
        /// </summary>
        /// <param name="applicationPath">The value reported by <see cref="EditorApplication.applicationPath"/>.</param>
        /// <returns>The executable path inside the bundle, or <see cref="string.Empty"/> when it cannot be found.</returns>
        private static string ResolveMacEditorExecutablePath(string applicationPath) {
            if (File.Exists(applicationPath)) {
                return applicationPath;
            }

            if (Directory.Exists(applicationPath) && applicationPath.EndsWith(".app")) {
                string bundleExecutablePath = Path.Combine(applicationPath, "Contents", "MacOS", "Unity");
                if (File.Exists(bundleExecutablePath)) {
                    return bundleExecutablePath;
                }
            }

            string contentsPath = EditorApplication.applicationContentsPath;
            if (Directory.Exists(contentsPath)) {
                string contentsExecutablePath = Path.Combine(contentsPath, "MacOS", "Unity");
                if (File.Exists(contentsExecutablePath)) {
                    return contentsExecutablePath;
                }
            }

            return string.Empty;
        }

        private static string Quote(string value) {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }
    }
}
