# Frontend (WPF / .NET)

## Prerequisites
- .NET 8 SDK (Windows)

## Run
1) Start backend:
```bash
cd backend
uvicorn main:app --reload --port 8000
```

2) Run WPF:
- Open `frontend-wpf/CondorcetWpf/CondorcetWpf.csproj` in Visual Studio
- Press F5
- Click **Load JSON...** and choose `backend/example_input.json`
- Click **Analyze**

## Notes
- Base URL is `http://127.0.0.1:8000` (change in `MainViewModel` if needed).


v2: shows Margin matrix and Copeland/Minimax methods.
