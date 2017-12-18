using UnityEngine;

namespace LeopotamGroup.Ecs.Tests {
    public sealed class TestSystem1 : IEcsSystem, IEcsDestroyableSystem {
        EcsWorld _world;

        void IEcsSystem.Initialize (EcsWorld world) {
            _world = world;
            Debug.LogFormat ("{0} => initialize", GetType ().Name);
        }

        void IEcsDestroyableSystem.Destroy () {
            Debug.LogFormat ("{0} => destroy", GetType ().Name);
        }
    }

    public sealed class TestSystem2 : IEcsSystem, IEcsUpdatableSystem, IEcsDestroyableSystem {
        EcsWorld _world;

        EcsFilter _filter;

        void IEcsSystem.Initialize (EcsWorld world) {
            _world = world;
            _world.OnComponentAttach += OnComponentAttach;
            _world.OnComponentDetach += OnComponentDetach;
            _filter = _world.GetFilter (typeof (WeaponComponent));

            var entity = _world.CreateEntity ();
            _world.AddComponent<HealthComponent> (entity);
            _world.AddComponent<WeaponComponent> (entity);

            Debug.LogFormat ("{0} => initialize", GetType ().Name);
        }

        private void OnComponentAttach (IEcsComponent obj) {
            Debug.LogFormat ("{0} => attach", obj.GetType ().Name);
        }

        private void OnComponentDetach (IEcsComponent obj) {
            Debug.LogFormat ("{0} => detach", obj.GetType ().Name);
        }

        void IEcsDestroyableSystem.Destroy () {
            _world.OnComponentAttach -= OnComponentAttach;
            _world.OnComponentDetach -= OnComponentDetach;
            Debug.LogFormat ("{0} => destroy", GetType ().Name);
        }

        void IEcsUpdatableSystem.Update () {
            // foreach (var entity in _filter.Entities) {
            //     var weapon = _world.GetComponent<WeaponComponent> (entity);
            //     weapon.Ammo = System.Math.Max (0, weapon.Ammo);
            // }
            // Debug.LogFormat ("Found {0} entities / {1}", _filter.Entities.Count, Time.time);
        }
    }
}