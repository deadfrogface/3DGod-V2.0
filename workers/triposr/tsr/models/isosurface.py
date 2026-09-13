from typing import Callable, Optional, Tuple

import numpy as np
import torch
import torch.nn as nn

def _marching_cubes(level: torch.Tensor, threshold: float = 0.0):
    """Prefer torchmcubes; fall back to scikit-image (CPU)."""
    try:
        from torchmcubes import marching_cubes as _mc
        return _mc(level, threshold)
    except Exception:
        from skimage.measure import marching_cubes as _sk_mc
        vol = level.detach().cpu().numpy()
        verts, faces, _normals, _values = _sk_mc(vol, level=threshold)
        # skimage returns (V,3) float and (F,3) int; torchmcubes uses same convention
        v = torch.from_numpy(verts.astype(np.float32))
        f = torch.from_numpy(faces.astype(np.int64))
        return v, f


class IsosurfaceHelper(nn.Module):
    points_range: Tuple[float, float] = (0, 1)

    @property
    def grid_vertices(self) -> torch.FloatTensor:
        raise NotImplementedError


class MarchingCubeHelper(IsosurfaceHelper):
    def __init__(self, resolution: int) -> None:
        super().__init__()
        self.resolution = resolution
        self.mc_func: Callable = _marching_cubes
        self._grid_vertices: Optional[torch.FloatTensor] = None

    @property
    def grid_vertices(self) -> torch.FloatTensor:
        if self._grid_vertices is None:
            x, y, z = (
                torch.linspace(*self.points_range, self.resolution),
                torch.linspace(*self.points_range, self.resolution),
                torch.linspace(*self.points_range, self.resolution),
            )
            x, y, z = torch.meshgrid(x, y, z, indexing="ij")
            verts = torch.cat(
                [x.reshape(-1, 1), y.reshape(-1, 1), z.reshape(-1, 1)], dim=-1
            ).reshape(-1, 3)
            self._grid_vertices = verts
        return self._grid_vertices

    def forward(
        self,
        level: torch.FloatTensor,
    ) -> Tuple[torch.FloatTensor, torch.LongTensor]:
        level = -level.view(self.resolution, self.resolution, self.resolution)
        try:
            v_pos, t_pos_idx = self.mc_func(level.detach(), 0.0)
        except Exception:
            print("marching cubes CUDA path failed; using CPU volume.")
            v_pos, t_pos_idx = self.mc_func(level.detach().cpu(), 0.0)
        v_pos = v_pos[..., [2, 1, 0]]
        v_pos = v_pos / (self.resolution - 1.0)
        return v_pos.to(level.device), t_pos_idx.to(level.device)
