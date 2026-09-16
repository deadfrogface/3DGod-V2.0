# skintokens-cpp worker (3D God)

Isolated Auto-Rig backend wrapping [skin-tokens.cpp](https://github.com/localai-org/skin-tokens.cpp)
(Apache-2.0, pin hint `43e885af2eadee9c40aa85849b71528d1c958293`).

## Runtime layout

- CLI: `%LocalAppData%/3DGod/Components/skintokens-cpp/bin/skintokens-cli[.exe]`
- Models: `%LocalAppData%/3DGod/Models/skintokens-cpp/F16` (from `LocalAI-io/SkinTokens-GGUF`)

## Invoke

```
skintokens-cli rig <MODEL_DIR> <input.glb> <output.glb> --device cpu|vulkan|auto
```

Product code: `SkinTokensCppRuntime` / `AutoRigProviderSelector`. Never fake a skinned GLB.
