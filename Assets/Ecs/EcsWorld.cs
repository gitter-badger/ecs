using System;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsWorld {
        readonly List<EcsSystem> _allSystems = new List<EcsSystem> (64);

        readonly Dictionary<Type, ComponentMask> _componentMasks = new Dictionary<Type, ComponentMask> (64);

        readonly List<IEcsComponent[]> _entities = new List<IEcsComponent[]> (1024);

        readonly List<int> _reservedEntities = new List<int> (256);

        bool _inited;

        public EcsWorld AddSystem (EcsSystem system) {
            system.SetWorld (this);
            _allSystems.Add (system);
            return this;
        }

        public void Initialize () {
            _inited = true;
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                _allSystems[i].Initialize ();
            }
        }

        public void Destroy () {
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                _allSystems[i].Destroy ();
            }
            _allSystems.Clear ();
            _componentMasks.Clear ();
            _entities.Clear ();
            _reservedEntities.Clear ();
        }

        public void Update () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.Update ();
                }
            }
        }

        public void FixedUpdate () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsFixedUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.FixedUpdate ();
                }
            }
        }

        public int CreateEntity () {
            int entity;
            if (_reservedEntities.Count > 0) {
                var id = _reservedEntities.Count - 1;
                entity = _reservedEntities[id];
                _reservedEntities.RemoveAt (id);
            } else {
                entity = _entities.Count;
                _entities.Add (new IEcsComponent[_componentMasks.Count]);
            }
            return entity;
        }

        public void RemoveEntity (int entity) {
            if (entity < 0 || entity >= _entities.Count) {
                throw new Exception ("Invalid entity");
            }
            if (_reservedEntities.IndexOf (entity) != -1) {
                throw new Exception ("Entity already removed");
            }
            _reservedEntities.Add (entity);
        }

        public T AddComponent<T> () where T : IEcsComponent {
            throw new NotImplementedException ();
        }

        public void RemoveComponent<T> () where T : IEcsComponent {
            throw new NotImplementedException ();
        }

        public ComponentMask GetComponentsMask (Type[] types) {
            var mask = new ComponentMask ();
            if (types != null) {
                for (var i = 0; i < types.Length; i++) {
                    mask |= GetComponentMask (types[i]);
                }
            }
            return mask;
        }

        public string GetDebugStats () {
            return string.Format ("components: {0}, entities: {1}", _componentMasks.Count, _entities.Count);
        }

        ComponentMask GetComponentMask (Type type) {
            if (!typeof (IEcsComponent).IsAssignableFrom (type)) {
                throw new Exception ("Invalid component");
            }
            ComponentMask retVal;
            if (!_componentMasks.TryGetValue (type, out retVal)) {
                if (!_inited) {
                    retVal = ComponentMask.Create (_componentMasks.Count);
                    _componentMasks[type] = retVal;
                } else {
                    // all systems initialized, no need to add new component types.
                    retVal = new ComponentMask ();
                }
            }
            return retVal;
        }
    }
}