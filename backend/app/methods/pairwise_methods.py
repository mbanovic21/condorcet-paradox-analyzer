from __future__ import annotations

from typing import Dict, List


def copeland_scores(candidates: List[str], margin: List[List[int]]) -> Dict[str, int]:
    """
    Copeland score: wins - losses in pairwise duels (ties count as 0).
    """
    m = len(candidates)
    out = {c: 0 for c in candidates}
    for i in range(m):
        for j in range(i + 1, m):
            if margin[i][j] > 0:
                out[candidates[i]] += 1
                out[candidates[j]] -= 1
            elif margin[i][j] < 0:
                out[candidates[i]] -= 1
                out[candidates[j]] += 1
    return out


def minimax_scores(candidates: List[str], margin: List[List[int]]) -> Dict[str, int]:
    """
    Minimax/Simpson: choose candidate whose WORST defeat is minimal.
    Score returned as negative worst defeat margin (higher is better).
    """
    m = len(candidates)
    out: Dict[str, int] = {}
    for i in range(m):
        worst_defeat = 0
        for j in range(m):
            if i == j:
                continue
            if margin[i][j] < 0:
                worst_defeat = max(worst_defeat, -margin[i][j])
        out[candidates[i]] = -worst_defeat
    return out
