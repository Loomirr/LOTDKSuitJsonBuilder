# JSON Compatibility Notes

This build has been checked against the current `dllmain(14).cpp` JSON reader.

## Top-level suit fields

The generator exports the fields expected by NewSuitSlotNative:

- `schema_version`
- `enabled`
- `menu_order`
- `slot_id`
- `display_name`
- `description`
- `source_tag`
- `source_actor_class_contains`
- `icon_asset`
- `uimd_icons.menu_icon`
- `uimd_icons.right_facing`
- `uimd_icons.left_facing`
- `operations`
- `equipment_replacements`

## Operation fields

Generated operations use these expected fields when needed:

- `type`
- `component`
- `component_class`
- `material_slot`
- `parameter_name`
- `asset`
- `materials`
- `value`
- `apply_to`
- `required`
- `enabled`
- `visible`
- `hidden`
- `propagate_to_children`
- `parent`
- `socket`
- `reinitialize_pose`

Vector values export as:

```json
"value": { "r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0 }
```

Scalar values export as:

```json
"value": 0.0
```

## Supported operation types in the builder

- `set_texture_parameter`
- `set_vector_parameter`
- `set_scalar_parameter`
- `set_material`
- `set_static_mesh`
- `set_skeletal_mesh`
- `clear_static_mesh`
- `clear_skeletal_mesh`
- `create_skeletal_component`
- `set_anim_class`
- `set_visibility`
- `set_hidden_in_game`
- `attach_component`
- `ensure_component`

`set_anim_class` sets a skeletal mesh component's animation blueprint at runtime
(via `SetAnimInstanceClass`). Its `asset` is the anim blueprint **generated class**
path, e.g. `/Game/Animation/LEGOfig/Nightwing/Traversal/ABP_Wingsuit.ABP_Wingsuit_C`
(note the `_C` suffix). Pair it with a `set_skeletal_mesh` on the same component
to swap both the mesh and its animation (e.g. a custom glider on the `Cape`).

`clear_static_mesh` / `clear_skeletal_mesh` remove the mesh from a component and
take no `asset`.

## Equipment replacement fields

Equipment replacements export as:

- `slot`
- `replace_equipment`
- `with_equipment`

The native parser also supports upgrade replacement fields, but the public builder UI currently focuses on equipment replacement only.
