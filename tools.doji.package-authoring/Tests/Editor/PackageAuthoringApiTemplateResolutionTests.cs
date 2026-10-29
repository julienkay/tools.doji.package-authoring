using System;
using System.IO;
using System.Reflection;
using Doji.PackageAuthoring.Models;
using NUnit.Framework;

namespace Doji.PackageAuthoring.Tests {
    /// <summary>
    /// Verifies tokenized generated text files produced by the public package authoring API.
    /// </summary>
    internal sealed class PackageAuthoringApiTemplateResolutionTests : PackageAuthoringApiTestBase {
        private static Type ProjectTemplateStorageType => typeof(PackageAuthoringApi).Assembly.GetType(
            "Doji.PackageAuthoring.Wizards.Presets.ProjectTemplateStorage",
            true);

        [TestCase(false)]
        [TestCase(true)]
        public void GeneratePackage_WritesSharedReleaseWorkflowCallerOnlyWhenEnabled(bool createWorkflow) {
            PackageSettings packageSettings = CreatePackageSettings(string.Empty, string.Empty);
            packageSettings.PackageName = "tools.doji.tests.shared-release";
            RepoSettings repoSettings = CreateRepoSettings();
            repoSettings.CreateSignUpmPackageWorkflow = createWorkflow;

            string rootDirectory = PackageAuthoringApi.GeneratePackage(
                CreateProjectSettings("Shared Release Companion"), packageSettings, repoSettings);
            string workflowPath = Path.Combine(rootDirectory, ".github", "workflows", "ci.yml");
            Assert.That(File.Exists(workflowPath), Is.EqualTo(createWorkflow));
            if (!createWorkflow) {
                return;
            }

            string workflow = File.ReadAllText(workflowPath).Replace("\r\n", "\n");
            Assert.That(workflow, Does.Contain("push:\n    tags:"));
            Assert.That(workflow, Does.Contain("contents: write"));
            Assert.That(workflow, Does.Contain(
                "uses: julienkay/tools.doji.package-authoring/.github/workflows/sign-upm-package.yml@master"));
            Assert.That(workflow, Does.Contain("package-path: \"tools.doji.tests.shared-release\""));
            foreach (string secret in new[] { "UPM_SERVICE_ACCOUNT_KEY_ID", "UPM_SERVICE_ACCOUNT_KEY_SECRET", "UPM_ORG_ID" }) {
                Assert.That(workflow, Does.Contain($"{secret}: ${{{{ secrets.{secret} }}}}"));
            }
            Assert.That(workflow, Does.Not.Contain("__PACKAGE_PATH__"));
            Assert.That(workflow, Does.Not.Contain("runs-on:"));
            Assert.That(workflow, Does.Not.Contain("steps:"));
            Assert.That(workflow, Does.Not.Contain("upm pack"));
        }

        [Test]
        public void GeneratePackage_ResolvesTokenizedTemplateFiles() {
            ProjectSettings projectSettings = CreateProjectSettings("Tokenized Companion");
            projectSettings.GenerateAgentsFile = true;

            string rootDirectory = PackageAuthoringApi.GeneratePackage(
                projectSettings,
                CreatePackageSettings(
                    "https://example.com/{{PACKAGE_NAME}}",
                    "https://docs.doji-tech.com/{{PACKAGE_NAME}}/"),
                CreateRepoSettings(true));

            string repositoryReadme = File.ReadAllText(Path.Combine(rootDirectory, "README.md"));
            string agentsFile = File.ReadAllText(Path.Combine(rootDirectory, "AGENTS.md"));
            string docsIndex = File.ReadAllText(Path.Combine(rootDirectory, "docs", "index.md"));
            string docfxJson = File.ReadAllText(Path.Combine(rootDirectory, "docs", "docfx.json"));

            Assert.That(repositoryReadme, Does.Contain("# Tokenized Companion"));
            Assert.That(repositoryReadme, Does.Contain("`com.doji.tests.tokenized`"));
            Assert.That(repositoryReadme, Does.Not.Contain("{{PACKAGE_NAME}}"));

            Assert.That(agentsFile, Does.Contain("`com.doji.tests.tokenized`"));
            Assert.That(agentsFile, Does.Contain("`Tokenized Companion`"));
            Assert.That(agentsFile, Does.Not.Contain("{{PROJECT_NAME}}"));

            Assert.That(docsIndex, Does.Contain("# Tokenized Package"));
            Assert.That(docsIndex, Does.Contain("Token resolution test package."));
            Assert.That(docsIndex, Does.Not.Contain("{{PACKAGE_DISPLAY_NAME}}"));

            Assert.That(docfxJson, Does.Contain("\"../com.doji.tests.tokenized\""));
            Assert.That(docfxJson, Does.Contain("https://docs.doji-tech.com/com.doji.tests.tokenized/"));
            Assert.That(docfxJson, Does.Not.Contain("{{DOCUMENTATION_URL}}"));
        }

        [Test]
        public void GeneratePackage_UsesRepositoryReadmeTemplateFileOverride() {
            string templateProjectRoot = Path.Combine(TempRoot, "TemplateProject");
            string repositoryReadmePath = Path.Combine(
                templateProjectRoot,
                "ProjectSettings",
                "PackageAuthoringTemplates",
                "Repository",
                "README.md");

            Directory.CreateDirectory(Path.GetDirectoryName(repositoryReadmePath)!);
            File.WriteAllText(
                repositoryReadmePath,
                "# Repo {{PROJECT_NAME}}\n\nPackage: {{PACKAGE_NAME}}\n");

            OverrideTemplateProjectRoot(templateProjectRoot);

            try {
                string rootDirectory = PackageAuthoringApi.GeneratePackage(
                    CreateProjectSettings("Tokenized Companion"),
                    CreatePackageSettings(
                        "https://example.com/{{PACKAGE_NAME}}",
                        "https://docs.doji-tech.com/{{PACKAGE_NAME}}/"),
                    CreateRepoSettings(true));

                string repositoryReadme = File.ReadAllText(Path.Combine(rootDirectory, "README.md"));

                Assert.That(repositoryReadme,
                    Is.EqualTo("# Repo Tokenized Companion\n\nPackage: com.doji.tests.tokenized\n"));
            }
            finally {
                ClearTemplateProjectRootOverride();
            }
        }

        [Test]
        public void ReapplyAllTemplateDefaults_WritesPlainTextTemplateFiles() {
            string templateProjectRoot = Path.Combine(TempRoot, "TemplateProject");
            OverrideTemplateProjectRoot(templateProjectRoot);

            try {
                PackageAuthoringApi.ReapplyAllTemplateDefaults();

                string repositoryReadmePath = Path.Combine(
                    templateProjectRoot,
                    "ProjectSettings",
                    "PackageAuthoringTemplates",
                    "Repository",
                    "README.md");
                string customLicensePath = Path.Combine(
                    templateProjectRoot,
                    "ProjectSettings",
                    "PackageAuthoringTemplates",
                    "Repository",
                    "CustomLicense.txt");
                string docfxJsonPath = Path.Combine(
                    templateProjectRoot,
                    "ProjectSettings",
                    "PackageAuthoringTemplates",
                    "Documentation",
                    "docfx.json");

                Assert.That(File.Exists(repositoryReadmePath), Is.True);
                Assert.That(File.Exists(customLicensePath), Is.True);
                Assert.That(File.Exists(docfxJsonPath), Is.True);

                string repositoryReadme = File.ReadAllText(repositoryReadmePath);
                string customLicense = File.ReadAllText(customLicensePath);
                string docfxJson = File.ReadAllText(docfxJsonPath);

                Assert.That(repositoryReadme,
                    Does.Contain("Document the public workflow, setup steps, and examples here."));
                Assert.That(repositoryReadme, Does.Not.Contain($"examples{Environment.NewLine}here."));
                Assert.That(customLicense, Does.Contain("Custom License"));
                Assert.That(docfxJson, Does.Contain("\"metadata\""));
                Assert.That(docfxJson.TrimStart(), Does.StartWith("{"));
            }
            finally {
                ClearTemplateProjectRootOverride();
            }
        }

        private static void OverrideTemplateProjectRoot(string projectRootPath) {
            ProjectTemplateStorageType.GetMethod(
                    "OverrideProjectRootPath",
                    BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, new object[] {
                    projectRootPath
                });
        }

        private static void ClearTemplateProjectRootOverride() {
            ProjectTemplateStorageType.GetMethod(
                    "ClearProjectRootPathOverride",
                    BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, null);
        }
    }
}
