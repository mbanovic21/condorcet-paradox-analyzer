from __future__ import annotations
from pydantic import BaseModel, Field, field_validator
from typing import List, Dict, Optional, Literal

class Ballot(BaseModel):
    ranking: List[str] = Field(..., description="Strict ranking of candidates, best to worst")
    count: int = Field(1, ge=1, description="How many voters submitted this ranking")

class ElectionInput(BaseModel):
    candidates: List[str]
    ballots: List[Ballot]

    @field_validator("candidates")
    @classmethod
    def candidates_unique_nonempty(cls, v: List[str]) -> List[str]:
        if not v:
            raise ValueError("candidates must be non-empty")
        if len(set(v)) != len(v):
            raise ValueError("candidates must be unique")
        return v

    @field_validator("ballots")
    @classmethod
    def ballots_nonempty(cls, v: List[Ballot]) -> List[Ballot]:
        if not v:
            raise ValueError("ballots must be non-empty")
        return v

class PairwiseResult(BaseModel):
    candidates: List[str]
    N: List[List[int]]
    A: List[List[int]] 
    margin: List[List[int]]

class MethodScores(BaseModel):
    borda: Dict[str, int]
    plurality: Dict[str, int]
    copeland: Dict[str, int]
    minimax: Dict[str, int]

class CycleResult(BaseModel):
    has_cycle: bool
    cycle: Optional[List[str]] = None

class DfsStep(BaseModel):
    index: int
    action: Literal[
        "START",
        "ENTER",
        "EDGE",
        "TREE_EDGE",
        "BACK_EDGE",
        "EXIT",
        "FOUND_CYCLE",
        "END",
    ]
    u: Optional[str] = None
    v: Optional[str] = None

    stack: List[str] = Field(default_factory=list) 
    colors: Dict[str, str] = Field(default_factory=dict) 
    parent: Dict[str, Optional[str]] = Field(default_factory=dict)

    message: Optional[str] = None
    cycle: Optional[List[str]] = None

class DfsTrace(BaseModel):
    steps: List[DfsStep] = Field(default_factory=list)
    has_cycle: bool = False
    cycle: Optional[List[str]] = None

class AnalysisResult(BaseModel):
    candidates: List[str]
    pairwise: PairwiseResult
    condorcet_winner: Optional[str] = None
    cycle_info: CycleResult
    scores: MethodScores
    winners: Dict[str, Optional[str]]
    graph_png_base64: Optional[str] = None
    artifact_run_id: Optional[str] = None
    dfs_trace: Optional[DfsTrace] = None
    graph_layout: Optional[Dict[str, List[float]]] = None
