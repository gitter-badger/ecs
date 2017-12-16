using System;

namespace LeopotamGroup.Ecs {
    public interface IEcsUpdateSystem {
        void Update ();
    }

    public interface IEcsFixedUpdateSystem {
        void FixedUpdate ();
    }

    public abstract class EcsSystem {
        public ComponentMask ComponentsMask { get; private set; }

        protected EcsWorld World { get; private set; }

        public void SetWorld (EcsWorld world) {
            if (world == null) {
                throw new Exception ("world is null");
            }
            if (World != null) {
                throw new Exception ("Already inited");
            }
            World = world;

            ComponentsMask = World.GetComponentsMask (GetRequiredComponents ());
        }

        protected abstract Type[] GetRequiredComponents ();

        public virtual void Initialize () { }

        public virtual void Destroy () { }
    }
}