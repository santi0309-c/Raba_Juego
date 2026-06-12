# Repository Guidelines

## Project Structure & Module Organization
This repository is a Unity 6 project (`ProjectSettings/ProjectVersion.txt` pins `6000.0.76f1`). Gameplay code lives in `Assets/Scripts`, with one main scene in `Assets/ArenaScene.unity`. Runtime assets and Unity-managed resources live under `Assets/Resources` and `Assets/MobileDependencyResolver`. Treat `Library`, `Logs`, and `UserSettings` as generated editor data, not hand-edited source. `profeReferences` contains professor-provided files and must be treated as the primary reference set. If a current implementation can be replaced or simplified with equivalent `profeReferences` logic, that replacement should be made.

## Build, Test, and Development Commands
Open the project through Unity Hub using Unity `6000.0.76f1`, or launch from the editor CLI with `Unity.exe -projectPath .`. Use `dotnet build Assembly-CSharp.csproj` to catch C# compile errors without entering Play Mode. For automated checks, the Unity Test Framework package is installed, so Play Mode tests can run with `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -logFile Logs/playmode-tests.log -quit`. Local player builds are currently expected from the Unity Editor because no repository build script is checked in.

## Coding Style & Naming Conventions
Follow the existing C# style in `Assets/Scripts`: 4-space indentation, one public type per file, PascalCase for classes, methods, properties, and serialized fields intended for inspector readability. Keep the `AC_` prefix for repository-owned gameplay scripts (for example, `AC_PlayerController`, `AC_GameManager`). Prefer short inspector headers and focused `MonoBehaviour` classes over large multipurpose scripts.

## Testing Guidelines
There are no repository-owned tests yet. When adding coverage, use Unity Test Framework and place tests in `Assets/Tests/EditMode` or `Assets/Tests/PlayMode`. Name test files after the target class, such as `AC_GameManagerTests.cs`. At minimum, cover round scoring, movement edge cases, and scene wiring that can break core gameplay.

## Commit & Pull Request Guidelines
Recent history favors short, imperative commit subjects, often with a `fix:` prefix. Keep that pattern: `fix: correct dash cooldown reset` is preferable to vague summaries. Pull requests should describe gameplay impact, list affected scripts/scenes, mention inspector or scene setup changes, and include screenshots or short clips when UI, camera, or arena behavior changes.

## Configuration Tips
Do not change Unity version or package versions casually; update `Packages/manifest.json` and `ProjectSettings` together when necessary. Avoid committing accidental edits from generated folders unless the change is intentional and reviewed.
