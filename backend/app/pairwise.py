from __future__ import annotations

from typing import List, Tuple
from .models import ElectionInput


def compute_pairwise(input_data: ElectionInput) -> Tuple[
    List[List[int]],
    List[List[int]],
    List[List[int]],
    List[List[float]],
    List[List[int]]
]:
    """
    Returns counts, adjacency, margins, percentages and Schulze strongest paths.
    """
    C = input_data.candidates
    m = len(C)
    total_voters = sum(b.count for b in input_data.ballots)

    N = [[0 for _ in range(m)] for _ in range(m)]
    
    required = set(C)
    for b in input_data.ballots:
        r = b.ranking
        if set(r) != required or len(r) != m:
            continue 

        pos = {cand: p for p, cand in enumerate(r)}
        for i in range(m):
            for j in range(m):
                if i == j:
                    continue
                if pos[C[i]] < pos[C[j]]:
                    N[i][j] += b.count

    margin = [[0 for _ in range(m)] for _ in range(m)]
    A = [[0 for _ in range(m)] for _ in range(m)]
    
    for i in range(m):
        for j in range(m):
            if i == j:
                continue
            margin[i][j] = N[i][j] - N[j][i]
            if margin[i][j] > 0:
                A[i][j] = 1

    percent_matrix = [[0.0 for _ in range(m)] for _ in range(m)]
    if total_voters > 0:
        for i in range(m):
            for j in range(m):
                if i != j:
                    percent_matrix[i][j] = round((N[i][j] / total_voters) * 100, 2)

    p = [[0 for _ in range(m)] for _ in range(m)]
    for i in range(m):
        for j in range(m):
            if i != j:
                if N[i][j] > N[j][i]:
                    p[i][j] = N[i][j]
                else:
                    p[i][j] = 0

    for k in range(m):
        for i in range(m):
            if i == k: continue
            for j in range(m):
                if j == i or j == k: continue
                p[i][j] = max(p[i][j], min(p[i][k], p[k][j]))

    return N, A, margin, percent_matrix, p