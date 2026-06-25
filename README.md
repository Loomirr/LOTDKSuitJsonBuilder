# Batman Suit JSON Builder

A Windows WPF tool for creating and editing `suit.json` files for the **NewSuitSlotNative** LEGO Batman suit-slot mod.

The app is meant to make suit config work less painful. It can import exported playable character JSON, detect known components/material slots, build supported operations, import an existing `suit.json`, and export a clean schema v2 config.

## Current status

This is a public beta / release-candidate cleanup build. The core workflow is usable, but the game/mod side is still experimental and some advanced operation behavior may need testing per suit.

## What the tool does

- Imports exported playable character JSON files.
- Imports existing `suit.json` files.
- Shows known editable parts such as `CharacterMesh0`, `Head`, `Cape`, `Face`, `Torso`, `Hip`, and related attachment components.
- Shows detected material slots for selected parts.
- Creates operations for texture parameters, vector colors, scalar values, material swaps, mesh swaps, visibility, hidden-in-game state, attachments, ensured components, and equipment replacements.
- Exports clean `schema_version: 2` suit configs for NewSuitSlotNative.

## What the tool does not do

- It does not edit `.uasset` files.
- It does not cook or package Unreal assets.
- It does not include game assets.
- It does not include licensed fonts.
- It does not include a private Discord Application ID or token.

## Requirements

- Windows
- .NET 8 SDK or newer
- Visual Studio 2022 with the .NET desktop development workload, or the .NET CLI

## Build

From the repo root:

```bat
build.bat
```

Or manually:

```bat
dotnet build BatmanSuitJsonBuilder.sln -c Debug
```

## Publish a single EXE

From the repo root:

```bat
publish_win_x64.bat
```

The release output will be created in:

```text
publish\win-x64-single
```

Use `BatmanSuitJsonBuilder.exe` from that folder for a release build.

## Optional Discord Rich Presence

Discord Rich Presence is disabled by default in the public source tree.

No Application ID, client secret, token, or private Discord config is committed.

To enable it locally, copy:

```text
src\BatmanSuitJsonBuilder\Data\DiscordPresence.example.json
```

to:

```text
src\BatmanSuitJsonBuilder\Data\DiscordPresence.local.json
```

Then edit `DiscordPresence.local.json` with your own Discord Application ID and optional Rich Presence image asset key.

`DiscordPresence.local.json` is ignored by Git and should not be committed.

You can also set this environment variable instead of using a local config file:

```text
BATMAN_SUIT_JSON_BUILDER_DISCORD_APP_ID
```

## Optional font

The project has a build hook for a local DIN Condensed Bold font, but the font file is not included.

To use it locally, place your licensed copy here:

```text
src\BatmanSuitJsonBuilder\Assets\Fonts\DIN Condensed Bold.ttf
```

The file is ignored by Git. Do not commit or redistribute font files unless you have the legal right to do so.

## Docs

- `docs/QUICK_START.txt` - simple user guide.
- `docs/FULL_TOOL_GUIDE.txt` - detailed user guide.
- `docs/USER_TUTORIAL.md` - original user tutorial.
- `docs/JSON_SCHEMA_COMPATIBILITY.md` - notes about matching the NewSuitSlotNative schema.
- `docs/MEMORY_AND_FONT_NOTES.md` - memory/font notes.
- `docs/GITHUB_REPO_SETUP.md` - step-by-step GitHub setup instructions.

## Examples

The `examples/` folder contains small sample suit configs. These are examples only and do not include game assets.

## Safety notes before publishing

Before making a repository public, check that the repo does not contain:

- `bin/`, `obj/`, `.vs/`, or `publish/`
- `.exe`, `.zip`, or generated release files
- local Discord config files
- API keys, tokens, secrets, or private IDs
- licensed font files
- extracted game assets
- personal file paths from your PC

## License

This cleanup package includes an MIT license file. Replace it if you want to use a different license before publishing.
