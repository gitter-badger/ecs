using System;
using UnityEngine;

namespace LeopotamGroup.Ecs.Tests {
    public sealed class TestSystem1 : EcsSystem {
        protected override Type[] GetRequiredComponents () {
            return new Type[] { typeof (HealthComponent) };
        }

        public override void Initialize () {
            Debug.LogFormat ("{0}.requrements mask: {1}", GetType ().Name, ComponentsMask);
        }
    }

    public sealed class TestSystem2 : EcsSystem {
        protected override Type[] GetRequiredComponents () {
            return new Type[] { typeof (WeaponComponent) };
        }

        public override void Initialize () {
            Debug.LogFormat ("{0}.requrements mask: {1}", GetType ().Name, ComponentsMask);
        }
    }
}