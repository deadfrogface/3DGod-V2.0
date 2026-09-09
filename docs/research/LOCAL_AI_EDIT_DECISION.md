# Local AI mesh-edit decision (PHASE 47)

Stand: 2026-09-09  
Frage: Gibt es einen **produktklaren** lokalen AI-Pfad für Arbitrary Local Mesh Editing (Region, Boundary, UV, Rig-Transfer, Identity) auf Anny/GLB?

**Entscheidung: Nein. Kein Provider, kein Production-Button.**  
`mesh.edit.ai` bleibt **NotImplemented**. Produktpfad bleibt Stufe A+B aus dem Masterplan:

- **A)** deterministisch: Parameter / Morph-Keys / Material / Garment-Parameter (`DeterministicAiParser`, `AiEditExecutor`)
- **B)** modular: Katalog-Replace/Add (`CreatureTextEditService` — Kopf/Horn/Prothese)

`morph.local` setzt einen Phenotype-Parameter, es ist **kein** lokales Mesh-Inpaint.

Diese Maschine hat **kein CUDA**. Selbst ein späterer Opt-in-Worker wäre hier **UnsupportedHardware**.

Kein End-to-End-PoC auf einem skinned Anny-GLB (gleiche Vertex-IDs, UV, Weights) existiert.

---

## Feasibility-Benchmark

| Kriterium | BlendedPC | StructLDM | GaussCtrl | TrAME | Produktpfad A+B |
|---|---|---|---|---|---|
| **Local region** | Point-Cloud-Inpaint (Stuhl/Lampe/Tisch, ShapeTalk) | SMPL-Teil-Latents aus 2D | LangSAM-Objekt in 3DGS | 3DGS-View-Edit | Parameter-Key / Katalog-Slot |
| **Boundary** | Inference-time Coordinate Blending auf Punkten | Clothing-agnostic Part-Blend (Paper) | ControlNet-Views, keine Mesh-Naht | TAS/VCAC auf Splats | Katalog-`boundary:*` Tags, kein Seam-Weld-PoC |
| **UV** | keine (Point Cloud) | `smpl_uv.obj` (SMPL, nicht Anny) | keine (Gaussians) | keine | vorhandenes Remesh/Anny-UV, nicht AI-editiert |
| **Rig transfer** | keines | SMPL; Repo nennt SMPL-X-Lizenzdateien. **Nie SMPL-X im Produkt.** | keines | keines | `garment.skin` / Anny-Skeleton, nicht AI-regeneriert |
| **Identity** | Möbel-Identität vs. Prompt | SMPL Identity-Swap (CUDA+SMPL) | Szenen-Konsistenz, nicht Character-ID | 3DGS-Konsistenz | Anny-Phenotypes + Katalogteile |
| **Output** | Point Cloud / Render-PNG | SMPL-Human, nicht Anny-GLB | 3DGS / NeRFStudio | 3DGS `.ply` | GLB + `.3dgod` |

**PoC-Hürde:** Keiner der vier Stacks schreibt ein Anny-kompatibles Dreiecksnetz mit erhaltenen UVs und Skin-Weights.

---

## 1. BlendedPC

| | |
|---|---|
| Repo | https://github.com/TAU-VAILab/BlendedPC |
| Paper | ICCV 2025, arXiv:2507.15399 |
| Lizenz | **MIT** (SPDX) |
| Zweck | Localized text-guided **Point-Cloud**-Edit, finetuned Point-E, Demos: chair/lamp/table |
| Weights | Hugging Face Hub, ShapeTalk |
| Hardware | PyTorch-Diffusion → GPU/CUDA |

**Blocker für 3D God:** keine Dreiecksnetze, keine Menschen, keine UV/Skin. MIT allein reicht nicht für einen Character-Edit-PoC.

---

## 2. StructLDM

| | |
|---|---|
| Repo | https://github.com/TaoHuUMD/StructLDM |
| Paper | ECCV 2024, arXiv:2404.01241 |
| Lizenz | **S-Lab License 1.0** — **nur non-commercial** |
| Zweck | Structured latent diffusion für 3D-Menschen aus 2D; Part-Blend, Try-on, Identity-Swap |
| Weights | OneDrive-Modelle; **SMPL_NEUTRAL.pkl** (SMPL-Registrierung). README verweist zusätzlich auf SMPL-X-Lizenz |
| Hardware | **NVIDIA GPU required**, getestet V100; PyTorch 1.10 + CUDA 11.1 + PyTorch3D |

**Blocker:** NonCommercial, SMPL (Produktpfad ist Anny, nie SMPL-X), CUDA, kein Anny-Topology-PoC.

---

## 3. GaussCtrl

| | |
|---|---|
| Repo | https://github.com/ActiveVisionLab/gaussctrl |
| Paper | ECCV 2024, arXiv:2403.08733 |
| Lizenz Code | **BSD-3-Clause** |
| Zweck | Text-driven **3D Gaussian Splatting** Scene-Edit (NeRFStudio `splatfacto` + ControlNet) |
| Weights | Stable Diffusion (`CompVis/stable-diffusion-v1-4` / früheres SD-1.5); tiny-cuda-nn; LangSAM |
| Hardware | CUDA 11.8, getestet **RTX A5000 24 GB**; Chunk-Size 3 ≈ 22 GB |

**Blocker:** 3DGS, nicht authored Mesh. SD-Gewichte + CUDA. Kein UV/Rig auf Anny. Code-Lizenz ok, Pipeline nicht.

---

## 4. TrAME

| | |
|---|---|
| Repo | https://github.com/luocfprime/TrAME |
| Paper | IEEE TMM 2025, doi:10.1109/TMM.2025.3557618 |
| Lizenz | **S-Lab License 1.0** (`LICENSE.txt`) — **nur non-commercial** |
| Zweck | Trajectory-Anchored Multi-View Edit auf **3DGS** (GaussianEditor + threestudio); Input: COLMAP + `.ply` |
| Hardware | Ubuntu, **CUDA 11.8**, `--gpu 0` |

**Blocker:** NonCommercial, 3DGS, CUDA, kein Mesh/UV/Skin-Output.

---

## 5. Hardware hier

- Kein `nvidia-smi` / kein CUDA (wie SkinTokens / generative Garment).
- Alle vier Research-Stacks wären **UnsupportedHardware**, kein Fake-Mesh.

---

## 6. Was wir stattdessen behalten

| Feature | Status |
|---|---|
| `creature.edit.text` | Available (Katalog Replace/Add) |
| Deterministic AI-Pläne | Available (Allow-List, kein Arbitrary Code) |
| `mesh.edit.ai` | **NotImplemented** — kein Button, kein Worker |
| LlamaSharp / FLUX / TripoSR | unverändert NotInstalled / NotImplemented |

Erneute Prüfung nur wenn: kommerzielle SPDX-Lizenz, CUDA-Gate ehrlich, **und** ein reproduzierbarer PoC ein Anny/GLB mit UV+Weights zurückschreibt (Vertex-Counts, Boundary-Naht, Identity-Metrik). Bis dahin Stufe A+B.
