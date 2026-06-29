# 3D-survivors-gameplay-systems
Modular gameplay systems for a Unity 3D Survivors game, including combat, weapons, progression, and UI.

The original Unity project remains private because it contains collaborative work and proprietary assets.
This repository showcases only the gameplay systems and source code that I personally developed, along with demonstration videos.

## Full Gameplay Demo
Demo Video 1
▶ https://youtu.be/zZ7PVjA1bSQ

Demo Video 2
▶ https://youtu.be/YGm99Whnb28


## Weapon Framework

One of the primary goals of this project was to build a scalable weapon framework that allows designers and developers to create new weapons with minimal implementation effort.

Instead of implementing every weapon as a separate system, the framework separates weapon configuration, firing logic, projectile behavior, and damage processing into independent, reusable modules.

This architecture enables rapid weapon prototyping while reducing duplicated code and simplifying future maintenance.

### Architecture
The weapon pipeline is organized as follows:

```
                 WeaponData
                     │
                     ▼
                 Weapon.cs
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
 Player Weapon              Enemy Weapon
        │                         │
        └────────────┬────────────┘
                     ▼
               Projectile.cs
                     │
                     ▼
                IDamageable
```

Each layer has a single responsibility:

- **WeaponData** stores configurable weapon parameters.
- **Weapon.cs** handles firing logic and cooldown management.
- **Projectile Prefab** defines the weapon's visual representation.
- **Projectile.cs** controls movement, collision, and special behaviors.
- **IDamageable** provides a common interface for applying damage to any valid target.

This separation allows individual components to be reused across multiple weapon types without modifying the core combat framework.

### Rapid Weapon Creation

<img src="demo/weapon-creation.gif" width="1000">
*Figure. Component-based weapon architecture used to configure and extend new weapon types.*

<img src="demo/weapon-component.png" width="600">

New weapons can be created by duplicating an existing `WeaponData` asset, assigning a projectile prefab, and adjusting configuration values in the Inspector.
No changes to the core weapon framework are required, allowing new gameplay content to be added quickly while preserving maintainability.

### Supported Weapon Behaviors

The framework currently supports multiple weapon behaviors through the same shared architecture.

- Projectile
- Melee
- Laser
- Meteor
- Orbit
- Homing Missile
- Explosive
- Piercing
- Bounce
- Chain Lightning

Additional weapon behaviors can be introduced by extending projectile logic or creating new prefabs without changing the existing weapon pipeline.

## Combat System

The combat system was designed around reusable gameplay components that support both melee and ranged combat through a unified damage pipeline.

Key Features

- Melee attacks
- Projectile attacks
- Hit detection
- Critical damage
- Attack cooldown management
- Object pooling

## Status Effect Framework

A reusable status effect framework was implemented to support multiple combat effects through a unified architecture.

Supported Effects

- Burn
- Freeze
- Shock
- Poison
- Stun

## Ability Node System

Implemented a modular progression system allowing players to strengthen their character through unlockable passive abilities.

Features

- Passive upgrades
- Character progression
- Runtime stat modification
- Build customization

## User Interface

Implemented gameplay UI systems for player interaction and combat feedback.

Features

- HUD
- Upgrade UI
- Damage Popup
- Reroll
- Ability UI

## Design Philosophy 
This project focuses on building scalable gameplay systems instead of implementing isolated game features.

The weapon framework, combat pipeline, status effect system, progression system, and UI were designed to be modular and data-driven, allowing new gameplay content to be created primarily through configuration and prefabs.

The same weapon framework is shared between both player and enemy characters, enabling weapons to be reused without duplicating combat logic. This approach improves maintainability, reduces code duplication, and accelerates gameplay prototyping.

The framework is shared by both player and enemy characters, allowing identical weapon behaviors to be reused across different gameplay entities without additional implementation.
