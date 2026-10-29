using Newtonsoft.Json;

namespace Doji.PackageAuthoring.Wizards.Templates {
    /// <summary>
    /// Connects generated package releases to the centrally maintained signing workflow.
    /// </summary>
    internal static class SignUpmPackageWorkflowTemplate {
        private const string PackagePathToken = "__PACKAGE_PATH__";

        public static string GetContent(string packageName) {
            return @"name: Sign and release

on:
  push:
    tags:
      - ""*""

permissions:
  contents: write

jobs:
  sign:
    uses: julienkay/tools.doji.package-authoring/.github/workflows/sign-upm-package.yml@master
    with:
      package-path: __PACKAGE_PATH__
    secrets:
      UPM_SERVICE_ACCOUNT_KEY_ID: ${{ secrets.UPM_SERVICE_ACCOUNT_KEY_ID }}
      UPM_SERVICE_ACCOUNT_KEY_SECRET: ${{ secrets.UPM_SERVICE_ACCOUNT_KEY_SECRET }}
      UPM_ORG_ID: ${{ secrets.UPM_ORG_ID }}
".Replace(PackagePathToken, JsonConvert.SerializeObject(packageName));
        }
    }
}
