# Artifacts (inputs/outputs for reproducibility & future AI)

This folder stores analysis runs in an AI-friendly format:

artifacts/
runs/
YYYY-MM-DD_HH-MM-SS_<tag>/
input.json
output.json
graph.png
meta.json
samples/
small curated examples

## Git policy
- Keep small curated examples in `artifacts/samples/`
- Ignore `artifacts/runs/` in git (can become large)

## meta.json example
```json
{
  "created_at": "2025-12-19T14:20:03+01:00",
  "code_version": "git:3f2a1c7",
  "methods": ["condorcet", "borda", "plurality", "copeland", "minimax"],
  "notes": "Condorcet cycle demo",
  "tags": ["cycle_demo"]
}
```