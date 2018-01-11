[![gitter](https://img.shields.io/gitter/room/leopotam/ecs.svg)](https://gitter.im/leopotam/ecs)
[![license](https://img.shields.io/github/license/Leopotam/ecs.svg)](https://github.com/Leopotam/ecs/blob/develop/LICENSE)
# Another one Entity Component System framework
Performance and zero memory allocation / no gc work / small size - main goals of this project.

> **Its early work-in-progress stage, not recommended to use in real projects, any api / behaviour can change later.**

> Tested / developed on unity 2017.3 and contains assembly definition for compiling to separate assembly file for performance reason.

> Components limit - 256 **different** components at each world (256 C# classes). Components limit on each entity: up to 256, but better to keep it less or equal 8 for performance reason.

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
Сontainer for logic for processing filtered entities. User class should implements **IEcsPreInitSystem**, **IEcsInitSystem** or **IEcsRunSystem** interfaces:
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
    EcsRunSystemType IEcsRunSystem.GetRunSystemType () {
        // Should returns type of run system,
        // when Run method will be called - Update() or FixedUpdate.
        return EcsRunSystemType.Update;
    }

    void IEcsRunSystem.Run () {
        // Will be called each FixedUpdate().
    }
}
```

# Data injection
With **[EcsWorld]**, **[EcsFilterInclude(typeof(X))]**, **[EcsFilterExclude(typeof(X))]** and **[EcsIndex(typeof(X))]** attributes any compatible field of custom IEcsSystem-class can be auto initialized (auto injected):
```
class HealthSystem : IEcsSystem {
    [EcsWorld]
    EcsWorld _world;

    [EcsFilterInclude(typeof(WeaponComponent))]
    EcsFilter _weaponFilter;

    [EcsIndex(typeof(WeaponComponent))]
    int _weaponId;
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
    EcsFilter _filter;

    void IEcsInitSystem.Initialize () {
        var newEntity = _world.CreateEntity ();
        _world.AddComponent<WeaponComponent> (newEntity);
    }

    void IEcsInitSystem.Destroy () { }

    EcsRunSystemType IEcsRunSystem.GetRunSystemType () {
        return EcsRunSystemType.Update;
    }

    void IEcsRunSystem.Run () {
        foreach (var entity in _filter.Entities) {
            var weapon = _world.GetComponent<WeaponComponent> (entity);
            weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
        }
    }
}
```

## EcsWorld
Root level container for all systems / entities / components, works like isolated environment:
```
class Startup : MonoBehaviour {
    EcsWorld _world;

    void OnEnable() {
        // create ecs environment.
        _world = new EcsWorld ()
            .AddSystem(new WeaponSystem ());
        _world.Initialize();
    }
    
    void Update() {
        // process all dependent systems.
        _world.RunUpdate ();
    }

    void FixedUpdate() {
        // process all dependent systems.
        _world.RunFixedUpdate ();
    }

    void OnDisable() {
        // destroy ecs environment.
        _world.Destroy ();
        _world = null;
    }
}
```

# Reaction on component / filter changes
Events **OnEntityComponentAdded** / **OnEntityComponentRemoved** at ecs-world instance and **OnEntityAdded** / **OnEntityRemoved** / **OnEntityUpdated** at ecs-filter instance allows to add reaction on component / filter changes to any ecs-system:
```
public sealed class TestSystem1 : IEcsInitSystem {
    [EcsWorld]
    EcsWorld _world;

    [EcsFilterInclude (typeof (WeaponComponent))]
    EcsFilter _weaponFilter;

    void IEcsInitSystem.Initialize () {
        _world.OnEntityComponentAdded += OnEntityComponentAdded;
        _weaponFilter.OnEntityAdded += OnFilterEntityAdded;
        _weaponFilter.OnEntityUpdated += OnFilterEntityUpdated;

        var entity = _world.CreateEntity ();
        _world.AddComponent<WeaponComponent> (entity);
        _world.AddComponent<HealthComponent> (entity);
    }

    void IEcsInitSystem.Destroy () {
        _world.OnEntityComponentAdded -= OnEntityComponentAdded;
        _weaponFilter.OnEntityAdded -= OnFilterEntityAdded;
        _weaponFilter.OnEntityUpdated -= OnFilterEntityUpdated;
    }

    void OnEntityComponentAdded(int entityId, int componentId) {
        // Component "componentId" was added to entity "entityId" - world event.
    }

    void OnFilterEntityAdded (int entity, int componentId) {
        // Entity "entityId" was added to _weaponFilter due to component "componentId" was added.
    }

    void OnFilterEntityUpdated(int entityId, int componentId) {
        // Component "componentId" was updated inplace on entity "entityId".
    }
}
```

There is special predefined helper-class - EcsReactSystem:
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

    // EcsReactSystem is IEcsRunSystem and should provides additional info.
    public override EcsRunSystemType GetRunSystemType () {
        return EcsRunSystemType.Update;
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

# Sharing data between systems
If ecs-world class should contains some shared fields, it can be implemented in this way:
```
class MySharedData : ScriptableObject {
    public string PlayerName = "Unknown";
    public GameObject PlayerModel;
}

class MyWorld: EcsWorldBase<MyWorld> {
    public readonly MySharedData Assets;

    public MyWorld(MySharedData data) {
        Assets = data;
    }
}

class ChangePlayerName : IEcsInitSystem {
    [EcsWorld]
    MyWorld _world;

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

    MyWorld _world;

    void OnEnable() {
        _world = new MyWorld(_sharedData)
            .AddSystem(ChangePlayerName())
            .AddSystem(SpawnPlayerModel());
        _world.Initialize();
    }
}
```

# Examples
[Snake game](https://github.com/Leopotam/ecs-snake)

# License
The software released under the terms of the MIT license. Enjoy.