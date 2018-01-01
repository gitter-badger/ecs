# Another one Entity Component System framework
Performance and zero memory allocation / no gc work - main goals of this project.

> Tested / developed on unity 2017.3 and contains assembly definition for compiling to separate assembly file for performance reason.

> **Its early work-in-progress stage, not recommended to use in real projects, any api / behaviour can change later.**

# Main parts of ecs

## Component
Container for user data without / with small logic inside. User class should implements **IEcsComponent** interface:
```
class WeaponComponent: IEcsComponent {
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
Сontainer for logic for processing filtered entities. User class should implements **IEcsSystem**, **IEcsInitSystem** or **IEcsRunSystem** interfaces:
```
class WeaponSystem : IEcsInitSystem {
    void IEcsInitSystem.Initialize () {
        // Will be called once during world initialization.
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
[EcsRunUpdate]
class WeaponSystem : IEcsInitSystem, IEcsRunSystem {
    [EcsWorld]
    EcsWorld _world;

    // We wants to get entities with WeaponComponent and without HealthComponent.
    [EcsFilterInclude(typeof(WeaponComponent))]
    [EcsFilterInclude(typeof(HealthComponent))]
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
Events **OnEntityComponentAdded** / **OnEntityComponentRemoved** at ecs-world instance and **OnFilterEntityAdded** / **OnFilterEntityRemoved** at ecs-filter instance allows to add reaction on component / filter changes for any ecs-system:
```
public sealed class TestSystem1 : IEcsInitSystem {
    [EcsWorld]
    EcsWorld _world;

    [EcsFilterInclude (typeof (WeaponComponent))]
    EcsFilter _weaponFilter;

    void IEcsInitSystem.Initialize () {
        _world.OnEntityComponentAdded += OnEntityComponentAdded;
        _weaponFilter.OnEntityAdded += OnFilterEntityAdded;

        var entity = _world.CreateEntity ();
        _world.AddComponent<WeaponComponent> (entity);
        _world.AddComponent<HealthComponent> (entity);
    }

    void IEcsInitSystem.Destroy () {
        _world.OnEntityComponentAdded -= OnEntityComponentAdded;
        _weaponFilter.OnEntityAdded -= OnFilterEntityAdded;
    }

    void OnEntityComponentAdded(int entityId, int componentId) {
        // Component "componentId" was added to entity "entityId".
    }

    void OnFilterEntityAdded(int entityId) {
        // Entity "entityId" was added to _weaponFilter.
    }
}
```

# Instant events
For instant events processing any ecs-system can subscribes callback to receive specified type of event data. Event data should be implemented as **struct**:
```
struct DamageReceived {
    public int Amount;
}

class WeaponSystem : IEcsSystem, IEcsInitSystem {
    [EcsWorld]
    EcsWorld _world;

    void IEcsSystem.Initialize () {
        _world.AddEventAction<DamageReceived> (OnDamageReceived);
        
        var eventData = new DamageReceived ();
        eventData.Amount = 10;
        _world.SendEvent (eventData);
    }

    void IEcsSystem.Destroy () {
        _world.RemoveEventAction<DamageReceived> (OnDamageReceived);
    }
    
    void OnDamageReceived (DamageReceived eventData) {
        Debug.Log("Damage " + e.Amount);
    }
}
```

# Examples
[Snake game](https://github.com/Leopotam/ecs-snake)

# License
The software released under the terms of the MIT license. Enjoy.