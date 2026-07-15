# Gubbins

An experimental collection of development tools, literally "gubbins", but also understood as "miscellaneous tools". It's a playground for trying out new ideas and techniques in software development, with a focus on performance, efficiency, and developer experience. It is currently experimental, which means that I may break compatibility in order to fix major bugs or include critical features. Furthermore, it will not be uploaded to the NuGet Gallery, NPM, the Unity Asset Store, or the Godot Asset Library for the time being.

## Requirement

* .NetFramework: `⩾ netstandard2.1`
* Unity: `⩾ 2021.3.x`
* Godot: `⩾ 4.x`

## Core

Basic components of the Gubbins collection, including:

* `Gubbins.Collections`: A collection of data structures and algorithms for working with collections of data.
* `Gubbins.Context`: IOC (Inversion of Control) container for managing dependencies and services.
* `Gubbins.Enhance`: Improving for . NET development, like duck typing, Unit type, atomic, etc.
* `Gubbins.Entities`: A archetype design for entities with CQS (Command-Query Separation) pattern.
* `Gubbins.Events`: Event system for decoupling components and enabling event-driven programming.
* `Gubbins.Generator`: Roslyn-based code generation tools for automating repetitive coding tasks and improving productivity.
* `Gubbins.Network`: Networking utilities and tools for working with network protocols and communication.
* `Gubbins.Pipeline`: Fluent, strongly-typed processing pipelines composed of chained phases, plus reactive pipelines.
* `Gubbins.Resource`: Managing resources, such as file handling, memory management, and resource pooling.
* `Gubbins.Span`: Contiguous memory operations with SIMD and parallel processing supported.
* `Gubbins.Spawner`: Factory and object pooling system for efficient object creation and management.
* `Gubbins.Unsafe`: Unsafe code utilities for performance-critical operations and low-level programming.
* `Gubbins.Unmanaged`: Unmanaged types collections.
* `Gubbins.Tests`: Unit testing.

### Install

Since the current version has not yet been released, it can be used at this stage by referencing the source code project.

## Unity

Adapts Gubbins for Unity, integrating the Core modules with the engine's lifecycle and serialization, and adding editor tooling so the workflow feels native to the Unity Editor.

### Install

Append the following URL to `Window/Package Manager/Add package from git URL` :

```
https://github.com/gatongone/Gubbins.git?path=/Gubbins.Unity
```

Or append the following entry to `Packages/manifest.json` :

```json
{
  "dependencies":
  {
    "com.gatongone.gubbins": "https://github.com/gatongone/Gubbins.git?path=/Gubbins.Unity"
  }
}
```

## Godot

Adapts Gubbins for Godot, integrating the Core modules with the engine's C# runtime and its scene and event model so game logic and dependencies can be structured directly within Godot scenes.

### Install

Clone `Gubbins.Context` into the addons directory of your Godot project, then enable Gubbins in ProjectSettings/Plugins and trigger a rebuild. This will automatically add a references to Gubbins in the project's root `.csproj` file.

## License

Gubbins is released under the [BSD 3-Clause License](LICENSE). Copyright (c) 2026, Gatongone.