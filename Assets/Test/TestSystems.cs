using UnityEngine;

namespace LeopotamGroup.Ecs.Tests {
    public sealed class TestSystem1 : IEcsSystem, IEcsUpdateSystem {
        EcsWorld _world;

        void IEcsSystem.Initialize (EcsWorld world) {
            _world = world;
            Debug.LogFormat ("{0} => initialize", GetType ().Name);
        }

        void IEcsSystem.Destroy () {
            Debug.LogFormat ("{0} => destroy", GetType ().Name);
        }

        void IEcsUpdateSystem.Update () {
            // send event.
            var eventData = new DamageEvent ();
            eventData.Amount = 10;
            _world.PublishEvent (eventData);
        }
    }

    public sealed class TestSystem2 : IEcsSystem, IEcsUpdateSystem {
        EcsWorld _world;

        EcsFilter _filter;

        int _weaponComponentId;

        void IEcsSystem.Initialize (EcsWorld world) {
            _world = world;
            _world.OnComponentAttach += OnComponentAttach;
            _world.OnComponentDetach += OnComponentDetach;

            // up to 4 types.
            _filter = _world.GetFilter<WeaponComponent> ();
            // if you need more - you can create it with component mask:
            // var mask = new EcsComponentMask ();
            // mask.SetBit (_world.GetComponentTypeId<A> (), true);
            // mask.SetBit (_world.GetComponentTypeId<B> (), true);
            // mask.SetBit (_world.GetComponentTypeId<C> (), true);
            // var filter = _world.GetFilter (mask, false);

            // Listen to event.
            _world.SubscribeToEvent<DamageEvent> (OnDamageEvent);

            var entity = _world.CreateEntity ();
            _world.AddComponent<HealthComponent> (entity);
            _world.AddComponent<WeaponComponent> (entity);

            _weaponComponentId = _world.GetComponentIndex<WeaponComponent> ();

            Debug.LogFormat ("{0} => initialize", GetType ().Name);
        }

        void IEcsSystem.Destroy () {
            // No need to listen event anymore.
            _world.UnsubscribeFromEvent<DamageEvent> (OnDamageEvent);

            _world.OnComponentAttach -= OnComponentAttach;
            _world.OnComponentDetach -= OnComponentDetach;
            Debug.LogFormat ("{0} => destroy", GetType ().Name);
        }

        void OnComponentAttach (IEcsComponent obj) {
            // Debug.LogFormat ("{0} => attach", obj.GetType ().Name);
        }

        void OnComponentDetach (IEcsComponent obj) {
            // Debug.LogFormat ("{0} => detach", obj.GetType ().Name);
        }

        void OnDamageEvent (DamageEvent eventData) {
            Debug.Log ("damage event: " + eventData.Amount);
        }

        void IEcsUpdateSystem.Update () {
            foreach (var entity in _filter.Entities) {
                var weapon = _world.GetComponent<WeaponComponent> (entity, _weaponComponentId);
                weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
            }
        }
    }
}