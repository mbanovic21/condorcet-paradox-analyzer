from __future__ import annotations
from typing import Dict, List, Optional
import base64
from io import BytesIO


def render_graph_png_base64(
    candidates: List[str],
    A: List[List[int]],
    margin: List[List[int]],
    cycle: Optional[List[str]],
) -> Optional[str]:
    """
    Renders directed graph using networkx+matplotlib (PNG) and returns base64.
    Edge width scales with |margin|. Cycle edges are emphasized.
    """
    try:
        import matplotlib
        matplotlib.use("Agg")  # headless backend (no GUI)
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
                G.add_edge(candidates[i], candidates[j], w=abs(margin[i][j]))

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
    if normal_edges:
        widths = []
        for (u, v) in normal_edges:
            w = G[u][v].get("w", 1)
            widths.append(1.2 + (w / 4.0))
        nx.draw_networkx_edges(G, pos, ax=ax, edgelist=normal_edges, arrows=True, width=widths, arrowsize=16)

    if cycle_edges:
        cyc_widths = []
        for (u, v) in cycle_edges:
            w = G[u][v].get("w", 1) if G.has_edge(u, v) else 1
            cyc_widths.append(4.0 + (w / 4.0))
        nx.draw_networkx_edges(G, pos, ax=ax, edgelist=list(cycle_edges), arrows=True, width=cyc_widths, arrowsize=18)

    buf = BytesIO()
    plt.tight_layout()
    plt.savefig(buf, format="png", bbox_inches="tight")
    plt.close(fig)

    return base64.b64encode(buf.getvalue()).decode("ascii")

def compute_layout_normalized(candidates: List[str], A: List[List[int]]) -> Optional[Dict[str, List[float]]]:
    try:
        import networkx as nx
    except Exception:
        return None

    G = nx.DiGraph()
    G.add_nodes_from(candidates)
    m = len(candidates)
    for i in range(m):
        for j in range(m):
            if A[i][j] == 1:
                G.add_edge(candidates[i], candidates[j])

    pos = nx.circular_layout(G)  # dict node -> np.array([x,y])

    # normalize to 0..1
    xs = [float(pos[c][0]) for c in candidates]
    ys = [float(pos[c][1]) for c in candidates]
    minx, maxx = min(xs), max(xs)
    miny, maxy = min(ys), max(ys)

    def norm(v, lo, hi):
        if hi - lo < 1e-9:
            return 0.5
        return (v - lo) / (hi - lo)

    layout: Dict[str, List[float]] = {}
    for c in candidates:
        x = float(pos[c][0])
        y = float(pos[c][1])
        layout[c] = [norm(x, minx, maxx), norm(y, miny, maxy)]
    return layout
