# GitHub Repository Setup Guide

This guide assumes you are starting from the cleaned project folder, not from an old messy working folder.

## 1. Unzip the cleaned project

Unzip the cleaned package somewhere simple, for example:

```text
C:\Users\YourName\Documents\GitHub\BatmanSuitJsonBuilder
```

Open that folder. The root should contain files like:

```text
BatmanSuitJsonBuilder.sln
README.md
.gitignore
build.bat
publish_win_x64.bat
src\
docs\
examples\
```

## 2. Make sure private files are not present

Before creating the Git repo, check that these are not in the folder:

```text
.vs\
bin\
obj\
publish\
*.exe
*.zip
*.ttf
*.otf
DiscordPresence.local.json
```

The included `.gitignore` should block them, but it is still better to check first.

## 3. Test the build before committing

From the project root, run:

```bat
build.bat
```

Or:

```bat
dotnet build BatmanSuitJsonBuilder.sln -c Debug
```

Fix build errors before publishing the code.

## 4. Create a new empty GitHub repo

On GitHub:

1. Click **New repository**.
2. Name it something like `BatmanSuitJsonBuilder`.
3. Start it as **Private** first.
4. Do not add a README, license, or `.gitignore` on GitHub, because this cleaned folder already has them.
5. Create the repo.

## 5. Initialize Git locally

Open Git Bash, PowerShell, or Terminal in the project root.

Run:

```bat
git init
git branch -M main
git status
```

Check the file list. Make sure no local/private files are staged.

## 6. First commit

Run:

```bat
git add .
git status
git commit -m "Initial public cleanup"
```

## 7. Connect to GitHub

Copy the repo URL from GitHub, then run:

```bat
git remote add origin https://github.com/YOUR_USERNAME/BatmanSuitJsonBuilder.git
git push -u origin main
```

Replace `YOUR_USERNAME` with your GitHub username.

## 8. Enable GitHub security options

After pushing, open the repo on GitHub.

Recommended settings:

1. Go to **Settings**.
2. Go to **Code security and analysis**.
3. Enable secret scanning if available.
4. Enable push protection if available.
5. Keep the repo private until you are confident it is clean.

## 9. Make a release build

From the project root, run:

```bat
publish_win_x64.bat
```

The release EXE will be in:

```text
publish\win-x64-single
```

Do not commit the `publish` folder.

## 10. Create a GitHub Release

On GitHub:

1. Open the repo.
2. Click **Releases**.
3. Click **Draft a new release**.
4. Tag it something like `v0.9.0-beta`.
5. Upload the built `BatmanSuitJsonBuilder.exe` from `publish\win-x64-single`.
6. Add release notes.
7. Publish the release.

## 11. Make the repo public later

Once the private repo looks clean:

1. Check the committed files one last time.
2. Check that no private config, app ID, font, or game asset was committed.
3. Make the repository public in GitHub settings.

## 12. Discord Rich Presence local setup

Do not commit your real Discord config.

For your own local build, copy:

```text
src\BatmanSuitJsonBuilder\Data\DiscordPresence.example.json
```

to:

```text
src\BatmanSuitJsonBuilder\Data\DiscordPresence.local.json
```

Then edit the local file. It is ignored by Git.

## 13. Useful Git commands

Check changed files:

```bat
git status
```

View files that will be committed:

```bat
git diff --staged
```

Commit changes:

```bat
git add .
git commit -m "Describe your change"
```

Push changes:

```bat
git push
```
