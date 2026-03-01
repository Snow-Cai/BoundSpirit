# Bound Spirit - Game Dev Review (Code + Visual + Design)

## Review context
- I reviewed the Unity scripts and project asset layout directly from the repository.
- I could not run the Unity game executable in this environment, so this review focuses on production-readiness, player experience risks, and feature/design opportunities visible in code and content structure.

## High-impact technical improvements

### 1) Guard against null-reference soft locks around input gates
- Multiple scripts directly access `InputLock.Instance` without null checks when opening/closing puzzle states.
- If `InputLock` is missing from a scene, this can hard-lock progression during interactable/puzzle transitions.
- Priority fix: centralize input lock/unlock in a single service with safe null fallbacks.

### 2) Reduce expensive per-frame lookups in movement and interaction code
- `CharMovement` calls `GetComponent<SpriteRenderer>()` in `Update`, and interaction scripts call collider/component lookups repeatedly.
- Priority fix: cache frequently used components in `Awake/Start`.
- Benefit: cleaner code + less overhead on lower-end laptops.

### 3) Harden save restore paths to avoid scene-specific null crashes
- Save restore logic assumes scene objects like `Map` always exist before use.
- Priority fix: add null checks + fallback paths before iterating scene hierarchy.
- Benefit: better cross-scene resilience and fewer hard-to-reproduce restore bugs.

### 4) Move puzzle credentials/answers out of hardcoded script fields
- Login puzzle credentials are hardcoded in script defaults.
- Priority fix: store puzzle answers in ScriptableObjects and inject by chapter/scene.
- Benefit: designers can tune difficulty without code edits; easier localization/variant support.

### 5) Introduce log-level controls for release builds
- Core systems (save, interactions, transitions) use verbose logging in normal flow.
- Priority fix: wrap logs behind debug flags or conditional compilation.
- Benefit: cleaner QA logs and less runtime noise.

## Visual/UI improvements

### 1) Improve interaction readability
- Add stronger contrast and larger hit feedback on key prompts (`E` interact, confirm, close).
- Add subtle pulse/outline for currently actionable objects only.

### 2) Clarify puzzle state transitions
- Several puzzle flows open/close panels while changing input state and timescale.
- Add consistent transition animation + header text ("Puzzle Open", "Press E to close").
- Add one standard "Exit Puzzle" button across all puzzle UIs for accessibility.

### 3) Tighten content pipeline naming
- Asset list includes duplicate/copy naming patterns (e.g., `copy`, inconsistent capitalization).
- This slows team iteration and increases wrong-reference risk.
- Add naming convention and prefab folder policy by chapter + system.

## Design and puzzle/mechanic additions to implement

### 1) Add a clue synthesis board (meta puzzle)
- Current puzzle set is solid individually, but add a cross-puzzle board where clues from login/safe/polaroid are pinned and linked.
- Player must infer one final relationship (timeline + motive + location) before chapter unlock.

### 2) Add fail-forward puzzle outcomes
- Instead of binary fail/retry, grant partial discoveries:
  - wrong order in timeline -> unlock hint dialogue
  - near-miss code -> reveal one digit category
- Keeps story momentum and reduces frustration.

### 3) Add layered puzzle verbs
- Current interactions are largely inspect/drag/input.
- Add 1-2 new verbs per chapter:
  - "reconstruct" (audio waveform or torn paper alignment)
  - "trace" (network path / movement map)
  - "compare" (forged vs authentic document overlays)

### 4) Add consequence-bearing dialogue checks
- You already track dialogue choice entries in save data.
- Expand this into gated puzzle affordances:
  - if player missed key confession dialogue, puzzle gives ambiguous clue text
  - if correct inference dialogue picked, puzzle gets one optional anchor hint

### 5) Add dynamic hint economy
- Introduce "insight points" earned via exploration.
- Spend points for tiered hints (nudge -> structure -> partial answer).
- Helps pacing for wider player skill ranges.

### 6) Add chapter-end mastery test
- Build a short "case reconstruction" sequence at end of each chapter with timed but forgiving steps.
- Reuses acquired mechanics and creates stronger chapter climax.

## Difficulty tuning recommendations
- Target first-solve times:
  - Easy puzzle: 2-4 minutes
  - Core puzzle: 6-10 minutes
  - Chapter capstone: 10-15 minutes
- Telemetry to add once playable analytics are available:
  - puzzle attempts
  - time to first correct placement
  - hint usage rate
  - quit point after failures

## Next sprint plan (practical)
1. Input lock safety refactor + null guards.
2. Save restore null safety and scene fallback checks.
3. Puzzle data externalization (ScriptableObject-driven answers/hints).
4. Unified puzzle shell UI (open/close animation, exit button, prompt language).
5. Add one fail-forward branch to the polaroid puzzle as a pilot.
