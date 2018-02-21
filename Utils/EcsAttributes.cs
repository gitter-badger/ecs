// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
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
    /// Processes injection to EcsFilter field - filter should be filled with compatible entities.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsFilterFillAttribute : Attribute { }
}