# AGENTS.md

This repository follows the Conclave-SDK 3-agent conventions.

## Goal

Ship Merchant, a no-cloud point of sale for pop-up shops and food trucks: an Avalonia till that works fully offline on encrypted SQLite and syncs with the shop's other tills over the local network.

## The project

- The code is C# on .NET 10: `src/Merchant.Core` (event store, shop state, sync, receipts, backup), `src/Merchant.SyncHost` (the HTTPS sync listener), `src/Merchant.Till` (the Avalonia till) and `tests/Merchant.Tests` (xUnit). The solution is `Merchant.slnx`.
- The interface contract is [design/SPEC.md](./design/SPEC.md): every control has an id, and the pages and reference images in `design/interface` are the exact source for how each screen looks. The till has three looks (Daylight by default, Counter, Tape) over one set of view models.
- SQLite is the only database. There is no server in this repository; one is being built separately.
- Verify with `dotnet test Merchant.slnx`, `python3 scripts/todo-graph.py validate` and `python3 scripts/todo-graph.py plan --check`. On a workstation run the test suite through `bosun run -- dotnet test Merchant.slnx` so it takes its turn.
- The folder is built from Windows and from WSL. `Directory.Build.props` gives each its own output under `build/dotnet/windows` and `build/dotnet/unix`; never point both at one `bin` or `obj`.
- A sale is written to disk before anything prints or syncs. Facts are never edited or deleted. Nothing in the till may need the internet.
- Brand assets are built by `python scripts/build-brand.py` and design references by `python scripts/render-design.py` (both need Chrome or Edge, on Windows).

## Unattended runs and their shortcuts

- The plan runs unattended in WSL through `scripts/unattended-run.py`, as [docs/unattended-run.md](./docs/unattended-run.md) describes. The profile is `.conclave/unattended.json`: run name `merchant`, Claude Code as the main runner and Antigravity (Carbon) as the second.
- Two Windows desktop shortcuts start it, one per runner: `Merchant Claude campaign` and `Merchant Carbon campaign`. Each opens WSL (Ubuntu) in `/mnt/d/Merchant` and runs `python3 scripts/unattended-run.py start --agent <runner>`; the preflight runs first and nothing starts on a FAIL.
- The shortcuts were made with `pwsh scripts/unattended-shortcut.ps1 -Distro Ubuntu -Icon assets/brand/icon/merchant.ico`. Run that again after changing the runners or the icon, and `pwsh scripts/unattended-shortcut.ps1 -Remove` to remove them. Their badged icons are in `%LOCALAPPDATA%\Merchant`.
- A run starts only when the operator starts it. Nothing here resumes one at boot, at logon or on a timer.

## Working rules

- Verify files, routes, schemas, tools, and commands before relying on them. A path someone mentions is a hypothesis until it is on disk.
- Inspect existing patterns, imports, the package manager, the framework, and the test commands before editing. Do not infer architecture or tool schemas from memory when the repository can answer.
- Resolve a tool by capability before path. Prefer explicit config, then discovery, then a setup-required state. Store discovered paths only in ignored local config and never hardcode user-specific absolute paths.
- Prefer global or user-level tool installs. Add repo-scoped wrappers only when they encode project-specific behavior or reproducible commands.
- Real data only. Demo or seed data stays behind an explicit demo mode and is labeled as such.
- Run the relevant typecheck, lint, and tests after a change.
- Fix at the owning layer: a setting in its setting, behaviour in the code that produces it, data in the data. No output filters that hide a problem. Code changes end the report with a `Layer:` line.
- Markdown prose is never hard-wrapped: one line per paragraph and per list item. Every Markdown table row stays `<= 120` characters so raw `.md` files never wrap.
- Never use em dashes or en dashes anywhere: prose, code, comments, commit messages and generated files.
- Temporary files and full logs go to scratch, never the repository: the session scratchpad when one is given, otherwise the folder `paths.scratch` names in `.conclave/local.toml`, otherwise the operating system's temporary folder.

## Operating directive

How to work without waiting to be told. When to ask the operator is listed under Safety, and only there.

- A task assigned is a task authorised: do it, then report what was done, with the evidence.
- Never end a turn with a question or an offer when the next step is known and unblocked: take the step.
- Finish the follow-ups this session can do safely (a config value, a setup step, a check to run again) and report them done, never as a list left for the operator.
- Ask the operator only for what Safety lists: once, with the options and the one you recommend, and keep working on everything the answer does not change.
- Fix forward: a risk found later is filed as ordinary work that blocks nothing, and a red check is fixed at its cause, never skipped or bypassed.
- Build the simplest design that meets the request in full, and add a check, gate or prerequisite only for a failure that has happened or one that would lose data, spend money or expose something.
- An unattended run never asks the operator: it takes the industry-standard option, records the decision and what reversing it would cost in the run file and files an owner item; it halts only through the run's own pause mechanism, and pauses instead of proceeding only for data loss, a credential, shared infrastructure or a calculation that looks wrong.

## Safety

- Local reversible work in the repository needs no confirmation, and neither does work outside it that the task authorised and that can be undone (a config value, a setup step, a deploy the task asked for).
- Ask the operator once before anything that destroys or exposes something, or that needs business context the repository lacks: dropping or overwriting data, a force-push, deleting outside the repository, creating, rotating or exposing a credential, sending mail or a message to a person, a SQL write or a production change with real blast radius.
- When a command needs sudo, give the operator the exact command to run with `! sudo ...`. Never store the sudo password.
- Never print, log, or commit a secret value.

## Git

A project with a branch workflow replaces the first rule with its own; the others hold everywhere.

- Develop on the main branch: commit there, with no feature branch, unless the operator asks for a branch in that task.
- Before staging, read `git status --short` and enough of the diff to understand every dirty file: another session or a person may be editing.
- Commit a safe side change (small, not secret, not generated, not in conflict) with the next coherent commit; stop and say why before touching one that conflicts, holds a secret or looks unsafe or generated.
- Never discard changes with `git reset`, `checkout`, `restore` or `clean` unless asked for that exact cleanup.
- After a commit, confirm it landed with `git log --oneline -1` and `git status --short`, and never filter hook output in a way that hides an error.
- When work is finished, remove the worktrees you or a subagent created, delete branches that are merged or whose content is verifiably on main, and prune dead worktree records.
- Never delete an unmerged branch whose content is not verifiably on main without asking.

## Skills

- Antigravity skills live in `.agents/skills/<name>/`, Claude skills in `.claude/skills/<name>/`, and Codex skills in `.codex/skills/<name>/`, all as real files, never symlinks. Change all three together.
- Follow [Agent Skills and Rules Structure](./docs/agent-skills-and-rules-structure.md) before adding or moving skills.
- Name a skill for its capability, not with a project prefix.
- Each agent reads and runs only its own directory (`.agents/` for Antigravity, `.claude/` for Claude Code, `.codex/` for Codex) and never edits another agent's skills unless asked.

## Subagents

- Delegate mechanical subtasks to subagents. Claude subagents pin an explicit `model:`: Sonnet by default, and Opus (with a `model_rationale:`) only where judgment quality needs it.
- Subagents read and run; they never write. Every Claude subagent declares an explicit `tools:` list without Edit, Write, MultiEdit, NotebookEdit or `*`. The main loop makes every change and verifies what subagents return.
- Antigravity uses native `Orchestrator (Carbon)` (`gemini-pro-orchestrator`), which plans on Carbon (`<plan_execute_protocol>`), hands execution to Hydrogen via `exit_plan_mode`, manages its own effort (rejecting `--effort`), and provides built-in subagents (`self`, `research`, `image-generator`) without custom `.agents/agents/*.md` files or wrapper scripts. Codex has no subagent definitions: it runs mechanical subtasks in the main session.
- Haiku is only for near-mechanical work, at least 90% mechanical (read-only lookups, inventories, structural checks), whose output a script or the main loop verifies. Pin `claude-haiku-5-5` or later, declare `mechanical_share:` (90 to 100), point `haiku_replay:` at a record of a known-answer replay that showed no loss against Sonnet, and give it no edit tools. Never use Haiku for review, audit, research, security, debugging, diagnosis, planning or design.
- Run `node scripts/verify-agent-tiers.mjs` after adding or changing a Claude agent definition in `.claude/agents/`; it enforces these rules.

## TODO workflow

Plan and build through the TODO system: [docs/todo-workflow.md](./docs/todo-workflow.md).
