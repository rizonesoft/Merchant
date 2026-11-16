---
schema_version: 1
id: release
domain: 06-release
status: active
title: "TODO-01 -- Whole-till checks and release"
depends_on: ["welcome", "sell", "payment", "sale-complete", "products", "sales-and-refunds", "day-end", "tills", "settings"]
---

# TODO-01 -- Whole-till checks and release

> **Goal:** The finished till is proven end to end in every look, the old window is gone, and a build can be handed to a merchant.

> **Current state:** Measured 2026-11-17. The old `MainViewModel` and its tests remain. Nothing publishes a build. `README.md` describes the stack and the old window.

## Inputs

- `design/SPEC.md` sections 3 (window sizes), 7 (keyboard rules) and 10 (out of scope).
- Every `todo/0*/TODO-*.md` before this one.

## Outcome

- One test per look runs first run, a sale, a refund and a day close through the real views.
- `python scripts/publish.py` leaves a runnable till for Windows and Linux in `build/publish`.

**Adjacency:** all=not-applicable (checks, cleanup and packaging over surfaces owned by the earlier files)

**Adjacency rationale:** No new record or surface is introduced here. Each check exercises adjacencies owned and declared by the files it depends on.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Window sizes | -- | [ ] |
| 2 | §2 | Keyboard | -- | [ ] |
| 3 | §3 | End-to-end flow in each look | §1, §2 | [ ] |
| 4 | §4 | Two tills through the interface | §3 | [ ] |
| 5 | §5 | Removing the old window | §3 | [ ] |
| 6 | §6 | Fidelity sweep and contract coverage | §3 | [ ] |
| 7 | §7 | Publishable builds | §5 | [ ] |
| 8 | §8 | README and user guide from the real screens | §6 | [ ] |

## 1. Window sizes

The layout rules for sizes other than the reference (`design/SPEC.md` section 3).

**Phase:** 6
**Needs:** none
**Surface:** yes
**Fidelity:** `design/SPEC.md` section 3 at 1024 x 700 and 1920 x 1080
**Job:** Work on a small tablet and a large monitor
**Treatment:** each look's own views
**Chrome:** each look's shell

**Build order:**

1. Apply and test the growth and minimum rules. Done when: 1024 x 700 and 1920 x 1080 renders hold the rules.

- [ ] Set the window minimum to 1024 x 700 and make fixed-width panels (cart, paper column, side panels) keep their width while content columns grow, in each look's views. Done when: a 1920 x 1080 render of each Sell screen keeps the cart or paper column at its page width.
- [ ] Below 1280 wide drop one product column on Sell and truncate the Items column on Sales with an ellipsis. Done when: a 1024 x 700 render shows three Counter tile columns and no clipped amounts.
- [ ] Add `tests/Merchant.Tests/Screens/WindowSizeTests.cs` for both sizes in all three looks. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~WindowSizeTests"` passes.
- [ ] Commit: `release: window size rules`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~WindowSizeTests"` passes; a fixed 1280 px root makes `Wide_window_grows_the_content` fail.

## 2. Keyboard

Enter, Escape and focus for operators with a keyboard (`DONE-03`, section 7).

**Phase:** 6
**Needs:** none
**Surface:** yes
**Fidelity:** `design/SPEC.md` section 7 keyboard rules
**Job:** Run the till from a keyboard
**Treatment:** each look's focus ring from its accent token
**Chrome:** each look's shell

**Build order:**

1. Test the keyboard rules through the views. Done when: a sale completes with the keyboard after the products are tapped.

- [ ] Give focus to `New sale` on Sale complete, to the first field of every dialog, and to the search field on Sell and Products. Done when: the focused element in each headless test is the named control.
- [ ] Make every control reachable by Tab in reading order with a visible focus ring from the look's accent. Done when: tabbing through Settings visits every field once.
- [ ] Add `tests/Merchant.Tests/Screens/KeyboardTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~KeyboardTests"` passes.
- [ ] Commit: `release: keyboard and focus`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~KeyboardTests"` passes; removing default focus from `New sale` makes `Enter_starts_the_next_sale` fail.

## 3. End-to-end flow in each look

One test per look that uses the till as a merchant does.

**Phase:** 6
**Needs:** none
**Surface:** no

**Build order:**

1. Write the flow test. Done when: it passes in all three looks.

- [ ] Add `tests/Merchant.Tests/Screens/FullFlowTests.cs`: from an empty data folder create a shop, add a category and two products, sell with cash, check the change, refund the sale, count the drawer and close the day, driving the real views of the look under test and asserting the event log after each step. Done when: the log holds exactly the expected facts in order. Cheaper substitute: calling view models directly must fail review: the test presses controls.
- [ ] Run the flow as a theory over Daylight, Counter and Tape. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~FullFlowTests"` passes three cases.
- [ ] Commit: `release: end-to-end flow per look`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~FullFlowTests"` passes; unbinding `Complete sale` in any look fails that look's case.

## 4. Two tills through the interface

The product's promise, tested through the new interface: two tills, no internet.

**Phase:** 6
**Needs:** none
**Surface:** no

**Build order:**

1. Write the two-till test. Done when: a sale made on one shows on the other.

- [ ] Add `tests/Merchant.Tests/Screens/TwoTillTests.cs`: two runtimes with their own data folders on loopback, the second joins with the first's pairing code through Welcome, the first sells, and after `Sync now` the second's Sales screen lists the sale and its Day end shows the takings. Done when: the second till's list has the sale with the first till's name.
- [ ] Assert a third runtime without the code gets nothing. Done when: its store stays empty.
- [ ] Commit: `release: two tills end to end`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TwoTillTests"` passes; skipping the join makes `Sale_reaches_the_other_till` fail.

## 5. Removing the old window

The first window and its view model are replaced by the shell.

**Phase:** 6
**Needs:** none
**Surface:** no

**Build order:**

1. Delete the old code and move its tests. Done when: no reference remains.

- [ ] Delete `src/Merchant.Till/ViewModels/MainViewModel.cs` and the old markup, moving any behaviour still needed into its new owner and replacing `tests/Merchant.Tests/TillTests.cs` cases with their equivalents against the new view models. Done when: `grep -r MainViewModel src tests` finds nothing. Cheaper substitute: leaving the old view model unused must fail review.
- [ ] Run the whole suite. Done when: `dotnet test Merchant.slnx` passes.
- [ ] Commit: `release: remove the old window`

**Test checkpoint:** `dotnet test Merchant.slnx` passes and `grep -r MainViewModel src tests` prints nothing.

## 6. Fidelity sweep and contract coverage

The last check that all 27 screens match and every control id has an owner and a test.

**Phase:** 6
**Needs:** none
**Surface:** no

**Build order:**

1. Add the coverage test. Done when: it lists no unowned id.

- [ ] Add `tests/Merchant.Tests/ContractCoverageTests.cs`: read every control id (`[A-Z]+-[0-9]+`) from `design/SPEC.md` section 5 and assert each appears in at least one file under `tests/Merchant.Tests`. Done when: the test passes, and adding an id to the SPEC makes it fail until a test cites it.
- [ ] Add `tests/Merchant.Tests/Screens/AllScreensTests.cs` asserting a `Matches_the_reference` test exists and passes for each of the 27 look and screen pairs, and that no destination is still registered as `ComingSoonViewModel`. Done when: 27 cases pass and the type `ComingSoonViewModel` is deleted.
- [ ] Fix any drift the sweep finds at its owning view. Done when: both tests pass.
- [ ] Commit: `release: fidelity sweep and contract coverage`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ContractCoverageTests"` and `dotnet test Merchant.slnx --filter "FullyQualifiedName~AllScreensTests"` pass; deleting one screen test fails the sweep.

## 7. Publishable builds

A merchant needs one folder to copy onto a till.

**Phase:** 6
**Needs:** none
**Surface:** no

**Build order:**

1. Add `scripts/publish.py`. Done when: both builds start.

- [ ] Add `scripts/publish.py` running `dotnet publish src/Merchant.Till -c Release -r <rid> --self-contained` for `win-x64` and `linux-x64` into `build/publish/<rid>`, with the version from the newest `v*` tag or `0.1.0`. Done when: both folders hold a `Merchant.Till` executable.
- [ ] Add a `--check` argument to `src/Merchant.Till/Program.cs`: open the store in `MERCHANT_DATA_DIR`, load the embedded fonts and the three looks' dictionaries, print `ok` and exit 0 without creating a window, or print the failure and exit 1. Done when: `Merchant.Till --check` prints `ok` with no display attached. Cheaper substitute: a check that only prints `ok` must fail review: it opens the store and loads the resources.
- [ ] Run `build/publish/linux-x64/Merchant.Till --check` from the script with `MERCHANT_DATA_DIR` set to a temporary folder under a 30 second limit, ending its whole process group on timeout. Done when: the script fails when the check prints anything but `ok`, and no `Merchant.Till` process remains afterwards. Cheaper substitute: leaving the process running must fail review.
- [ ] Add `scripts/tests/test_publish.py` for the version rule. Done when: `python3 -m unittest scripts.tests.test_publish` passes.
- [ ] Commit: `release: publish script`

**Test checkpoint:** `python3 scripts/publish.py` exits 0 and `build/publish/linux-x64/Merchant.Till --check` prints `ok`; `python3 -m unittest scripts.tests.test_publish` passes.

## 8. README and user guide from the real screens

The public README and a short guide show the till as built.

**Phase:** 6
**Needs:** none
**Surface:** no

**Build order:**

1. Update `README.md` and add `docs/guide.md`. Done when: every image is a render from this build.

- [ ] Copy the three Sell renders from `build/screens/<look>/sell.png` to `docs/images/` and show them in `README.md` under a `Three looks` heading, and replace the description of the old window. Done when: the README has no mention of the Setup tab.
- [ ] Add `docs/guide.md`: first run, selling, refunds, day end, adding a till, backup, and choosing a look, one short section each, in plain words. Done when: each section names the controls by their on-screen text. Cheaper substitute: describing controls that do not exist must fail review.
- [ ] Run `python3 scripts/todo-graph.py validate`. Done when: it exits 0.
- [ ] Commit: `docs: README screenshots and user guide`

**Test checkpoint:** `grep -c "docs/images/" README.md` prints 3 or more and `docs/guide.md` exists with seven sections.

## Verification

- `dotnet test Merchant.slnx` passes with every screen, flow and coverage test.
- `python3 scripts/publish.py` produces both builds.
- `python3 scripts/todo-graph.py validate` and `python3 scripts/todo-graph.py plan --check` pass.
