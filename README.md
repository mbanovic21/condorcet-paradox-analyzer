# Condorcet Paradoks — Python backend + WPF frontend

This starter project:
- reads preferential ballots (JSON),
- computes pairwise duels (N matrix),
- builds directed majority graph (A matrix),
- detects a directed cycle (Condorcet paradox) via DFS (returns the concrete cycle),
- checks for a Condorcet winner,
- computes Borda + Plurality scores,
- renders a PNG graph (base64) for the WPF UI.

## Quickstart
### Backend
```bash
cd backend
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

### Frontend (WPF)
Open `frontend-wpf/CondorcetWpf/CondorcetWpf.csproj` in Visual Studio and run.
Load `backend/example_input.json` and click Analyze.
