using UnityEngine;

namespace LeopotamGroup.Ecs.Tests {
    public sealed class TestStartup : MonoBehaviour {
        EcsWorld _world;

        void OnEnable () {
            _world = new EcsWorld ()
                .AddSystem (new TestSystem1 ())
                .AddSystem (new TestSystem2 ());
            _world.Initialize ();

            var stats = _world.GetStats ();
            Debug.LogFormat ("[Systems: {0}] [Entities: {1}/{2}] [Components: {3}] [Filters: {4}] [DelayedUpdates: {5}]",
                stats.AllSystems, stats.AllEntities, stats.ReservedEntities, stats.Components, stats.Filters, stats.DelayedUpdates);
        }

        void Update () {
            _world.Update ();
        }

        void FixedUpdate () {
            _world.FixedUpdate ();
        }

        void OnDisable () {
            _world.Destroy ();
            _world = null;
        }
    }
}