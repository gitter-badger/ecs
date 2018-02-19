// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace LeopotamGroup.Ecs.Internals {
    interface IEcsComponentPool {
        object Get (int idx);
        void Recycle (int id);
        int GetComponentTypeIndex ();
    }

    /// <summary>
    /// Components pool container.
    /// </summary>
    sealed class EcsComponentPool<T> : IEcsComponentPool where T : class, new () {
        public static readonly EcsComponentPool<T> Instance = new EcsComponentPool<T> ();

        public T[] Items = new T[MinSize];

        const int MinSize = 8;

        int _typeIndex;

        int[] _reservedItems = new int[MinSize];

        int _itemsCount;

        int _reservedItemsCount;

        Func<T> _creator;

        EcsComponentPool () {
            _typeIndex = EcsHelpers.ComponentsCount++;
        }

        public int GetIndex () {
            int id;
            if (_reservedItemsCount > 0) {
                id = _reservedItems[--_reservedItemsCount];
            } else {
                id = _itemsCount;
                if (_itemsCount == Items.Length) {
                    var newItems = new T[_itemsCount << 1];
                    Array.Copy (Items, 0, newItems, 0, _itemsCount);
                    Items = newItems;
                }
                Items[_itemsCount++] = _creator != null ? _creator () : (T) Activator.CreateInstance (typeof (T));
            }
            return id;
        }

        public void Recycle (int id) {
            if (_reservedItemsCount == _reservedItems.Length) {
                var newItems = new int[_reservedItemsCount << 1];
                Array.Copy (_reservedItems, 0, newItems, 0, _reservedItemsCount);
                _reservedItems = newItems;
            }
            _reservedItems[_reservedItemsCount++] = id;
        }

        object IEcsComponentPool.Get (int idx) {
            return Items[idx];
        }

        public int GetComponentTypeIndex () {
            return _typeIndex;
        }

        public void SetCreator (Func<T> creator) {
            _creator = creator;
        }

        public void Shrink () {
            var newSize = EcsHelpers.GetPowerOfTwoSize (_itemsCount < MinSize ? MinSize : _itemsCount);
            if (newSize < Items.Length) {
                var newItems = new T[newSize];
                Array.Copy (Items, 0, newItems, 0, _itemsCount);
                Items = newItems;
            }
            if (_reservedItems.Length > MinSize) {
                _reservedItems = new int[MinSize];
                _reservedItemsCount = 0;
            }
        }
    }
}