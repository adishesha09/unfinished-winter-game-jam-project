# Mage Switch

> A 3D puzzle-platformer developed for the **IIE Vega School at Emeris Campus Game Jam**

---

## Overview

**Mage Switch** is a 3D puzzle-platformer in which you play as a mage who possesses the magical ability to **switch the positions of objects and platforms** in the environment. Use this power wisely to overcome obstacles, traverse dangerous terrain, and find your way to the exit door — all within a limited number of switches.

The game draws aesthetic and gameplay inspiration from classic Nintendo platformers (Super Mario Bros. Wii / 3DS), featuring tight, responsive controls, smooth jumping physics, and a cinematic side-scrolling camera perspective.

---

## Core Gameplay

| Action | Input |
|---|---|
| Move Left / Right | `A` / `D` or `←` / `→` |
| Jump | `Space` |
| Sprint | `Left Shift` |
| Switch two objects | `Left Mouse Button` — click the first object, then the second |
| Drag an object | Hold `Left Mouse Button` and drag |
| Undo last move | `Ctrl + Z` |

### The Switch Mechanic

The heart of *Mage Switch* is the **position-switching mechanic**:

1. Click on any **Switchable Object** to select it (it will be highlighted).
2. Click a second object to **swap their world positions** instantly.
3. Alternatively, **click and drag** a switchable object to reposition it freely.
4. Each switch or drag costs one move from your **move budget**.
5. Undo is available for up to 10 previous operations.
6. When you run out of moves, no further switching is possible — plan ahead!

A **cast animation** plays on the mage character whenever a switch is performed.

---

## Features

### Movement System
- Smooth acceleration and deceleration with configurable walk and sprint speeds.
- Nintendo-style jump physics: variable jump height (hold for higher), apex hang-time, ramping fall gravity, and jump buffering.
- Coyote time for forgiving edge jumps.
- Terminal fall speed cap.

### Camera System
- Pseudo-2D side-scrolling camera locked to a fixed Z-offset with horizontal look-ahead.
- Dynamic FOV scaling — widens during sprinting and falling for added momentum feel.
- Screen-shake trauma system for impactful moments.
- Progressive pitch tilt — camera sits lower at the start of a level and tilts higher toward the exit, reinforcing a sense of height and progression.
- Configurable level bounds to keep the camera within the playable area.

### Environment & Hazards
| Object | Description |
|---|---|
| **Switchable Objects** | Any tagged object can be switched or dragged. Supports move limits per group. |
| **Moving Platforms** | Waypoint-based platforms with Ping-Pong, Loop, or One-Way travel modes. Carry the player smoothly. |
| **Mushroom Springboard** | Launches the player into the air with a boosted jump. Plays a squish/bounce animation on contact. Position is fully frozen to prevent physics drift. |
| **Slippery Boulders** | Surfaces that apply low friction — the player slides off immediately on contact, encouraging careful navigation. |
| **Exit Door** | Reaching the exit triggers the level-complete sequence. |

### Character & Animation
- Playable mage character (`mage switch character.test`) with three animations:
  - **Walk** — plays during horizontal movement.
  - **Jump** — plays while airborne.
  - **Cast** — plays the full animation before the switch takes effect.
- `CharacterRootMotionGuard` prevents animation root motion from displacing the character's physics position.

### UI
- **Switch Counter** — live HUD element displaying "Switches: X / Y".
- **Level Complete Panel** — fades to black and reveals a results panel showing total switches used, with *Play Again* and *Quit* buttons.

### Custom Cursor
- A custom cursor (`Cursor.png`) is displayed whenever the player is in switching mode, providing clear visual feedback that the mechanic is active.

---

## Project Structure

```
Assets/
├── Animation/          — Animation clips and controllers
├── Cursor/             — Custom cursor texture (Cursor.png)
├── Models/             — FBX character and environment models
│   ├── mage switch character.test
│   ├── mushroom.fbx
│   ├── Boulder.fbx
│   └── Exit_Door.fbx
├── Scenes/             — Unity scene files
├── Scripts/
│   ├── Player/
│   │   ├── PlayerController.cs           — Movement, jumping, physics
│   │   ├── PlayerCameraController.cs     — Side-scroll camera with look-ahead & FOV
│   │   ├── PlayerAnimationController.cs  — Animator state management
│   │   └── CharacterRootMotionGuard.cs   — Prevents root-motion drift
│   ├── Mechanic/
│   │   ├── SwitchController.cs           — Core switch & drag mechanic
│   │   ├── SwitchableObject.cs           — Component marking an object as switchable
│   │   ├── MovingPlatform.cs             — Waypoint-based platform movement
│   │   ├── MushroomSpringboard.cs        — Springboard bounce & animation
│   │   ├── SlipperySurface.cs            — Slippery contact behaviour
│   │   └── LevelExit.cs                 — End-of-level trigger
│   └── UI/
│       ├── MoveCounterUI.cs              — Live switch counter HUD
│       └── LevelCompleteUI.cs            — End screen with stats
├── Textures/           — Texture assets
└── Settings/           — Input System, render pipeline settings
```

---

## Technical Stack

| Technology | Usage |
|---|---|
| **Unity 6.3 LTS** | Game engine |
| **Unity New Input System** | All player input via `InputSystem_Actions` |
| **Unity CharacterController** | Player movement & collision |
| **TextMeshPro** | All in-game UI text |
| **Unity Animator** | Character & mushroom animations |

---

## Getting Started

1. Clone or download the repository.
2. Open the project in **Unity 6.3 LTS**.
3. Open the main scene from `Assets/Scenes/`.
4. Press **Play** to test in the editor.

### Scene Setup Checklist
- Player GameObject requires: `PlayerController`, `PlayerAnimationController`, `CharacterController`, `Animator`.
- Camera GameObject requires: `PlayerCameraController` with `Follow Target` set to the player.
- Each switchable object requires a `SwitchableObject` component and must be on the layer specified in `SwitchController.switchableMask`.
- `SwitchController` lives on a persistent manager GameObject alongside the `MoveCounterUI` canvas.
- The `LevelCompleteUI` canvas should reference a full-screen fade `Image` and a `CanvasGroup` panel.

---

## Credits

Developed by **Team Unfinished Winter** for the **IIE Vega School – Emeris Campus Game Jam**.

---

## License

This project was created for educational and game-jam purposes. All assets and code are the property of their respective creators.
