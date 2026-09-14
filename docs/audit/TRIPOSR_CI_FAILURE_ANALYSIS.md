# TripoSR CI Failure Analysis

**Failing run ID:** [34790344214](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34790344214)  
**Workflow:** TripoSR CPU Online Proof (`runtime-triposr-cpu.yml`)  
**Job:** TripoSR CPU PASS_REAL  
**Failed step:** Build + run TripoSR live generate  
**main HEAD at failure:** `51038585848a6f34a5a1c3cff8480242799f4f81`

## Exact failure

```
System.IO.IOException : The process cannot access the file
'...\3DGodCreator.Core.Tests\bin\Release\net10.0\artifacts\runtime\triposr\chair-cpu.glb'
because it is being used by another process.

at System.IO.FileSystem.CopyFile(...)
at CiOnlineRuntimeProofTests.CopyArtifact(...)  // line 42 (pre-fix)
at CiOnlineRuntimeProofTests.TripoSr_CpuGenerate_WritesValidatedGlb()  // line 99
```

- Exit code: **1**
- Failed test duration: **~1 m 26 s**
- Checkpoint acquire, `uv sync`, and Release build: all **success**

## What already worked (critical)

1. MIT checkpoint acquire (SHA-256 pinned) + cache hit/miss path
2. `uv sync --project workers/triposr --frozen`
3. Release build of `3DGodCreator.Core.Tests`
4. **Real TripoSR CPU inference** completed and produced a GLB
5. `AssertRealGlb` ran **before** the failing `CopyArtifact` (stack order: assert at ~98, copy at 99)

This was **not** an inference, dependency, checkpoint, timeout, OOM, or product-runtime failure.

## Root cause classification

**H. test bug** (harness path / Windows same-path `File.Copy`)

Evidence:

1. The test writes the GLB to `Path.Combine(ArtifactRoot(), "triposr", "chair-cpu.glb")`.
2. It then calls `CopyArtifact(path, "triposr/chair-cpu.glb")`, which resolves to the **same full path**.
3. On Windows, `File.Copy(src, dest, overwrite: true)` when `src` equals `dest` throws `IOException` (“used by another process”).
4. Secondary issue: `THREEDGOD_CI_ARTIFACT_DIR=artifacts/runtime` was relative. Under `dotnet test`, CWD is often `bin/Release/net10.0`, so artifacts landed under the test output tree instead of the repo-root path the workflow uploads (`artifacts/runtime/triposr/`).

## Proposed fix (minimal)

1. Resolve relative `THREEDGOD_CI_ARTIFACT_DIR` against **repo root** (`RepoPaths.FindRepoRoot()`), not process CWD.
2. In `CopyArtifact`, if source and destination are the same full path, **skip** the copy.
3. Set workflow env `THREEDGOD_CI_ARTIFACT_DIR` to `${{ github.workspace }}/artifacts/runtime` (absolute).

**Not changed:** GLB validation thresholds, inference path, model pin, or soft-skip behavior.

**Why minimal:** Product/worker inference already succeeded on the failing run; only post-success artifact path handling was wrong.

## Rejected classifications

| Class | Why rejected |
|-------|----------------|
| A CI config | Prior steps green; job timeout 180m unused |
| C deps | `uv sync` succeeded |
| D checkpoint | Acquire succeeded |
| E/F Windows/CPU | Real inference finished in ~86s on `windows-latest` |
| G timeout/OOM | Failure is `IOException` on copy, not kill/timeout |
| I product | Worker produced a GLB that reached `AssertRealGlb` |
| J network | No download failure in the failed step |

## Expected status after fix

`PASS_REAL` on GitHub-hosted `windows-latest`, given the same inference path already completed successfully on run 34790344214 before the harness bug.

---

## Final verification (GitHub-hosted)

| Field | Value |
|-------|-------|
| Final classification | **PASS_REAL** |
| Fix tip SHA | `f933145d8ac1d4da244fd887958a955d87e44e5a` |
| Green workflow run | [34792261073](https://github.com/deadfrogface/3DGod-V2.0/actions/runs/34792261073) |
| Job | TripoSR CPU PASS_REAL — **success** |
| Proof line | `TRIPOSR_RUNTIME=PASS_REAL bytes=228776` |
| Test | `CiOnlineRuntimeProofTests.TripoSr_CpuGenerate_WritesValidatedGlb` **Passed** (~1 m 42 s) |
| Runner | `windows-latest` (Windows Server 2025) |
| Checkpoint cache | used (acquire step success) |
| Validation weakened? | **No** |
| Remaining TripoSR limits | Heavy job (~1.6GB ckpt); kept off default every-PR CI except path filters / weekly / dispatch |
| PR | https://github.com/deadfrogface/3DGod-V2.0/pull/8 |

