// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017 Leopotam <leopotam@gmail.com>
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
            var attrEcsReactFilterInclude = typeof (EcsReactFilterIncludeAttribute);
            var attrEcsReactFilterExclude = typeof (EcsReactFilterExcludeAttribute);
            var attrEcsIndex = typeof (EcsIndexAttribute);

            foreach (var f in type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                // [EcsWorld]
                if (f.FieldType == ecsWorld && !f.IsStatic && Attribute.IsDefined (f, attrEcsWorld)) {
                    f.SetValue (system, world);
                }

                // [EcsFilterInclude]
                if (f.FieldType == ecsFilter && !f.IsStatic) {
                    var includeMask = new EcsComponentMask ();
                    var standardFilterIncDefined = Attribute.IsDefined (f, attrEcsFilterInclude);
                    if (standardFilterIncDefined) {
                        var components = ((EcsFilterIncludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterInclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            includeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
                    }
                    var reactFilterIncDefined = Attribute.IsDefined (f, attrEcsReactFilterInclude);
                    if (reactFilterIncDefined) {
                        var components = ((EcsReactFilterIncludeAttribute) Attribute.GetCustomAttribute (f, attrEcsReactFilterInclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            includeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
                    }

                    var excludeMask = new EcsComponentMask ();
                    var standardFilterExcDefined = Attribute.IsDefined (f, attrEcsFilterExclude);
                    if (standardFilterExcDefined) {
                        var components = ((EcsFilterExcludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterExclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            excludeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
                    }
                    var reactFilterExcDefined = Attribute.IsDefined (f, attrEcsReactFilterExclude);
                    if (reactFilterExcDefined) {
                        var components = ((EcsReactFilterExcludeAttribute) Attribute.GetCustomAttribute (f, attrEcsReactFilterExclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            excludeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
                    }
#if DEBUG
                    if (standardFilterIncDefined && reactFilterIncDefined) {
                        throw new Exception ("EcsFilterInclude and EcsReactFilterInclude cant be applied to one field.");
                    }
                    if ((standardFilterIncDefined || reactFilterIncDefined) && includeMask.IsEmpty ()) {
                        throw new Exception ("Include filter cant be empty.");
                    }
                    if (standardFilterExcDefined && reactFilterExcDefined) {
                        throw new Exception ("EcsFilterExclude and EcsReactFilterExclude cant be applied to one field.");
                    }
                    if (standardFilterExcDefined && reactFilterExcDefined) {
                        throw new Exception ("EcsFilterExclude and EcsReactFilterExclude cant be applied to one field.");
                    }
                    if ((standardFilterExcDefined || reactFilterExcDefined) && excludeMask.IsEmpty ()) {
                        throw new Exception ("Include filter cant be empty.");
                    }
                    if ((!standardFilterIncDefined && standardFilterExcDefined) || (!reactFilterIncDefined && reactFilterExcDefined)) {
                        throw new Exception ("EcsFilterExclude or EcsReactFilterExclude can be applied only as pair to EcsFilterInclude or EcsReactFilterInclude.");
                    }
                    if (includeMask.IsIntersects (excludeMask)) {
                        throw new Exception ("Exclude and include filters are intersected.");
                    }
#endif
                    f.SetValue (system, world.GetFilter (includeMask, excludeMask, reactFilterIncDefined));
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