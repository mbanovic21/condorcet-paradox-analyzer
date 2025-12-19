from __future__ import annotations
from pydantic import BaseModel, Field, field_validator
from typing import List, Dict, Optional

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
    N: List[List[int]]  # N[i][j] = voters pref i over j
    A: List[List[int]]  # A[i][j] = 1 if i beats j, else 0 (ties => 0)

class MethodScores(BaseModel):
    borda: Dict[str, int]
    plurality: Dict[str, int]

class CycleResult(BaseModel):
    has_cycle: bool
    cycle: Optional[List[str]] = None  # e.g. ["A","B","C","A"]

class AnalysisResult(BaseModel):
    candidates: List[str]
    pairwise: PairwiseResult
    condorcet_winner: Optional[str] = None
    cycle_info: CycleResult
    scores: MethodScores
    winners: Dict[str, Optional[str]]  # method -> winner
    graph_png_base64: Optional[str] = None
