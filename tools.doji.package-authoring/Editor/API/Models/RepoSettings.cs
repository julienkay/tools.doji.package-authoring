using System;
using UnityEngine;

namespace Doji.PackageAuthoring.Models {
    /// <summary>
    /// Repository-facing settings that shape generated root files and metadata outside the package manifest.
    /// </summary>
    [Serializable]
    public class RepoSettings {
        /// <summary>
        /// Name written into generated repository-level copyright notices such as the license file.
        /// </summary>
        [field: SerializeField]
        public string CopyrightHolder { get; set; } = "Your Name";

        /// <summary>
        /// License template used for generated repository files.
        /// </summary>
        [field: SerializeField]
        public LicenseType LicenseType { get; set; } = LicenseType.None;

        /// <summary>
        /// Whether generated repositories should be initialized as git repositories.
        /// Unity first resolves and imports the companion project so its lockfile is included in the initial commit,
        /// even when auto-open is disabled. This can take several minutes.
        /// </summary>
        [field: SerializeField]
        public bool InitializeGitRepository { get; set; } = true;

        /// <summary>
        /// Whether to generate a repository-level <c>README.md</c> file.
        /// </summary>
        [field: SerializeField]
        public bool IncludeReadme { get; set; } = true;

        /// <summary>
        /// Whether to generate a GitHub Actions caller for the shared package signing and release workflow.
        /// </summary>
        [field: SerializeField]
        public bool CreateSignUpmPackageWorkflow { get; set; }

        /// <summary>
        /// Remote URL assigned to the generated repository's <c>origin</c> remote when provided.
        /// </summary>
        [field: SerializeField]
        public string RepositoryUrl { get; set; } = string.Empty;

        /// <summary>
        /// Copies all repository-facing values from another settings instance.
        /// </summary>
        /// <param name="other">The source settings to copy from.</param>
        public void CopyFrom(RepoSettings other) {
            if (other == null) {
                return;
            }

            CopyrightHolder = other.CopyrightHolder;
            LicenseType = other.LicenseType;
            InitializeGitRepository = other.InitializeGitRepository;
            IncludeReadme = other.IncludeReadme;
            CreateSignUpmPackageWorkflow = other.CreateSignUpmPackageWorkflow;
            RepositoryUrl = other.RepositoryUrl;
        }
    }
}
