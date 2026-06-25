# Font Embedding Notes

This project is set up so the custom font can be included inside the compiled EXE.

Place your licensed font file here before building:

```text
src/BatmanSuitJsonBuilder/Assets/Fonts/DIN Condensed Bold.ttf
```

When that file exists, `BatmanSuitJsonBuilder.csproj` embeds it as a WPF `Resource`:

```xml
<Resource Include="Assets\Fonts\DIN Condensed Bold.ttf" Condition="Exists('Assets\Fonts\DIN Condensed Bold.ttf')" />
```

The app uses this embedded resource through `App.xaml`:

```xml
pack://application:,,,/Assets/Fonts/#DIN Condensed Bold
```

The font file is intentionally not included in this patch package.
