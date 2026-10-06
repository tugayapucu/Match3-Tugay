# AGENTS.md

## Project

Unity 6 mobile Match-3 learning and portfolio project.

The long-form roadmap and feature requirements live in `PLAN.md`.
Read only the relevant section of `PLAN.md` when a task depends on it.

## Engineering Rules

- Prefer simple solutions over speculative abstractions.
- Prefer composition over inheritance.
- Avoid God classes and hidden global state.
- Keep deterministic core gameplay logic independent of `MonoBehaviour` where practical.
- Keep presentation concerns separate from gameplay rules where practical.
- Do not introduce a design pattern unless it solves a concrete problem.
- Do not add third-party packages without explicit approval.
- Do not silently suppress errors or hide failures with broad `try/catch`.
- Avoid unrelated refactors while implementing a scoped task.
- Preserve existing behavior unless the requested change requires otherwise.
- Write or update tests for deterministic gameplay logic.
- Avoid premature optimization; distinguish real issues from theoretical ones.
- Keep mobile performance in mind, especially allocations and unnecessary per-frame work.

## Unity Rules

- Use Unity MCP for Editor operations when available and appropriate.
- Prefer Unity Editor APIs / MCP tools over manually editing scene or prefab YAML.
- After meaningful Unity changes, check compilation and the Unity Console.
- Run relevant EditMode / PlayMode tests when available.
- Do not add unnecessary `Update()` methods.
- Keep the logical board state authoritative; views should represent it rather than own game rules.

## Workflow

Before a non-trivial change:

1. Inspect the relevant existing code.
2. Read only the relevant `PLAN.md` phase if roadmap context is needed.
3. Briefly state the intended approach when architecture is non-trivial.
4. Implement only the requested scope.
5. Run relevant tests / validation.
6. Check Unity Console when Unity Editor state is involved.
7. Summarize meaningful changed files, tests run, and remaining risks.

For small fixes, do not load the entire roadmap unnecessarily.

## Git Discipline

- Inspect the current diff before broad edits.
- Do not overwrite unrelated user changes.
- Keep changes reviewable and scoped.
- Do not create commits unless explicitly requested.
- If the working tree contains unrelated changes, preserve them.

## Learning Goal

This repository is intentionally AI-assisted, but the human developer must be able to understand and defend the important code.

For major architectural choices, prefer code that is easy to explain in terms of:

- responsibility
- coupling
- data flow
- testability
- extensibility
- trade-offs

Do not optimize for showing off patterns or abstractions.
