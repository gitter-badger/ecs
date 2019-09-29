[![discord](https://img.shields.io/discord/404358247621853185.svg?label=discord)](https://discord.gg/5GZVde6)
[![license](https://img.shields.io/github/license/Leopotam/ecs.svg)](https://github.com/Leopotam/ecs/blob/develop/LICENSE)
# LeoECS - Simple lightweight C# Entity Component System framework
Performance, zero/small memory allocations/footprint, no dependencies on any game engine - main goals of this project.

> C#7.3 or above required for this framework.

> Tested on unity 2019.1 (not dependent on it) and contains assembly definition for compiling to separate assembly file for performance reason.

> **Important!** Dont forget to use `DEBUG` builds for development and `RELEASE` builds in production: all internal error checks / exception throwing works only in `DEBUG` builds and eleminated for performance reasons in `RELEASE`.


# Installation

## As unity module
This repository can be installed as unity module directly from git url. In this way new line should be added to `Packages/manifest.json`:
```
"com.leopotam.ecs": "https://github.com/Leopotam/ecs.git",
```
By default last released version will be used. If you need trunk / developing version then `develop` name of branch should be added after hash:
```
"com.leopotam.ecs": "https://github.com/Leopotam/ecs.git#develop",
```

## As source
If you can't / don't want to use unity modules, code can be downloaded as sources archive of required release from [Releases page](`https://github.com/Leopotam/ecs/releases`).

# Main parts of ecs

## Component
Container for user data without / with small logic inside. Can be used any user class without any additional inheritance:
```csharp
class WeaponComponent {
    public int Ammo;
    public string GunName;
}
```

> **Important!** Dont forget to manually init all fields of new added component. Default value initializers will not work due all components can be reused automatically multiple times through builtin pooling mechanism (no destroying / creating new instance for each request for performance reason).

> **Important!** Dont forget to cleanup reference links to instances of any classes before removing components from entity, otherwise it can lead to memory leaks.
>
> By default all `marshal-by-reference` typed fields of component (classes in common case) will be checked for null on removing attempt in `DEBUG`-mode. If you know that you have object instance that should be not null (preinited collections for example) - `[EcsIgnoreNullCheck]` attribute can be used for disabling these checks.

## Entity
Сontainer for components. Implemented as `EcsEntity` for wrapping internal identifiers:
```csharp
EcsEntity entity = _world.NewEntity();
Component1 c1 = entity.Set<Component1> ();
Component2 c2 = entity.Set<Component2> ();
```
There are some helpers to simplify creation of entity with multiple components:
```csharp
EcsEntity entity = _world.NewEntityWith<Component1, Component2>(out Component1 c1, out Component2 c2);
```

> **Important!** Entities without components on them will be automatically removed on last `EcsEntity.Unset()` call.

## System
Сontainer for logic for processing filtered entities. User class should implements `IEcsPreInitSystem`, `IEcsInitSystem` or / and `IEcsRunSystem` interfaces:
```csharp
class WeaponSystem : IEcsPreInitSystem, IEcsInitSystem, IEcsDestroySystem, IEcsPreDestroySystem {
    void IEcsPreInitSystem.PreInit () {
        // Will be called once during world initialization and before IEcsInitSystem.Init.
    }

    void IEcsInitSystem.Init () {
        // Will be called once during world initialization.
    }

    void IEcsDestroySystem.Destroy () {
        // Will be called once during world destruction.
    }

    void IEcsPreDestroySystem.PreDestroy () {
        // Will be called once during world destruction and after IEcsDestroySystem.Destroy.
    }
}
```

```csharp
class HealthSystem : IEcsRunSystem {
    void IEcsRunSystem.Run () {
        // Will be called on each EcsSystems.Run() call.
    }
}
```

# Data injection
All compatible `EcsWorld` and `EcsFilter<T>` fields of ecs-system will be auto-initialized (auto-injected):
```csharp
class HealthSystem : IEcsSystem {
    // auto-injected fields.
    EcsWorld _world = null;
    EcsFilter<WeaponComponent> _weaponFilter = null;
}
```
Instance of any custom type can be injected to all systems through `EcsSystems.Inject()` method:
```csharp
var systems = new EcsSystems (world)
    .Add (new TestSystem1 ())
    .Add (new TestSystem2 ())
    .Add (new TestSystem3 ())
    .Inject (a)
    .Inject (b)
    .Inject (c)
    .Inject (d);
systems.Init ();
```
Each system will be scanned for compatible fields (can contains all of them or no one) with proper initialization.

> **Important!** Data injection for any user type can be used for sharing external data between systems.

## Data Injection with multiple EcsSystems

If you want to use multiple `EcsSystems` you can find strange behaviour with DI:

```csharp
class Component1 { }

class System1 : IEcsInitSystem {
    EcsWorld _world = null;

    void IEcsInitSystem.Init () {
        _world.NewEntity().Set<Component1> ();
    } 
}

class System2 : IEcsInitSystem {
    EcsFilter<Component1> _filter = null;

    void IEcsInitSystem.Init () {
        Debug.Log (_filter.GetEntitiesCount());
    }
}

var systems1 = new EcsSystems (world);
var systems2 = new EcsSystems (world);
systems1.Add (new System1 ());
systems2.Add (new System2 ());
systems1.Init ();
systems2.Init ();
```
You will get "0" at console. Problem is that DI starts at `Init` method inside each `EcsSystems`. It means that any new `EcsFilter` instance (with lazy initialization) will be correctly injected only at current `EcsSystems`. 

To fix this behaviour startup code should be modified in this way:

```csharp
var systems1 = new EcsSystems (world);
var systems2 = new EcsSystems (world);
systems1.Add (new System1 ());
systems2.Add (new System2 ());
systems1.ProcessInjects();
systems2.ProcessInjects();
systems1.Init ();
systems2.Init ();
```
You should get "1" at console after fix.

# Special classes

## EcsFilter<T>
Container for keeping filtered entities with specified component list:
```csharp
class WeaponSystem : IEcsInitSystem, IEcsRunSystem {
    EcsWorld _world = null;

    // We wants to get entities with "WeaponComponent" and without "HealthComponent".
    EcsFilter<WeaponComponent>.Exclude<HealthComponent> _filter = null;

    void IEcsInitSystem.Init () {
        // new C# syntax can be used if component instance not required right now.
        _world.NewEntityWith<WeaponComponent> (out _);
    }

    void IEcsRunSystem.Run () {
        foreach (var i in _filter) {
            // entity that contains WeaponComponent.
            // Performance hint: use 'ref' prefixes for disable copying entity structure.
            ref var entity = ref _filter.Entities[i];

            // Get1 will return link to attached "WeaponComponent".
            var weapon = _filter.Get1[i];
            weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
        }
    }
}
```

All components from filter `Include` constraint can be fast accessed through `filter.Get1()`, `filter.Get2()`, etc - in same order as they were used in filter type declaration.

If fast access not required (for example, for flag-based components without data), component can implements `IEcsIgnoreInFilter` interface for decrease memory usage and increase performance:
```csharp
class Component1 { }

class Component2 : IEcsIgnoreInFilter { }

class TestSystem : IEcsSystem {
    EcsFilter<Component1, Component2> _filter = null;

    public Test() {
        foreach (var i in _filter) {
            // its valid code.
            var component1 = _filter.Get1[i];

            // its invalid code due to cache for _filter.Get2() calls is null for memory / performance reasons.
            var component2 = _filter.Get2[i];
        }
    }
}
```

> Important: Any filter supports up to 4 component types as "include" constraints and up to 2 component types as "exclude" constraints. Shorter constraints - better performance.

> Important: If you will try to use 2 filters with same components but in different order - you will get exception with detailed info about conflicted types, but only in `DEBUG` mode. In `RELEASE` mode all checks will be skipped.

## EcsWorld
Root level container for all entities / components, works like isolated environment.

> Important: Do not forget to call `EcsWorld.Destroy()` method when instance will not be used anymore.

## EcsSystems
Group of systems to process `EcsWorld` instance:
```csharp
class Startup : MonoBehaviour {
    EcsSystems _systems;

    void OnEnable() {
        // create ecs environment.
        var world = new EcsWorld ();
        _systems = new EcsSystems(world)
            .Add (new WeaponSystem ());
        _systems.Init ();
    }
    
    void Update() {
        // process all dependent systems.
        _systems.Run ();
        _world.EndFrame ();
    }

    void OnDisable() {
        // destroy systems logical group.
        _systems.Destroy ();
        // destroy world.
        _world.Destroy ();
    }
}
```
> Important: Do not forget to call `EcsWorld.EndFrame()` method when all `EcsSystems` completed.

`EcsSystems` instance can be used as nested system (any types of `IEcsInitSystem`, `IEcsRunSystem`, ecs behaviours are supported):
```csharp
// initialization.
var nestedSystems = new EcsSystems (_world)
    .Add (new NestedSystem ());
// dont call nestedSystems.Init() here, rootSystems will do it automatically.

var rootSystems = new EcsSystems (_world)
    .Add (nestedSystems);
rootSystems.Init();

// update loop.
// dont call nestedSystems.Run() here, rootSystems will do it automatically.
rootSystems.Run();

// destroying.
// dont call nestedSystems.Destroy() here, rootSystems will do it automatically.
rootSystems.Destroy();
```

Any `IEcsRunSystem` or `EcsSystems` instance can be enabled or disabled from processing in runtime:
```csharp
class TestSystem : IEcsRunSystem {
    public void Run () { }
}
var systems
    .Add (new TestSystem (), "my special system")
    .Init ();
var idx = systems.GetNamedRunSystem ("my special system");

// state will be true here, all systems are active by default.
var state = systems.GetRunSystemState (idx);

// disable system from execution.
systems.SetRunSystemState (idx, false);
```

# Examples
Not ready yet.

# Extensions

[Engine independent types](https://github.com/Leopotam/ecs-types)

# License
The software released under the terms of the [MIT license](./LICENSE). Enjoy.

# Donate
Its free opensource software, but you can buy me a coffee:

<a href="https://www.buymeacoffee.com/leopotam" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/yellow_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

# FAQ

### My project complex enough, I need more than 256 components. How I can do it?

There are no components limit, but for performance / memory usage reason better to keep amount of components on each entity less or equals 8.

### I want to process one system at MonoBehaviour.Update() and another - at MonoBehaviour.FixedUpdate(). How I can do it?

For splitting systems by `MonoBehaviour`-method multiple `EcsSystems` logical groups should be used:
```csharp
EcsSystems _update;
EcsSystems _fixedUpdate;

void OnEnable () {
    var world = new EcsWorld ();
    _update = new EcsSystems (world).Add (new UpdateSystem ());
    _fixedUpdate = new EcsSystems (world).Add (new FixedUpdateSystem ());
}

void Update () {
    _update.Run ();
}

void FixedUpdate () {
    _fixedUpdate.Run ();
}
```

### I like how dependency injection works, but i want to skip some fields from initialization. How I can do it?

You can use `[EcsIgnoreInject]` attribute on any field of system:
```csharp
...
// will be injected.
EcsFilter<C1> _filter1 = null;

// will be skipped.
[EcsIgnoreInject]
EcsFilter<C2> _filter2 = null;
```

### I do not like foreach-loops, I know that for-loops are faster. How I can use it?

Current implementation of foreach-loop fast enough (custom enumerator, no memory allocation), small performance differences can be found on 10k items and more. Current version doesnt support for-loop iterations anymore.

### I copy&paste my reset components code again and again. How I can do it in other manner?

If you want to simplify your code and keep reset-code in one place, you can use `IEcsAutoResetComponent` interface for components:
```csharp
class MyComponent : IEcsAutoReset {
    public object LinkToAnotherComponent;

    void IEcsAutoReset.Reset() {
        // Cleanup all marshal-by-reference fields here.
        LinkToAnotherComponent = null;
    }
}
```
This method will be automatically called after component removing from entity and before recycling to component pool.


### I use components as events that works only one frame, then remove it at last system in execution sequence. It's boring, how I can automate it?

If you want to remove one-frame components without additional custom code, you can implement `IEcsOneFrame` interface:
```csharp
class MyComponent : IEcsOneFrame { }
```
> Important: Do not forget to call `EcsWorld.RemoveOneFrameComponents` method once after all `EcsSystems.Run` calls.

> Important: Do not forget that if one-frame component contains `marshal-by-reference` typed fields - this component should implements `IEcsAutoReset` interface.

### I need more than 4 components in filter, how i can do it?

Check `EcsFilter<Inc1, Inc2, Inc3, Inc4>` type source, copy&paste it to your project and add additional components support in same manner.