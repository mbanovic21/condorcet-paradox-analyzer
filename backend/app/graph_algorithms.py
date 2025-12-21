from __future__ import annotations
from typing import List, Optional, Dict, Tuple
from .models import DfsTrace, DfsStep


def dfs_with_trace(candidates: List[str], A: List[List[int]]) -> Tuple[Optional[List[str]], DfsTrace]:
    """
    DFS cycle detection with full step trace for UI simulation.
    Returns: (cycle_or_none, trace)
    cycle format: ["A","B","C","A"]
    """
    m = len(candidates)
    WHITE, GRAY, BLACK = 0, 1, 2
    color = [WHITE] * m
    parent = [-1] * m
    stack: List[int] = []
    steps: List[DfsStep] = []
    step_idx = 0

    def colors_map() -> Dict[str, str]:
        out: Dict[str, str] = {}
        for i, c in enumerate(candidates):
            out[c] = "WHITE" if color[i] == WHITE else ("GRAY" if color[i] == GRAY else "BLACK")
        return out

    def parent_map() -> Dict[str, Optional[str]]:
        out: Dict[str, Optional[str]] = {}
        for i, c in enumerate(candidates):
            out[c] = candidates[parent[i]] if parent[i] != -1 else None
        return out

    def stack_labels() -> List[str]:
        return [candidates[i] for i in stack]

    def log(action: str, u: Optional[int] = None, v: Optional[int] = None, message: Optional[str] = None, cycle: Optional[List[str]] = None):
        nonlocal step_idx
        steps.append(
            DfsStep(
                index=step_idx,
                action=action,
                u=(candidates[u] if u is not None else None),
                v=(candidates[v] if v is not None else None),
                stack=stack_labels(),
                colors=colors_map(),
                parent=parent_map(),
                message=message,
                cycle=cycle,
            )
        )
        step_idx += 1

    def neighbors(u: int):
        for v in range(m):
            if A[u][v] == 1:
                yield v

    def reconstruct(u: int, v: int) -> List[str]:
        # reconstruct cycle from u -> v where v is GRAY
        path = [v]
        cur = u
        while cur != v and cur != -1:
            path.append(cur)
            cur = parent[cur]
        path.append(v)
        path.reverse()
        return [candidates[i] for i in path]

    found_cycle: Optional[List[str]] = None

    def visit(u: int) -> bool:
        nonlocal found_cycle
        color[u] = GRAY
        stack.append(u)
        log("ENTER", u=u, message="Mark node GRAY and push to stack")

        for v in neighbors(u):
            log("EDGE", u=u, v=v, message="Inspect outgoing edge")
            if color[v] == WHITE:
                parent[v] = u
                log("TREE_EDGE", u=u, v=v, message="Tree edge to WHITE node; set parent and recurse")
                if visit(v):
                    return True
            elif color[v] == GRAY:
                cyc = reconstruct(u, v)
                found_cycle = cyc
                log("BACK_EDGE", u=u, v=v, message="Back edge to GRAY node => cycle found")
                log("FOUND_CYCLE", u=u, v=v, cycle=cyc, message="Cycle reconstructed")
                return True

        stack.pop()
        color[u] = BLACK
        log("EXIT", u=u, message="All neighbors processed; pop stack and mark BLACK")
        return False

    log("START", message="Begin DFS over all nodes")
    for s in range(m):
        if color[s] == WHITE:
            if visit(s):
                break
    log("END", message="DFS finished")

    trace = DfsTrace(steps=steps, has_cycle=(found_cycle is not None), cycle=found_cycle)
    return found_cycle, trace


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