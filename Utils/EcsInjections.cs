// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Reflection;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Processes injection to EcsWorld field.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsWorldAttribute : Attribute { }

    /// <summary>
    /// Processes injection to EcsFilter field - declares required components.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsFilterIncludeAttribute : Attribute {
        public readonly Type[] Components;

        public EcsFilterIncludeAttribute (params Type[] components) {
            Components = components;
        }
    }

    /// <summary>
    /// Processes injection to EcsFilter field - declares denied components.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsFilterExcludeAttribute : Attribute {
        public readonly Type[] Components;

        public EcsFilterExcludeAttribute (params Type[] components) {
            Components = components;
        }
    }

    /// <summary>
    /// Processes injection to int field for component index of specified type.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsIndexAttribute : Attribute {
        public readonly Type Component;

        public EcsIndexAttribute (Type component) {
            Component = component;
        }
    }

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
                    var includeMask = new EcsComponentMask ();
                    if (Attribute.IsDefined (f, attrEcsFilterInclude)) {
                        var components = ((EcsFilterIncludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterInclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            includeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
#if DEBUG
                        if (includeMask.IsEmpty ()) {
                            throw new Exception ("Include filter cant be empty.");
                        }
#endif
                    }
                    var excludeMask = new EcsComponentMask ();
                    if (Attribute.IsDefined (f, attrEcsFilterExclude)) {
                        var components = ((EcsFilterExcludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterExclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            excludeMask.SetBit (world.GetComponentIndex (components[i]), true);
                        }
                    }
#if DEBUG
                    if (includeMask.IsEmpty () && !excludeMask.IsEmpty ()) {
                        throw new Exception ("Exclude filter cant be applied for empty include filter.");
                    }
                    if (includeMask.IsEquals (excludeMask)) {
                        throw new Exception ("Exclude and include filters are equals.");
                    }
#endif
                    f.SetValue (system, world.GetFilter (includeMask, excludeMask));
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