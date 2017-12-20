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
int entity = _world.CreateEntity ();
_world.RemoveEntity (entity);
```

## System
Сontainer for logic for processing filtered entities. User class should implements IEcsSystem interface:
```
class WeaponSystem : IEcsSystem {
    EcsWorld _world;

    void IEcsSystem.Initialize (EcsWorld world) {
        _world = world;

        var entity = _world.CreateEntity ();
        _world.RemoveEntity (entity);
    }

    void IEcsSystem.Destroy () {
    }
}
```

## EcsFilter
Container for keep filtered entities with specified component list:
```
class WeaponSystem : IEcsSystem, IEcsUpdateSystem {
    EcsWorld _world;
    EcsFilter _filter;

    void IEcsSystem.Initialize (EcsWorld world) {
        _world = world;

        // we want to filter entities only with WeaponComponent on them.
        _filter = _world.GetFilter<WeaponComponent> (false);
        
        var newEntity = _world.CreateEntity ();
        _world.AddComponent<WeaponComponent> (newEntity);
    }

    void IEcsUpdateSystem.Update () {
        foreach (var entity in _filter.Entities) {
            var weapon = _world.GetComponent<WeaponComponent> (entity);
            weapon.Ammo--;
        }
    }
}
```
For events processing EcsFilter should be created with flag "forEvents"=true,
event data is standard class with IEcsComponent implementation:
```
public class DamageComponent : IEcsComponent {
    public int Amount;
}
class WeaponSystem : IEcsSystem, IEcsUpdateSystem {
    EcsWorld _world;
    EcsFilter _event;

    void IEcsSystem.Initialize (EcsWorld world) {
        _world = world;

        _event = _world.GetFilter<DamageComponent> (true);
        
        var eventData = _world.CreateEvent<DamageComponent> ();
        eventData.Amount = 10;
    }

    void IEcsUpdateSystem.Update () {
        foreach (var eventData in _event.Entities) {
            var damage = _world.GetComponent<DamageComponent> (eventData);
            Debug.Log("Damage " + damage.Amount);
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
        var world = new EcsWorld ()
            .AddSystem(new WeaponSystem ());
        world.Initialize();
    }
    
    void Update() {
        // process all dependent systems.
        world.Update ();
    }

    void OnDisable() {
        // destroy ecs environment.
        world.Destroy ();
        world = null;
    }
}
```