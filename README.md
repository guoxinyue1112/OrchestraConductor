# Orchestra Conductor

Version: `0.0.1-test`

`Orchestra Conductor` is a small first-person Unity prototype where the player stands inside a concert hall and brings different sections of Beethoven's Fifth Symphony in and out in real time.

## Overview

Walk into the hall, face the orchestra, and conduct a classical performance as if you were standing on the podium yourself.

`Orchestra Conductor` turns orchestral control into an immediate, expressive play experience: bring in strings, woodwinds, brass, and percussion at will, reshape the balance of the ensemble in real time, and experience classical music not just as a listener, but as the person guiding the performance.

It is designed as an interactive classical conducting experience with a live concert-hall atmosphere, first-person movement, and hands-on control over orchestral sections.

![Orchestra Conductor preview](Docs/Images/screenshot-v0.0.1.png)

## Features

- Playable Unity 6 concert-hall prototype
- Four controllable orchestral groups: strings, woodwinds, brass, percussion
- Start menu with blurred concert-hall background
- Runtime scene builder for reconstructing the demo scene
- Automatic audio-stem assignment from filenames in `Assets/Audio`

## Gameplay

The player begins at the title screen facing the orchestra. Press `Play`, step into the hall, and shape the performance by bringing each section in and out on a shared musical timeline.

## Controls

- `W A S D`: move
- `Mouse`: look
- `Left Shift`: move faster
- Hold `1`: strings
- Hold `2`: woodwinds
- Hold `3`: brass
- Hold `4`: percussion
- Hold `Space`: full orchestra
- `R`: restart the piece
- `Esc`: release the mouse cursor

## Build And Run

### Unity Editor

1. Open the project in Unity `6000.5.10f1`.
2. Open `Assets/Scenes/Beethoven5Demo.unity`, or regenerate it from `Tools > Orchestra Conductor > Create Beethoven 5 Demo Scene`.
3. Press `Play`.

### Windows Test Build

1. Open the project in Unity.
2. Run `Tools > Orchestra Conductor > Release > Build Windows v0.0.1`.
3. Find the build in `Builds/Windows/v0.0.1/`.

## Release Tooling

Release helpers live in `Assets/Editor/ReleaseTools.cs`.

- `Tools > Orchestra Conductor > Release > Capture README Screenshot`
- `Tools > Orchestra Conductor > Release > Build Windows v0.0.1`

## Audio Assignment Protocol

When the demo scene is generated, filenames in `Assets/Audio` are scanned and matched by keyword.

- `STRINGS`: violin, viola, cello, violoncello, contrabass, double bass aliases
- `WOODWINDS`: flute, oboe, clarinet, bassoon
- `BRASS`: horn, french horn, english horn, cor anglais aliases
- `PERCUSSION`: timpani, kettledrum

Unmatched clips remain available for manual review in the Unity Editor.

## Project Structure

- `Assets/Scenes/Beethoven5Demo.unity`: main playable scene
- `Assets/Scripts/`: runtime gameplay scripts
- `Assets/Editor/Beethoven5DemoSceneBuilder.cs`: scene generator
- `Assets/Editor/ReleaseTools.cs`: release screenshot and build helpers
- `Assets/Audio/`: orchestral stems
- `Assets/Materials/`, `Assets/Textures/`: environment visuals

## License

The project source code is released under the MIT License. See [LICENSE](LICENSE).

Third-party art, Unity packages, and imported media keep their own original licenses. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Release Notes

`0.0.1-test` is the first playable test release, prepared on August 31, 2026.

Current limitations:

- Visual presentation is still prototype-grade in several gameplay elements
- The orchestral performers are represented by simplified stand-in forms
- Third-party asset redistribution rights should be confirmed before broad public release
