# Backend (Python / FastAPI)

## Setup
```bash
cd backend
python -m venv .venv
# Windows:
.venv\Scripts\activate
# macOS/Linux:
# source .venv/bin/activate

pip install -r requirements.txt
```

## Run
```bash
uvicorn main:app --reload --port 8000
```

## Test quickly
```bash
curl -X POST http://127.0.0.1:8000/election/analyze \
  -H "Content-Type: application/json" \
  --data-binary @example_input.json
```
