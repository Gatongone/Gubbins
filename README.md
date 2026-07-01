# Gubbins

An experimental collection of development tools, literally "gubbins", but also understood as "miscellaneous tools". It's a playground for trying out new ideas and techniques in software development, with a focus on performance, efficiency, and developer experience. It is currently experimental, which means that I may break compatibility in order to fix major bugs or include critical features.

## Core

Basic components of the Gubbins collection, including:

- `Gubbins.Collections`: A collection of data structures and algorithms for working with collections of data.
- `Gubbins.Context`: IOC (Inversion of Control) container for managing dependencies and services.
- `Gubbins.Enhance`: Improving for .NET development, like duck typing, Unit type, atomic, etc.
- `Gubbins.Entities`: A archetype design for entities with CQS (Command-Query Separation) pattern.
- `Gubbins.Events`: Event system for decoupling components and enabling event-driven programming.
- `Gubbins.Network`: Networking utilities and tools for working with network protocols and communication.
- `Gubbins.Resource`: Managing resources, such as file handling, memory management, and resource pooling.
- `Gubbins.Spawner`: Factory and object pooling system for efficient object creation and management.
- `Gubbins.Span`: Contiguous memory operations with SIMD and parallel processing supported.
- `Gubbins.Unsafe`: Unsafe code utilities for performance-critical operations and low-level programming.
- `Gubbins.Unmanaged`: Unmanaged types collections.
- `Gubbins.Generator`: Roslyn-based code generation tools for automating repetitive coding tasks and improving productivity.
- `Gubbins.Pipeline`: Fluent, strongly-typed processing pipelines composed of chained phases, plus reactive (event-driven) pipelines wired through `Gubbins.Context`.
- `Gubbins.Tests`: Unit testing.

## Unity

Adapts Gubbins for Unity, integrating the Core modules with the engine's lifecycle and serialization, and adding editor tooling so the workflow feels native to the Unity Editor.

## Godot

Adapts Gubbins for Godot, integrating the Core modules with the engine's C# runtime and its scene and event model so game logic and dependencies can be structured directly within Godot scenes.

## License

Gubbins is released under the [BSD 3-Clause License](LICENSE). Copyright (c) 2025, Gatongone.