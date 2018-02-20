// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace LeopotamGroup.Ecs.Internals {
    interface IEcsComponentPool {
        object GetExistItemById (int idx);
        void RecycleById (int id);
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

        public void RecycleById (int id) {
            if (_reservedItemsCount == _reservedItems.Length) {
                Array.Resize (ref _reservedItems, _reservedItemsCount << 1);
            }
            _reservedItems[_reservedItemsCount++] = id;
        }

        object IEcsComponentPool.GetExistItemById (int idx) {
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
                Array.Resize (ref Items, newSize);
            }
            if (_reservedItems.Length > MinSize) {
                _reservedItems = new int[MinSize];
                _reservedItemsCount = 0;
            }
        }
    }
}