# Hook Shot — Setup Instructions

## Component Assignment Checklist

### 1. Create an empty GameObject named "GameManager" in each scene
- Add components:
  - `GameManager.cs`
  - `SpeedController.cs`

### 2. Ball (Red Sphere)
- Add components:
  - `BallController.cs`
  - `Rigidbody`
  - `SphereCollider`
- **Rigidbody settings:**
  - `Is Kinematic` = **true**
  - `Use Gravity` = **false**
  - `Collision Detection` = Continuous (optional safety)
- **SphereCollider settings:**
  - `Is Trigger` = **false** (standard collider — the ball itself is NOT a trigger)
  - `Radius` = 0.5 (default sphere)
- **BallController Inspector fields:**
  - `Move Speed` = 8
  - `Ground Half Extent` = 25 (adjust to match your ground plane size)
  - `Indicator Length` = 1.5
  - `Indicator Width` = 0.15

### 3. TrajectoryLine (child of Ball)
- Should already exist as a child GameObject of Ball
- Add components:
  - `TrajectoryLine.cs`
  - `LineRenderer` (if not already present)
- **LineRenderer settings:**
  - `Width` = 0.08
  - `Color` = White or light blue (set start/end color)
  - `Material` = Assign your `TrajectoryMat` material (use Sprites/Default or Unlit/Color if no custom material)
  - `Use World Space` = **true**
  - `Positions` = leave at 0 (script manages this)
  - `Alignment` = Transform Z (for top-down view)

### 4. HookLine (child of Ball)
- Should already exist as a child GameObject of Ball
- Add components:
  - `HookLine.cs`
  - `LineRenderer` (if not already present)
- **LineRenderer settings:**
  - `Width` = 0.1
  - `Color` = Cyan (RGBA: 0, 1, 1, 1) — set both start and end color
  - `Material` = Assign your `HookLineMat` material (use Sprites/Default or Unlit/Color if no custom material)
  - `Use World Space` = **true**
  - `Alignment` = Transform Z

### 5. Main Camera
- Add component:
  - `CameraController.cs`
- **Camera settings:**
  - `Projection` = **Orthographic**
  - `Orthographic Size` = **8** (default zoomed-out)
  - Position: Above the scene looking down (e.g., Y=20)
  - Rotation: (90, 0, 0) — straight down
- **CameraController Inspector fields:**
  - `Ball` = Drag the Ball GameObject here
  - `Follow Speed` = 5
  - `Zoomed Out Size` = 8
  - `Zoomed In Size` = 4
  - `Zoom Duration` = 0.4

### 6. Each Block (pink cube)
- Add component:
  - `BlockController.cs`
- **Tag:** `Block`
- **Collider settings:**
  - `BoxCollider` with `Is Trigger` = **true**
  - This is critical — blocks must be triggers so the kinematic ball detects them via `OnTriggerEnter`
- No Rigidbody needed on blocks

### 7. GrayBlock (goal)
- **Tag:** `Goal`
- **Collider settings:**
  - `BoxCollider` with `Is Trigger` = **true**
- No additional scripts needed
- No Rigidbody needed

### 8. Ground Plane
- **Tag:** `Ground`
- **Collider:** `MeshCollider` or `BoxCollider` (default plane collider is fine)
  - `Is Trigger` = **false** (standard collider)

### 9. Canvas
- Already set up — just wire the references on UIManager

### 10. UIManager (on the Canvas or a dedicated empty child of Canvas)
- Add component:
  - `UIManager.cs`
- **Inspector field assignments:**
  - `Start Panel` = Drag the StartPanel GameObject
  - `Restart Panel` = Drag the RestartPanel GameObject
  - `Next Level Panel` = Drag the NextLevelPanel GameObject
  - `Start Button` = Drag the StartButton (Button component)
  - `Restart Button` = Drag the RestartButton (Button component)
  - `Next Level Button` = Drag the NextLevelButton (Button component)
  - `Zoom Toggle` = Drag the ZoomButton (Toggle component)
  - `Speed Button` = Drag the SpeedButton (Button component)
  - `Zoom Button Text` = Drag the Text child of ZoomButton
  - `Speed Button Text` = Drag the Text child of SpeedButton
  - `Camera Controller` = Drag the Main Camera (which has CameraController)

---

## Tag Setup

Open **Edit → Project Settings → Tags and Layers** and create these tags if they don't exist:

| Tag      | Used On                    |
|----------|----------------------------|
| `Block`  | All pink cube wall blocks  |
| `Goal`   | The gray goal block        |
| `Ground` | The green ground plane     |

---

## Layer Setup (optional but recommended)

| Layer       | Used On           | Purpose                                    |
|-------------|-------------------|--------------------------------------------|
| `Block`     | Block prefabs     | TrajectoryLine raycast mask                |
| `Ground`    | Ground plane      | Separate from default for raycasting       |

If you set up custom layers, update the `Raycast Mask` field on `TrajectoryLine.cs` in the Inspector to include only the layers you want the trajectory to hit (Block, Ground, Goal).

---

## Build Settings — Scene Order

**File → Build Settings → Scenes in Build:**

| Index | Scene File       |
|-------|------------------|
| 0     | `Scenes/l1`      |
| 1     | `Scenes/l1 1`    |
| 2     | `Scenes/l1 3`    |

Drag your 3 scene files into the Build Settings list in this exact order. Level 1 = index 0, Level 2 = index 1, Level 3 = index 2.

---

## Collision Design Explanation

The system uses **Trigger colliders on Blocks and Goal** + **Kinematic Rigidbody on the Ball**:

- Ball: `Rigidbody (isKinematic=true)` + `SphereCollider (isTrigger=false)`
- Blocks: `BoxCollider (isTrigger=true)` + No Rigidbody
- Goal: `BoxCollider (isTrigger=true)` + No Rigidbody

This works because Unity fires `OnTriggerEnter` when a kinematic Rigidbody moves into a trigger collider. The ball moves via `transform.position` in `FixedUpdate`, and the kinematic Rigidbody ensures trigger callbacks are generated.

---

## Panel CanvasGroup Requirements

Each panel (StartPanel, RestartPanel, NextLevelPanel) **must** have a `CanvasGroup` component for the fade animations to work. The UIManager script controls:
- `alpha` (0→1 for show, 1→0 for hide)
- `interactable` (disabled during fade)
- `blocksRaycasts` (disabled during fade)

---

## Quick Verification Checklist

- [ ] GameManager + SpeedController on a "GameManager" object in every scene
- [ ] BallController on Ball, with Rigidbody (kinematic) + SphereCollider
- [ ] TrajectoryLine.cs + LineRenderer on the TrajectoryLine child of Ball
- [ ] HookLine.cs + LineRenderer on the HookLine child of Ball
- [ ] CameraController.cs on Main Camera, `Ball` field assigned
- [ ] BlockController.cs on every Block prefab, tag = "Block", BoxCollider isTrigger = true
- [ ] GrayBlock tag = "Goal", BoxCollider isTrigger = true
- [ ] Ground tag = "Ground"
- [ ] UIManager.cs on Canvas (or child), ALL 11 Inspector fields assigned
- [ ] All 3 panels have CanvasGroup components
- [ ] Build Settings: 3 scenes in order (index 0, 1, 2)
- [ ] Tags created: "Block", "Goal", "Ground"
