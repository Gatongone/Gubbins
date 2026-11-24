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
* `Gubbins.Events`: Event system for decoupling components and enabling event-driven programming, with `EventStream` for basic reactive composition (`Where`, `Select`, `DistinctUntilChanged`, `Take`).
* `Gubbins.Network`: Networking utilities and tools for working with network protocols and communication.
* `Gubbins.Pipeline`: Fluent, strongly-typed processing pipelines composed of chained phases, plus reactive pipelines.
* `Gubbins.Resource`: Managing resources, such as file handling, memory management, and resource pooling.
* `Gubbins.Span`: Contiguous memory operations with SIMD and parallel processing supported.
* `Gubbins.Spawner`: Factory and object pooling system for efficient object creation and management.
* `Gubbins.Unsafe`: Unsafe code utilities for performance-critical operations and low-level programming.
* `Gubbins.Unmanaged`: Unmanaged types collections.

* `Gubbins.Graphic`: Compute kernels written as C# methods, sharing their types and helpers with the code that runs on the CPU.

Source generators, tests, benchmarks and engine integrations live inside the module they belong to:

```text
Unsafe/
  Core/
    Gubbins.Unsafe.csproj
  Unity/
  Godot/
  Generator/
  Tests/
```

## Graphic

A kernel is an ordinary static method. It compiles, the central processor can call it directly as the reference
implementation, and the source generator writes a typed entry point beside it that dispatches it on the device.

```csharp
using Gubbins.Graphic;

[GpuStruct]
public struct Position
{
    public float X;
    public float Y;
    public float Z;

    public Position(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public static partial class Motion
{
    private const float Retention = 0.98f;

    [GpuCompatible]
    public static float Damping(float value) => value * Retention;

    [GpuKernel]
    public static void Integrate(int index, ReadOnlySpan<Position> source, Span<Position> target)
        => target[index] = new Position(Damping(source[index].X), source[index].Y, source[index].Z);
}

// CPU: the method itself.
for (var index = 0; index < positions.Length; index++)
{
    Motion.Integrate(index, positions, output);
}

// GPU: the entry point the generator wrote, over spans or over buffers of this layer.
using var gpu = Gpu.Create();
Motion.Run(gpu, positions, output);
```

The type that declares a kernel is `partial`, because the entry point is a member of it, and the project references
`Gubbins.Graphic.Generator` as an analyzer. A kernel reads `ReadOnlySpan<T>` and writes `Span<T>`; `Shared<T>()` and
`Barrier()` declare the memory of a group and `GpuWave` its wave operations, none of which has a central processor
meaning, so calling one there throws instead of returning a value that only looks right.

The translation happens while the program compiles, which is what turns a construct the device cannot run into an editor
error. The artifacts are built after the program, by `Graphic/ArtifactTool`, which runs `slangc`: set `SLANGC` to its path,
and have the Windows driver kit on `PATH` for the D3D12 target. A machine without the compiler still builds the program
and skips the artifacts, so the same code runs on a build server that never dispatches a kernel.

## Unity

Adapts Gubbins for Unity, integrating the Core modules with the engine's lifecycle and serialization, and adding editor tooling so the workflow feels native to the Unity Editor.

### Install

The Unity integration is split into one package per core module. Each module package carries its own core assembly and generator analyzer; install the module packages you use together with their dependencies, for example:

```
https://github.com/gatongone/Gubbins.git?path=/Events/Unity
https://github.com/gatongone/Gubbins.git?path=/Enhance/Unity
https://github.com/gatongone/Gubbins.git?path=/Unsafe/Unity
```

Or append entries to `Packages/manifest.json` :

```json
{
  "dependencies":
  {
    "com.gatongone.gubbins.events": "https://github.com/gatongone/Gubbins.git?path=/Events/Unity",
    "com.gatongone.gubbins.enhance": "https://github.com/gatongone/Gubbins.git?path=/Enhance/Unity",
    "com.gatongone.gubbins.unsafe": "https://github.com/gatongone/Gubbins.git?path=/Unsafe/Unity"
  }
}
```

Editor tooling is provided by `Editor/Unity`.

## Godot

Adapts Gubbins for Godot, integrating the Core modules with the engine's C# runtime and its scene and event model so game logic and dependencies can be structured directly within Godot scenes.

### Install

The Godot integration is split into one addon per core module. Copy the module addons you use into the `addons/` directory of your Godot project, then enable them in `ProjectSettings/Plugins` and trigger a rebuild. Each module addon injects its own assembly references and generator analyzer; enable the module addons for the dependencies you use as well.

## License

Gubbins is released under the [BSD 3-Clause License](LICENSE). Copyright (c) 2026, Gatongone.
