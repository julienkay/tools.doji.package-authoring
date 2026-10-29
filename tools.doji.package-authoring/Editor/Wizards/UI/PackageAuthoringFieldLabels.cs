using Doji.PackageAuthoring.Wizards.Templates;
using UnityEditor;
using UnityEngine;

namespace Doji.PackageAuthoring.Wizards.UI {
    /// <summary>
    /// Centralizes reusable GUI labels and tooltips for package authoring editor surfaces.
    /// </summary>
    internal static class PackageAuthoringFieldLabels {
        internal static class Project {
            public static GUIContent CompanyName { get; } =
                new("Company Name", "Company or organization name written into generated Unity project settings.");

            public static GUIContent Version { get; } =
                new("Version", "Version applied to generated project and package metadata.");

            public static GUIContent PreferredEditor { get; } =
                new("Preferred Editor", "IDE integration package added to generated project manifests.");

            public static GUIContent IncludedPackages { get; } =
                new("Included Packages",
                    "Packages merged into generated project manifests after the baseline manifest and preferred IDE integration are applied.");

            public static GUIContent IncludedPackagesInfo { get; } =
                new("These packages are added on top of the template project's baseline manifest.");

            public static GUIContent Advanced { get; } =
                new("Advanced");

            public static GUIContent GenerateAgentsFile { get; } =
                new("Generate AGENTS.md",
                    "Controls whether generated repositories include an AGENTS.md instructions file at the repository root.");

            public static GUIContent TargetLocation { get; } =
                new("Target Location", "Base output folder used when scaffolding projects from the editor windows.");

            public static GUIContent ProductName(string label) {
                return new GUIContent(
                    label,
                    "User-facing project name for generated standalone or companion projects.");
            }
        }

        internal static class Package {
            public static GUIContent Identifier { get; } =
                new("Identifier", "Package identifier written to package.json, for example com.company.package.");

            public static GUIContent PackageName { get; } =
                new("Package Name", "User-facing package display name written to package.json.");

            public static GUIContent AssemblyName { get; } =
                new("Assembly Name",
                    "Assembly name used for the generated runtime asmdef and related editor/sample asmdefs.");

            public static GUIContent Namespace { get; } =
                new("Namespace", "Root namespace used by generated source files and tokenized templates.");

            public static GUIContent Description { get; } =
                new("Description", "Short package description written into metadata and documentation templates.");

            public static GUIContent CompanyName { get; } =
                new("Company Name", "Company name written into generated package metadata.");

            public static GUIContent IncludeAuthorMetadata { get; } =
                new("Include Author Metadata",
                    "Controls whether author URL and email metadata are emitted into the generated package manifest.");

            public static GUIContent AuthorUrl { get; } =
                new("URL", "Optional author URL written into the generated package manifest.");

            public static GUIContent AuthorEmail { get; } =
                new("Email", "Optional author email written into the generated package manifest.");

            public static GUIContent MinimumUnityVersion { get; } =
                new("Minimum Unity Version",
                    "Unity compatibility metadata is always included in the generated package manifest.");

            public static GUIContent MinimumUnityMajor { get; } =
                new("Major",
                    "Major Unity version written into the generated package manifest.");

            public static GUIContent MinimumUnityMinor { get; } =
                new("Minor",
                    "Minor Unity version written into the generated package manifest.");

            public static GUIContent MinimumUnityRelease { get; } =
                new("Release",
                    "Unity release component written into the generated package manifest.");

            public static GUIContent DocumentationUrl { get; } =
                new("Documentation URL",
                    $"Optional documentation URL written into the generated package manifest. {TemplateTokenResolver.SupportedTokensTooltipSuffix}");

            public static GUIContent IncludePackageReadme { get; } =
                new("Include Package README",
                    "Controls whether a README.md file is generated inside the package root.");

            public static GUIContent CreateDocumentationFolder { get; } =
                new("Create Documentation Folder",
                    "Controls whether a docs folder scaffold is created at the repository root.");

            public static GUIContent CreateSamplesFolder { get; } =
                new("Create Samples Folder",
                    "Controls whether a Samples~ folder scaffold is created inside the package.");

            public static GUIContent CreateEditorFolder { get; } =
                new("Create Editor Folder",
                    "Controls whether an Editor folder and editor-only asmdef are created inside the package.");

            public static GUIContent CreateTestsFolder { get; } =
                new("Create Tests Folder",
                    "Controls whether a Tests folder is created and the package is marked testable in the generated companion project.");

            public static GUIContent Dependencies { get; } =
                new("Dependencies", "Dependencies written into the generated package manifest.");
        }

        internal static class Repository {
            public static GUIContent FilesSection { get; } =
                EditorGUIUtility.TrTextContent("Repository Files");

            public static GUIContent CopyrightHolder { get; } =
                EditorGUIUtility.TrTextContent("Copyright Holder",
                    "Name written into generated copyright and license templates.");

            public static GUIContent IncludeReadme { get; } =
                EditorGUIUtility.TrTextContent("Include README",
                    "Controls whether a README.md file is generated at the repository root.");

            public static GUIContent CreateSignUpmPackageWorkflow { get; } =
                EditorGUIUtility.TrTextContent(
                    "Create 'Sign UPM package' GitHub workflow",
                    "Generates a GitHub Actions workflow that signs and releases the package when a version tag is pushed. " +
                    "Requires the UPM_SERVICE_ACCOUNT_KEY_ID, UPM_SERVICE_ACCOUNT_KEY_SECRET, and UPM_ORG_ID GitHub Actions secrets.");

            public static GUIContent LicenseType { get; } =
                EditorGUIUtility.TrTextContent("Open Source License", "Controls the generated license template.");

            public static GUIContent RepositoryUrl { get; } =
                EditorGUIUtility.TrTextContent(
                    "Remote URL",
                    $"Optional URL assigned to the repository's origin remote after git initialization. {TemplateTokenResolver.SupportedTokensTooltipSuffix}");

            public static GUIContent InitializeGitRepository { get; } =
                EditorGUIUtility.TrTextContent(
                    "Initialize Git Repository",
                    "Resolves and imports the companion project before creating its initial Git commit, including the package lockfile. This can take several minutes and runs even when auto-open is off. Git must be installed and available on PATH.");
        }
    }
}
