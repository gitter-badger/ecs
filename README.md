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
Component's container for components. Implemented with int id-s for less memory allocation:
```
int entity = World.CreateEntity();
World.RemoveEntity(entity);
```

## System
Logic container for filtered entities processing. User class should inherits EcsSystem class:
```
class WeaponSystem : EcsSystem {
    // we want to filter entities only with WeaponComponent on them.
    protected override Type[] GetRequiredComponents () {
        return new Type[] { typeof (WeaponComponent) };
    }
}
```

## EcsWorld
Root level container for all systems / entities / components, works like isolated environment:
```
var world = new EcsWorld()
    .AddSystem(new WeaponSystem());
world.Initialize();
...
// process all dependent systems.
world.Update();
```