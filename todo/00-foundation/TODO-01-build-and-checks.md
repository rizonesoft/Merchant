---
schema_version: 1
id: build-and-checks
domain: 00-foundation
status: active
title: "TODO-01 -- Fonts, render harness and screen comparison"
---

# TODO-01 -- Fonts, render harness and screen comparison

> **Goal:** The till embeds its own typefaces, any view can be rendered to a PNG in a test without a display, and a test can say whether that render matches a design reference.

> **Current state:** Measured 2026-11-17. The solution `Merchant.slnx` has `Merchant.Core`, `Merchant.SyncHost`, `Merchant.Till` and `tests/Merchant.Tests`; `dotnet test Merchant.slnx` passes 20 tests on Windows and on Linux (WSL, .NET SDK 10). The three variable fonts are in `assets/fonts` with their OFL texts but the till still uses `Avalonia.Fonts.Inter` (`src/Merchant.Till/Program.cs`, `.WithInterFont()`). `design/interface` holds 27 pages and their PNGs. No test renders a view, and nothing compares images. CI (`.github/workflows/ci.yml`) builds and tests on every push.

## Inputs

- `design/SPEC.md` section 3 (what 1:1 means: the 1280 x 800 window, the three typefaces, the comparison limits).
- `assets/fonts`: `Sora[wght].ttf`, `Inter[opsz,wght].ttf`, `JetBrainsMono[wght].ttf`.
- `design/interface/<look>/<screen>.png`: the references the comparer reads.

## Outcome

- A view rendered in a test on Linux and on Windows uses Sora, Inter and JetBrains Mono from the till's own resources.
- `ScreenRenderer` writes `build/screens/<look>/<screen>.png` at 1280 x 800, and `ScreenComparer` passes a matching render and fails a shifted one.

**Adjacency:** all=not-applicable (test tooling and embedded fonts: no records a user lists, prints, configures or reverses)

**Adjacency rationale:** Nothing here is user-facing on its own. The fonts reach users through every screen section, which owns its own fidelity check.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Typefaces embedded in the till | -- | [ ] |
| 2 | §2 | Headless screen renderer for tests | §1 | [ ] |
| 3 | §3 | Screen comparison against the design references | §2 | [ ] |

## 1. Typefaces embedded in the till

The designs use Sora, Inter and JetBrains Mono. A till must not depend on fonts installed on the machine, and a Linux test run must measure text exactly as Windows does.

**Phase:** 0
**Needs:** none
**Surface:** no

**Build order:**

1. Link the three files from `assets/fonts` into `src/Merchant.Till/Merchant.Till.csproj` as Avalonia resources under `Assets/Fonts`. Done when: the built assembly lists them as resources.
2. Register them as the font collection `fonts:Merchant` in `src/Merchant.Till/Program.cs` and make Inter the default family. Done when: the test below resolves all three by name.

- [ ] Add `<AvaloniaResource Include="..\..\assets\fonts\*.ttf" Link="Assets\Fonts\%(Filename)%(Extension)" />` to `src/Merchant.Till/Merchant.Till.csproj`. Done when: `dotnet build src/Merchant.Till` succeeds and the three files appear under `avares://Merchant.Till/Assets/Fonts/`. Cheaper substitute: copying the font files into the project folder must fail review: `assets/fonts` stays the one copy.
- [ ] Add `src/Merchant.Till/Looks/MerchantFonts.cs` with an `EmbeddedFontCollection` keyed `fonts:Merchant` and register it in `Program.BuildAvaloniaApp` through `ConfigureFonts`, with `Inter` as the default family. Done when: `new FontFamily("fonts:Merchant#Sora")` resolves a glyph typeface whose family name is Sora.
- [ ] Remove `.WithInterFont()` from `src/Merchant.Till/Program.cs` and the `Avalonia.Fonts.Inter` package from `src/Merchant.Till/Merchant.Till.csproj`. Done when: `dotnet build Merchant.slnx` succeeds with no reference to `Avalonia.Fonts.Inter`.
- [ ] Add `tests/Merchant.Tests/Screens/FontTests.cs` asserting that Sora 700, Inter 400 and JetBrains Mono 500 each resolve from `fonts:Merchant` and that a missing family name does not. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~FontTests"` passes and fails when the resource include is removed.
- [ ] Commit: `fonts: embed Sora, Inter and JetBrains Mono in the till`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~FontTests"` passes; removing the `AvaloniaResource` line makes it fail.

## 2. Headless screen renderer for tests

Every surface section proves itself with a render. This section gives tests one way to turn a view into a 1280 x 800 PNG with real Skia drawing and no display, so the same test runs in WSL and in CI.

**Phase:** 0
**Needs:** none
**Surface:** no

**Build order:**

1. Add the headless packages and a test application to `tests/Merchant.Tests`. Done when: an Avalonia test runs under `dotnet test`.
2. Write `ScreenRenderer` in `tests/Merchant.Tests/Screens/ScreenRenderer.cs`. Done when: it writes a PNG of the stated size.

- [ ] Add `Avalonia.Headless.XUnit` and `Avalonia.Skia` (the till's Avalonia version) to `tests/Merchant.Tests/Merchant.Tests.csproj`. Done when: `dotnet restore Merchant.slnx` succeeds.
- [ ] Add `tests/Merchant.Tests/Screens/TestApp.cs`: an `Application` that loads the till's styles and fonts, configured with `UseSkia()` and `UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })`, registered with `[assembly: AvaloniaTestApplication]`. Done when: an `[AvaloniaFact]` that creates a `TextBlock` runs without a display. Cheaper substitute: headless drawing left on must fail: it renders blank frames.
- [ ] Add `tests/Merchant.Tests/Screens/ScreenRenderer.cs` with `Render(Control view, string look, string screen)`: host the view in a 1280 x 800 window, run layout and render, capture the frame and save it to `build/screens/<look>/<screen>.png` under the repository root. Done when: the file exists and its PNG header reads 1280 x 800. Cheaper substitute: saving next to the test assembly must fail review: reviewers read `build/screens`.
- [ ] Add `tests/Merchant.Tests/Screens/ScreenRendererTests.cs` rendering a view with one Sora heading on a `#F5F6FB` ground and asserting the corner pixel colour and that some pixel is ink. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ScreenRendererTests"` passes on Linux and Windows and fails when the ground colour is changed.
- [ ] Add `build/` to `.gitignore` if it is not ignored. Done when: `git status --short` shows no file under `build/` after a test run.
- [ ] Commit: `tests: headless screen renderer`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ScreenRendererTests"` passes and writes `build/screens/test/heading.png`; changing the ground colour in the test view makes it fail.

## 3. Screen comparison against the design references

The contract's fidelity rule (`design/SPEC.md` section 3) is a number: after a 2 px blur, at most 6% of pixels may differ by more than 40 of 255 in any channel. This section makes that rule a test helper so every screen is held to the same limit.

**Phase:** 0
**Needs:** none
**Surface:** no

**Build order:**

1. Write `ScreenComparer` in `tests/Merchant.Tests/Screens/ScreenComparer.cs` on SkiaSharp. Done when: it returns the differing fraction for two PNGs.
2. Prove both directions with real references. Done when: a reference passes against itself and fails against a shifted copy.

- [ ] Add `tests/Merchant.Tests/Screens/ScreenComparer.cs` with `Compare(string renderedPng, string referencePng)` returning the fraction of pixels that differ by more than 40 in any channel after a 2 px Gaussian blur of both images, and `AssertMatches(...)` that fails above 0.06 with the fraction in the message. Done when: comparing `design/interface/daylight/sell.png` with itself returns 0. Cheaper substitute: limits passed in by the caller must fail review: 2 px, 40 and 0.06 are constants named after the SPEC section.
- [ ] Make `AssertMatches` write a difference image to `build/screens/diff/<look>-<screen>.png` (differing pixels in magenta over the reference) whenever it fails. Done when: a failing comparison leaves the file and names it in the failure message.
- [ ] Refuse images of different sizes with a message that states both sizes. Done when: comparing a 1280 x 800 image with a 1200 x 630 one throws that message. Cheaper substitute: scaling one image to fit must fail review: a wrong window size is a defect to see.
- [ ] Add `tests/Merchant.Tests/Screens/ScreenComparerTests.cs`: identical passes; `daylight/sell.png` shifted 20 px right fails; `daylight/sell.png` against `counter/sell.png` fails; the same image with every channel raised by 10 passes. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ScreenComparerTests"` passes, and setting the allowed fraction to 1 makes the shifted case fail the test.
- [ ] Commit: `tests: compare screen renders with design references`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ScreenComparerTests"` passes; changing the allowed fraction constant to 1.0 makes `Shifted_reference_fails` fail.

## Verification

- A view can be rendered and compared in one test on Linux with no display: the three test classes above pass under `dotnet test Merchant.slnx`.
