from __future__ import annotations

from typing import Dict
from ..models import ElectionInput


def borda_scores(input_data: ElectionInput) -> Dict[str, int]:
    C = input_data.candidates
    m = len(C)
    scores = {c: 0 for c in C}
    for b in input_data.ballots:
        for pos, cand in enumerate(b.ranking):
            scores[cand] += (m - 1 - pos) * b.count
    return scores


def plurality_scores(input_data: ElectionInput) -> Dict[str, int]:
    scores = {c: 0 for c in input_data.candidates}
    for b in input_data.ballots:
        scores[b.ranking[0]] += b.count
    return scores
