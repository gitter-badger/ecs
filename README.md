[![discord](https://img.shields.io/discord/404358247621853185.svg?label=discord)](https://discord.gg/5GZVde6)
[![license](https://img.shields.io/github/license/Leopotam/ecs.svg)](https://github.com/Leopotam/ecs/blob/develop/LICENSE)
# LeoECS - Simple lightweight C# Entity Component System framework
Performance, zero/small memory allocations/footprint, no dependencies on any game engine - main goals of this project.

> **Important! It's "structs-based" version, if you search "classes-based" version - check [classes-based branch](https://github.com/Leopotam/ecs/tree/classes-based)!**

> C#7.3 or above required for this framework.

> Tested on unity 2019.1 (not dependent on it) and contains assembly definition for compiling to separate assembly file for performance reason.

> **Important!** Don't forget to use `DEBUG` builds for development and `RELEASE` builds in production: all internal error checks / exception throwing works only in `DEBUG` builds and eleminated for performance reasons in `RELEASE`.

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
Container for user data without / with small logic inside:
```csharp
struct WeaponComponent {
    public int Ammo;
    public string GunName;
}
```

> **Important!** Don't forget to manually init all fields for each new component - they will be reset to default values on recycling to pool.

## Entity
Сontainer for components. Implemented as `EcsEntity` for wrapping internal identifiers:
```csharp
// Creates new entity in world context.
EcsEntity entity = _world.NewEntity ();
// Get() returns component on entity. If component not exists - it will be added.
ref Component1 c1 = ref entity.Get<Component1> ();
ref Component2 c2 = ref entity.Get<Component2> ();
// Del() removes component from entity.
entity.Del<Component2> ();
// Component can be replaced with new instance of component. If component not exist - it will be added.
var weapon = new WeaponComponent () { Ammo = 10, GunName = "Handgun" };
entity.Replace (weapon);
// With Replace() you can chain component's creation:
var entity2 = world.NewEntity ();
entity2.Replace (new Component1 { Id = 10 }).Replace (new Component2 { Name = "Username" });
// Any entity can be copied with all components:
var entity2Copy = entity2.Copy ();
// Any entity can be merged / "moved" to another entity (source will be destroyed):
var newEntity = world.NewEntity ();
entity2Copy.MoveTo (newEntity); // all components from entity2Copy moved to newEntity, entity2Copy destroyed.
// any entity can be destroyed. 
entity.Destroy ();
```

> **Important!** Entities without components on them will be automatically removed on last `EcsEntity.Del()` call.

## System
Сontainer for logic for processing filtered entities. User class should implements `IEcsInitSystem`, `IEcsDestroySystem`, `IEcsRunSystem` (or other supported) interfaces:
```csharp
class WeaponSystem : IEcsPreInitSystem, IEcsInitSystem, IEcsDestroySystem, IEcsPostDestroySystem {
    public void PreInit () {
        // Will be called once during EcsSystems.Init() call and before IEcsInitSystem.Init.
    }

    public void Init () {
        // Will be called once during EcsSystems.Init() call.
    }

    public void Destroy () {
        // Will be called once during EcsSystems.Destroy() call.
    }

    public void PostDestroy () {
        // Will be called once during EcsSystems.Destroy() call and after IEcsDestroySystem.Destroy.
    }
}
```

```csharp
class HealthSystem : IEcsRunSystem {
    public void Run () {
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
struct Component1 { }

class System1 : IEcsInitSystem {
    EcsWorld _world = null;

    public void Init () {
        _world.NewEntity ().Get<Component1> ();
    } 
}

class System2 : IEcsInitSystem {
    EcsFilter<Component1> _filter = null;

    public void Init () {
        Debug.Log (_filter.GetEntitiesCount ());
    }
}

var systems1 = new EcsSystems (world);
var systems2 = new EcsSystems (world);
systems1.Add (new System1 ());
systems2.Add (new System2 ());
systems1.Init ();
systems2.Init ();
```
You will get "0" at console. Problem is that DI starts at `Init()` method inside each `EcsSystems`. It means that any new `EcsFilter` instance (with lazy initialization) will be correctly injected only at current `EcsSystems`. 

To fix this behaviour startup code should be modified in this way:

```csharp
var systems1 = new EcsSystems (world);
var systems2 = new EcsSystems (world);
systems1.Add (new System1 ());
systems2.Add (new System2 ());
systems1.ProcessInjects ();
systems2.ProcessInjects ();
systems1.Init ();
systems2.Init ();
```
You should get "1" at console after fix.

# Special classes

## EcsFilter<T>
Container for keeping filtered entities with specified component list:
```csharp
class WeaponSystem : IEcsInitSystem, IEcsRunSystem {
    // auto-injected fields: EcsWorld instance and EcsFilter.
    EcsWorld _world = null;
    // We wants to get entities with "WeaponComponent" and without "HealthComponent".
    EcsFilter<WeaponComponent>.Exclude<HealthComponent> _filter = null;

    public void Init () {
        _world.NewEntity ().Get<WeaponComponent> ();
    }

    public void Run () {
        foreach (var i in _filter) {
            // entity that contains WeaponComponent.
            ref var entity = ref _filter.GetEntity (i);

            // Get1 will return link to attached "WeaponComponent".
            ref var weapon = ref _filter.Get1 (i);
            weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
        }
    }
}
```
> **Important!** You should not use `ref` modifier for any filter data outside of foreach-loop over this filter if you want to destroy part of this data (entity or component) - it will break memory integrity.

All components from filter `Include` constraint can be fast accessed through `EcsFilter.Get1()`, `EcsFilter.Get2()`, etc - in same order as they were used in filter type declaration.

If fast access not required (for example, for flag-based components without data), component can implements `IEcsIgnoreInFilter` interface for decrease memory usage and increase performance:
```csharp
struct Component1 { }

struct Component2 : IEcsIgnoreInFilter { }

class TestSystem : IEcsRunSystem {
    EcsFilter<Component1, Component2> _filter = null;

    public void Run () {
        foreach (var i in _filter) {
            // its valid code.
            ref var component1 = ref _filter.Get1 (i);

            // its invalid code due to cache for _filter.Get2() is null for memory / performance reasons.
            ref var component2 = ref _filter.Get2 (i);
        }
    }
}
```

> Important: Any filter supports up to 6 component types as "include" constraints and up to 2 component types as "exclude" constraints. Shorter constraints - better performance.

> Important: If you will try to use 2 filters with same components but in different order - you will get exception with detailed info about conflicted types, but only in `DEBUG` mode. In `RELEASE` mode all checks will be skipped.

## EcsWorld
Root level container for all entities / components, works like isolated environment.

> Important: Do not forget to call `EcsWorld.Destroy()` method when instance will not be used anymore.

## EcsSystems
Group of systems to process `EcsWorld` instance:
```csharp
class Startup : MonoBehaviour {
    EcsWorld _world;
    EcsSystems _systems;

    void Start () {
        // create ecs environment.
        _world = new EcsWorld ();
        _systems = new EcsSystems (_world)
            .Add (new WeaponSystem ());
        _systems.Init ();
    }
    
    void Update () {
        // process all dependent systems.
        _systems.Run ();
    }

    void OnDestroy () {
        // destroy systems logical group.
        _systems.Destroy ();
        // destroy world.
        _world.Destroy ();
    }
}
```

`EcsSystems` instance can be used as nested system (any types of `IEcsInitSystem`, `IEcsRunSystem`, ecs behaviours are supported):
```csharp
// initialization.
var nestedSystems = new EcsSystems (_world).Add (new NestedSystem ());
// don't call nestedSystems.Init() here, rootSystems will do it automatically.

var rootSystems = new EcsSystems (_world).Add (nestedSystems);
rootSystems.Init ();

// update loop.
// don't call nestedSystems.Run() here, rootSystems will do it automatically.
rootSystems.Run ();

// destroying.
// don't call nestedSystems.Destroy() here, rootSystems will do it automatically.
rootSystems.Destroy ();
```

Any `IEcsRunSystem` or `EcsSystems` instance can be enabled or disabled from processing in runtime:
```csharp
class TestSystem : IEcsRunSystem {
    public void Run () { }
}
var systems = new EcsSystems (_world);
systems.Add (new TestSystem (), "my special system");
systems.Init ();
var idx = systems.GetNamedRunSystem ("my special system");

// state will be true here, all systems are active by default.
var state = systems.GetRunSystemState (idx);

// disable system from execution.
systems.SetRunSystemState (idx, false);
```

# Engine integration

## Unity
In [Unity editor integration](https://github.com/Leopotam/ecs-unityintegration) there are templates for startup and systems at project assets context menu.

## Custom engine
Code example - each part should be integrated in proper place of engine execution flow.
```csharp
using Leopotam.Ecs;

class EcsStartup {
    EcsWorld _world;
    EcsSystems _systems;

    // Initialization of ecs world and systems.
    void Init () {        
        _world = new EcsWorld ();
        _systems = new EcsSystems (_world);
        _systems
            // register your systems here, for example:
            // .Add (new TestSystem1 ())
            // .Add (new TestSystem2 ())
            
            // register one-frame components (order is important), for example:
            // .OneFrame<TestComponent1> ()
            // .OneFrame<TestComponent2> ()
            
            // inject service instances here (order doesn't important), for example:
            // .Inject (new CameraService ())
            // .Inject (new NavMeshSupport ())
            .Init ();
    }

    // Engine update loop.
    void UpdateLoop () {
        _systems?.Run ();
    }

    // Cleanup.
    void Destroy () {
        if (_systems != null) {
            _systems.Destroy ();
            _systems = null;
            _world.Destroy ();
            _world = null;
        }
    }
}
```

# Projects powered by LeoECS.
## With sources:
* [SpaceInvaders (Guns&Bullets variation) game](https://github.com/GoodCatGames/SpaceInvadersEcs)
* [Runner game](https://github.com/t1az2z/RunnerECS)
* [Pacman game](https://github.com/SH42913/pacmanecs)
* [TicTacToe game (obsoleted api)](https://github.com/GreatVV/TicToe). "Making of" [video (in Russian)](https://www.youtube.com/watch?v=J3HG8i-DrL8)
* [GTA5 custom wounds mod (powered by classes-based version)](https://github.com/SH42913/gunshotwound3)

## Released games:
* ["Idle Delivery City Tycoon"](https://play.google.com/store/apps/details?id=com.Arctic.IdleTransportTycoon)
* ["Saboteur"](https://play.google.com/store/apps/details?id=com.zlodeystudios.saboteur)
* ["Nasty Bird"](https://play.google.com/store/apps/details?id=magic.bird.fly)
* ["I Am Arrow"](https://play.google.com/store/apps/details?id=com.BigMoodGame.ArrowingAround)
* ["HypnoTap"](https://play.google.com/store/apps/details?id=com.ZlodeyStudios.HypnoTap)
* ["TowerRunner Revenge"](https://play.google.com/store/apps/details?id=ru.zlodey.towerrunner20)
* ["Natives"](https://alex-kpojb.itch.io/natives-ecs)

# Extensions
* [Unity editor integration](https://github.com/Leopotam/ecs-unityintegration)
* [Unity uGui events support](https://github.com/Leopotam/ecs-ui)
* [Multi-threading support](https://github.com/Leopotam/ecs-threads)
* [Service locator](https://github.com/Leopotam/globals)
* [Engine independent types](https://github.com/Leopotam/ecs-types)

# License
The software released under the terms of the [MIT license](./LICENSE.md). Enjoy.

# Donate
Its free open source software, but you can buy me a coffee:

<a href="https://www.buymeacoffee.com/leopotam" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/yellow_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

# FAQ

### Structs-based, classes-based versions? Which better and why?

Classes-based version is stable, but not longer under active development - except bug fixes (can be found at ``classes-based` branch).

Structs-based only one version that under active development. It should be faster than classed-based version, simpler in component-cleanup and you can switch to `unity-ecs` easier later (if you want). This framework will be under development even after `unity-ecs` release.

### I want to know - is component already added to entity and get it / add new one otherwise, how I can do it?

If you don't care about fact is component already added and you just want to be sure that entity contains it - just call `EcsEntity.Get<T>` - it will return already exist component or add brand new one if not.

If you want to know that component exist or not (to use it in custom logic later) - use `EcsEntity.Has<T>` method that will return fact that component was added before.  

### I want to process one system at MonoBehaviour.Update() and another - at MonoBehaviour.FixedUpdate(). How I can do it?

For splitting systems by `MonoBehaviour`-method multiple `EcsSystems` logical groups should be used:
```csharp
EcsSystems _update;
EcsSystems _fixedUpdate;

void Start () {
    var world = new EcsWorld ();
    _update = new EcsSystems (world).Add (new UpdateSystem ());
    _update.Init ();
    _fixedUpdate = new EcsSystems (world).Add (new FixedUpdateSystem ());
    _fixedUpdate.Init ();
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

If you want to simplify your code and keep reset/init code at one place, you can setup custom handler to process cleanup / initialization for component:
```csharp
struct MyComponent : IEcsAutoReset<MyComponent> {
    public int Id;
    public object LinkToAnotherComponent;

    public void AutoReset (ref MyComponent c) {
        c.Id = 2;
        c.LinkToAnotherComponent = null;
    }
}
```
This method will be automatically called for brand new component instance and after component removing from entity and before recycling to component pool.
> Important: With custom `AutoReset` behaviour there are no any additional checks for reference-type fields, you should provide correct cleanup/init behaviour without possible memory leaks.

### I use components as events that works only one frame, then remove it at last system in execution sequence. It's boring, how I can automate it?

If you want to remove one-frame components without additional custom code, you can register them at `EcsSystems`:
```csharp
struct MyOneFrameComponent { }

EcsSystems _update;

void Start () {
    var world = new EcsWorld ();
    _update = new EcsSystems (world);
    _update
        .Add (new CalculateSystem ())
        .Add (new UpdateSystem ())
        .OneFrame<MyOneFrameComponent> ()
        .Init ();
}

void Update () {
    _update.Run ();
}
```

> Important: All one-frame components with specified type will be removed at position in execution flow where this component was registered with OneFrame() call.

### I need more control on default cache size of internal structures, how I can do it?

You can set custom cache sizes with `EcsWorldConfig` instance:
```csharp
var config = new EcsWorldConfig() {
    // World.Entities default cache size.
    WorldEntitiesCacheSize = 1024,
    // World.Filters default cache size.
    WorldFiltersCacheSize = 128,
    // World.ComponentPools default cache size.
    WorldComponentPoolsCacheSize = 512,
    // Entity.Components default cache size (not doubled).
    EntityComponentsCacheSize = 8,
    // Filter.Entities default cache size.
    FilterEntitiesCacheSize = 256,
};
var world = new EcsWorld(config);
...
```

### I need more than 6 "included" or more than 2 "excluded" components in filter, how i can do it?

You can use [EcsFilter autogen-tool](https://leopotam.github.io/ecs/filter-gen.html) and replace `EcsFilter.cs` file with brand new generated content.

### I want to add some reactive behaviour on filter items change, how I can do it?

You can use `LEOECS_FILTER_EVENTS` definition to enable custom event listeners support on filters:
```charp
class CustomListener: IEcsFilterListener {
    public void OnEntityAdded (in EcsEntity entity) {
        // reaction on compatible entity was added to filter.
    }
    
    public void OnEntityRemoved (in EcsEntity entity) {
        // reaction on noncompatible entity was removed from filter.
    }
}

class MySystem : IEcsInitSystem, IEcsDestroySystem {
    readonly EcsFilter<Component1> _filter = null;
    readonly CustomListener _listener = new CustomListener ();
    public void Init () {
        // subscribe listener to filter events.
        _filter.AddListener (_listener);
    }
    public void Destroy () {
        // unsubscribe listener to filter events.
        _filter.RemoveListener (_listener);
    }
}
``` 