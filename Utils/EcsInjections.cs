using System;
using System.Reflection;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Processes injection to EcsWorld field.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsWorldAttribute : Attribute { }

    /// <summary>
    /// Processes injection to EcsFilter field.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsFilterAttribute : Attribute {
        public readonly Type[] Components;

        public EcsFilterAttribute (params Type[] components) {
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

            var attrEcsWorldType = typeof (EcsWorldAttribute);
            var attrEcsFilterType = typeof (EcsFilterAttribute);
            var attrEcsIndexType = typeof (EcsIndexAttribute);

            foreach (var f in type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                // [EcsWorld]
                if (f.FieldType == ecsWorld && !f.IsStatic && Attribute.IsDefined (f, attrEcsWorldType)) {
                    f.SetValue (system, world);
                }
                // [EcsFilter]
                if (f.FieldType == ecsFilter && !f.IsStatic && Attribute.IsDefined (f, attrEcsFilterType)) {
                    var components = ((EcsFilterAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterType)).Components;
                    var mask = new EcsComponentMask ();
                    for (var i = 0; i < components.Length; i++) {
                        mask.SetBit (world.GetComponentIndex (components[i]), true);
                    }
                    f.SetValue (system, world.GetFilter (mask));
                }
                // [EcsIndex]
                if (f.FieldType == ecsIndex && !f.IsStatic && Attribute.IsDefined (f, attrEcsIndexType)) {
                    var component = ((EcsIndexAttribute) Attribute.GetCustomAttribute (f, attrEcsIndexType)).Component;
                    f.SetValue (system, world.GetComponentIndex (component));
                }
            }
        }
    }
}