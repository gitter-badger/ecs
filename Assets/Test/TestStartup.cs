using UnityEngine;

namespace LeopotamGroup.Ecs.Tests {
    public sealed class TestStartup : MonoBehaviour {
        EcsWorld _world;

        void OnEnable () {
            _world = new EcsWorld ()
                .AddSystem (new TestSystem1 ())
                .AddSystem (new TestSystem2 ());
            _world.Initialize ();

            Debug.Log (_world.GetDebugStats ());
        }

        void Update () {
            if (_world != null) {
                _world.Update ();
            }
        }

        void FixedUpdate () {
            if (_world != null) {
                _world.FixedUpdate ();
            }
        }

        void OnDisable () {
            if (_world != null) {
                _world.Destroy ();
                _world = null;
            }
        }
    }
}