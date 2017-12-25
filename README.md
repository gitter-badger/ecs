# Test Entity Component System implementation.
Performance and zero memory allocation / no gc - main goals of this project. 

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
class WeaponSystem : IEcsSystem, IEcsInitSystem {
    [EcsWorld]
    EcsWorld _world;

    void IEcsInitSystem.Initialize () {
        var entity = _world.CreateEntity ();
        _world.RemoveEntity (entity);
    }
    void IEcsInitSystem.Destroy () { }
}
```
With [EcsWorld], [EcsFilter(typeof(X))] and [EcsIndex(typeof(X))] any compatible fields of system class can be auto initialized (auto injection).

## EcsFilter
Container for keep filtered entities with specified component list:
```
class WeaponSystem : IEcsSystem, IEcsInitSystem, IEcsUpdateSystem {
    [EcsWorld]
    EcsWorld _world;
    [EcsFilter(typeof(WeaponComponent))]
    EcsFilter _filter;

    void IEcsInitSystem.Initialize () {
        var newEntity = _world.CreateEntity ();
        _world.AddComponent<WeaponComponent> (newEntity);
    }
    void IEcsInitSystem.Destroy () { }
    void IEcsUpdateSystem.Update () {
        foreach (var entity in _filter.Entities) {
            var weapon = _world.GetComponent<WeaponComponent> (entity);
            weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
        }
    }
}
```
For events processing EcsFilter should be created with flag "forEvents"=true,
event data is standard class with IEcsComponent implementation:
```
struct DamageReceived {
    public int Amount;
}
class WeaponSystem : IEcsSystem, IEcsInitSystem, IEcsUpdateSystem {
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
    void IEcsUpdateSystem.Update () {
        foreach (var eventData in _event.Entities) {
            var damage = _world.GetComponent<DamageComponent> (eventData);
            Debug.Log ("Damage " + damage.Amount);
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
        _world.Update ();
    }

    void OnDisable() {
        // destroy ecs environment.
        _world.Destroy ();
        _world = null;
    }
}
```

# Examples
[Snake game](https://github.com/Leopotam/ecs-snake)

# License
The software released under the terms of the MIT license. Enjoy.