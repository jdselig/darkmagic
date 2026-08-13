# Testing DarkMagic

DarkMagic includes Unity Test Framework assemblies under `Tests/`:

- EditMode: Stats, StatBlock, V owner lifetime, state registry, and Flow configuration.
- PlayMode: dynamic font rendering and floating outcome smoke tests.

## Test-project manifest

Reference the checkout being tested:

```json
{
  "dependencies": {
    "com.archenemy.darkmagic": "file:/absolute/path/to/darkmagic",
    "com.unity.test-framework": "1.7.0"
  },
  "testables": [
    "com.archenemy.darkmagic"
  ]
}
```

Keep any other dependencies your project needs. DarkMagic itself relies on Unity’s built-in UGUI/TextMeshPro assemblies.

## One-command validation

Close that Unity project, then run:

```bash
Scripts~/validate.sh /absolute/path/to/DarkMagicTest
```

The script:

1. Confirms the test project references this exact checkout.
2. Rejects Unity 6.5-incompatible `GetInstanceID()` calls.
3. Rejects `GetEntityId()` so Unity 6.3/6.4 remain supported.
4. Checks README/package version consistency.
5. Compiles the package in batch mode.
6. Runs EditMode tests.
7. Runs PlayMode tests.

Results and Unity logs are written to `TestResults~/`, which Unity and Git ignore.

After committing a release and creating its `vX.Y.Z` tag, run `Scripts~/check-release.sh` to verify that the working tree, tag, README install URL, and changelog agree.

Push the release commit before its tag:

```bash
git push -u origin your-release-branch
git push origin vX.Y.Z
```

The tag push runs `.github/workflows/release.yml`, which creates a public GitHub Release with generated release notes. Pushing only the branch does not publish the release.

To choose a specific editor:

```bash
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity" \
  Scripts~/validate.sh /absolute/path/to/DarkMagicTest
```

## Manual visual smoke pass

Before a UI release, also verify in the Game view:

- Default JRPG and Liberation font presets render letters, numbers, and punctuation.
- Banner, dialogue, choice, and display placement.
- Target marker cycling and cancellation.
- `OutcomeAnchor`, colliders, and fallback outcome positioning.
- One active Main camera, or an explicit camera passed to `PopOutcome`/target rules.
