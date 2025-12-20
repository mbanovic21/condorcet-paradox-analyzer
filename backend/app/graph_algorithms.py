from __future__ import annotations

from typing import List, Optional


def find_condorcet_winner(candidates: List[str], A: List[List[int]]) -> Optional[str]:
    """Condorcet winner = beats everyone in A (outdegree m-1)."""
    m = len(candidates)
    for i, c in enumerate(candidates):
        outdeg = sum(A[i][j] for j in range(m) if j != i)
        if outdeg == m - 1:
            return c
    return None


def dfs_find_cycle(candidates: List[str], A: List[List[int]]) -> Optional[List[str]]:
    """
    Returns a directed cycle as candidate labels ending where it starts, e.g. [A,B,C,A],
    or None if no cycle exists.
    """
    m = len(candidates)
    WHITE, GRAY, BLACK = 0, 1, 2
    color = [WHITE] * m
    parent = [-1] * m

    def neighbors(u: int):
        for v in range(m):
            if A[u][v] == 1:
                yield v

    def reconstruct(u: int, v: int) -> List[str]:
        path = [v]
        cur = u
        while cur != v and cur != -1:
            path.append(cur)
            cur = parent[cur]
        path.append(v)
        path.reverse()
        return [candidates[i] for i in path]

    def visit(u: int) -> Optional[List[str]]:
        color[u] = GRAY
        for v in neighbors(u):
            if color[v] == WHITE:
                parent[v] = u
                cyc = visit(v)
                if cyc is not None:
                    return cyc
            elif color[v] == GRAY:
                return reconstruct(u, v)
        color[u] = BLACK
        return None

    for s in range(m):
        if color[s] == WHITE:
            cyc = visit(s)
            if cyc is not None:
                return cyc
    return None