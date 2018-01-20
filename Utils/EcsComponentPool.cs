// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace LeopotamGroup.Ecs.Internals {

    interface IEcsComponentPool {
        object GetItem (int idx);
        void RecycleIndex (int id);
        int GetComponentIndex ();
        void ConnectToWorld (EcsWorld world, int index);
    }
    /// <summary>
    /// Components pool container.
    /// </summary>
    sealed class EcsComponentPool<T> : IEcsComponentPool where T : class, new () {
        public static readonly EcsComponentPool<T> Instance = new EcsComponentPool<T> ();

        public T[] Items = new T[512];

        public int TypeIndex = -1;

        public EcsWorld World;

        static int[] _reservedItems = new int[256];

        int _itemsCount;

        int _reservedItemsCount;

        public int GetIndex () {
            int id;
            if (_reservedItemsCount > 0) {
                id = _reservedItems[--_reservedItemsCount];
            } else {
                id = _itemsCount;
                if (_itemsCount == Items.Length) {
                    var newItems = new T[_itemsCount << 1];
                    Array.Copy (Items, newItems, _itemsCount);
                    Items = newItems;
                }
                Items[_itemsCount++] = new T (); // Activator.CreateInstance (_type);
            }
            return id;
        }

        public void RecycleIndex (int id) {
            if (_reservedItemsCount == _reservedItems.Length) {
                var newItems = new int[_reservedItemsCount << 1];
                Array.Copy (_reservedItems, newItems, _reservedItemsCount);
                _reservedItems = newItems;
            }
            _reservedItems[_reservedItemsCount++] = id;
        }

        object IEcsComponentPool.GetItem (int idx) {
            return Items[idx];
        }

        int IEcsComponentPool.GetComponentIndex () {
            return TypeIndex;
        }

        public void ConnectToWorld (EcsWorld world, int index) {
            if (world != null && World != null) {
                throw new Exception ("Already connected to another world.");
            }
            World = world;
            TypeIndex = index;
            if (World == null) {
                Items = new T[512];
                _reservedItems = new int[256];
                _itemsCount = 0;
                _reservedItemsCount = 0;
            }
        }
    }
}