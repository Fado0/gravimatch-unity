# GraviMatch: Gravity-Shifting Match-3 System

Welcome to **GraviMatch**, a fully functional, highly modular, and reusable match-3 gameplay system built in Unity (C#). 

Unlike standard match-3 games where gravity always pulls tiles downwards, GraviMatch introduces **Dynamic Gravity Control** as an active player tool. The player can shift gravity to any of the four cardinal directions (Up, Down, Left, Right). Shifting gravity costs 1 move, disintegrates 3 random tiles to create gravity voids, and slides all elements on the board in the chosen direction—triggering cascades and refilling tiles from the opposite edge.

---

## 🚀 Key Features

*   **Four-Way Board Gravity**: Slide tiles and refill cells in all four cardinal directions.
*   **Tactical Gravity Shift**: Shifting gravity acts as an active move that shuffles the board and starts cascades.
*   **Model-View-Controller (MVC) Decoupling**: Core board data models (`BoardModel.cs`, `MatchResolver.cs`) are pure C# and do not depend on the Unity Engine. They can be unit-tested independently of Unity.
*   **Zero-Setup Integration**: Key feedback managers (`AudioManager.cs`, `TileVFX.cs`, `BoardShake.cs`) automatically wire and instantiate themselves at runtime.
*   **Procedural Synth Audio Engine**: All game sound effects (beeps, chord chime arpeggios, swaps, gravity rumbles, victory fanfares) are generated programmatically in C# on startup. No audio files are downloaded or imported!
*   **Polished Game Juice**: Dynamic tile scaling, board-shake impacts, and flat-color particle burst effects.
*   **Level Objectives & Win/Lose System**: Collect target amounts of specific tile colors within a limited moves budget.

---

## 🛠️ How to Open and Play

### Prerequisite
*   **Unity Editor**: Unity 6000.0.66f2 or later is recommended (or any modern Unity version supporting C# 9.0 and NetStandard 2.1).

### Setup Instructions
1.  Clone or download this repository folder.
2.  Open **Unity Hub**, click **Add**, and select the `SAVVY` folder.
3.  Open the project in Unity.
4.  Navigate to the project window and open the main scene:
    `Assets/Scenes/SampleScene.unity`
5.  Press **Play** in the Unity Editor.

### How to Control
*   **Swap Tiles**: Click a tile, then click an adjacent tile (horizontal or vertical) to swap them (costs 1 move).
*   **Shift Gravity**: 
    *   **Keyboard**: Press **W** (Up), **S** (Down), **A** (Left), or **D** (Right) or the **Arrow Keys** (costs 1 move).
    *   **UI Buttons**: Click the corresponding Shift Buttons on the Canvas (once wired).

---

## 📂 Project Structure & Key Scripts

The system logic is divided into modular folders in `Assets/Scripts`:

*   **Model Layer (Pure C#)**:
    *   [`BoardCoord.cs` / `TileMove.cs` / `TileSpawnMove.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/BoardModel.cs): Structural grid representation types.
    *   [`BoardModel.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/BoardModel.cs): Holds the 2D grid state. Performs the math for 4-way gravity shifts (`ApplyGravity`) and edge-spawning coordinate offset calculation (`FillEmptyCellsAndGetMoves`).
    *   [`MatchResolver.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/MatchResolver.cs): Pure algorithmic helper that searches the grid for vertical and horizontal match combinations and merges overlapping shapes (L and T configurations).
*   **Controller Layer**:
    *   [`Match3GameManager.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/Match3GameManager.cs): Coordinates the turn lifecycle via an enum-driven state machine (`Idle`, `Swapping`, `Resolving`, `Refilling`, `CheckEnd`). Manages scoring, turn accounting, objectives, game win/lose evaluation, and raises C# events.
*   **View & Feedback Layer**:
    *   [`BoardView.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/BoardView.cs): Renders tiles, handles mouse click selections and keyboard inputs, and controls interpolation coroutines for tile slide animations.
    *   [`GameHUD.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/GameHUD.cs): Listens to game state events to update text labels (score, moves left, gravity direction, tile objectives checklist) and displays victory/loss overlays.
    *   [`TileVFX.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/TileVFX.cs): Instantiates color-tinted particle bursts on tile clearances.
    *   [`BoardShake.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/BoardShake.cs): Smoothly shakes the main camera on board impact occurrences.
*   **Audio Engine**:
    *   [`ProceduralAudioSynth.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/ProceduralAudioSynth.cs): DSP synthesizer that builds WAV-like wave buffers in memory for retro sound effects.
    *   [`AudioManager.cs`](file:///c:/Users/faad1/Desktop/SAVVY/Assets/Scripts/AudioManager.cs): Caches and plays synthesized audio.

---

## 🎨 Architectural Decisions & Code Quality

### Decoupling
By keeping core mechanics inside `BoardModel` and `MatchResolver` as pure C# scripts, we separate the logic of matching grids from Unity rendering components. If you decide to port this system to a 3D board, a VR interface, or a console application, only the `BoardView` and `GameHUD` scripts would need to be rewritten.

### Zero Scene Friction (Self-Wiring Singletons)
Managers use a lazy-initialized singleton. If they are referenced but missing from the scene hierarchy, they auto-instantiate themselves, configure themselves, and bind onto relevant objects (e.g. `BoardShake` finds the `Camera.main` and attaches itself as a component), making integration fully automatic.

### DSP Synthesized Audio
Rather than forcing download size overhead for sound assets, the project uses a script-based synthesizer. On game startup, it generates sine waves, frequency sweeps, and arpeggios, compiling them into memory-cached `AudioClip` elements.
