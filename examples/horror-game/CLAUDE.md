# CLAUDE.md — Horror Game Project

## Project Overview

This is a first-person horror game built in s&box. The player explores a dark
abandoned hospital, avoiding a patrolling monster and collecting key items to
escape. The core gameplay loop is stealth-and-survival: no combat, just hiding,
distracting, and running.

## Scene Structure

The main scene is `horror-game.scene`. It uses the following root objects:

| Object name     | Purpose                                              |
|-----------------|------------------------------------------------------|
| `GameManager`   | Singleton with `GameManagerComponent` — win/lose logic |
| `Player`        | Has `PlayerController` and a child `Camera`          |
| `Monster`       | Has `MonsterAI` — patrols waypoints, chases on sight |
| `Environment`   | Root for all level geometry (walls, floors, props)   |
| `Lighting`      | Ambient + spotlight rigs                             |

## Coding Conventions

- All game scripts live in `code/` and use the `HorrorGame` namespace.
- Component class names end in `Component` only if they are pure data holders;
  behaviour components use a plain noun/verb name (e.g. `PlayerController`,
  `MonsterAI`).
- Properties exposed to the editor use `[Property]`. Keep ranges tight.
- Use `Log.Info` for game state transitions (entering/exiting states), `Log.Warning`
  for unexpected but recoverable conditions.

## Key Components

### PlayerController

Handles movement, crouching, footstep sounds, and item pickup.
The `[Property] float WalkSpeed` defaults to 180 units/s; `CrouchSpeed` is 90.
Call `PlayerController.Hide()` to force the crouch-and-freeze state used by
scripted events.

### MonsterAI

State machine: `Patrol → Alert → Chase → Search → Patrol`.
- `[Property] float SightRange` — how far the monster can see (default: 600).
- `[Property] float HearingRange` — radius for footstep/noise events (default: 400).
- `[Property] float MoveSpeed` — units/s while chasing (default: 240).

The monster raises `OnPlayerDetected` and `OnPlayerLost` events that
`GameManagerComponent` listens to for triggering jump-scares and search music.

## Asset Conventions

- Materials: `materials/horror/` prefix, `.vmat` extension.
- Models: `models/hospital/` for environmental props.
- Sounds: `sounds/horror/` — ambient loops end in `_loop`, one-shots end in `_sfx`.

## What Claude Should Help With

- Adding new room sections under `Environment` with correct lighting.
- Implementing inventory items (create a `PickupItem` component that fires an
  event on player overlap).
- Tuning `MonsterAI` parameters — use `get_all_properties` to check current values
  before suggesting changes.
- Checking the console for runtime errors after playtesting a change.
- Creating prefabs for repeated room layouts (closet, bathroom, office).

## Off-Limits

- Do not rename or re-parent `GameManager` — it uses a singleton pattern and
  other components find it by name.
- Do not change `PlayerController.WalkSpeed` above 250; the animation system
  breaks above that value.
