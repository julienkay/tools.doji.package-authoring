using System.IO;
using System.Linq;
using Doji.PackageAuthoring.Models;
using Doji.PackageAuthoring.Utilities;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Doji.PackageAuthoring.Tests {
    /// <summary>
    /// Verifies generated project configuration and rendering assets produced by the public package authoring API.
    /// </summary>
    internal sealed class PackageAuthoringApiProjectGenerationTests : PackageAuthoringApiTestBase {
        [Test]
        [Explicit("Launches a separate Unity Editor to resolve and import the generated project, then reopens it.")]
        [Timeout(900000)]
        public void GeneratePackage_CommitsResolvedDependenciesWithAutoOpenDisabled() {
            ProjectSettings projectSettings = CreateProjectSettings("Lockfile");
            projectSettings.IncludedPackages.Items.Add(new PackageDependencyEntry {
                PackageName = "com.unity.2d.sprite",
                Version = "1.0.0"
            });
            PackageSettings packageSettings = CreatePackageSettings(string.Empty, string.Empty);
            packageSettings.PackageName = "com.doji.test";
            packageSettings.CreateDocsFolder = false;
            RepoSettings repoSettings = CreateRepoSettings();
            repoSettings.InitializeGitRepository = true;
            repoSettings.RepositoryUrl = string.Empty;

            string rootDirectory = PackageAuthoringApi.GeneratePackage(
                projectSettings, packageSettings, repoSettings, openProjectAfterCreation: false);
            string projectDirectory = Path.Combine(rootDirectory, "projects", projectSettings.ProductName);
            GitCommandRunner git = new("git", 10000);
            GitCommandResult committedLock = git.Run(
                $"show HEAD:\"projects/{projectSettings.ProductName}/Packages/packages-lock.json\"", rootDirectory);
            Assert.That(committedLock.Succeeded, Is.True);
            JObject dependencies = (JObject)JObject.Parse(committedLock.Output)["dependencies"];
            Assert.That(dependencies?["com.unity.2d.sprite"]?["version"]?.Value<string>(), Is.EqualTo("1.0.0"));
            Assert.That(dependencies?[packageSettings.PackageName]?["source"]?.Value<string>(), Is.EqualTo("local"));
            Assert.That(git.Run("rev-list --count HEAD", rootDirectory).Output.Trim(), Is.EqualTo("1"));
            Assert.That(git.Run("status --porcelain --untracked-files=all", rootDirectory).Output, Is.Empty);

            GeneratedProjectInitializationUtility.Initialize(projectDirectory);
            Assert.That(git.Run("status --porcelain --untracked-files=all", rootDirectory).Output, Is.Empty);
        }

        [Test]
        [Explicit("Launches a separate Unity Editor with an unresolvable local package dependency.")]
        [Timeout(900000)]
        public void GeneratePackage_DoesNotCommitWhenDependencyResolutionFails() {
            ProjectSettings projectSettings = CreateProjectSettings("Unresolved");
            projectSettings.IncludedPackages.Items.Add(new PackageDependencyEntry {
                PackageName = "tools.doji.missing-package",
                Version = "file:../missing-package"
            });
            PackageSettings packageSettings = CreatePackageSettings(string.Empty, string.Empty);
            packageSettings.CreateDocsFolder = false;
            RepoSettings repoSettings = CreateRepoSettings();
            repoSettings.InitializeGitRepository = true;

            Assert.That(() => PackageAuthoringApi.GeneratePackage(
                    projectSettings, packageSettings, repoSettings, openProjectAfterCreation: false),
                Throws.InvalidOperationException.With.Message.Contains("No initial commit was created"));
            string rootDirectory = Path.Combine(projectSettings.TargetLocation, packageSettings.PackageName);
            Assert.That(Directory.Exists(Path.Combine(rootDirectory, ".git")), Is.False);
            Assert.That(File.Exists(Path.Combine(rootDirectory, "projects", projectSettings.ProductName,
                "Packages", "manifest.json")), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GenerateProject_PreservesUrpAssetsAndReferences(bool createCompanionProject) {
            ProjectSettings projectSettings = CreateProjectSettings("URP Project");
            string projectDirectory;
            if (createCompanionProject) {
                string rootDirectory = PackageAuthoringApi.GeneratePackage(
                    projectSettings,
                    CreatePackageSettings(string.Empty, string.Empty),
                    CreateRepoSettings());
                projectDirectory = Path.Combine(rootDirectory, "projects", projectSettings.ProductName);
            }
            else {
                projectDirectory = PackageAuthoringApi.GenerateProject(projectSettings);
            }

            JObject manifest = JObject.Parse(File.ReadAllText(Path.Combine(projectDirectory, "Packages", "manifest.json")));
            Assert.That(manifest["dependencies"]?["com.unity.render-pipelines.universal"], Is.Not.Null);

            foreach (string settingsFile in new[] { "GraphicsSettings.asset", "QualitySettings.asset" }) {
                string relativePath = Path.Combine("ProjectSettings", settingsFile);
                Assert.That(File.ReadAllBytes(Path.Combine(projectDirectory, relativePath)),
                    Is.EqualTo(File.ReadAllBytes(relativePath)), relativePath);
            }

            // These include Graphics and both Quality pipelines, their renderers, and transitive volume profiles.
            foreach (string assetName in new[] {
                         "PC_RPAsset", "Mobile_RPAsset", "PC_Renderer", "Mobile_Renderer",
                         "UniversalRenderPipelineGlobalSettings", "DefaultVolumeProfile", "SampleSceneProfile"
                     }) {
                string relativePath = Path.Combine("Assets", "Settings", $"{assetName}.asset");
                Assert.That(File.Exists(Path.Combine(projectDirectory, relativePath)), Is.True, relativePath);
                Assert.That(File.ReadAllBytes(Path.Combine(projectDirectory, relativePath)),
                    Is.EqualTo(File.ReadAllBytes(relativePath)), relativePath);
                Assert.That(File.ReadAllBytes(Path.Combine(projectDirectory, $"{relativePath}.meta")),
                    Is.EqualTo(File.ReadAllBytes($"{relativePath}.meta")), $"{relativePath}.meta");
            }

            Assert.That(File.ReadAllBytes(Path.Combine(projectDirectory, "Assets", "Settings.meta")),
                Is.EqualTo(File.ReadAllBytes(Path.Combine("Assets", "Settings.meta"))));
            Assert.That(Directory.Exists(Path.Combine(projectDirectory, "Assets", "Editor")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(projectDirectory, "Assets", "Input")), Is.False);
        }

        [Test]
        public void GeneratePackage_WritesCompanionManifestForPreferredEditorAndTests() {
            ProjectSettings projectSettings = CreateProjectSettings("Companion With Tests");
            projectSettings.PreferredEditor = PreferredEditor.Rider;

            PackageSettings packageSettings = CreatePackageSettings(
                string.Empty,
                string.Empty);
            packageSettings.CreateTestsFolder = true;

            string rootDirectory = PackageAuthoringApi.GeneratePackage(
                projectSettings,
                packageSettings,
                CreateRepoSettings());

            string companionManifestPath = Path.Combine(
                rootDirectory,
                "projects",
                projectSettings.ProductName,
                "Packages",
                "manifest.json");
            JObject companionManifest = JObject.Parse(File.ReadAllText(companionManifestPath));
            JObject dependencies = (JObject)companionManifest["dependencies"];

            Assert.That(
                dependencies?["com.doji.tests.tokenized"]?.Value<string>(),
                Is.EqualTo("file:../../../com.doji.tests.tokenized"));
            Assert.That(
                dependencies?["com.unity.ide.rider"]?.Value<string>(),
                Is.EqualTo("3.0.39"));
            Assert.That(companionManifest["testables"]?.Values<string>(), Does.Contain("com.doji.tests.tokenized"));
        }

        [Test]
        public void GeneratePackage_SortsCompanionManifestDependenciesToMatchUnityPackageManagerOrder() {
            ProjectSettings projectSettings = CreateProjectSettings("Companion Dependency Ordering");
            projectSettings.PreferredEditor = PreferredEditor.Rider;
            projectSettings.IncludedPackages.Items.Add(new PackageDependencyEntry {
                PackageName = "com.unity.test-framework",
                Version = "1.5.1"
            });
            projectSettings.IncludedPackages.Items.Add(new PackageDependencyEntry {
                PackageName = "com.unity.2d.sprite",
                Version = "1.0.0"
            });

            PackageSettings packageSettings = CreatePackageSettings(
                string.Empty,
                string.Empty);
            packageSettings.CreateTestsFolder = true;

            string rootDirectory = PackageAuthoringApi.GeneratePackage(
                projectSettings,
                packageSettings,
                CreateRepoSettings());

            string companionManifestPath = Path.Combine(
                rootDirectory,
                "projects",
                projectSettings.ProductName,
                "Packages",
                "manifest.json");
            JObject companionManifest = JObject.Parse(File.ReadAllText(companionManifestPath));
            JObject dependencies = (JObject)companionManifest["dependencies"];

            Assert.That(dependencies, Is.Not.Null);
            string[] dependencyNames = dependencies.Properties().Select(property => property.Name).ToArray();
            Assert.That(dependencyNames,
                Is.EqualTo(dependencyNames.OrderBy(name => name, System.StringComparer.Ordinal).ToArray()));
        }

        [Test]
        public void GeneratePackage_OmitsCompanionManifestTestablesWhenTestsFolderIsDisabled() {
            ProjectSettings projectSettings = CreateProjectSettings("Companion Without Tests");
            PackageSettings packageSettings = CreatePackageSettings(
                string.Empty,
                string.Empty);
            packageSettings.CreateTestsFolder = false;

            string rootDirectory = PackageAuthoringApi.GeneratePackage(
                projectSettings,
                packageSettings,
                CreateRepoSettings());

            string companionManifestPath = Path.Combine(
                rootDirectory,
                "projects",
                projectSettings.ProductName,
                "Packages",
                "manifest.json");
            JObject companionManifest = JObject.Parse(File.ReadAllText(companionManifestPath));

            Assert.That(companionManifest["testables"], Is.Null);
        }

        [Test]
        public void GeneratePackage_DoesNotCopyPackageAuthoringTemplateDirectoryOrAssetsIntoCompanionProjectSettings() {
            ProjectSettings projectSettings = CreateProjectSettings("Companion Without Template Assets");

            string rootDirectory = PackageAuthoringApi.GeneratePackage(
                projectSettings,
                CreatePackageSettings(string.Empty, string.Empty),
                CreateRepoSettings());

            string companionProjectSettingsPath = Path.Combine(
                rootDirectory,
                "projects",
                projectSettings.ProductName,
                "ProjectSettings");

            Assert.That(
                File.Exists(Path.Combine(companionProjectSettingsPath, "ProjectSettings.asset")),
                Is.True);
            Assert.That(
                Directory.Exists(Path.Combine(companionProjectSettingsPath, "PackageAuthoringTemplates")),
                Is.False);
            Assert.That(
                Directory.GetFileSystemEntries(
                    Path.Combine(companionProjectSettingsPath),
                    "PackageAuthoringTemplates",
                    SearchOption.AllDirectories),
                Is.Empty);
            Assert.That(
                Directory.GetFiles(
                    companionProjectSettingsPath,
                    "PackageAuthoring*.asset",
                    SearchOption.TopDirectoryOnly),
                Is.Empty);
        }

        [Test]
        public void GenerateProject_DoesNotCopyPackageAuthoringTemplateDirectoryOrAssetsIntoProjectSettings() {
            ProjectSettings projectSettings = CreateProjectSettings("Standalone Without Template Assets");

            string projectDirectory = PackageAuthoringApi.GenerateProject(
                projectSettings);

            string projectSettingsPath = Path.Combine(projectDirectory, "ProjectSettings");

            Assert.That(
                File.Exists(Path.Combine(projectSettingsPath, "ProjectSettings.asset")),
                Is.True);
            Assert.That(
                Directory.Exists(Path.Combine(projectSettingsPath, "PackageAuthoringTemplates")),
                Is.False);
            Assert.That(
                Directory.GetFileSystemEntries(
                    projectSettingsPath,
                    "PackageAuthoringTemplates",
                    SearchOption.AllDirectories),
                Is.Empty);
            Assert.That(
                Directory.GetFiles(
                    projectSettingsPath,
                    "PackageAuthoring*.asset",
                    SearchOption.TopDirectoryOnly),
                Is.Empty);
        }
    }
}
