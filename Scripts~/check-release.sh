#!/usr/bin/env bash

set -euo pipefail

package_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$package_root"

version="$(sed -n 's/.*"version": "\([^"]*\)".*/\1/p' package.json | head -1)"
tag="v$version"

if [[ -n "$(git status --porcelain)" ]]; then
  echo "Release check requires a clean working tree."
  exit 1
fi

if ! grep -Fq "darkmagic.git#$tag" README.md; then
  echo "README install URL does not use $tag."
  exit 1
fi

if ! grep -Fq "## [$version]" CHANGELOG.md; then
  echo "CHANGELOG.md has no $version section."
  exit 1
fi

if ! grep -Fq "MIT License" LICENSE; then
  echo "LICENSE is missing the MIT license text."
  exit 1
fi

if ! grep -Fq 'gh release create' .github/workflows/release.yml; then
  echo "The tag-triggered GitHub Release workflow is missing."
  exit 1
fi

if ! git rev-parse --verify --quiet "$tag^{commit}" >/dev/null; then
  echo "Create the release tag: git tag $tag"
  exit 1
fi

if [[ "$(git rev-parse HEAD)" != "$(git rev-parse "$tag^{commit}")" ]]; then
  echo "$tag does not point at HEAD."
  exit 1
fi

echo "DarkMagic $version release metadata is consistent."
