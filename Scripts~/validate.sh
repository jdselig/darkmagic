#!/usr/bin/env bash

set -euo pipefail

package_root="$(cd "$(dirname "$0")/.." && pwd)"
project_path="${1:-}"
results_dir="$package_root/TestResults~"

if [[ -z "$project_path" ]]; then
  echo "Usage: Scripts~/validate.sh /path/to/UnityTestProject"
  exit 2
fi

if [[ ! -f "$project_path/ProjectSettings/ProjectVersion.txt" ]]; then
  echo "Not a Unity project: $project_path"
  exit 2
fi

if [[ -f "$project_path/Temp/UnityLockfile" ]]; then
  echo "Close the Unity project before running batch validation: $project_path"
  exit 2
fi

unity_path="${UNITY_PATH:-}"
if [[ -z "$unity_path" ]]; then
  project_editor="$(sed -n 's/^m_EditorVersion: //p' "$project_path/ProjectSettings/ProjectVersion.txt" | head -1)"
  matching_editor="/Applications/Unity/Hub/Editor/$project_editor/Unity.app/Contents/MacOS/Unity"
  if [[ -x "$matching_editor" ]]; then
    unity_path="$matching_editor"
  else
    unity_path="$(find /Applications/Unity/Hub/Editor -path '*/Unity.app/Contents/MacOS/Unity' -type f 2>/dev/null | sort | tail -1)"
  fi
fi

if [[ -z "$unity_path" || ! -x "$unity_path" ]]; then
  echo "Unity was not found. Set UNITY_PATH to the Unity executable."
  exit 2
fi

if ! grep -Fq "file:$package_root" "$project_path/Packages/manifest.json"; then
  echo "The test project must reference this checkout: file:$package_root"
  exit 2
fi

if ! grep -A4 '"testables"' "$project_path/Packages/manifest.json" | grep -Fq 'com.archenemy.darkmagic'; then
  echo 'Add "com.archenemy.darkmagic" to the manifest.json "testables" array.'
  exit 2
fi

if rg -n '\.GetInstanceID\s*\(' "$package_root/Runtime" "$package_root/Editor" --glob '*.cs'; then
  echo "Unity 6.5-incompatible GetInstanceID call found."
  exit 1
fi

version="$(sed -n 's/.*"version": "\([^"]*\)".*/\1/p' "$package_root/package.json" | head -1)"
if ! grep -Fq "darkmagic.git#v$version" "$package_root/README.md"; then
  echo "README install tag does not match package version $version."
  exit 1
fi

mkdir -p "$results_dir"

echo "Compiling DarkMagic with $($unity_path -version)..."
"$unity_path" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$project_path" \
  -logFile "$results_dir/compile.log"

echo "Running EditMode tests..."
"$unity_path" \
  -batchmode \
  -nographics \
  -projectPath "$project_path" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$results_dir/editmode.xml" \
  -logFile "$results_dir/editmode.log"

echo "Running PlayMode tests..."
"$unity_path" \
  -batchmode \
  -nographics \
  -projectPath "$project_path" \
  -runTests \
  -testPlatform PlayMode \
  -testResults "$results_dir/playmode.xml" \
  -logFile "$results_dir/playmode.log"

for result in "$results_dir/editmode.xml" "$results_dir/playmode.xml"; do
  if ! grep -q 'result="Passed"' "$result" || ! grep -q 'failed="0"' "$result"; then
    echo "Tests did not pass: $result"
    exit 1
  fi
done

echo "DarkMagic $version passed compile, EditMode, and PlayMode validation."
