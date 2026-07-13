# Release notes workflow

Track mod changes in a form you can turn into CurseForge / GitHub release notes each upload.

## Day to day

1. After meaningful changes, add bullets to [`unreleased.md`](unreleased.md) under the right heading.
2. Keep bullets **short and player-facing** where possible.
3. Agents should append to `unreleased.md` when completing player-visible work (see `.cursor/rules/release-notes.mdc`).

## Before a CurseForge / GitHub upload

```powershell
.\tools\release_notes.ps1 preview
.\tools\release_notes.ps1 cut --version 1.2.0 --bump-version
```

Then follow [`../PUBLISHING.md`](../PUBLISHING.md): build, `package_release.ps1`, upload, and paste the new `CHANGELOG.md` section into the file description.

`cut` prepends a dated section to [`CHANGELOG.md`](../../CHANGELOG.md) and resets `unreleased.md`. With `--bump-version` it also updates `VERSION`, `MODULE.bazel`, and `src/ModInfo.xml`.

Game support rows still live in [`../COMPATIBILITY.md`](../COMPATIBILITY.md).

## Files

| Path | Role |
|------|------|
| `docs/releases/unreleased.md` | Running draft |
| `docs/releases/unreleased.template.md` | Post-cut reset |
| `CHANGELOG.md` | Published history |
| `tools/release_notes.ps1` | Wrapper → sibling `7dtd-mod-dev-tools` script |
