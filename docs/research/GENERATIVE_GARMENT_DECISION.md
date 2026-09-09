# Generative garment decision (PHASE 45)

Stand: 2026-09-09  
Frage: Soll 3D God einen **Experimental**-Provider für Text/Bild→Schnittmuster/Kleidung bekommen?

**Entscheidung: Nein. Kein Provider, kein UI-Button.**  
`garment.generate.ai` bleibt **NotImplemented**. Produktpfad bleibt pygarment (PHASE 42) + parametrische Templates (PHASE 41).

Diese Maschine hat **kein CUDA**. Selbst ein späterer Opt-in-Worker wäre hier **UnsupportedHardware**.

---

## 1. DressCode (SewingGPT)

| | |
|---|---|
| Repo | https://github.com/IHe-KaiI/DressCode |
| Paper | SIGGRAPH 2024, arXiv:2401.16465 |
| Lizenz | **Keine SPDX-Lizenz** auf GitHub (`license: null`). Unklar für Produkt-Bundling. |
| Weights | Hugging Face `IHe-KaiI/DressCode` (SewingGPT + optionales PBR-Modell) |
| Datasets | Korosteleva/Lee 2021 sewing patterns + GPT-4V captions |
| Dependencies | Conda-Env, **Stable Diffusion 2-1**, optional **Maya** (`mayapy`) + **Blender** für Sim/Render, OpenAI-API für Chat-UI |
| Hardware | GPU-Training/Inferenz impliziert; Sim Windows-only |

**Blocker:** fehlende Produktlizenz, SD-2.1-Gewichte, Maya-Sim, CUDA. Kein isolierter Headless-Pfad ohne diese Stacks.

---

## 2. GarmentDiffusion

| | |
|---|---|
| Repo | https://github.com/Shenfu-Research/GarmentDiffusion |
| Paper | IJCAI 2025, arXiv:2504.21476 |
| Lizenz | **Keine SPDX-Lizenz** (`license: null`). README ist Paper-Code-Hinweis, kein Install-Worker. |
| Weights | Nicht als klarer, lizenzierter Checkpoint für Produktnutzung dokumentiert |
| Datasets | DressCodeData, SewFactory, GarmentCodeData |
| Dependencies | Diffusion Transformer; referenziert DressCode / PyGarment / SewFormer |
| Hardware | GPU-Diffusion |

**Blocker:** keine geklärte Lizenz, kein reproduzierbarer isolierter Worker, CUDA.

---

## 3. Garment3DGen

| | |
|---|---|
| Repo | https://github.com/nsarafianos/Garment3DGen |
| Paper | 3DV 2025, arXiv:2403.18816 (Meta Reality Labs) |
| Lizenz | **CC BY-NC 4.0** (`LICENSE.md`) — **nicht kommerziell** |
| Weights / Deps | PyTorch CUDA 11.8, **nvdiffrast**, **PyTorch3D**, CLIP, Fashion-CLIP, InstantMesh/Wonder3D-Targets |
| Zweck | Stylize eines **bestehenden** Garment-Mesh (nicht Schnittmuster aus Text) |

**Blocker:** NonCommercial schließt Produkt-Bundling aus. Schwerer GPU-Stack. Kein Ersatz für pygarment-Jacken.

---

## 4. Hardware hier

- Kein `nvidia-smi` / kein CUDA in der Build-Umgebung (bereits SkinTokens **UnsupportedHardware**).
- Generative Garment-Modelle wären selbst mit Lizenz **UnsupportedHardware**, kein Fake-Mesh.

---

## 5. Was wir stattdessen behalten

| Feature | Status |
|---|---|
| `garment.templates` | Available (parametrisch) |
| `garment.garmentcode` | Experimental (pygarment MIT-Core, kein Warp-Bundling) |
| `clothing.fit` | Experimental (geometry3Sharp) |
| `garment.skin` | Experimental (Weight-Transfer) |
| `garment.generate.ai` | **NotImplemented** — kein Button, kein Worker |

Erneute Prüfung nur wenn: SPDX-Lizenz kommerziell ok, Weights mit klarer Produktfreigabe, isolierter uv-Worker, CUDA-Gate ehrlich **UnsupportedHardware** ohne Mesh-Fake.
