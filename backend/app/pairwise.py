from __future__ import annotations

from typing import List, Tuple
from .models import ElectionInput


def compute_pairwise(input_data: ElectionInput) -> Tuple[List[List[int]], List[List[int]], List[List[int]]]:
    """
    Returns:
      N: counts of voters preferring i over j
      A: win adjacency (i -> j if margin[i][j] > 0)
      margin: margin[i][j] = N[i][j] - N[j][i]
    """
    C = input_data.candidates
    m = len(C)

    N = [[0 for _ in range(m)] for _ in range(m)]

    required = set(C)
    for b in input_data.ballots:
        r = b.ranking
        if set(r) != required or len(r) != m:
            raise ValueError(f"Ballot ranking must be a permutation of candidates. Got: {r}")
        if len(set(r)) != len(r):
            raise ValueError(f"Ballot ranking contains duplicates. Got: {r}")

        pos = {cand: p for p, cand in enumerate(r)}  # lower = better
        for i in range(m):
            for j in range(m):
                if i == j:
                    continue
                if pos[C[i]] < pos[C[j]]:
                    N[i][j] += b.count

    margin = [[0 for _ in range(m)] for _ in range(m)]
    for i in range(m):
        for j in range(m):
            if i == j:
                continue
            margin[i][j] = N[i][j] - N[j][i]

    A = [[0 for _ in range(m)] for _ in range(m)]
    for i in range(m):
        for j in range(m):
            if i == j:
                continue
            if margin[i][j] > 0:
                A[i][j] = 1

    return N, A, margin