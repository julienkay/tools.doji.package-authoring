using System;
using System.IO;
using System.Text.RegularExpressions;
using Doji.PackageAuthoring.Models;
using Doji.PackageAuthoring.Utilities;
using Doji.PackageAuthoring.Wizards.Presets;
using Doji.PackageAuthoring.Wizards.Templates;
using UnityEngine;
using static Doji.PackageAuthoring.Utilities.GuidUtility;

namespace Doji.PackageAuthoring.Wizards {
    /// <summary>
    /// Runs the package and project scaffolding flows shared by the wizards and the scripting API.
    /// </summary>
    internal static class PackageAuthoringGenerationUtility {
        /// <summary>
        /// Generates a standalone Unity project from the configured project settings.
        /// </summary>
        /// <param name="projectSettings">Project-facing metadata and output location.</param>
        /// <param name="openProjectAfterCreation">Whether to open the generated project in the current Unity editor.</param>
        /// <returns>The generated project directory.</returns>
        internal static string GenerateProject(ProjectSettings projectSettings, bool openProjectAfterCreation) {
            if (projectSettings == null) {
                throw new ArgumentNullException(nameof(projectSettings));
            }

            string projectDirectory = Path.Combine(projectSettings.TargetLocation, projectSettings.ProductName);
            string gitIgnoreTemplate = GitIgnoreTemplateSettings.Instance.GetResolvedContent(projectSettings);

            GeneratedProjectScaffoldingUtility.CopyTemplateProjectBaseline(
                projectDirectory,
                gitIgnoreTemplate,
                ProjectManifestTemplate.GetProjectManifest(projectSettings));
            GeneratedProjectScaffoldingUtility.ApplyGeneratedProjectSettings(
                projectDirectory,
                projectSettings,
                GeneratedProjectScaffoldingUtility.BuildDefaultApplicationIdentifier(
                    projectSettings.CompanyName,
                    projectSettings.ProductName),
                GeneratedProjectScaffoldingUtility.SanitizeRootNamespace(projectSettings.ProductName));

            Debug.Log($"Project created successfully at {projectDirectory}");

            if (openProjectAfterCreation) {
                UnityHubProjectRegistryUtility.TryRegisterProject(projectDirectory);
                UnityEditorLauncherUtility.TryOpenProjectInCurrentEditor(projectDirectory);
            }

            return projectDirectory;
        }

        /// <summary>
        /// Generates a package repository together with its companion Unity project.
        /// </summary>
        /// <param name="projectSettings">Companion project metadata and repository output root.</param>
        /// <param name="packageSettings">Package manifest and folder layout settings.</param>
        /// <param name="repoSettings">Repository-level files and git behavior.</param>
        /// <param name="openProjectAfterCreation">Whether to open the generated companion project in the current Unity editor.</param>
        /// <returns>The generated repository root directory.</returns>
        internal static string GeneratePackage(
            ProjectSettings projectSettings,
            PackageSettings packageSettings,
            RepoSettings repoSettings,
            bool openProjectAfterCreation) {
            if (projectSettings == null) {
                throw new ArgumentNullException(nameof(projectSettings));
            }

            if (packageSettings == null) {
                throw new ArgumentNullException(nameof(packageSettings));
            }

            if (repoSettings == null) {
                throw new ArgumentNullException(nameof(repoSettings));
            }

            PackageContext context = new(projectSettings, packageSettings, repoSettings);
            string rootDirectory = Path.Combine(projectSettings.TargetLocation, packageSettings.PackageName);
            string packageDirectory = Path.Combine(rootDirectory, packageSettings.PackageName);
            string projectDirectory = Path.Combine(rootDirectory, "projects", projectSettings.ProductName);
            string runtimeAssemblyGuid = null;

            Directory.CreateDirectory(rootDirectory);
            Directory.CreateDirectory(packageDirectory);
            Directory.CreateDirectory(projectDirectory);

            string editorAssemblyGuid = null;

            if (packageSettings.CreateDocsFolder) {
                CreateDocsFolder(rootDirectory, context);
            }

            CreateRuntimeFolder(packageDirectory, context, packageSettings, ref runtimeAssemblyGuid);

            if (packageSettings.CreateSamplesFolder) {
                CreateSamplesFolder(packageDirectory, context, packageSettings, runtimeAssemblyGuid);
            }

            if (packageSettings.CreateEditorFolder) {
                editorAssemblyGuid = CreateEditorFolder(
                    packageDirectory,
                    context,
                    packageSettings,
                    runtimeAssemblyGuid);
            }

            if (packageSettings.CreateTestsFolder) {
                CreateTestsFolder(
                    packageDirectory,
                    context,
                    packageSettings,
                    runtimeAssemblyGuid,
                    editorAssemblyGuid);
            }

            CreatePackageFiles(packageDirectory, context, packageSettings);
            CreateCompanionProject(projectDirectory, context, projectSettings, packageSettings);
            CreateRootFiles(rootDirectory, context, projectSettings, repoSettings);

            if (repoSettings.InitializeGitRepository) {
                if (!Directory.Exists(Path.Combine(rootDirectory, ".git"))) {
                    GeneratedProjectInitializationUtility.Initialize(projectDirectory);
                }

                string repositoryUrl = TemplateTokenResolver.Resolve(repoSettings.RepositoryUrl, context);
                GitUtility.InitializeRepository(rootDirectory, repositoryUrl);
            }

            Debug.Log($"Package scaffolding created successfully at {rootDirectory}");

            if (openProjectAfterCreation) {
                UnityHubProjectRegistryUtility.TryRegisterProject(projectDirectory);
                UnityEditorLauncherUtility.TryOpenProjectInCurrentEditor(projectDirectory);
            }

            return rootDirectory;
        }

        private static void CreateCompanionProject(
            string projectDirectory,
            PackageContext context,
            ProjectSettings projectSettings,
            PackageSettings packageSettings) {
            string gitIgnoreTemplate = GitIgnoreTemplateSettings.Instance.GetResolvedContent(context);
            GeneratedProjectScaffoldingUtility.CopyTemplateProjectBaseline(
                projectDirectory,
                gitIgnoreTemplate,
                context.GetProjectManifest());
            GeneratedProjectScaffoldingUtility.ApplyGeneratedProjectSettings(
                projectDirectory,
                projectSettings,
                GeneratedProjectScaffoldingUtility.BuildDefaultApplicationIdentifier(
                    projectSettings.CompanyName,
                    projectSettings.ProductName),
                GeneratedProjectScaffoldingUtility.SanitizeRootNamespace(packageSettings.NamespaceName));
        }

        private static void CreateRootFiles(
            string rootDirectory,
            PackageContext context,
            ProjectSettings projectSettings,
            RepoSettings repoSettings) {
            string license = context.GetLicense();
            if (!string.IsNullOrWhiteSpace(license)) {
                GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(rootDirectory, "LICENSE"), license);
            }

            string agentsInstructions = projectSettings.GenerateAgentsFile
                ? context.GetAgentsInstructions()
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(agentsInstructions)) {
                GeneratedProjectScaffoldingUtility.CreateFile(
                    Path.Combine(rootDirectory, "AGENTS.md"),
                    agentsInstructions,
                    true);
            }

            if (repoSettings.IncludeReadme) {
                GeneratedProjectScaffoldingUtility.CreateFile(
                    Path.Combine(rootDirectory, "README.md"),
                    context.GetRepositoryReadme());
            }

            if (repoSettings.CreateSignUpmPackageWorkflow) {
                GeneratedProjectScaffoldingUtility.CreateFile(
                    Path.Combine(rootDirectory, ".github", "workflows", "ci.yml"),
                    context.GetSignUpmPackageWorkflow());
            }
        }

        private static void CreatePackageFiles(
            string packageDirectory,
            PackageContext context,
            PackageSettings packageSettings) {
            GeneratedProjectScaffoldingUtility.CreateFileWithMeta(
                Path.Combine(packageDirectory, "package.json"),
                context.GetPackageManifest(),
                AssetMetaTemplate.GetPackageManifestMeta(NewMetaGuid()),
                true);

            if (packageSettings.IncludeReadme) {
                GeneratedProjectScaffoldingUtility.CreateFileWithMeta(
                    Path.Combine(packageDirectory, "README.md"),
                    context.GetPackageReadme(),
                    AssetMetaTemplate.GetTextAssetMeta(NewMetaGuid()));
            }

            GeneratedProjectScaffoldingUtility.CreateFileWithMeta(
                Path.Combine(packageDirectory, "CHANGELOG.md"),
                context.GetChangelog(),
                AssetMetaTemplate.GetTextAssetMeta(NewMetaGuid()));
        }

        private static void CreateRuntimeFolder(
            string packageDirectory,
            PackageContext context,
            PackageSettings packageSettings,
            ref string runtimeAssemblyGuid) {
            string runtimePath = Path.Combine(packageDirectory, "Runtime");
            GeneratedProjectScaffoldingUtility.CreateFolderWithMeta(runtimePath);
            runtimeAssemblyGuid = CreateAsmDefWithMeta(
                Path.Combine(runtimePath, $"{packageSettings.AssemblyName}.asmdef"),
                context.GetRuntimeAsmDef());
            GeneratedProjectScaffoldingUtility.CreateFileWithMeta(
                Path.Combine(runtimePath, "AssemblyInfo.cs"),
                context.GetAssemblyInfo(),
                AssetMetaTemplate.GetMonoScriptMeta(NewMetaGuid()));
        }

        private static string CreateEditorFolder(
            string packageDirectory,
            PackageContext context,
            PackageSettings packageSettings,
            string runtimeAssemblyGuid) {
            string editorPath = Path.Combine(packageDirectory, "Editor");
            GeneratedProjectScaffoldingUtility.CreateFolderWithMeta(editorPath);
            return CreateAsmDefWithMeta(
                Path.Combine(editorPath, $"{packageSettings.AssemblyName}.Editor.asmdef"),
                context.GetEditorAsmDef(runtimeAssemblyGuid));
        }

        private static void CreateTestsFolder(
            string packageDirectory,
            PackageContext context,
            PackageSettings packageSettings,
            string runtimeAssemblyGuid,
            string editorAssemblyGuid) {
            string testsPath = Path.Combine(packageDirectory, "Tests");
            string editorTestsPath = Path.Combine(testsPath, "Editor");
            GeneratedProjectScaffoldingUtility.CreateFolderWithMeta(testsPath);
            GeneratedProjectScaffoldingUtility.CreateFolderWithMeta(editorTestsPath);
            CreateAsmDefWithMeta(
                Path.Combine(editorTestsPath, $"{packageSettings.AssemblyName}.Editor.Tests.asmdef"),
                context.GetEditorTestsAsmDef(runtimeAssemblyGuid, editorAssemblyGuid));
        }

        private static void CreateSamplesFolder(
            string packageDirectory,
            PackageContext context,
            PackageSettings packageSettings,
            string runtimeAssemblyGuid) {
            string samplesPath = Path.Combine(packageDirectory, "Samples~");
            Directory.CreateDirectory(samplesPath);
            GeneratedProjectScaffoldingUtility.CreateFolderWithMeta(Path.Combine(samplesPath, "00-SharedSampleAssets"));
            GeneratedProjectScaffoldingUtility.CreateFolderWithMeta(Path.Combine(samplesPath, "01-BasicSample"));
            CreateAsmDefWithMeta(
                Path.Combine(samplesPath, $"{packageSettings.AssemblyName}.asmdef"),
                context.GetSamplesAsmDef(runtimeAssemblyGuid));
            GeneratedProjectScaffoldingUtility.CreateFileWithMeta(
                Path.Combine(samplesPath, "01-BasicSample", "BasicSample.cs"),
                context.GetSampleScript(),
                AssetMetaTemplate.GetMonoScriptMeta(NewMetaGuid()));
        }

        private static void CreateDocsFolder(string rootDirectory, PackageContext context) {
            string docsPath = Path.Combine(rootDirectory, "docs");
            Directory.CreateDirectory(docsPath);
            Directory.CreateDirectory(Path.Combine(docsPath, "api"));
            Directory.CreateDirectory(Path.Combine(docsPath, "manual"));
            Directory.CreateDirectory(Path.Combine(docsPath, "pdf"));

            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, ".gitignore"),
                context.GetDocsGitIgnore());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "docfx.json"), context.GetDocfxJson());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "docfx-pdf.json"),
                context.GetDocfxPdfJson());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "filterConfig.yml"),
                context.GetFilterConfig());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "index.md"), context.GetIndexMD());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "api", ".gitignore"),
                context.GetDocsApiGitIgnore());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "api", "index.md"),
                context.GetDocsApiIndex());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "toc.yml"), context.GetRootToc());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "manual", "toc.yml"),
                context.GetManualToc());
            GeneratedProjectScaffoldingUtility.CreateFile(Path.Combine(docsPath, "pdf", "toc.yml"),
                context.GetPdfToc());

            DocsBrandingImageSettings brandingImages = DocsBrandingImageSettings.Instance;
            if (!brandingImages.HasAnyImage) {
                return;
            }

            string imagesPath = Path.Combine(docsPath, "images");
            if (!DocumentationImageUtility.TryWriteDocumentationImages(
                    brandingImages.LogoTexture,
                    brandingImages.FaviconTexture,
                    imagesPath)) {
                Debug.LogWarning(
                    "Failed to generate one or more documentation branding images from the configured source textures.");
            }
        }

        private static string CreateAsmDefWithMeta(string asmDefPath, string content) {
            string metaPath = $"{asmDefPath}.meta";

            if (File.Exists(asmDefPath)) {
                string existingGuid = TryReadMetaGuid(metaPath);
                if (!string.IsNullOrWhiteSpace(existingGuid)) {
                    return existingGuid;
                }

                string generatedGuid = NewMetaGuid();
                GeneratedProjectScaffoldingUtility.CreateFile(
                    metaPath,
                    AssetMetaTemplate.GetAsmDefMeta(generatedGuid));
                return generatedGuid;
            }

            string guid = NewMetaGuid();
            GeneratedProjectScaffoldingUtility.CreateFile(asmDefPath, content);
            GeneratedProjectScaffoldingUtility.CreateFile(
                metaPath,
                AssetMetaTemplate.GetAsmDefMeta(guid));
            return guid;
        }

        private static string TryReadMetaGuid(string metaPath) {
            if (!File.Exists(metaPath)) {
                return null;
            }

            Match match = Regex.Match(
                File.ReadAllText(metaPath),
                @"^guid:\s*(?<guid>[0-9a-fA-F]{32})\s*$",
                RegexOptions.Multiline);
            return match.Success ? match.Groups["guid"].Value : null;
        }
    }
}
