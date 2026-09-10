# geometry3Sharp Reducer remesh decision

**Decision: WRAP as optional backend `geometry3sharp-qem`; KEEP `vertex-cluster` as default.**

## Need

Higher-quality mesh simplification than vertex clustering for game LODs.

## Candidate

- Library: [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp) (already referenced for garment proximity fit)
- API: `g3.Reducer.ReduceToTriangleCount`

## Evaluation (synthetic grid meshes)

| Metric | vertex-cluster (default) | geometry3sharp-qem |
|--------|--------------------------|--------------------|
| Produces valid mesh (MeshValidator) | Yes | Yes |
| Reduces triangle count | Yes | Yes |
| Extra native deps | No | No (managed) |
| Already in dependency graph | Indirectly unused for remesh | Yes |
| Risk to shipping assets / skin / UV | Low (battle-tested in CI) | Medium (needs broader asset corpus) |

## Verdict

Do **not** silently replace the default. Expose QEM behind `IMeshProcessor` / `THREEDGOD_MESH_PROCESSOR=geometry3sharp-qem` and DI override. Revisit default only after skinned character + garment regression corpus stays green.
