# Font and Memory Notes

## Custom Font

The builder now looks for an optional loose font file here:

```text
src/BatmanSuitJsonBuilder/Assets/Fonts/DIN Condensed Bold.ttf
```

For a published build, place the same file next to the exe here if you want the custom font locally:

```text
Assets/Fonts/DIN Condensed Bold.ttf
```

The release zip does not include the font file. If the font is missing, the app falls back to installed fonts such as DIN Condensed, Bahnschrift Condensed, Agency FB, and Segoe UI.

## Memory cleanup

This build reduces memory use by clearing the hidden raw detection cache after playable JSON scanning. The UI keeps only the compact Known Editable Parts list that users actually work with.

The app logo image is decoded at a small display size instead of loading the full PNG resolution into the header.

The project file no longer embeds unused old UI PNGs. Only AppLogo.png and AppIcon.ico are embedded.
