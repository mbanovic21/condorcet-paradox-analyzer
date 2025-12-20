from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.models import ElectionInput, AnalysisResult
from app.analyzer import analyze_election

app = FastAPI(title="Condorcet Analyzer", version="0.1.0")

# Allow WPF dev-time access (localhost). Tighten for production.
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost", "http://localhost:3000", "http://127.0.0.1", "http://127.0.0.1:8000"],
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
):
    result = analyze_election(payload)

    if save_artifact:
        from app.artifacts import save_run_artifact
        save_run_artifact(payload, result, tag=tag, notes=notes)

    return result

