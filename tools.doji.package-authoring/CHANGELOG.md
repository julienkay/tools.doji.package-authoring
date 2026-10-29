# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this package adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Generated package release workflows call the shared signing workflow instead of duplicating its implementation in every repository.

## [1.0.4] - 2026-10-29

### Fixed

- Preserve render pipeline assets, renderers, volume profiles, and their Unity metadata in generated standalone and companion projects so URP settings retain valid asset references.
- Resolve and import generated companion projects before Git initialization so their package lockfile is included in the initial commit, including when auto-open is disabled.

## [1.0.3] - 2026-10-20

### Added

- Generated package manifests use the current Unity editor version for compatibility metadata by default.
- Generate Editor test assembly definitions with Unity's `TestAssemblies` optional reference when package tests are enabled.
- Optional generation of a GitHub Actions workflow that signs and releases the package when a version tag is pushed.

### Fixed

- Configure the editor test assembly through Unity's `TestAssemblies` optional reference.
- Default blank `unityRelease` values to the current editor release in generated package manifests.

### Changed

- Always include Unity compatibility metadata in generated package manifests; configure its values in a collapsed-by-default foldout.
- Rename the package and repository to `tools.doji.package-authoring`.

## [1.0.2] - 2026-10-19

### Fixed

- Use Unity's `EntityId` API for editor drawer state keys on supported Unity versions, while preserving compatibility with older Unity releases.

## [1.0.1] - 2026-05-18

### Fixed

- Prevented repository initialization from freezing the Unity editor when git commands block on output pipes, prompts, signing, hooks, or other long-running child processes.

## [1.0.0] - 2026-05-17

### Added

- Package and companion project creation wizards for scaffolding UPM-ready repository layouts.
- Standalone project creation wizard for cloning the template project's `Packages` and `ProjectSettings` into a new Unity project.
- Template generation for package manifests, companion project manifests, asmdefs, assembly info, licenses, readmes, changelogs, sample scripts, docfx documentation files, and repository `AGENTS.md` files.
- Project-wide defaults and preset assets for reusing shared package, repository, and project authoring configuration across both wizards.
- Structure preview and output path previews for generated package repositories and companion projects, including repository hover mappings and inspector previews.
- Dependency editing with manifest-style list controls and package autocomplete sourced from Unity and scoped registries.
- Registry-aware autocomplete suggestions with source badges, version hints, and scrollable overflow handling inside Project Settings and wizard UIs.
- Configurable repository initialization, remote URL templating, optional readme generation, configurable documentation branding assets, and configurable preferred editor selection for generated companion projects.
- Unity `.meta` file generation for scaffolded package assets so generated repositories preserve expected Unity asset metadata.
- Public editor scripting API through `Doji.PackageAuthoring.PackageAuthoringApi` for generating standalone Unity projects and package repositories from code.
- Public settings models under `Doji.PackageAuthoring.Models` for configuring generated projects, packages, repository metadata, preferred editor packages, and explicit manifest dependencies.
