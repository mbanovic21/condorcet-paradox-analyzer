from __future__ import annotations

from .models import ElectionInput, AnalysisResult, PairwiseResult, MethodScores, CycleResult
from .pairwise import compute_pairwise
from .graph_algorithms import dfs_with_trace, find_condorcet_winner, dfs_find_cycle
from .methods import (
    borda_scores,
    plurality_scores,
    copeland_scores,
    minimax_scores,
    winner_from_scores,
)
from .rendering import render_graph_png_base64, compute_layout_normalized


def analyze_election(input_data: ElectionInput, include_trace: bool = False) -> AnalysisResult:
    N, A, margin, percents, schulze = compute_pairwise(input_data)

    condorcet = find_condorcet_winner(input_data.candidates, A)

    dfs_trace = None
    if include_trace:
        cycle, dfs_trace = dfs_with_trace(input_data.candidates, A)
    else:
        cycle = dfs_find_cycle(input_data.candidates, A)

    borda = borda_scores(input_data)
    plurality = plurality_scores(input_data)
    copeland = copeland_scores(input_data.candidates, margin)
    minimax = minimax_scores(input_data.candidates, margin)

    scores = MethodScores(
        borda=borda,
        plurality=plurality,
        copeland=copeland,
        minimax=minimax,
    )

    winners = {
        "condorcet": condorcet,
        "borda": winner_from_scores(borda),
        "plurality": winner_from_scores(plurality),
        "copeland": winner_from_scores(copeland),
        "minimax": winner_from_scores(minimax),
    }

    graph_png_b64 = render_graph_png_base64(input_data.candidates, A, margin, cycle)
    layout = compute_layout_normalized(input_data.candidates, A)

    return AnalysisResult(
        candidates=input_data.candidates,
        pairwise=PairwiseResult(candidates=input_data.candidates, N=N, A=A, margin=margin, percent=percents, schulze_paths=schulze),
        condorcet_winner=condorcet,
        cycle_info=CycleResult(has_cycle=(cycle is not None), cycle=cycle),
        scores=scores,
        winners=winners,
        graph_png_base64=graph_png_b64,
        dfs_trace=dfs_trace,
        graph_layout=layout,
    )
