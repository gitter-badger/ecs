[![gitter](https://img.shields.io/gitter/room/leopotam/ecs.svg)](https://gitter.im/leopotam/ecs)
[![license](https://img.shields.io/github/license/Leopotam/ecs.svg)](https://github.com/Leopotam/ecs/blob/develop/LICENSE)
# Another one Entity Component System framework
Performance and zero memory allocation / no gc work / small size - main goals of this project.

> **This software in work-in-progress stage, api mostly stable.**

> Tested / developed on unity 2017.3 and contains assembly definition for compiling to separate assembly file for performance reason.

> Components limit - 256 **different** components at each world (256 C# classes), can be changed with preprocessor defines: `ECS_COMPONENT_LIMIT_512`, `ECS_COMPONENT_LIMIT_1024` or `ECS_COMPONENT_LIMIT_2048`.

> Components limit on each entity: up to component limit at ecs-world, but better to keep it less or equal 6 for performance reason.

# Main parts of ecs

## Component
Container for user data without / with small logic inside. Can be used any user class without any additional inheritance:
```
class WeaponComponent {
    public int Ammo;
    public string GunName;
}
```

## Entity
Сontainer for components. Implemented with int id-s for more simplified api:
```
int entity = _world.CreateEntity ();
_world.RemoveEntity (entity);
```

## System
Сontainer for logic for processing filtered entities. User class should implements `IEcsPreInitSystem`, `IEcsInitSystem` or `IEcsRunSystem` interfaces:
```
class WeaponSystem : IEcsPreInitSystem, IEcsInitSystem {
    void IEcsPreInitSystem.PreInitialize () {
        // Will be called once during world initialization before IEcsInitSystem.Initialize().
    }

    void IEcsInitSystem.Initialize () {
        // Will be called once during world initialization.
    }

    void IEcsPreInitSystem.PreDestroy () {
        // Will be called once during world destruction before IEcsInitSystem.Destroy().
    }

    void IEcsInitSystem.Destroy () {
        // Will be called once during world destruction.
    }
}
```

```
class HealthSystem : IEcsRunSystem {
    void IEcsRunSystem.Run () {
        // Will be called on each EcsSystems.Run() call.
    }
}
```

# Data injection
With `[EcsWorld]`, `[EcsFilterInclude(typeof(X))]` and `[EcsFilterExclude(typeof(X))]` attributes any compatible field of custom `IEcsSystem` class can be auto initialized (auto injected):
```
class HealthSystem : IEcsSystem {
    [EcsWorld]
    EcsWorld _world;

    [EcsFilterInclude(typeof(WeaponComponent))]
    EcsFilter _weaponFilter;
}
```

# Special classes

## EcsFilter
Container for keep filtered entities with specified component list:
```
class WeaponSystem : IEcsInitSystem, IEcsRunSystem {
    [EcsWorld]
    EcsWorld _world;

    // We wants to get entities with WeaponComponent and without HealthComponent.
    [EcsFilterInclude(typeof(WeaponComponent))]
    [EcsFilterExclude(typeof(HealthComponent))]
    // If this filter not exists (will be created) - force scan world for compatible entities.
    [EcsFilterFill]
    EcsFilter _filter;

    void IEcsInitSystem.Initialize () {
        var newEntity = _world.CreateEntity ();
        _world.AddComponent<WeaponComponent> (newEntity);
    }

    void IEcsInitSystem.Destroy () { }

    void IEcsRunSystem.Run () {
        foreach (var entity in _filter.Entities) {
            var weapon = _world.GetComponent<WeaponComponent> (entity);
            weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
        }
    }
}
```

## EcsWorld
Root level container for all entities / components, works like isolated environment.

## EcsSystems
Group of systems to process `EcsWorld` instance:
```
class Startup : MonoBehaviour {
    EcsSystems _systems;

    void OnEnable() {
        // create ecs environment.
        var world = new EcsWorld ();
        _systems = new EcsSystems(world)
            .Add (new WeaponSystem ());
        _systems.Initialize ();
    }
    
    void Update() {
        // process all dependent systems.
        _systems.Run ();
    }

    void OnDisable() {
        // destroy systems logical group.
        _systems.Destroy ();
    }
}
```

# Reaction on component / filter changes
## Process events from EcsFilter as stream
`EcsReactSystem` class can be used for this case.

> Important: this system not supported processing of `OnRemove` event.

```
public sealed class TestReactSystem : EcsReactSystem {
    [EcsWorld]
    EcsWorld _world;

    [EcsFilterInclude (typeof (WeaponComponent))]
    EcsFilter _weaponFilter;

    // Should returns filter that react system will watch for.
    public override EcsFilter GetReactFilter () {
        return _weaponFilter;
    }

    // On which event at filter this react-system should be alerted -
    // "new entity in filter" or "entity inplace update".
    public override EcsReactSystemType GetReactSystemType () {
        return EcsReactSystemType.OnUpdate;
    }

    // Filtered entities processing, will be raised only if entities presents.
    public override void RunReact (List<int> entities) {
        foreach (var entity in entities) {
            var weapon = _world.GetComponent<WeaponComponent> (entity);
            Debug.LogFormat ("Weapon updated on {0}", entity);
        }
    }
}
```

## Process events from EcsFilter immediately
`EcsInstantReactSystem` class can be used for this case.

Useful case for using this type of processing - reaction from OnRemove event.

```
public sealed class TestReactInstantSystem : EcsReactInstantSystem {
    [EcsWorld]
    EcsWorld _world;

    [EcsFilterInclude (typeof (WeaponComponent))]
    EcsFilter _weaponFilter;

    // Should returns filter that react system will watch for.
    public override EcsFilter GetReactFilter () {
        return _weaponFilter;
    }

    // On which event at filter this react-system should be alerted -
    // "enity was removed from filter".
    public override EcsReactSystemType GetReactSystemType () {
        return EcsReactSystemType.OnRemove;
    }

    // Entity processing, will be raised only when entity will be removed from filter.
    public override void RunReact (int entity) {
        var weapon = _world.GetComponent<WeaponComponent> (entity);
        Debug.LogFormat ("Weapon removed from {0}", entity);
    }
}
```

## Custom reaction
For handling of filter events `custom class` should implements `IEcsFilterListener` interface with 3 methods: `OnFilterEntityAdded` / `OnFilterEntityRemoved` / `OnFilterEntityUpdated`. Then it can be added to any filter as compatible listener.

> **Not recommended if you dont understand how it works internally, this api / behaviour can be changed later.**

```
public sealed class TestSystem1 : IEcsInitSystem, IEcsFilterListener {
    [EcsWorld]
    EcsWorld _world;

    [EcsFilterInclude (typeof (WeaponComponent))]
    EcsFilter _weaponFilter;

    void IEcsInitSystem.Initialize () {
        _weaponFilter.AddListener(this);

        var entity = _world.CreateEntity ();
        _world.AddComponent<WeaponComponent> (entity);
        _world.AddComponent<HealthComponent> (entity);
    }

    void IEcsInitSystem.Destroy () {
        _weaponFilter.RemoveListener(this);
    }

    void IEcsFilterListener.OnFilterEntityAdded (int entity) {
        // Entity "entityId" was added to _weaponFilter due to component "WeaponComponent" was added to entity.
    }

    void IEcsFilterListener.OnFilterEntityUpdated(int entityId) {
        // Component "WeaponComponent" was updated inplace on entity "entityId".
    }

    void IEcsFilterListener.OnFilterEntityRemoved (int entity) {
        // Entity "entityId" was removed from _weaponFilter due to component "WeaponComponent" was removed from entity.
    }
}
```

# Sharing data between systems
If `EcsWorld` class should contains some shared fields (useful for sharing assets / prefabs), it can be implemented in this way:
```
class MySharedData : ScriptableObject {
    public string PlayerName = "Unknown";
    public GameObject PlayerModel;
}

class MyWorld: EcsWorld {
    public readonly MySharedData Assets;

    public MyWorld(MySharedData data) {
        Assets = data;
    }
}

class ChangePlayerName : IEcsInitSystem {
    [EcsWorld]
    MyWorld _world;

    // This field will be initialized with same reference as _world field.
    [EcsWorld]
    EcsWorld _standardWorld;

    void IEcsInitSystem.Initialize () {
        _world.Assets.PlayerName = "Jack";
    }

    void IEcsInitSystem.Destroy () { }
}

class SpawnPlayerModel : IEcsInitSystem {
    [EcsWorld]
    MyWorld _world;

    void IEcsInitSystem.Initialize () {
        GameObject.Instantiate(_world.Assets.PlayerModel);
    }

    void IEcsInitSystem.Destroy () { }
}

class Startup : Monobehaviour {
    [SerializedField]
    MySharedData _sharedData;

    EcsSystems _systems;

    void OnEnable() {
        var world = new MyWorld (_sharedData);
        _systems = new EcsSystems(world)
            .Add (ChangePlayerName())
            .Add (SpawnPlayerModel());
        _systems.Initialize();
    }
}
```

# Limitations
## I want to create alot of new entities with new components on start, how to speed up this process?

In this case custom component creator can be used (for speed up 2x or more):

```
class MyComponent { }

class Startup : Monobehaviour {
    EcsSystems _systems;

    void OnEnable() {
        var world = new MyWorld (_sharedData);
        world.RegisterComponentCreator<MyComponent> (() => new MyComponent());
        
        _systems = new EcsSystems(world)
            .Add (MySystem());
        _systems.Initialize();
    }
}
```
Reference to custom creator will be reset on `world.Destroy` call.

## I want to process one system at `MonoBehaviour.Update` and another - at `MonoBehaviour.FixedUpdate`. How I can do it?

For splitting systems by `MonoBehaviour`-method multiple `EcsSystems` logical groups should be used:
```
EcsSystems _update;
EcsSystems _fixedUpdate;

void OnEnable() {
    var world = new EcsWorld();
    _update = new EcsSystems(world).Add(new UpdateSystem());
    _fixedUpdate = new EcsSystems(world).Add(new FixedUpdateSystem());
}

void Update() {
    _update.Run();
}

void FixedUpdate() {
    _fixedUpdate.Run();
}
```

# Examples
[Snake game](https://github.com/Leopotam/ecs-snake)

# Extensions
[UnityEditor integration](https://github.com/Leopotam/ecs-unityintegration)

[uGui event bindings](https://github.com/Leopotam/ecs-ui)

# License
The software released under the terms of the MIT license. Enjoy.