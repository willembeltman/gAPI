using gAPI.EntityFrameworkDisk.DbContextExtenders;
using gAPI.EntityFrameworkDisk.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace gAPI.EntityFrameworkDisk
{
    public class DbContext
    {
        private readonly Dictionary<Type, IDbSet> DbSets;

        public DbContext(DirectoryInfo? directory = null)
        {
            directory ??= new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Database"));
            DbSets = [];

            var factory = DbContextExtenderCollection.GetOrCreate(this);
            factory.LoadDbSetsFromDirectory(this, directory);
        }

        public void AddDbSet(IDbSet dbSet)
        {
            var type = dbSet.GetType();
            if (DbSets.ContainsKey(type))
            {
                DbSets.Remove(type);
            }
            DbSets.Add(type, dbSet);
        }

        public void Clear()
        {
            foreach (var dbSet in DbSets.Values)
                dbSet.Clear();
        }
        public void SaveChanges()
        {
            foreach (var dbSet in DbSets.Values)
                dbSet.SaveChanges();
        }
        public IEnumerable<IDbSet> GetDbSets()
            => DbSets.Values;
        public IEnumerable<object> GetAllAddedEntities()
            => DbSets.Values
                .SelectMany(a => a.GetAddedEntities());
        public IEnumerable<ChangedEntityObject> GetAllChangedEntities()
            => DbSets.Values
                .SelectMany(a => a.GetChangedEntities());
        public IEnumerable<object> GetAllRemoveEntities()
            => DbSets.Values
                .SelectMany(a => a.GetRemoveEntities());
    }
}
