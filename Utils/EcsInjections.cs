// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Reflection;

namespace LeopotamGroup.Ecs.Internals {
    /// <summary>
    /// Processes dependency injection to ecs systems. For internal use only.
    /// </summary>
    static class EcsInjections {
        public static void Inject (EcsWorld world, IEcsSystem system) {
            var type = system.GetType ();

            var ecsWorld = typeof (EcsWorld);
            var ecsFilter = typeof (EcsFilter);
            var ecsIndex = typeof (int);

            var attrEcsWorld = typeof (EcsWorldAttribute);
            var attrEcsFilterInclude = typeof (EcsFilterIncludeAttribute);
            var attrEcsFilterExclude = typeof (EcsFilterExcludeAttribute);
            var attrEcsIndex = typeof (EcsIndexAttribute);

            foreach (var f in type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                // [EcsWorld]
                if (f.FieldType == ecsWorld && !f.IsStatic && Attribute.IsDefined (f, attrEcsWorld)) {
                    f.SetValue (system, world);
                }

                // [EcsFilterInclude]
                if (f.FieldType == ecsFilter && !f.IsStatic) {
                    EcsComponentMask includeMask = null;
                    var standardFilterIncDefined = Attribute.IsDefined (f, attrEcsFilterInclude);
                    if (standardFilterIncDefined) {
                        includeMask = new EcsComponentMask ();
                        var components = ((EcsFilterIncludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterInclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            includeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
                    }
                    EcsComponentMask excludeMask = null;
                    var standardFilterExcDefined = Attribute.IsDefined (f, attrEcsFilterExclude);
                    if (standardFilterExcDefined) {
                        excludeMask = new EcsComponentMask ();
                        var components = ((EcsFilterExcludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterExclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            excludeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
                    }
#if DEBUG && !ECS_PERF_TEST
                    if (standardFilterIncDefined && includeMask.IsEmpty ()) {
                        throw new Exception ("Include filter cant be empty at system: " + type.Name);
                    }
                    if (standardFilterExcDefined && excludeMask.IsEmpty ()) {
                        throw new Exception ("Exclude filter cant be empty at system: " + type.Name);
                    }
                    if (!standardFilterIncDefined && standardFilterExcDefined) {
                        throw new Exception ("EcsFilterExclude can be applied only as pair to EcsFilterInclude at system: " + type.Name);
                    }
                    if (includeMask != null && excludeMask != null && includeMask.IsIntersects (excludeMask)) {
                        throw new Exception ("Exclude and include filters are intersected at system: " + type.Name);
                    }
#endif
                    if (standardFilterIncDefined) {
                        f.SetValue (system, world.GetFilter (includeMask, excludeMask ?? new EcsComponentMask ()));
                    }
                }

                // [EcsIndex]
                if (f.FieldType == ecsIndex && !f.IsStatic && Attribute.IsDefined (f, attrEcsIndex)) {
                    var component = ((EcsIndexAttribute) Attribute.GetCustomAttribute (f, attrEcsIndex)).Component;
                    f.SetValue (system, world.GetComponentIndex (component));
                }
            }
        }
    }
}