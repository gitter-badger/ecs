// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;

namespace LeopotamGroup.Ecs {
    /// <summary>
    /// Base system for multithreading processing.
    /// </summary>
    public abstract class EcsMultiThreadSystem : IEcsInitSystem, IEcsRunSystem {
        WorkerDesc[] _descs;

        ManualResetEvent[] _syncs;

        EcsWorld _world;

        EcsFilter _filter;

        Action<EcsMultiThreadJob> _worker;

        int _jobSize;

        void IEcsInitSystem.Destroy () {
            for (var i = 0; i < _descs.Length; i++) {
                var desc = _descs[i];
                _descs[i] = null;
                desc.Thread.Interrupt ();
                desc.Thread.Join (10);
                _syncs[i].Close ();
                _syncs[i] = null;
            }
            _world = null;
            _filter = null;
            _worker = null;
        }

        void IEcsInitSystem.Initialize () {
            _world = GetWorld ();
            _filter = GetFilter ();
            _worker = GetWorker ();
            _jobSize = GetJobSize ();
            var threadsCount = GetThreadsCount ();
#if DEBUG
            if (_world == null) {
                throw new Exception ("Invalid EcsWorld");
            }
            if (_filter == null) {
                throw new Exception ("Invalid EcsFilter");
            }
            if (_jobSize < 1) {
                throw new Exception ("Invalid JobSize");
            }
            if (threadsCount < 1) {
                throw new Exception ("Invalid ThreadsCount");
            }
            var hash = this.GetHashCode ();
#endif
            _descs = new WorkerDesc[threadsCount];
            _syncs = new ManualResetEvent[threadsCount];
            var job = new EcsMultiThreadJob ();
            job.World = _world;
            job.Entities = _filter.Entities;
            for (var i = 0; i < _descs.Length; i++) {
                var desc = new WorkerDesc ();
                desc.Job = job;
                desc.Thread = new Thread (ThreadProc);
#if DEBUG
                desc.Thread.Name = string.Format ("ECS-{0:X}-{1}", hash, i);
#endif
                desc.HasWork = new ManualResetEvent (false);
                desc.WorkDone = new ManualResetEvent (true);
                desc.Worker = _worker;
                _descs[i] = desc;
                _syncs[i] = desc.WorkDone;
                desc.Thread.Start (desc);
            }
        }

        void IEcsRunSystem.Run () {
            var count = _filter.Entities.Count;
            // no need to use threads on short tasks.
            if (count < _jobSize * 2) {
                _descs[0].Job.From = 0;
                _descs[0].Job.To = count;
                _worker (_descs[0].Job);
            } else {
                var processed = 0;
                var workerId = 0;
                while (processed < count) {
                    if (workerId < _descs.Length) {
                        _descs[workerId].Job.From = processed;
                        var size = count - processed;
                        if (size > _jobSize) {
                            size = _jobSize;
                        }
                        processed += size;
                        _descs[workerId].Job.To = processed;
                        _descs[workerId].WorkDone.Reset ();
                        _descs[workerId].HasWork.Set ();
                        workerId++;
                    } else {
                        workerId = WaitHandle.WaitAny (_syncs);
                        _syncs[workerId].Reset ();
                    }
                }
                WaitHandle.WaitAll (_syncs);
            }
        }

        void ThreadProc (object rawDesc) {
            WorkerDesc desc = (WorkerDesc) rawDesc;
            try {
                while (Thread.CurrentThread.IsAlive) {
                    desc.HasWork.WaitOne ();
                    desc.HasWork.Reset ();
                    desc.Worker (desc.Job);
                    desc.WorkDone.Set ();
                }
            } catch { }
        }

        /// <summary>
        /// EcsWorld instance to use in custom worker.
        /// </summary>
        protected abstract EcsWorld GetWorld ();

        /// <summary>
        /// Source filter for processing entities from it.
        /// </summary>
        protected abstract EcsFilter GetFilter ();

        /// <summary>
        /// Custom processor of received entities.
        /// </summary>
        protected abstract Action<EcsMultiThreadJob> GetWorker ();

        /// <summary>
        /// Amount of entities to process by one worker.
        /// </summary>
        protected abstract int GetJobSize ();

        /// <summary>
        /// How many threads should be used by this system.
        /// </summary>
        protected abstract int GetThreadsCount ();

        sealed class WorkerDesc {
            public Thread Thread;
            public ManualResetEvent HasWork;
            public ManualResetEvent WorkDone;
            public Action<EcsMultiThreadJob> Worker;
            public EcsMultiThreadJob Job;
        }
    }

    /// <summary>
    /// Job info for multithreading processing.
    /// </summary>
    public struct EcsMultiThreadJob {
        /// <summary>
        /// EcsWorld instance.
        /// </summary>
        public EcsWorld World;

        /// <summary>
        /// Entities list to processing.
        /// </summary>
        public List<int> Entities;

        /// <summary>
        /// Index of first entity in list to processing.
        /// </summary>
        public int From;

        /// <summary>
        /// Index of entity after last item to processing (should be excluded from processing).
        /// </summary>
        public int To;
    }
}