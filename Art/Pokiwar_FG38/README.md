# Portable Pokiwar art workspace

All active art paths are relative to the repository root. Clone or pull this repository on another
machine; no external `D:` directory is required to continue the art set.

Read `Docs/Pokiwar_Art_Codex_Next.md` for the current order, then the files in this directory:

- `ART_BIBLE.md`: drawing style, palette, silhouette, shading and export rules.
- `AssetManifest.json`: accepted exports and their canonical filenames under `Assets/`.
- `Previews/Battle_form01_v01.png`: the visual style reference.
- `Sources/Batch19/definitions.json`: the exact five remaining prompts, sizes and reference paths.
- `Sources/**/*.prompt.txt`: preserved generation prompts from earlier batches.
- `AnimationSpec.json`: animation timing; `ASSET_PLAN.md`: the original production plan.

The current order overrides the original production plan. Check actual files before generating:
existing exports are `HAVE`, even if an old plan or provenance entry says otherwise. Batch 19 still
has five missing exports; this migration does not generate them or change previous approvals.

Paths inside Batch 19 definitions are relative to this art workspace. Save accepted PNGs under
`Assets/<file from the definition>`, preserve new raw sources and prompts under `Sources/<batch>`,
and update `AssetManifest.json` after verification. The Unity importer resolves this workspace from
the project location. `Tools/ReframeSecondForms.ps1` uses the same local character source folder.
Keep the existing Unity scene; importing art is not permission to regenerate the scene.

Manifest raw-source paths, generator-session paths and earlier reference paths are historical
provenance from the original workstation. The old raw archive and external FG38 reference game
are not included. They are unnecessary for the five current prompts: all their references are
bundled here, together with the accepted exported art. Do not treat those historical paths as
required setup, or claim an original raw file has been bundled.

The generator must be available in the assistant session used on the other machine. These files
provide the art inputs; they do not install an image-generation tool or transfer its quota.
