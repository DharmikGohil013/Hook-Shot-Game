# Hook-Shot-Game
Hook Shot Game
# Hook Shot — Unity 6000.3.6f1 | Mobile Puzzle Game

**Developer:** Dharmik Gohil  
**Project Type:** Game Programming Test — FunCell Games  
**Engine:** Unity 2022.3.x (LTS)  
**Platform:** Mobile (Android) — PC mouse input for Editor testing  
**Task Duration:** 6 Hours  

---

## Project Overview

Hook Shot is a top-down mobile puzzle game where the player taps the screen to launch a ball in a rotating direction. The ball travels in a straight line, destroys wall blocks on impact, and must reach the gray goal block to complete the level. The game has 3 progressively difficult levels.

---

## Folder Structure

```
Assets/
├── Scenes/
│   ├── Level1.unity          # Easy layout
│   ├── Level2.unity          # Medium layout
│   └── Level3.unity          # Hard layout
│
├── Scripts/
│   ├── GameManager.cs        # Singleton — game state machine, scene loading
│   ├── BallController.cs     # Ball movement, input, collision, direction indicator
│   ├── TrajectoryLine.cs     # Dashed raycast preview line (child of Ball)
│   ├── HookLine.cs           # Trail line from launch to current position (child of Ball)
│   ├── CameraController.cs   # Smooth follow + orthographic zoom toggle
│   ├── UIManager.cs          # All panel fade logic, speed button, zoom button text
│   ├── BlockController.cs    # Attached to block prefab — shrink + destroy on hit
│   └── SpeedController.cs    # Singleton — manages rotation speed % cycling
│
├── Prefabs/
│   └── Block.prefab          # Pink wall block with BoxCollider + BlockController
│
└── UI/
    ├── StartPanel
    ├── RestartPanel
    └── NextLevelPanel
```

---

## Scene Hierarchy (each Level scene)

```
Scene
├── GameManager          → GameManager.cs
├── Ball (Red Sphere)
│   ├── TrajectoryLine   → TrajectoryLine.cs + LineRenderer
│   └── HookLine         → HookLine.cs + LineRenderer
├── Ground (Plane)       → Tag: "Ground"
├── Block (x N)          → Tag: "Block", BlockController.cs
├── GrayBlock            → Tag: "Goal"
├── Main Camera          → CameraController.cs
└── Canvas
    ├── StartPanel       → CanvasGroup
    │   └── StartButton
    ├── RestartPanel     → CanvasGroup
    │   └── RestartButton
    ├── NextLevelPanel   → CanvasGroup
    │   └── NextLevelButton
    ├── ZoomButton       → Toggle, child Text: "Zoom In" / "Zoom Out"
    └── SpeedButton      → Button, child Text: "80%" (default)
```

---

## Build Settings — Scene Order

| Index | Scene      | Difficulty |
|-------|------------|------------|
| 0     | Level1     | Easy       |
| 1     | Level2     | Medium     |
| 2     | Level3     | Hard       |

---

## Script Responsibilities

### GameManager.cs
- Singleton (no external packages — manual instance pattern)
- Game states: `Idle → Playing → Moving → LevelComplete → Failed`
- Fires C# events: `OnGameStart`, `OnBallMoved`, `OnBallStopped`, `OnLevelComplete`, `OnGameFailed`
- Handles StartButton / RestartButton / NextLevelButton callbacks
- Loads next scene via `SceneManager.LoadScene(buildIndex + 1)`
- On Level 3 complete (index 2), wraps back to index 0

### BallController.cs
- On game start: direction indicator (LineRenderer arrow) rotates around ball on Y axis
- Rotation speed sourced from `SpeedController.CurrentDegreesPerSecond`
- Input: `TouchPhase.Began` on device, `Input.GetMouseButtonDown(0)` in Editor
- Tap fires only when `GameManager.State == Playing` (ball is stationary)
- Ball moves via `transform.Translate` in `Update` — isKinematic Rigidbody
- `OnTriggerEnter` detects Block (tag "Block") and Goal (tag "Goal")
- Block hit → calls `BlockController.OnHit()` → ball stops
- Goal hit → fires `OnLevelComplete` event → ball stops
- Out of bounds → detected by Y position check or boundary trigger → fires `OnGameFailed`
- Direction indicator hidden while ball is moving

### TrajectoryLine.cs
- Uses LineRenderer on the `TrajectoryLine` child GameObject
- Every frame while ball is stationary: `Physics.Raycast` from ball in current facing direction
- Draws dashed line: alternating draw/skip segments (0.3 unit draw, 0.2 unit gap) up to 15 units or first hit
- Hides when ball is moving
- Width: 0.05, Color: white with 50% alpha

### HookLine.cs
- Uses LineRenderer on the `HookLine` child GameObject
- Records `launchPosition` when ball starts moving
- Each frame while moving: sets line from `launchPosition` to `ball.position`
- On ball stop: waits 1 second, then fades LineRenderer color alpha 1→0 over 0.3s via coroutine
- Width: 0.08, Color: yellow/gold

### CameraController.cs
- Orthographic camera follows ball using `Vector3.Lerp` (speed: 5f) on X and Z only
- Default orthographic size: 8 (Zoom Out)
- Zoomed size: 4 (Zoom In)
- `ZoomToggle()` public method — called by ZoomButton
- Zoom transition: coroutine with `Mathf.Lerp` over 0.4 seconds
- ZoomButton text: "Zoom In" (default, camera is out) ↔ "Zoom Out" (camera is in)

### UIManager.cs
- Inspector references: StartPanel, RestartPanel, NextLevelPanel (CanvasGroups), ZoomText, SpeedText
- `ShowPanel(CanvasGroup)`: SetActive true → coroutine alpha 0→1 over 0.4s
- `HidePanel(CanvasGroup)`: coroutine alpha 1→0 over 0.3s → SetActive false
- `SpeedCycle()`: called by SpeedButton → delegates to SpeedController, updates SpeedText
- Subscribes to all GameManager events

### BlockController.cs
- Attached to every Block prefab
- `OnHit()` called by BallController
- Plays shrink coroutine: `transform.localScale` from (1,1,1) to (0,0,0) over 0.2s
- `Destroy(gameObject)` at end of coroutine

### SpeedController.cs
- Static Singleton — persists across scenes via `DontDestroyOnLoad`
- Speed cycle array: `[70, 80, 90, 100]` — mapped to `[70, 90, 110, 130]` degrees/second
- Default index: 1 (80%)
- `CycleSpeed()`: increments index, wraps at 4
- Property: `float CurrentDegreesPerSecond`, `string CurrentLabel`

---

## Physics & Collider Setup

| GameObject  | Rigidbody         | Collider            | Is Trigger |
|-------------|-------------------|---------------------|------------|
| Ball        | Yes — isKinematic | SphereCollider      | Yes        |
| Block       | None              | BoxCollider         | Yes        |
| GrayBlock   | None              | BoxCollider         | Yes        |
| Ground      | None              | MeshCollider        | No         |

> All collision detection is done via `OnTriggerEnter` on the Ball. Blocks and Goal have trigger colliders. Ground uses a non-trigger collider only for visual grounding.

---

## Tags Required

| Tag    | Used On          |
|--------|------------------|
| Block  | All pink blocks  |
| Goal   | Gray end block   |
| Ground | Ground plane     |

---

## UI Button Behavior

### ZoomButton (Toggle)
- Default: Camera zoomed OUT → Button shows **"Zoom In"**
- Press: Camera zooms IN → Button shows **"Zoom Out"**
- Press again: Camera zooms OUT → Button shows **"Zoom In"**

### SpeedButton (Cycle Button)
- Tap cycles through: 70% → 80% → 90% → 100% → 70% → ...
- Default: **80%**
- Each % maps to a rotation speed in degrees/second
- Button text updates each tap

---

## Panels Flow

```
Scene Load
    └── StartPanel shown
         └── [Start] → Game begins, indicator spins
              ├── Ball hits Goal → NextLevelPanel shown
              │       └── [Next Level] → Load next scene
              └── Ball out of bounds → RestartPanel shown
                      └── [Restart] → Reload current scene
```

---

## LineRenderer Settings

| LineRenderer   | Width  | Color               | Use Texture  |
|----------------|--------|---------------------|--------------|
| TrajectoryLine | 0.05   | White, alpha 0.5    | Default/None |
| HookLine       | 0.08   | Gold/Yellow, alpha 1| Default/None |
| DirectionArrow | 0.05   | Red, alpha 1        | Default/None |

> Direction arrow is drawn by `BallController` using a separate LineRenderer on the Ball GameObject itself (not the children).

---

## Animations & FX

| Event             | FX Type                         | Implementation            |
|-------------------|---------------------------------|---------------------------|
| Block destroyed   | Scale shrink to zero            | Coroutine in BlockController |
| Level complete    | Particle burst at ball position | ParticleSystem configured in script — no external packages |
| Panel show/hide   | CanvasGroup alpha fade          | Coroutine in UIManager    |
| Camera zoom       | Orthographic size lerp          | Coroutine in CameraController |
| HookLine fade     | LineRenderer color alpha lerp   | Coroutine in HookLine     |

---

## Constraints & Rules

- No DOTween or external tweening libraries
- No ready-made Unity packages for game logic
- No asset store scripts
- Coroutines used for all timed transitions
- Input system: Legacy `Input` class (not new Input System package)
- All scripts are flat (no namespaces) for simplicity

---

## How to Open & Run

1. Open in **Unity 2022.3.x**
2. Go to **File → Build Settings** and confirm scene order (Level1=0, Level2=1, Level3=2)
3. Set tags: `Block`, `Goal`, `Ground` in the Tag Manager
4. Drag scripts to GameObjects as listed in the Scene Hierarchy section above
5. Assign Inspector fields in UIManager and CameraController
6. Press **Play** in Level1 scene to test

---

## Submission Info

- **File Name:** `DharmikGohil_170426`
- **Format:** Unity Source Project (Assets folder + ProjectSettings)
- **Unity Version:** 6000.3.6f1 LTS