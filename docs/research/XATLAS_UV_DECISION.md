# xatlas UV unwrap decision

**Decision: REJECT as default / shipped backend for this cycle.**

## Need

Game-ready UV atlas unwrap beyond spherical projection.

## Candidates

- https://github.com/jpcy/xatlas (MIT, C++)
- https://github.com/EvergineTeam/xatlas.NET (MIT bindings)

## Why not shipped now

1. Requires native binaries per RID (win-x64 primary; Linux CI agent secondary).
2. Packaging + test matrix cost exceeds benefit while spherical UVs already satisfy remesh validation gates.
3. Integration glue would rival maintaining the current `UvUnwrapper.Spherical` path.

## What we did instead

- Introduced `IUvUnwrapper` with `SphericalUvUnwrapper` as the default implementation.
- Documented xatlas as a future optional backend behind the same interface when native packaging is justified.

## Custom code remaining

Spherical unwrap (~40 LOC) kept as the production default.
