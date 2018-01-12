// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace LeopotamGroup.Ecs.Internals {
    /// <summary>
    /// Components pool container.
    /// </summary>
    sealed class EcsComponentPool {
        public object[] Items = new object[512];

        int[] _reservedItems = new int[256];

        Type _type;

        int _itemsCount;

        int _reservedItemsCount;

        public EcsComponentPool (Type type) {
            _type = type;
        }

        public int GetIndex () {
            int id;
            if (_reservedItemsCount > 0) {
                id = _reservedItems[--_reservedItemsCount];
            } else {
                id = _itemsCount;
                if (_itemsCount == Items.Length) {
                    var newItems = new object[_itemsCount << 1];
                    Array.Copy (Items, newItems, _itemsCount);
                    Items = newItems;
                }
                Items[_itemsCount++] = Activator.CreateInstance (_type);
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
    }
}