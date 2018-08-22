// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace Leopotam.Ecs {
    interface IEcsComponentPool {
        object GetExistItemById (int idx);
        void RecycleById (int id);
        int GetComponentTypeIndex ();
    }

    /// <summary>
    /// Marks field of component to be not checked for null on component removing.
    /// Works only in DEBUG mode!
    /// </summary>
    [System.Diagnostics.Conditional ("DEBUG")]
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsIgnoreNullCheckAttribute : Attribute { }

    /// <summary>
    /// Components pool container.
    /// </summary>
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsComponentPool<T> : IEcsComponentPool where T : class, new () {
        const int MinSize = 8;

        public static readonly EcsComponentPool<T> Instance = new EcsComponentPool<T> ();

        public T[] Items = new T[MinSize];

        public readonly bool IsIgnoreInFilter = Attribute.IsDefined (typeof (T), typeof (EcsIgnoreInFilterAttribute));

        int _typeIndex;

        int[] _reservedItems = new int[MinSize];

        int _itemsCount;

        int _reservedItemsCount;

        Func<T> _creator;

#if DEBUG
        System.Collections.Generic.List<System.Reflection.FieldInfo> _nullableFields = new System.Collections.Generic.List<System.Reflection.FieldInfo> (8);
#endif

        EcsComponentPool () {
            _typeIndex = Internals.EcsHelpers.ComponentsCount++;
#if DEBUG
            // collect all marshal-by-reference fields.
            var fields = typeof (T).GetFields ();
            for (var i = 0; i < fields.Length; i++) {
                var field = fields[i];
                var type = field.FieldType;
                if (!type.IsValueType || (Nullable.GetUnderlyingType (type) != null) && !Nullable.GetUnderlyingType (type).IsValueType) {
                    if (type != typeof (string) && !Attribute.IsDefined (field, typeof (EcsIgnoreNullCheckAttribute))) {
                        _nullableFields.Add (fields[i]);
                    }
                }
            }
#endif
        }

#if NET_4_6 || NET_STANDARD_2_0
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public int RequestNewId () {
            int id;
            if (_reservedItemsCount > 0) {
                id = _reservedItems[--_reservedItemsCount];
            } else {
                id = _itemsCount;
                if (_itemsCount == Items.Length) {
                    Array.Resize (ref Items, _itemsCount << 1);
                }
                Items[_itemsCount++] = _creator != null ? _creator () : (T) Activator.CreateInstance (typeof (T));
            }
            return id;
        }

#if NET_4_6 || NET_STANDARD_2_0
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public void RecycleById (int id) {
#if DEBUG
            // check all marshal-by-reference types for nulls.
            var obj = Items[id];
            for (var i = 0; i < _nullableFields.Count; i++) {
                if (_nullableFields[i].GetValue (obj) != null) {
                    throw new Exception (string.Format (
                        "Memory leak for \"{0}\" component: \"{1}\" field not nulled. If you are sure that it's not - mark field with [EcsIgnoreNullCheck] attribute",
                        typeof (T).Name, _nullableFields[i].Name));
                }
            }
#endif
            if (_reservedItemsCount == _reservedItems.Length) {
                Array.Resize (ref _reservedItems, _reservedItemsCount << 1);
            }
            _reservedItems[_reservedItemsCount++] = id;
        }

#if NET_4_6 || NET_STANDARD_2_0
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        object IEcsComponentPool.GetExistItemById (int idx) {
            return Items[idx];
        }

#if NET_4_6 || NET_STANDARD_2_0
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public int GetComponentTypeIndex () {
            return _typeIndex;
        }

        public void SetCreator (Func<T> creator) {
            _creator = creator;
        }
    }
}