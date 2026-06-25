# Public Release Notes

## Public-source cleanup

This build is prepared for a clean GitHub repository.

Changes in this cleanup pass:

- Removed hardcoded Discord Application ID from source.
- Removed hardcoded Discord image/GIF URL from source.
- Added `DiscordPresence.example.json` as a safe template.
- Added support for private ignored `DiscordPresence.local.json`.
- Added environment variable support for `BATMAN_SUIT_JSON_BUILDER_DISCORD_APP_ID`.
- Added a root `.gitignore` for build outputs, local configs, and licensed font files.
- Moved user documentation into the `docs/` folder.
- Added GitHub setup instructions.
- Kept optional local font embedding without including the font file.

## Single EXE publish

Run:

```bat
publish_win_x64.bat
```

The release output will be in:

```text
publish\win-x64-single
```

Upload the built `.exe` to GitHub Releases. Do not commit built `.exe` files to the repository.

## Discord Rich Presence

Discord Rich Presence is disabled by default in the public source tree.

To enable it locally, copy:

```text
src\BatmanSuitJsonBuilder\Data\DiscordPresence.example.json
```

to:

```text
src\BatmanSuitJsonBuilder\Data\DiscordPresence.local.json
```

Then add your own Application ID and optional image asset key. The local config is ignored by Git.
