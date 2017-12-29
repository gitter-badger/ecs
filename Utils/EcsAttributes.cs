// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

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
    /// Processes injection to EcsFilter field - declares required components for react filter.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsReactFilterIncludeAttribute : Attribute {
        public readonly Type[] Components;

        public EcsReactFilterIncludeAttribute (params Type[] components) {
            Components = components;
        }
    }

    /// <summary>
    /// Processes injection to EcsFilter field - declares denied components for react filter.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsReactFilterExcludeAttribute : Attribute {
        public readonly Type[] Components;

        public EcsReactFilterExcludeAttribute (params Type[] components) {
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
}