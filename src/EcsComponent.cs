// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2019 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Leopotam.Ecs {
    /// <summary>
    /// Marks component type to be not autofilled as GetX in filter.
    /// </summary>
    public interface IEcsIgnoreInFilter { }

    /// <summary>
    /// Marks component type to be auto removed from world.
    /// </summary>
    public interface IEcsOneFrame { }

    /// <summary>
    /// Marks component type as resettable with custom logic.
    /// </summary>
    public interface IEcsAutoReset {
        void Reset ();
    }

    /// <summary>
    /// Marks field of IEcsSystem class to be ignored during dependency injection.
    /// </summary>
    public sealed class EcsIgnoreInjectAttribute : Attribute { }

    /// <summary>
    /// Marks field of component to be not checked for null on component removing.
    /// Works only in DEBUG mode!
    /// </summary>
    [System.Diagnostics.Conditional ("DEBUG")]
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class EcsIgnoreNullCheckAttribute : Attribute { }

    static class EcsComponentPools {
        public static readonly Dictionary<int, IEcsComponentPool> Items = new Dictionary<int, IEcsComponentPool> (512);
        // started from 1 for correct filters updating (add component on positive and remove on negative).
        public static int Count = 1;
    }

    interface IEcsComponentPool {
        void Recycle (int idx);
        object GetItem (int idx);
    }

#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsComponentPool<T> : IEcsComponentPool where T : class {
        public readonly int TypeIndex;
        public readonly bool IsAutoReset;
        public readonly bool IsIgnoreInFilter;
        public readonly bool IsOneFrame;
        public static readonly EcsComponentPool<T> Instance = new EcsComponentPool<T> ();
        public T[] Items = new T[128];
        int[] _reservedItems = new int[128];
        int _itemsCount;
        int _reservedItemsCount;
        Func<T> _customCtor;

#if DEBUG
        readonly System.Collections.Generic.List<System.Reflection.FieldInfo> _nullableFields = new System.Collections.Generic.List<System.Reflection.FieldInfo> (8);
#endif

        EcsComponentPool () {
            TypeIndex = EcsComponentPools.Count++;
            EcsComponentPools.Items[TypeIndex] = this;
            IsAutoReset = typeof (IEcsAutoReset).IsAssignableFrom (typeof (T));
#pragma warning disable 618
            IsIgnoreInFilter = typeof (IEcsIgnoreInFilter).IsAssignableFrom (typeof (T)) || Attribute.IsDefined (typeof (T), typeof (EcsIgnoreInFilterAttribute));
            IsOneFrame = typeof (IEcsOneFrame).IsAssignableFrom (typeof (T)) || Attribute.IsDefined (typeof (T), typeof (EcsOneFrameAttribute));
#pragma warning restore 618
#if DEBUG
            // collect all marshal-by-reference fields.
            var fields = typeof (T).GetFields ();
            for (var i = 0; i < fields.Length; i++) {
                var field = fields[i];
                if (!Attribute.IsDefined (field, typeof (EcsIgnoreNullCheckAttribute))) {
                    var type = field.FieldType;
                    if (!type.IsValueType || (Nullable.GetUnderlyingType (type) != null) && !Nullable.GetUnderlyingType (type).IsValueType) {
                        if (type != typeof (string)) {
                            _nullableFields.Add (field);
                        }
                    }
                    if (type == typeof (EcsEntity)) {
                        _nullableFields.Add (field);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Sets custom constructor for component instances.
        /// </summary>
        /// <param name="ctor"></param>
        public void SetCustomCtor (Func<T> ctor) {
#if DEBUG
            if (ctor == null) {
                throw new Exception ("Ctor is null.");
            }
#endif
            _customCtor = ctor;
        }

        /// <summary>
        /// Sets new capacity (if more than current amount).
        /// </summary>
        /// <param name="capacity">New value.</param>
        public void SetCapacity (int capacity) {
            if (capacity > Items.Length) {
                Array.Resize (ref Items, capacity);
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int New () {
            int id;
            if (_reservedItemsCount > 0) {
                id = _reservedItems[--_reservedItemsCount];
            } else {
                id = _itemsCount;
                if (_itemsCount == Items.Length) {
                    Array.Resize (ref Items, _itemsCount << 1);
                }
                Items[_itemsCount++] = _customCtor != null ? _customCtor () : (T) Activator.CreateInstance (typeof (T));
            }
            return id;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public object GetItem (int idx) {
            return Items[idx];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Recycle (int idx) {
            if (IsAutoReset) {
                ((IEcsAutoReset) Items[idx]).Reset ();
            }
#if DEBUG
            // check all marshal-by-reference typed fields for nulls.
            ref var obj = ref Items[idx];
            for (int i = 0, iMax = _nullableFields.Count; i < iMax; i++) {
                if (_nullableFields[i].FieldType.IsValueType) {
                    if (_nullableFields[i].FieldType == typeof (EcsEntity) && ((EcsEntity) _nullableFields[i].GetValue (obj)).Owner != null) {
                        var entity = (EcsEntity) _nullableFields[i].GetValue (obj);
                        throw new Exception (string.Format (
                            "Memory leak for \"{0}\" component: \"{1}\" field not nulled with EcsEntity.Null. If you are sure that it's not - mark field with [EcsIgnoreNullCheck] attribute.",
                            typeof (T).Name, _nullableFields[i].Name));
                    }
                } else {
                    if (_nullableFields[i].GetValue (obj) != null) {
                        throw new Exception (string.Format (
                            "Memory leak for \"{0}\" component: \"{1}\" field not nulled. If you are sure that it's not - mark field with [EcsIgnoreNullCheck] attribute.",
                            typeof (T).Name, _nullableFields[i].Name));
                    }
                }
            }
#endif
            if (_reservedItemsCount == _reservedItems.Length) {
                Array.Resize (ref _reservedItems, _reservedItemsCount << 1);
            }
            _reservedItems[_reservedItemsCount++] = idx;
        }
    }
}