# KartRider-Style Racing Game

A Unity-based kart racing game inspired by KartRider's Speed Mode, featuring drift mechanics, boost system, and lap-based racing.

## Features

- **Drift Mechanics**: Hold SHIFT while turning to drift. Drifting charges your booster.
- **Two-Slot Booster System**: Store up to 2 boosters. Each drift that charges to 100% fills one slot.
- **Boost Activation**: Press `/` key to activate a 3-second speed boost.
- **Complex Track**: KartRider-style track with curves, straights, and elevation changes.
- **Lap Counting**: Checkpoint-based lap system with timing.
- **Speed Mode**: Pure racing without items - skill-based gameplay.

## Controls

| Key | Action |
|-----|--------|
| W / Up Arrow | Accelerate |
| S / Down Arrow | Brake / Reverse |
| A / Left Arrow | Steer Left |
| D / Right Arrow | Steer Right |
| Left Shift | Drift (hold while turning) |
| / (Slash) | Activate Boost |
| R | Restart Race |

## Unity Setup Instructions

### Requirements
- Unity 2021.3 LTS or newer
- TextMeshPro package (usually included)

### Step 1: Create Unity Project
1. Open Unity Hub
2. Create a new 3D project
3. Copy the `Assets` folder from this repository into your project

### Step 2: Scene Setup

#### Create the Kart
1. Create a new empty GameObject, name it "PlayerKart"
2. Add the `KartController` script
3. Add a Rigidbody component (script will auto-configure it)
4. Add a Collider (BoxCollider recommended)
5. Set the tag to "Player"
6. Add your kart 3D model as a child (or use primitives for testing)

#### Create the Track
1. Create an empty GameObject named "Track"
2. Add the `TrackGenerator` script
3. In the Inspector, right-click the component and select "Generate Default KartRider Track"
4. Then right-click and select "Build Track Mesh"
5. Create materials for track, walls, and finish line

#### Create Checkpoints
1. The TrackGenerator creates checkpoints automatically
2. Or manually create GameObjects with `Checkpoint` script
3. Create a parent with `CheckpointManager` script
4. Tag walls as "Wall" for collision detection

#### Create the Camera
1. Add `CameraController` script to Main Camera
2. Assign the PlayerKart transform as target
3. Adjust offset and follow settings as desired

#### Create the Game Manager
1. Create empty GameObject "GameManager"
2. Add `GameManager` script
3. Assign references:
   - Player Kart
   - Start Position (empty transform at start line)
   - Checkpoint Manager

#### Create the UI
1. Create a Canvas (Screen Space - Overlay)
2. Add UI elements:
   - Speed text (TextMeshPro)
   - Lap counter text
   - Timer texts (current, best lap, total)
   - Boost meter (Image with fill)
   - 2 Booster slot images
   - Countdown panel with text
   - Finish panel
3. Add `UIManager` script to Canvas
4. Assign all UI references

### Step 3: Layer and Tag Setup
1. Create tag: "Player", "Wall"
2. Assign "Player" tag to kart
3. Assign "Wall" tag to track walls

### Step 4: Materials (Optional)
Create materials in `Assets/Materials`:
- TrackMaterial: Dark gray asphalt look
- WallMaterial: Red/white striped barriers
- FinishLineMaterial: Checkered pattern

## Project Structure

```
Assets/
├── Scripts/
│   ├── KartController.cs      # Kart physics, drift, boost
│   ├── GameManager.cs         # Race state, laps, timing
│   ├── UIManager.cs           # HUD display
│   ├── Checkpoint.cs          # Individual checkpoint trigger
│   ├── CheckpointManager.cs   # Manages all checkpoints
│   ├── CameraController.cs    # Follow camera with effects
│   └── TrackGenerator.cs      # Procedural track builder
├── Prefabs/
├── Scenes/
└── Materials/
```

## Drift & Boost Mechanics

### How Drifting Works
1. Reach minimum speed (configurable, default: 5 units)
2. Hold SHIFT while pressing a turn direction
3. Your drift charge meter fills up
4. At 100% charge, one booster slot is filled
5. Continue drifting to fill the second slot

### How Boosting Works
1. Fill at least one booster slot through drifting
2. Press `/` to activate boost
3. Boost lasts 3 seconds (configurable)
4. Speed increases by 1.8x multiplier (configurable)
5. One booster slot is consumed per use

## Customization

### KartController Settings
- `maxSpeed`: Base maximum speed (default: 15)
- `acceleration`: How fast the kart speeds up
- `driftChargeRate`: How fast drift charges boosters (% per second)
- `boostDuration`: How long boost lasts (default: 3 seconds)
- `boostSpeedMultiplier`: Speed increase during boost (default: 1.8x)

### Track Settings
Edit `TrackGenerator` to customize:
- Track width
- Wall height
- Track point positions for custom layouts
- Banking angles for turns

## Tips for Best Gameplay

1. **Chain Drifts**: Enter corners early and maintain drift through the turn
2. **Save Boosters**: Use boosts on straightaways for maximum effect
3. **Learn the Track**: Memorize checkpoint locations and optimal racing lines
4. **Don't Hit Walls**: Wall collisions drastically reduce speed and cancel boost
