# 3D-survivors-gameplay-systems
Modular gameplay systems for a Unity 3D Survivors game, including combat, weapons, progression, and UI.

The original Unity project remains private because it contains collaborative work and proprietary assets.
This repository showcases only the gameplay systems and source code that I personally developed, along with demonstration videos.

### Weapon Framework
Rather than implementing each weapon independently, I designed a reusable weapon framework that allows new weapons to be created primarily through data and prefabs.
The framework separates weapon data, firing logic, projectile behavior, and gameplay effects, enabling rapid prototyping without modifying existing weapon code.

WeaponData (ScriptableObject) -> Weapon.cs(Firing Logic) -> Projectile Prefab -> Projectile.cs -> IDamageable

The framework supports multiple weapon behaviors through shared infrastructure.

Implemented weapon types include:
- Projectile
- Melee
- Laser
- Meteor
- Orbit
- Homing Missile
- Explosive
- Piercing
- Bounce
- Chain

