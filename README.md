# Neon Fever

## About the Game

The player has to navigate a car through a procedural generated level. In doing so, they must avoid obstacles and must not touch the side walls.
To navigate better through the course, the player can temporarily slow down the time.

    Key aspects:

    - Made with Unity 2020.3.10f1
    - Developed for Android
    - Visual effects depending on the music 

https://github.com/user-attachments/assets/a95ac41f-2274-48bb-ad59-2620049beedb

## Code samples

### Level Creator ([View Script](scripts/LevelCreator.cs))

This script creates a procedural level.
[LineRenderers](https://docs.unity3d.com/ScriptReference/LineRenderer.html) are used for the side walls, which are provided with BoxColliders.
In addition, random traps are created on the line.

### Level Designer ([View Script](scripts/LevelDesigner.cs))

The Level Designer sets the visuals of the level.
Depending on the setting in the inspector, the colours are generated randomly or selected from a list of predefined presets.

### Audio Visualizer ([View Script](scripts/AudioVisualizer.cs))

In addition to the LineRenderers for the track boundaries, there is another LineRenderer for the visual representation of the music.
The lines move in a definable cycle depending on the spectrum of the music.