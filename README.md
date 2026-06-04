# DreadWood 🌲🌑

A third-person horror-survival game built in **Unity 6** (Built-In Render Pipeline).

You wake alone in a cabin deep in the woods. A pale light glows on the horizon — the only way out. Sneak past or fight the creatures that roam the trail, scavenge what you can, and reach the light… where something is waiting.

---

## 🎮 Gameplay

- **Explore** an eerie, hand-sculpted forest at dusk, guided by a distant beacon.
- **Stealth or combat** — creep past wandering zombies with vision cones, or draw your axe and fight.
- **Melee combat** — a light attack combo, a running leap attack, a dodge with invincibility frames, and real hit feedback.
- **Survive** — pick up health along the trail to stay alive.
- **Boss fight** — reach the light and the guiding glow dies… revealing the creature you must defeat to escape.

## ⌨️ Controls

| Key | Action |
| --- | --- |
| **WASD** | Move |
| **Shift** | Run |
| **Ctrl** | Sneak (harder to detect) |
| **Left Click** | Attack |
| **Right Click** | Dodge |
| **Space** | Jump |
| **Y** | Open door |
| **Esc** | Pause |

## ✨ Features

- Hand-sculpted **Terrain** with a winding forest trail
- Stealth AI: vision cones, line-of-sight, wander → chase behaviour
- 3-hit melee combo + running attack with slash VFX and hit-stop
- Crouch-stealth that shrinks enemy detection range
- Health pickups scattered along the route
- Eerie dusk atmosphere (fog + cold lighting)
- Full menu system: Main / Pause / Win-Lose, with a controls legend
- A boss encounter that turns off the safe light when the fight begins

## 🛠️ Built With

- **Unity 6000.4** — Built-In Render Pipeline
- Starter Assets (Third Person Controller), with custom combat/stealth/dodge scripts
- Mixamo character & animations (the "Brute")
- Nature Starter Kit 2 (trees), Hovl Studio effects (VFX)

## ▶️ How to Run

**Play the build:** download the build folder, unzip, and run `DreadWood.exe` (Windows). Keep all files together in the same folder.

**Open in Unity:** clone the repo and open the project with Unity 6000.4 or newer. The playable scene is `Assets/Scenes/SampleScene`.

```bash
git clone https://github.com/Amit-Eizen/DreadWood.git
```

> Uses **Git LFS** for large binary assets — install it before cloning (`git lfs install`).

## 👥 Authors

- **Amit Eizenberg**
- Ofir Shviro

---

*A student game project. Made with Unity.* 🎃