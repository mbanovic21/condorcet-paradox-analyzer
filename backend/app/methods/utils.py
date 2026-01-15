from __future__ import annotations

from typing import Dict, Optional


def winner_from_scores(scores: Dict[str, int]) -> Optional[str]:
    """Return single winner, or None if tie / empty."""
    if not scores:
        return None
    best = max(scores.values())
    winners = [k for k, v in scores.items() if v == best]
    return winners[0] if len(winners) == 1 else None