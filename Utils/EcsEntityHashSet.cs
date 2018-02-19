// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;

namespace LeopotamGroup.Ecs.Internals {
    /// <summary>
    /// HashSet implementation with performance tweaks.
    /// </summary>
    sealed class EcsEntityHashSet {
        const int MinCapacity = 8;

        struct Entry {
            public int hashCode; // Lower 31 bits of hash code, -1 if unused
            public int next; // Index of next entry, -1 if last
        }

        int[] _buckets;

        Entry[] _entries;

        int _count;

        int _freeList;

        int _freeCount;

        public EcsEntityHashSet (int capacity = 0) {
            if (capacity < MinCapacity) {
                capacity = MinCapacity;
            }
            var size = EcsHelpers.GetPowerOfTwoSize (capacity);
            _buckets = new int[size];
            for (var i = 0; i < _buckets.Length; i++) {
                _buckets[i] = -1;
            }
            _entries = new Entry[size];
            _freeList = -1;
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public void Clear () {
            if (_count > 0) {
                for (var i = 0; i < _buckets.Length; i++) {
                    _buckets[i] = -1;
                }
                Array.Clear (_entries, 0, _count);
                _freeList = -1;
                _count = 0;
                _freeCount = 0;
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public bool Contains (int key) {
            unchecked {
                for (var i = _buckets[key % _buckets.Length]; i >= 0; i = _entries[i].next) {
                    if (_entries[i].hashCode == key) {
                        return true;
                    }
                }
                return false;
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public bool Add (int key) {
            unchecked {
                var targetBucket = key % _buckets.Length;
                for (var i = _buckets[targetBucket]; i >= 0; i = _entries[i].next) {
                    if (_entries[i].hashCode == key) {
                        return false;
                    }
                }
                int index;
                if (_freeCount > 0) {
                    index = _freeList;
                    _freeList = _entries[index].next;
                    _freeCount--;
                } else {
                    if (_count == _entries.Length) {
                        var newSize = _count << 1;
                        var newBuckets = new int[newSize];
                        for (int ii = 0; ii < newBuckets.Length; ii++) {
                            newBuckets[ii] = -1;
                        }
                        var newEntries = new Entry[newSize];
                        Array.Copy (_entries, 0, newEntries, 0, _count);
                        for (var ii = 0; ii < _count; ii++) {
                            if (newEntries[ii].hashCode >= 0) {
                                var bucket = newEntries[ii].hashCode % newSize;
                                newEntries[ii].next = newBuckets[bucket];
                                newBuckets[bucket] = ii;
                            }
                        }
                        _buckets = newBuckets;
                        _entries = newEntries;
                        targetBucket = key % _buckets.Length;
                    }
                    index = _count;
                    _count++;
                }

                _entries[index].hashCode = key;
                _entries[index].next = _buckets[targetBucket];
                _buckets[targetBucket] = index;
                return true;
            }
        }

#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public bool Remove (int key) {
            unchecked {
                var bucket = key % _buckets.Length;
                var last = -1;
                for (var i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].next) {
                    if (_entries[i].hashCode == key) {
                        if (last < 0) {
                            _buckets[bucket] = _entries[i].next;
                        } else {
                            _entries[last].next = _entries[i].next;
                        }
                        _entries[i].hashCode = -1;
                        _entries[i].next = _freeList;
                        _freeList = i;
                        _freeCount++;
                        return true;
                    }
                }
                return false;
            }
        }
    }
}