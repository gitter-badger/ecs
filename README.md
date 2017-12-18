# Test Entity Component System implementation.

## Component
Container for user data without / with small logic inside. User class should implements IEcsComponent interface:
```
class WeaponComponent: IEcsComponent {
    public int Ammo;
    public string GunName;
}
```

## Entity
Сontainer for components. Implemented with int id-s for more simplified api:
```
int entity = _world.CreateEntity();
_world.RemoveEntity(entity);
```

## System
Сontainer for logic for processing filtered entities. User class should implements IEcsSystem interface:
```
class WeaponSystem : IEcsSystem {
    EcsWorld _world;

    void IEcsSystem.Initialize (EcsWorld world) {
        _world = world;

        var entity = _world.CreateEntity();
        _world.RemoveEntity(entity);
    }

    void IEcsSystem.Destroy () {
    }
}
```

## EcsFilter
Container for keep filtered entities for specified component list:
```
class WeaponSystem : IEcsSystem {
    EcsWorld _world;
    EcsFilter _filter;

    void IEcsSystem.Initialize (EcsWorld world) {
        _world = world;

        // we want to filter entities only with WeaponComponent on them.
        _filter = _world.GetFilter (typeof (WeaponComponent));
        
        var newEntity = _world.CreateEntity();
        _world.AddComponent<WeaponComponent>(newEntity);

        foreach (var entity in _filter.Entities) {
            var weapon = _world.GetComponent<WeaponComponent>(entity);
            weapon.Ammo--;
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
        var world = new EcsWorld()
            .AddSystem(new WeaponSystem());
        world.Initialize();
    }
    
    void Update() {
        // process all dependent systems.
        world.Update();
    }

    void OnDisable() {
        // destroy ecs environment.
        world.Destroy();
        world = null;
    }
}
```