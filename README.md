# Solar System Explorer

A real-time interactive solar system simulation built in Unity, combining procedural generation, physics-based orbital mechanics, and a fully autonomous probe that navigates the system, scans planets, and reacts to dynamic space events.

## What it is

Solar System Explorer puts you inside a scale model of the solar system — eight planets, an asteroid belt, moons for all the gas giants and Earth and Mars, the Sun with a dynamic glow and corona, and a spacecraft that can navigate it all on its own or respond to manual input. The visual side leans on real planet textures, a custom atmosphere shader, post-processing bloom, and a procedurally generated starfield and nebula system. On top of the simulation, there's a cosmic timeline that lets you fast-forward through the Sun's entire 13-billion-year life cycle and watch the solar system change in real time.

The project was built entirely from code — no prefab drag-and-drop. Everything from planet materials and moon spawning to UI canvases and post-processing volumes is constructed at runtime by a single bootstrap script that reads the scene and configures it.

## Getting started

Open `Assets/Scenes/SampleScene.unity` in Unity (URP, Unity 6 or later) and hit Play. The bootstrap system handles the rest: it lays out the solar system, applies materials, starts orbital motion, configures lighting, and builds the HUD. There are no manual configuration steps.

If the planet materials are missing or need to be regenerated, run `Tools > Solar System > Create Planet Materials` from the Unity menu bar. This bakes the material assets into `Assets/Resources/Materials/Planets/`.

## Core systems

### Scene bootstrap

`SceneBootstrap` is the entry point for everything. It runs at the start of Play Mode with a high execution order (-100) so all other scripts can safely reference the scene state it creates. It positions the sun and each planet at scaled orbital distances, applies their materials and atmospheres, spawns moons, creates the asteroid belt, configures the camera and lighting, adds post-processing, and builds the entire HUD programmatically. The same script also runs a lighter preview mode in the editor so the Scene and Game tabs show the correct layout without entering Play Mode.

### Orbital mechanics

Each planet follows a circular orbit around the Sun using `OrbitalMotion`, with angular speed derived from Kepler's third law so inner planets orbit faster than outer ones. Separately, `GravitySimulator` runs an N-body gravitational simulation for objects tagged as `CelestialBody`, applying Newton's law of gravitation each physics step. The two systems can coexist — planets use the simplified orbital animation, while the probe and other dynamic bodies are subject to actual gravity.

### Probe and state machine

The probe is the main interactive element. It uses a finite state machine with seven states:

`Idle` waits for a target. `ChooseTarget` scans the planet registry and selects the highest-priority unvisited planet based on exploration score. `Travel` smooth-damps the probe toward a sequence of A\*-computed waypoints, slowing down when passing through nebula regions. `Scan` holds the probe at the target for two seconds, plays a scanning ring animation, and logs planet data to the mission log. `AvoidCollision` fires when a forward raycast detects an obstacle, steering the probe clear for three seconds before resuming. `Return` brings the probe back to its starting position once all planets are explored. `ManualControl` switches to direct keyboard piloting, where solar wind and gravity still apply.

The A\* pathfinder (`AStarPathfinder`) generates waypoint shells around planets and other obstacles, with heavier planets incurring a higher path cost so the probe routes around them rather than through their gravity wells.

### Cosmic timeline

`CosmicTimelineManager` tracks cosmic time from 0 to 13 billion years and maps it to five solar stages: Main Sequence (0–5 Gyr), Sub-Giant (5–6 Gyr), Red Giant (6–8.5 Gyr), Planetary Nebula (8.5–9.5 Gyr), and White Dwarf (9.5–13 Gyr). Advancing the timeline triggers cascading changes across the system.

`SunEvolutionController` updates the Sun's scale, light intensity, light color, and emission color per stage. `PlanetEvolutionController` adjusts each planet's atmosphere tint and scale, and triggers `PlanetExplosionVFX` when a planet is destroyed by the expanding Red Giant. The UI slider in the bottom-right corner controls the timeline directly, and the HUD logs stage transitions and milestone events like Earth's oceans evaporating at 7 Gyr.

### Random events

`RandomEventManager` fires three types of encounter at randomized intervals: solar storms that apply a brief outward force to nearby objects, rogue asteroids that cross the system on a physics trajectory, and meteor showers that spawn clusters of projectiles. These are independent of the timeline and can happen at any point during Play Mode.

## Visuals

Planet textures are loaded from `Assets/Resources/PlanetTextures/`. If a texture file is present for a planet, the material uses it with a URP Lit shader. If no file exists, `ProceduralPlanetTexture` generates an albedo and normal map at runtime using layered noise. Saturn's rings are a procedurally generated mesh with a transparent ring material.

Atmospheres use a custom Fresnel rim-light shader (`FresnelAtmosphere`) that produces a bright glowing edge scaled to the planet's atmospheric density. Airless bodies like Mercury skip it entirely. The Sun has two layered billboard quads (a glow and a larger corona) rendered with additive blending so they contribute to bloom.

Post-processing is configured at runtime: ACES tonemapping, subtle vignette, a bloom pass tuned to catch the Sun and emissive planet surfaces, and FXAA. Two very dim directional fill lights prevent the fully-dark shadow sides that a single point light source would otherwise produce.

The `ReactiveNebula` system places nebula volumes at four positions around the outer system. Each has three internal states — Calm, Warning, and Danger — based on how deep the probe is inside it. The probe slows down in nebula regions, and `NebulaVisionEffect` tints the screen accordingly.

## Controls

During Play Mode the probe operates autonomously by default. You can switch to manual control from the Planet Selection panel or by direct input.

**Camera** — right-click and drag to look around, scroll to zoom, WASD to pan. The camera also auto-focuses on the selected planet when the probe arrives.

**Manual probe control** — WASD to move in the horizontal plane, Q/E for up and down, left shift to boost speed. Space triggers a manual scan if the probe is near a planet.

**Time scale** — the bottom-right slider adjusts simulation speed from 1× to 100×. The pause button freezes everything except the UI.

**Cosmic timeline** — the slider above the time scale panel scrubs through the 13 Gyr lifecycle. The "Present Day" button snaps back to 4.5 Gyr.

**Planet selection** — the top-left dropdown lists all planets. Selecting one shows orbital data and surface facts in the info panel, and the "Send Probe" button dispatches the probe immediately.

## Project structure

```
Assets/
  Editor/               PlanetMaterialCreator — menu tool to bake planet materials
  Resources/
    Materials/Planets/  Pre-baked planet .mat files
    PlanetTextures/     Real planet texture maps (JPG)
  Scenes/               SampleScene.unity — the only scene
  Scripts/
    Camera/             CameraController, FreeFlyCamera
    Events/             RandomEventManager, MeteorMover
    Pathfinding/        AStarPathfinder, PathVisualizer
    Planets/            Planet, PlanetData, PlanetManager
    Probe/              ProbeController, ProceduralRocket, RocketExhaust
      States/           IdleState, ChooseTargetState, TravelState, ScanState,
                        AvoidCollisionState, ReturnState, ManualControlState
    Simulation/         OrbitalMotion, GravitySimulator, CelestialBody,
                        SunEvolutionController, PlanetEvolutionController,
                        CosmicTimelineManager, ReactiveNebula, AsteroidBelt,
                        SaturnRings, Starfield, SolarWind, and others
    UI/                 HUDController, PlanetSelectionUI, StartScreen,
                        TimeScaleController, MissionLog, CosmicTimelineUIController
  Shaders/              FresnelAtmosphere.shader
  Settings/             URP renderer and volume profiles
```

## Dependencies

The project uses Unity's Universal Render Pipeline and TextMesh Pro. No third-party paid assets are required for core functionality.
