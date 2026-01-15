import uvicorn
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.models import ElectionInput, AnalysisResult
from app.analyzer import analyze_election

app = FastAPI(title="Condorcet Analyzer", version="0.1.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/election/analyze", response_model=AnalysisResult)
def election_analyze(
    payload: ElectionInput,
    save_artifact: bool = False,
    tag: str = "run",
    notes: str = "",
    include_trace: bool = False,
):
    result = analyze_election(payload, include_trace=include_trace)

    if save_artifact:
        from app.artifacts import save_run_artifact
        run_id = save_run_artifact(payload, result, tag=tag, notes=notes)
        result.artifact_run_id = run_id

    return result

if __name__ == "__main__":
    import uvicorn
    import sys
    import os

    if sys.stdout is None:
        sys.stdout = open(os.devnull, "w")
    if sys.stderr is None:
        sys.stderr = open(os.devnull, "w")

    uvicorn.run(
        app, 
        host="127.0.0.1", 
        port=8000, 
        log_config=None
    )