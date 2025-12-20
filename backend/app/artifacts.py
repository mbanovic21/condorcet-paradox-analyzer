from __future__ import annotations

from pathlib import Path
from typing import Optional
import base64
import datetime
import json
import subprocess

from .models import ElectionInput, AnalysisResult


def _repo_root() -> Path:
    # backend/app/artifacts.py -> backend/app -> backend -> repo root
    return Path(__file__).resolve().parents[2]


def _safe_slug(s: str) -> str:
    s = (s or "").strip().lower().replace(" ", "_")
    out = []
    for ch in s:
        if ch.isalnum() or ch in ("_", "-"):
            out.append(ch)
    return "".join(out) or "run"


def _git_short_hash(repo_root: Path) -> str:
    try:
        r = subprocess.run(
            ["git", "rev-parse", "--short", "HEAD"],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            check=True,
        )
        return r.stdout.strip()
    except Exception:
        return "unknown"


def save_run_artifact(
    payload: ElectionInput,
    result: AnalysisResult,
    *,
    tag: str = "run",
    notes: str = "",
    extra_tags: Optional[list[str]] = None,
) -> Path:
    """
    Saves a run to:
      artifacts/runs/YYYY-MM-DD_HH-MM-SS_<tag>/
        input.json
        output.json
        graph.png
        meta.json
    Returns the created run directory path.
    """
    repo_root = _repo_root()
    artifacts_root = repo_root / "artifacts" / "runs"
    artifacts_root.mkdir(parents=True, exist_ok=True)

    ts = datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    slug = _safe_slug(tag)
    run_dir = artifacts_root / f"{ts}_{slug}"
    run_dir.mkdir(parents=True, exist_ok=False)

    # input.json
    (run_dir / "input.json").write_text(
        payload.model_dump_json(indent=2),
        encoding="utf-8",
    )

    # output.json (strip image field; store as graph.png)
    out_dict = result.model_dump()
    out_dict["graph_png_base64"] = None
    (run_dir / "output.json").write_text(
        json.dumps(out_dict, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )

    # graph.png
    if result.graph_png_base64:
        try:
            png_bytes = base64.b64decode(result.graph_png_base64)
            (run_dir / "graph.png").write_bytes(png_bytes)
        except Exception:
            pass

    # meta.json
    tags = [slug]
    if extra_tags:
        tags.extend([_safe_slug(t) for t in extra_tags if t and t.strip()])

    meta = {
        "created_at": datetime.datetime.now().astimezone().isoformat(),
        "code_version": f"git:{_git_short_hash(repo_root)}",
        "methods": list(result.winners.keys()),
        "notes": notes,
        "tags": tags,
    }
    (run_dir / "meta.json").write_text(
        json.dumps(meta, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )

    return run_dir
