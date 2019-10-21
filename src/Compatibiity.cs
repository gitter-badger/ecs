// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2019 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace Leopotam.Ecs {
    /// <summary>
    /// Backward compatibility helpers.
    /// </summary>
    public static class Compatibility {
        [Obsolete ("Use world.NewEntity() instead")]
        public static EcsEntity CreateEntity (this EcsWorld world) {
            return world.NewEntity ();
        }

        [Obsolete ("Use world.NewEntityWith() instead")]
        public static EcsEntity CreateEntityWith<C1> (this EcsWorld world, out C1 c1) where C1 : class {
            return world.NewEntityWith (out c1);
        }

        [Obsolete ("Use world.NewEntityWith() instead")]
        public static EcsEntity CreateEntityWith<C1, C2> (this EcsWorld world, out C1 c1, out C2 c2) where C1 : class where C2 : class {
            return world.NewEntityWith (out c1, out c2);
        }

        [Obsolete ("Use world.NewEntityWith() instead")]
        public static EcsEntity CreateEntityWith<C1, C2, C3> (this EcsWorld world, out C1 c1, out C2 c2, out C3 c3) where C1 : class where C2 : class where C3 : class {
            return world.NewEntityWith (out c1, out c2, out c3);
        }

        [Obsolete ("Use world.EndFrame() instead")]
        public static void RemoveOneFrameComponents (this EcsWorld world) {
            world.EndFrame ();
        }

        [Obsolete ("Use entity.Set() instead")]
        public static C1 AddComponent<C1> (this EcsWorld world, in EcsEntity entity) where C1 : class {
            return entity.Set<C1> ();
        }

        [Obsolete ("Use entity.Get() instead")]
        public static C1 GetComponent<C1> (this EcsWorld world, in EcsEntity entity) where C1 : class {
            return entity.Get<C1> ();
        }

        [Obsolete ("Use entity.Set() instead")]
        public static C1 EnsureComponent<C1> (this EcsWorld world, in EcsEntity entity, out bool isNew) where C1 : class {
            isNew = entity.Get<C1> () == null;
            return entity.Set<C1> ();
        }

        [Obsolete ("Use entity.Unset() instead")]
        public static void RemoveComponent<C1> (this EcsWorld world, in EcsEntity entity) where C1 : class {
            entity.Unset<C1> ();
        }

        [Obsolete ("Use entity.Destroy() instead")]
        public static void RemoveEntity (this EcsWorld world, in EcsEntity entity) {
            entity.Destroy ();
        }

        [Obsolete ("Use world.GetFilter(typeof(<>)) instead")]
        public static F GetFilter<F> (this EcsWorld world) where F : EcsFilter {
            return (F) world.GetFilter (typeof (F));
        }

        [Obsolete ("Use world.Destroy() instead")]
        public static void Dispose (this EcsWorld world) {
            world.Destroy ();
        }

        [Obsolete ("There are no delayed operations anymore")]
        public static void ProcessDelayedUpdates (this EcsWorld world) { }

        [Obsolete ("Use systems.Init() instead")]
        public static void Initialize (this EcsSystems systems) {
            systems.Init ();
        }

        [Obsolete ("Use systems.Destroy() instead")]
        public static void Dispose (this EcsSystems systems) {
            systems.Destroy ();
        }

        [Obsolete ("Use EcsComponentPool<>.Instance.SetCustomCtor() instead")]
        public static void SetCreator<C1> (this EcsComponentPool<C1> pool, Func<C1> ctor) where C1 : class {
            EcsComponentPool<C1>.Instance.SetCustomCtor (ctor);
        }
    }

    [Obsolete ("Use IEcsAutoReset interface instead")]
    public interface IEcsAutoResetComponent : IEcsAutoReset { }

    [Obsolete ("Use IEcsOneFrame interface instead")]
    public class EcsOneFrameAttribute : Attribute { }

    [Obsolete ("Injection already works without any additional attributes")]
    public class EcsInjectAttribute : Attribute { }

    [Obsolete ("Use IEcsIgnoreInFilter interface instead")]
    public class EcsIgnoreInFilterAttribute : Attribute { }
}