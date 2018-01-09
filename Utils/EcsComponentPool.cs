// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs.Internals {
    /// <summary>
    /// Components pool container.
    /// </summary>
    sealed class EcsComponentPool {
        public IEcsComponent[] Items = new IEcsComponent[512];

        readonly List<int> _reservedItems = new List<int> (512);

        readonly Type _type;

        int _itemsCount;

        int _reservedItemsCount;

        public EcsComponentPool (Type type) {
            _type = type;
        }

        public int GetIndex () {
            int id;
            if (_reservedItemsCount > 0) {
                _reservedItemsCount--;
                id = _reservedItems[_reservedItemsCount];
                _reservedItems.RemoveAt (_reservedItemsCount);
            } else {
                id = _itemsCount;
                if (_itemsCount == Items.Length) {
                    var newItems = new IEcsComponent[_itemsCount << 1];
                    Array.Copy (Items, newItems, _itemsCount);
                    Items = newItems;
                }
                Items[_itemsCount++] = Activator.CreateInstance (_type) as IEcsComponent;
            }
            return id;
        }

        public void RecycleIndex (int id) {
            _reservedItems.Add (id);
        }
    }
}