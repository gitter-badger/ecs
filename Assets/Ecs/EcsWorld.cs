using System;
using System.Collections.Generic;

namespace LeopotamGroup.Ecs {
    public sealed class EcsWorld {
        /// <summary>
        /// Raises after component was attached to entity.
        /// </summary>
        public event Action<IEcsComponent> OnComponentAttach = delegate { };

        /// <summary>
        /// Raises before component was detached to entity.
        /// </summary>
        public event Action<IEcsComponent> OnComponentDetach = delegate { };

        /// <summary>
        /// All registered systems.
        /// </summary>
        readonly List<EcsSystem> _allSystems = new List<EcsSystem> (64);

        /// <summary>
        /// Dictionary for fast search component -> type id.
        /// </summary>
        /// <returns></returns>
        readonly Dictionary<Type, int> _componentIds = new Dictionary<Type, int> (64);

        /// <summary>
        /// List of all entities (their components).
        /// </summary>
        readonly List<EcsEntity> _entities = new List<EcsEntity> (1024);

        /// <summary>
        /// List of removed entities - they can be reused later.
        /// </summary>
        readonly List<int> _reservedEntityIds = new List<int> (256);

        /// <summary>
        /// List of entities with added / removed components on them.
        /// </summary>
        readonly List<int> _dirtyEntityIds = new List<int> (256);

        /// <summary>
        /// Is Initialize method was called?
        /// </summary>
        bool _inited;

        /// <summary>
        /// Adds new system to processing.
        /// </summary>
        /// <param name="system">System instance.</param>
        public EcsWorld AddSystem (EcsSystem system) {
            if (_inited) {
                throw new Exception ("Already initialized, cant add new system.");
            }
            system.SetWorld (this);
            _allSystems.Add (system);
            return this;
        }

        /// <summary>
        /// Closes registration for new external data, initialize all registered systems.
        /// </summary>
        public void Initialize () {
            _inited = true;
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                _allSystems[i].Initialize ();
            }
        }

        /// <summary>
        /// Destroys all registered external data, full cleanup for internal data.
        /// </summary>
        public void Destroy () {
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                _allSystems[i].Destroy ();
            }
            _allSystems.Clear ();
            _componentIds.Clear ();
            _entities.Clear ();
            _reservedEntityIds.Clear ();
            _dirtyEntityIds.Clear ();
            // TODO: raise all destroy entities or suppress?
        }

        /// <summary>
        /// Processing for IEcsUpdateSystem systems.
        /// </summary>
        public void Update () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.Update ();
                }
                ProcessDirtyEntities ();
            }
        }

        /// <summary>
        /// Processing for IEcsFixedUpdateSystem systems.
        /// </summary>
        public void FixedUpdate () {
            for (int i = 0, iMax = _allSystems.Count; i < iMax; i++) {
                var updateSystem = _allSystems[i] as IEcsFixedUpdateSystem;
                if (updateSystem != null) {
                    updateSystem.FixedUpdate ();
                }
                ProcessDirtyEntities ();
            }
        }

        /// <summary>
        /// Creates new entity.
        /// </summary>
        public int CreateEntity () {
            int entity;
            if (_reservedEntityIds.Count > 0) {
                var id = _reservedEntityIds.Count - 1;
                entity = _reservedEntityIds[id];
                _reservedEntityIds.RemoveAt (id);
            } else {
                entity = _entities.Count;
                _entities.Add (new EcsEntity ());
            }
            return entity;
        }

        /// <summary>
        /// Removes exists entity or throws exception on invalid one.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveEntity (int entity) {
            if (entity < 0 || entity >= _entities.Count) {
                throw new Exception ("Invalid entity");
            }
            if (_reservedEntityIds.IndexOf (entity) != -1) {
                throw new Exception ("Entity already removed");
            }
            _reservedEntityIds.Add (entity);
            _entities[entity].DirtyMask = new ComponentMask ();
            _dirtyEntityIds.Add (entity);
        }

        /// <summary>
        /// Adds component to entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public T AddComponent<T> (int entity) where T : class, IEcsComponent {
            if (entity < 0 || entity >= _entities.Count) {
                throw new Exception ("Invalid entity");
            }
            var componentId = GetComponentId (typeof (T));
            var mask = new ComponentMask (componentId);
            var entityData = _entities[entity];
            if (ComponentMask.AreCompatible (ref entityData.Mask, ref mask)) {
                return entityData.Components[componentId] as T;
            }
            entityData.Mask.EnableBits (mask);
            entityData.DirtyMask.EnableBits (mask);
            // TODO: add pooling.
            var component = Activator.CreateInstance (typeof (T)) as T;
            while (entityData.Components.Count <= componentId) {
                entityData.Components.Add (null);
            }
            entityData.Components[componentId] = component;
            OnComponentAttach (component);
            return component;
        }

        /// <summary>
        /// Removes component from entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void RemoveComponent<T> (int entity) where T : class, IEcsComponent {
            if (entity < 0 || entity >= _entities.Count) {
                throw new Exception ("Invalid entity");
            }
            var componentId = GetComponentId (typeof (T));
            if (componentId != -1) {
                var entityData = _entities[entity];
                var mask = new ComponentMask (componentId);
                if (ComponentMask.AreCompatible (ref entityData.Mask, ref mask)) {
                    entityData.DirtyMask.DisableBits (mask);
                    _dirtyEntityIds.Add (entity);
                }
            }
        }

        /// <summary>
        /// For internal use only, dont call it directly.
        /// </summary>
        public string GetDebugStats () {
            return string.Format ("Components: {0}\nEntities: {1}", _componentIds.Count, _entities.Count);
        }

        /// <summary>
        /// Gets component index in EcsEntity.Components list.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        public int GetComponentId (Type componentType) {
            int retVal;
            if (!_componentIds.TryGetValue (componentType, out retVal)) {
                retVal = _componentIds.Count;
                _componentIds[componentType] = retVal;
            }
            return retVal;
        }

        void ProcessDirtyEntities () {
            var iMax = _dirtyEntityIds.Count;
            for (var i = 0; i < iMax; i++) {
                var entity = _dirtyEntityIds[i];
                var entityData = _entities[entity];
                if (!ComponentMask.AreEquals (ref entityData.Mask, ref entityData.DirtyMask)) {
                    var mask = entityData.Mask;
                    var dirty = entityData.DirtyMask;
                    while (!ComponentMask.AreEquals (ref mask, ref dirty)) {
                        // TODO: scan bits and recycle entityData.Components[bit] to pool + call OnComponentDetach(entity).
                    }
                }
            }
            if (_dirtyEntityIds.Count == iMax) {
                _dirtyEntityIds.Clear ();
            } else {
                _dirtyEntityIds.RemoveRange (0, iMax);
                ProcessDirtyEntities ();
            }
        }

        sealed class EcsEntity {
            public ComponentMask Mask = new ComponentMask ();

            public ComponentMask DirtyMask = new ComponentMask ();

            public readonly List<IEcsComponent> Components = new List<IEcsComponent> (8);
        }
    }
}