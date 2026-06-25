# Batman Suit JSON Builder - Quick User Tutorial

Batman Suit JSON Builder is a helper tool for making and editing `suit.json` files for custom LEGO Batman suits. The tool is meant to save time by reading an exported playable character JSON, showing the parts/materials it finds, and generating the correct suit operations for the mod.

## What you need before starting

Before using the tool, you should have:

- An exported playable Batman JSON file, such as `BP_Batman_JusticeLeague_Playable.json`.
- Any custom Unreal asset paths you want to use, such as textures, materials, icons, or equipment assets.
- The correct source suit you want your custom suit to copy from.

You can also import an existing `suit.json` if you already made one and only want to add more changes.

## 1. Import your files

Open the tool and start on the Setup page.

To make a new suit, click **Import Playable JSON** and choose the exported playable character JSON that matches the suit you want to use as the source.

To continue working on an existing custom suit, click **Import Suit.JSON** and choose your existing `suit.json`. Then import the matching playable JSON too. The playable JSON should match the suit's `source_tag` and source actor class so the tool knows it is editing the correct suit.

After importing, the tool should fill in the source suit information and show available parts/components on the Build Changes page.

## 2. Fill out the Setup page

On the Setup page, fill in the basic information for your suit.

- **Slot ID** is the internal ID for the custom suit. Use lowercase text with letters, numbers, dashes, or underscores. Example: `batman_space_suit`.
- **Display Name** is the name players will see in the menu. Example: `Space Suit`.
- **Description** is the menu description for the suit.
- **Source Pawn Tag** is the original suit this custom suit copies from.
- **Source Actor Class Contains** should match the playable actor class for the source suit.
- **Icon Asset** is the main icon path.
- **UIMD Icons** are the menu, right-facing, and left-facing portrait/icon paths.

Only use Unreal-style asset paths. For example:

```json
/Game/Mods/SpaceLBM3/SpaceSuit.SpaceSuit
```

## 3. Choose a part to edit

Go to the **Build Changes** page.

The left side shows the parts/components found in the playable JSON. Select the part you want to edit, such as `CharacterMesh0`, `Head`, `Cape`, `Torso`, or another attachment component.

When you select a part, the Selected Part area shows information about it. If the part has materials, the Material Slots box shows which material is in each slot.

For example:

```text
[0] /Game/Characters/...
[1] /Game/Characters/...
```

The slot number matters. If you want to replace material slot 0, make sure your operation also uses material slot 0.

## 4. Create operations

After selecting a part, use the operation buttons to create changes.

Common operations include:

- **Set Texture Parameter** changes a texture parameter like `BC`, `DNRM`, `MMR`, or `NRM`.
- **Set Vector Parameter** changes a color parameter like `Base Color`, `Base Colour`, or `EmissiveColour`.
- **Set Scalar Parameter** changes a number value on a material.
- **Set Material** replaces a material in a specific material slot.
- **Set Static Mesh** replaces a mesh asset.
- **Set Visibility / Hidden In Game** hides or shows a component.

Once an operation is created, edit its value in the Generated Changes table. You can right-click the Value cell to copy or paste values.

## 5. Editing colors

Vector color operations export as RGBA values.

Example:

```json
"value": {
  "r": 0.953,
  "g": 0.914,
  "b": 0.863,
  "a": 1
}
```

Use values from 0 to 1, not 0 to 255. A value of `1` is full strength, and `0` is none.

## 6. Editing equipment

Go to the **Equipment** page if the suit should replace equipment.

For example, a suit could replace Batman's normal batarang with another equipment asset.

Make sure the original equipment path and replacement equipment path are both valid Unreal asset paths.

## 7. Export the suit

When you are finished, go to the **Export** page and review the final output.

Click **Export Suit.JSON** to save the file.

The exported file should be placed in the custom suit folder used by the NewSuitSlotNative mod.

## 8. Test in game

After exporting, test the suit in game.

Check these things:

- The suit appears in the menu.
- The name, description, and icons are correct.
- The preview model updates correctly.
- The playable suit applies correctly in gameplay.
- Materials, textures, colors, hidden parts, and equipment changes work as expected.

If something does not work, reopen the `suit.json`, import the matching playable JSON, make changes, export again, and retest.

## Helpful screenshots to include with this tutorial

Add screenshots anywhere they help users understand the tool. The most useful screenshots would be:

1. **Main Setup page after opening the tool**  
   Show the Import Suit.JSON and Import Playable JSON buttons.

2. **Setup page filled out with an example suit**  
   Show Slot ID, Display Name, Description, Source Pawn Tag, Source Actor Class Contains, Icon Asset, and UIMD Icons filled in.

3. **Build Changes page after importing a playable JSON**  
   Show the detected part/component list.

4. **Selected Part with Material Slots visible**  
   Show a part like Head, Cape, or CharacterMesh0 with material slots `[0]`, `[1]`, etc.

5. **Creating an operation**  
   Show the operation buttons and one selected part.

6. **Generated Changes table**  
   Show a few created operations and the Value column.

7. **Right-click Copy/Paste Value menu**  
   Show the context menu on a Value cell.

8. **Equipment page**  
   Show where equipment replacements are added.

9. **Export page before saving**  
   Show the final review/output area.

10. **In-game result**  
   Show the custom suit in the suit menu or gameplay after the exported `suit.json` works.

## Quick troubleshooting

If the tool rejects a playable JSON after importing a suit config, make sure the playable JSON matches the source suit used by that `suit.json`.

If a material change does not work, check the component name and material slot number.

If a texture or icon does not load, check that the Unreal asset path is spelled correctly and includes both the package and object name.

If a color change does not work, make sure the parameter name matches the material exactly, including spaces and spelling.
