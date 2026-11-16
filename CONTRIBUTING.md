# Contributing to Merchant

Thank you for looking. Merchant is small and opinionated: a till that must keep selling with no internet and never lose a sale. Changes are welcome when they keep those two promises.

## Build and test

You need the .NET 10 SDK and, for the planning tools, Python 3.11 or later.

```
dotnet test Merchant.slnx
python scripts/todo-graph.py validate
```

Both run in CI on every push and pull request, on Windows and Linux.

## Where things are decided

- **What the interface does** is in [`design/SPEC.md`](design/SPEC.md). Each control has an id (`SELL-03`, `PAY-06`). A change to behaviour changes the contract first.
- **What the interface looks like** is in [`design/interface`](design/interface): one page and one reference image per screen and look. A screen is done when its render matches its reference.
- **What is being built next** is in [`todo/implementation-plan.md`](todo/implementation-plan.md), generated from the files in [`todo`](todo). The format is in [`todo/README.md`](todo/README.md).

## Rules that keep the till honest

- A sale is written to disk before anything else happens. Printing, syncing and the screen come after.
- Facts are never edited or deleted. A correction is a new fact (a refund, a new price).
- Nothing in the till may need the internet.
- No view holds behaviour. The three looks are views over the same view models.
- A fix goes in the layer that owns the problem, with a test that fails before it and passes after.

## Sending a change

1. Open an issue first for anything larger than a small fix, so the approach can be agreed.
2. Keep a pull request to one subject, with tests.
3. Write commit messages in the imperative, with a short scope: `core: order events by a logical clock`.
4. Do not use em dashes or en dashes in prose, comments or commit messages, and keep Markdown paragraphs on one line.

By contributing you agree that your work is licensed under the [AGPL-3.0](LICENSE).

## Reporting a security problem

Please do not open a public issue. See [SECURITY.md](SECURITY.md).
