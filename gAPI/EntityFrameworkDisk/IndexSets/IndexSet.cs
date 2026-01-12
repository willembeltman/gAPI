using gAPI.EntityFrameworkDisk.EntityDefinitions;
using gAPI.EntityFrameworkDisk.Interfaces;
using gAPI.EntityFrameworkDisk.Locks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace gAPI.EntityFrameworkDisk.IndexSets;

public class IndexSet<T> : IIndexSet<T>
{
    private const int BufferSize = 1024;
    private readonly ReaderWriterLockSlim Lock;
    private readonly EntityDefinition EntityDefinition;
    private readonly string InfoFullName;
    private readonly string IndexFullName;
    private long? LastIdNumber;

    internal IndexSet(DirectoryInfo directory)
    {
        var entityType = typeof(T);
        IndexFullName = Path.Combine(directory.FullName, entityType.Name + ".index");
        InfoFullName = Path.Combine(directory.FullName, entityType.Name + ".info");
        Lock = LockCollection.GetOrCreate<IndexSet<T>>();
        EntityDefinition = EntityDefinitionCollection.GetInstance<T>();

        // Guard
        if (KeyType != null &&
            KeyType != typeof(int) &&
            KeyType != typeof(long) &&
            KeyType != typeof(string) &&
            KeyType != typeof(Guid))
        {
            throw new NotSupportedException($"keyType is of type '{KeyType.Name}', " +
                $"which is not supported, please use int, long, string or guid for " +
                $"primary keys, or don't use primary keys at all. Otherwise contact " +
                $"me on github.");
        }

        LoadInfo();
    }

    public long Count { get; private set; }
    public Type? KeyType => EntityDefinition.KeyType;
    public string? KeyName => EntityDefinition.KeyName;
    public object? GetKeyValue(T item) => EntityDefinition.GetKeyValue(item);

    private IEnumerable<IndexItem> GetEnumerable()
    {
        try
        {
            Lock.EnterReadLock();

            using (var indexStream = File.OpenRead(IndexFullName))
            using (var indexReader = new BinaryReader(indexStream))
            {
                indexStream.Position = 0;

                var endOfFile = false;
                var indexPosition = 0L;
                var dataPosition = 0L;
                bool deleted = false;
                IndexItem[] buffer = new IndexItem[BufferSize];
                int readIndex = 0;

                if (KeyType != null)
                {
                    object id = new object();
                    byte[] idGuidBuffer = new byte[16];

                    while (!endOfFile)
                    {
                        readIndex = 0;
                        while (readIndex < BufferSize)
                        {
                            indexPosition = indexStream.Position;

                            // Section 0: Deleted inlezen
                            if (indexStream.Position >= indexStream.Length) { endOfFile = true; break; }
                            deleted = indexReader.ReadBoolean();

                            // Section 1: Id inlezen
                            if (indexStream.Position >= indexStream.Length) { endOfFile = true; break; }
                            id = ReadPrimaryKeyValue(indexStream, indexReader, id, idGuidBuffer);

                            // Section 2: Data position inlezen
                            if (indexStream.Position >= indexStream.Length) { endOfFile = true; break; }
                            dataPosition = indexReader.ReadInt64();

                            // Skip deleted
                            if (deleted) continue;

                            // Store in buffer
                            var newItem = new IndexItem(id, indexPosition, dataPosition); ;
                            buffer[readIndex] = newItem;

                            readIndex++;
                        }

                        for (var i = 0; i < readIndex; i++)
                        {
                            var item = buffer[i];
                            yield return item;
                        }
                    }
                }
                else
                {
                    while (!endOfFile)
                    {
                        readIndex = 0;
                        while (readIndex < BufferSize)
                        {
                            indexPosition = indexStream.Position;

                            // Section 0: Deleted inlezen
                            if (indexStream.Position >= indexStream.Length) { endOfFile = true; break; }
                            deleted = indexReader.ReadBoolean();

                            // Section 1: Data position inlezen
                            if (indexStream.Position >= indexStream.Length) { endOfFile = true; break; }
                            dataPosition = indexReader.ReadInt64();

                            // Skip deleted
                            if (deleted) continue;

                            // Store in buffer
                            buffer[readIndex] = new IndexItem(null, indexPosition, dataPosition);

                            readIndex++;
                        }

                        for (var i = 0; i < readIndex; i++)
                        {
                            yield return buffer[i];
                        }
                    }
                }
            }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    public IEnumerable<IndexItem> FindRange(IEnumerable<T> range)
    {
        if (KeyType == null)
            throw new NotSupportedException("You cannot get the Index by Key if " +
                "there is no Key defined. Quit messing with my code ;)");

        var rangeKeys = range
            .Select(a => EntityDefinition.GetKeyValue(a)!);

        return FindRange(rangeKeys);
    }
    public IEnumerable<IndexItem> FindRange(IEnumerable<object> keys)
    {
        if (KeyType == null)
            throw new NotSupportedException("You cannot get the Index by Key if " +
                "there is no Key defined. Quit messing with my code ;)");

        var stringKeys = keys
            .Select(a => a?.ToString())
            .ToArray();

        var indexItems = new IndexItem[stringKeys.Length];

        foreach (var item in GetEnumerable())
        {
            for (var i = 0; i < stringKeys.Length; i++)
            {
                if (item.Key != null && stringKeys[i] == item.Key.ToString())
                {
                    indexItems[i] = item;
                    break;
                }
            }

            if (!indexItems.Any(a => a == null))
                break;
        }

        return indexItems;
    }

    public IndexItem? Find(T item)
    {
        if (KeyType == null)
            throw new NotSupportedException("You cannot get the Index by Key if " +
                "there is no Key defined. Quit messing with my code ;)");

        if (item == null)
            return null;

        var keyValue = EntityDefinition.GetKeyValue(item);
        if (keyValue == null)
            return null;

        return Find(keyValue);
    }
    public IndexItem? Find(object keyValue)
    {
        if (KeyType == null)
            throw new NotSupportedException("You cannot get the Index by Key if " +
                "there is no Key defined. Quit messing with my code ;)");

        foreach (var item in GetEnumerable())
        {
            if (item.Key != null && item.Key.ToString() == keyValue.ToString())
            {
                return item;
            }
        }
        return null;
    }
    public void Update(List<ChangedEntity<T>> changedEntities, Func<T, long> AddDataCallback)
    {
        if (!changedEntities.Any()) return;

        var keys = changedEntities.Select(a => a.KeyValue);
        var range = FindRange(keys);
        RemoveRange(range);

        try
        {
            Lock.EnterWriteLock();

            // Open stream
            using (var indexstream = File.Open(IndexFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var indexwriter = new BinaryWriter(indexstream))
            {
                indexstream.Position = indexstream.Length;

                foreach (var item in changedEntities)
                {
                    // Store index positions
                    var indexPosition = indexstream.Position;
                    var dataPosition = AddDataCallback(item.Item);

                    // Write new index item
                    WriteToIndexStream(indexstream, indexwriter, item.KeyValue, dataPosition);

                    // Update info
                    Count++;
                }

                SaveInfo();
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public void Add(T item, long dataPosition)
    {
        if (item == null)
            throw new ArgumentNullException("item");
        try
        {
            Lock.EnterWriteLock();

            // Open stream
            using (var indexstream = File.Open(IndexFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var indexwriter = new BinaryWriter(indexstream))
            {
                indexstream.Position = indexstream.Length;

                // If the Key is known, get/update new keys
                var keyValue = GetOrSetNumberKey(item);

                // Store index position
                var indexPosition = indexstream.Position;

                // Write new index item
                WriteToIndexStream(indexstream, indexwriter, keyValue, dataPosition);

                // Update info
                Count++;
                SaveInfo();
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public void AddRange(IEnumerable<T> range, Func<T, long> AddDataCallback)
    {
        try
        {
            Lock.EnterWriteLock();

            // Open stream
            using (var indexstream = File.Open(IndexFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var indexwriter = new BinaryWriter(indexstream))
            {
                indexstream.Position = indexstream.Length;

                foreach (var item in range)
                {
                    // If the Key is known, get/update new keys
                    var keyValue = GetOrSetNumberKey(item);

                    // Store index positions
                    var indexPosition = indexstream.Position;
                    var dataPosition = AddDataCallback(item); // item now contains the new key, if needed

                    // Write new index item
                    WriteToIndexStream(indexstream, indexwriter, keyValue, dataPosition);

                    // Update info
                    Count++;
                }

                SaveInfo();
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public bool Remove(IndexItem item)
    {
        try
        {
            Lock.EnterWriteLock();

            // Open stream
            using (var indexstream = File.Open(IndexFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var indexwriter = new BinaryWriter(indexstream))
            {
                indexstream.Position = item.IndexPosition;

                // Write PrimaryKey item
                indexwriter.Write(true); // Deleted!
                Count--;

                return true;
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public bool RemoveRange(IEnumerable<IndexItem> indexes)
    {
        try
        {
            Lock.EnterWriteLock();

            var removedAll = true;

            // Open stream
            using (var indexstream = File.Open(IndexFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var indexwriter = new BinaryWriter(indexstream))
            {
                foreach (var item in indexes)
                {
                    if (item == null)
                    {
                        removedAll = false;
                        continue;
                    }

                    indexstream.Position = item.IndexPosition;

                    // Write PrimaryKey item
                    indexwriter.Write(true); // Deleted!
                    Count--;
                }

                return removedAll;
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public void Clear()
    {
        try
        {
            Lock.EnterWriteLock();

            if (File.Exists(IndexFullName))
                File.Delete(IndexFullName);
            if (File.Exists(InfoFullName))
                File.Delete(InfoFullName);

            Count = 0;
            if (KeyType == null)
                LastIdNumber = null;
            else
                LastIdNumber = 0;
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    private void WriteToIndexStream(FileStream indexstream, BinaryWriter indexwriter, object? keyValue, long dataPosition)
    {
        if (KeyType != null && keyValue == null)
            throw new ArgumentNullException($"For not auto increment keys you cannot use null. Please set your key field.");

        indexwriter.Write(false); // Not deleted
        if (KeyType != null)
        {
            WritePrimaryKeyValue(indexstream, indexwriter, keyValue!); // Id
        }
        indexwriter.Write(dataPosition); // Data position
    }
    private object? GetOrSetNumberKey(T item)
    {
        if (item == null) return null;

        object? keyValue = null;
        if (KeyType != null)
        {
            if (KeyType == typeof(int) || KeyType == typeof(long))
            {
                LastIdNumber = (LastIdNumber ?? 0) + 1;
                if (KeyType == typeof(int))
                {
                    keyValue = Convert.ToInt32(LastIdNumber);
                }
                else
                {
                    keyValue = Convert.ToInt64(LastIdNumber);
                }
                EntityDefinition.SetKeyValue(item, keyValue);
            }
            else
            {
                keyValue = EntityDefinition.GetKeyValue(item);
            }

            if (keyValue == null)
                throw new Exception("Your primary key appears to be null, which is not possible. I dont know...");
        }

        return keyValue;
    }

    private void LoadInfo()
    {
        if (!File.Exists(InfoFullName))
        {
            if (KeyType == typeof(int) || KeyType == typeof(long))
                LastIdNumber = 0;
            else
                LastIdNumber = null;
            return;
        }

        using (var stream = File.OpenRead(InfoFullName))
        using (var reader = new BinaryReader(stream))
        {
            Count = reader.ReadInt64();

            if (KeyType != typeof(int) && KeyType != typeof(long))
                return;

            if (KeyType == typeof(int))
                LastIdNumber = reader.ReadInt32();
            if (KeyType == typeof(long))
                LastIdNumber = reader.ReadInt64();
        }
    }
    private void SaveInfo()
    {
        using (var stream = File.Open(InfoFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Count);

            if (KeyType != typeof(int) && KeyType != typeof(long) || LastIdNumber == null)
                return;

            if (KeyType == typeof(int))
                writer.Write(Convert.ToInt32(LastIdNumber.Value));
            if (KeyType == typeof(long))
                writer.Write(LastIdNumber.Value);
        }
    }

    private object ReadPrimaryKeyValue(FileStream indexStream, BinaryReader indexReader, object tempId, byte[] idGuidBuffer)
    {
        if (KeyType == typeof(int))
        {
            tempId = indexReader.ReadInt32();
        }
        else if (KeyType == typeof(long))
        {
            tempId = indexReader.ReadInt64();
        }
        else if (KeyType == typeof(string))
        {
            tempId = indexReader.ReadString();
        }
        else if (KeyType == typeof(Guid))
        {
            tempId = new Guid(indexReader.ReadBytes(16));
        }
        else
        {
            throw new NotSupportedException("Not supported, check constructor if the types above match the type gaurd.");
        }
        return tempId;
    }
    private void WritePrimaryKeyValue(FileStream indexStream, BinaryWriter indexWriter, object id)
    {
        if (KeyType == typeof(int))
        {
            indexWriter.Write((int)id);
        }
        else if (KeyType == typeof(long))
        {
            indexWriter.Write((long)id);
        }
        else if (KeyType == typeof(string))
        {
            indexWriter.Write((string)id);
        }
        else if (KeyType == typeof(Guid))
        {
            indexWriter.Write(((Guid)id).ToByteArray());
        }
        else
        {
            throw new NotSupportedException("Not supported, check constructor if the types above match the type gaurd.");
        }
    }

    public IEnumerator<IndexItem> GetEnumerator() => GetEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}
