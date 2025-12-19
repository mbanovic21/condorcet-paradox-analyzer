from __future__ import annotations
from typing import Dict, List, Optional, Tuple
from .models import ElectionInput, AnalysisResult, PairwiseResult, MethodScores, CycleResult
import base64
from io import BytesIO

def _index_map(candidates: List[str]) -> Dict[str, int]:
    return {c: i for i, c in enumerate(candidates)}

def compute_pairwise(input_data: ElectionInput) -> Tuple[List[List[int]], List[List[int]]]:
    """
    Returns:
      N: counts of voters preferring i over j
      A: win adjacency (i -> j if N[i][j] > N[j][i])
    """
    C = input_data.candidates
    m = len(C)
    idx = _index_map(C)

    N = [[0 for _ in range(m)] for _ in range(m)]

    required = set(C)
    for b in input_data.ballots:
        r = b.ranking
        if set(r) != required or len(r) != m:
            raise ValueError(f"Ballot ranking must be a permutation of candidates. Got: {r}")
        if len(set(r)) != len(r):
            raise ValueError(f"Ballot ranking contains duplicates. Got: {r}")

        pos = {cand: p for p, cand in enumerate(r)}
        for i in range(m):
            for j in range(m):
                if i == j:
                    continue
                ci, cj = C[i], C[j]
                if pos[ci] < pos[cj]:
                    N[i][j] += b.count

    A = [[0 for _ in range(m)] for _ in range(m)]
    for i in range(m):
        for j in range(m):
            if i == j:
                continue
            if N[i][j] > N[j][i]:
                A[i][j] = 1
    return N, A

def find_condorcet_winner(candidates: List[str], A: List[List[int]]) -> Optional[str]:
    m = len(candidates)
    for i, c in enumerate(candidates):
        outdeg = sum(A[i][j] for j in range(m) if j != i)
        if outdeg == m - 1:
            return c
    return None

def dfs_find_cycle(candidates: List[str], A: List[List[int]]) -> Optional[List[str]]:
    """
    Returns a concrete directed cycle as list of candidate labels ending where it starts, e.g. [A,B,C,A],
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

def winner_from_scores(scores: Dict[str, int]) -> Optional[str]:
    if not scores:
        return None
    best = max(scores.values())
    winners = [k for k, v in scores.items() if v == best]
    if len(winners) == 1:
        return winners[0]
    # tie
    return None

def render_graph_png_base64(candidates: List[str], A: List[List[int]], cycle: Optional[List[str]]) -> Optional[str]:
    """
    Renders a simple directed graph using networkx+matplotlib (PNG) and returns base64.
    If plotting libraries aren't available, returns None.
    """
    try:
        import networkx as nx
        import matplotlib.pyplot as plt
    except Exception:
        return None

    m = len(candidates)
    G = nx.DiGraph()
    G.add_nodes_from(candidates)
    for i in range(m):
        for j in range(m):
            if A[i][j] == 1:
                G.add_edge(candidates[i], candidates[j])

    pos = nx.circular_layout(G)

    cycle_edges = set()
    if cycle and len(cycle) >= 2:
        for a, b in zip(cycle[:-1], cycle[1:]):
            cycle_edges.add((a, b))

    fig = plt.figure(figsize=(6, 6), dpi=160)
    ax = fig.add_subplot(1, 1, 1)
    ax.set_axis_off()

    nx.draw_networkx_nodes(G, pos, ax=ax, node_size=900)
    nx.draw_networkx_labels(G, pos, ax=ax, font_size=10)

    normal_edges = [e for e in G.edges() if e not in cycle_edges]
    nx.draw_networkx_edges(G, pos, ax=ax, edgelist=normal_edges, arrows=True, width=1.5, arrowsize=16)

    if cycle_edges:
        nx.draw_networkx_edges(G, pos, ax=ax, edgelist=list(cycle_edges), arrows=True, width=4.0, arrowsize=18)

    buf = BytesIO()
    plt.tight_layout()
    plt.savefig(buf, format="png", bbox_inches="tight")
    plt.close(fig)

    return base64.b64encode(buf.getvalue()).decode("ascii")

def analyze_election(input_data: ElectionInput) -> AnalysisResult:
    N, A = compute_pairwise(input_data)
    cw = find_condorcet_winner(input_data.candidates, A)
    cycle = dfs_find_cycle(input_data.candidates, A)

    borda = borda_scores(input_data)
    plurality = plurality_scores(input_data)

    scores = MethodScores(borda=borda, plurality=plurality)

    winners = {
        "condorcet": cw,
        "borda": winner_from_scores(borda),
        "plurality": winner_from_scores(plurality),
    }

    graph_png_b64 = render_graph_png_base64(input_data.candidates, A, cycle)

    return AnalysisResult(
        candidates=input_data.candidates,
        pairwise=PairwiseResult(candidates=input_data.candidates, N=N, A=A),
        condorcet_winner=cw,
        cycle_info=CycleResult(has_cycle=(cycle is not None), cycle=cycle),
        scores=scores,
        winners=winners,
        graph_png_base64=graph_png_b64,
    )
