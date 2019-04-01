﻿using System;
using System.Threading.Tasks;
using NEventLite.Core;

namespace NEventLite.Storage
{
    public interface IEventSnapshotStorageProvider<TSnapshotKey, TAggregateKey> : ISnapshotStorageProvider<TSnapshotKey, TAggregateKey>
    {
        Task<ISnapshot<TSnapshotKey, TAggregateKey>> GetSnapshotAsync(Type aggregateType, TAggregateKey aggregateId, int version);
    }
}