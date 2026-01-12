using gAPI.EntityFrameworkDisk.Models;
using gAPI.EntityFrameworkDisk.Navigators.Extenders;
using gAPI.Helpers;

namespace gAPI.EntityFrameworkDisk.Navigator;

internal static class NavigatorFactory<T>
{
    public static Navigator<T> CreateInstance(DbContext dbContext)
    {
        var dbContextType = dbContext.GetType();
        var dbContextModel = DbContextModelCollection.GetOrCreate(dbContextType);

        var dbSetModel = dbContextModel.GetDbSet(typeof(T));

        var className = $"{dbSetModel!.Name}EntityExtender";
        var extendMethodName = "Extend";
        var findForeignKeyUsageMethodName = "EntityFindForeignKeyUsage";

        var Code = $@"
            using System;
            using System.IO;
            using System.Linq;

            public static class {className}
            {{
                {GenerateExtendCode(dbContextModel, dbSetModel, extendMethodName, dbContext)}
                {GenerateForeignKeyUsageCode(dbContextModel, dbSetModel, findForeignKeyUsageMethodName, dbContext)}
            }}";

        var asm = CodeCompiler.Compile(Code);
        var serializerType = asm.GetType(className);

        var extendEntityMethod = serializerType!.GetMethod(extendMethodName);
        var findForeignKeyUsageMethod = serializerType.GetMethod(findForeignKeyUsageMethodName);

        var ExtendDelegate = (Action<T, DbContext>)Delegate.CreateDelegate(
            typeof(Action<T, DbContext>), extendEntityMethod!);

        var FindForeignKeyUsageDelegate = (Func<T, DbContext, bool, bool>)Delegate.CreateDelegate(
            typeof(Func<T, DbContext, bool, bool>), findForeignKeyUsageMethod!);

        return new Navigator<T>(ExtendDelegate, FindForeignKeyUsageDelegate, Code);
    }

    private static string GenerateForeignKeyUsageCode(DbContextModel dbContextModel, DbSetModel dbSetModel, string methodName, DbContext dbContext)
    {
        var className = dbSetModel.Type.Name;
        var fullClassName = dbSetModel.Type.FullName;

        var codeRemoveIfFound = string.Empty;
        var codeExceptionIfFound = string.Empty;

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        //var applicationDbContextType = dbContext.GetType();
        //var applicationDbContextTypeFullName = applicationDbContextType.FullName;
        //if (applicationDbContextTypeFullName == null)
        //    throw new Exception("Cannot find dbcontext type");

        //if (ReflectionHelper.IsIEntity(dbSet))
        //{
        //    var applicationDbContextProps = applicationDbContextType.GetProperties();

        //    var listIndex = 0;
        //    foreach (var applicationDbContextProp in applicationDbContextProps)
        //    {
        //        var applicationDbContextPropType = applicationDbContextProp.PropertyType;
        //        if (!ReflectionHelper.IsDbSet(applicationDbContextProp)) continue;

        //        var entityType = ReflectionHelper.GetDbSetType(applicationDbContextPropType);
        //        if (entityType == null) continue;
        //        if (entityType == dbSet) continue;

        //        var dbSetName = applicationDbContextProp.Name;
        //        GenerateForeignKeyUsageCode_GenerateForEntity(dbSet, entityType, dbSetName, applicationDbContextTypeFullName,
        //            ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex);
        //    }
        //}

        var listIndex = 0;
        foreach (var otherDbSet in dbSetModel.DbContext.DbSets)
        {
            if (otherDbSet == dbSetModel) continue;

            GenerateForeignKeyUsageCode_GenerateForEntity(dbContextModel, dbSetModel, otherDbSet,
                ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex);
        }

        return $@"
                public static bool {methodName}({fullClassName} item, {dbContextTypeFullName} objDb, bool removeIfFound)
                {{
                    var db = objDb as {dbContextModel.FullName};
                    if (db == null) throw new Exception(""dbContext is not of type {dbContextModel.FullName}"");
                    var res = false;
                    if (removeIfFound)
                    {{{codeRemoveIfFound}
                    }}
                    else
                    {{{codeExceptionIfFound}
                    }}
                    return res;
                }}
                ";
    }
    private static void GenerateForeignKeyUsageCode_GenerateForEntity(
        DbContextModel dbContextModel,
        DbSetModel dbSetModel,
        DbSetModel otherDbSetModel,
        ref string codeRemoveIfFound, ref string codeExceptionIfFound, ref int listIndex,
        List<DbSetModel>? doneTypes = null, string baseProperty = "", string basePropertyNull = "")
    {
        //if (!ReflectionHelper.IsIEntity(dbSetModel)) return;

        if (doneTypes == null)
        {
            doneTypes = new List<DbSetModel>();
        }
        else
        {
            if (doneTypes.Contains(otherDbSetModel)) return;
        }
        doneTypes.Add(otherDbSetModel);

        var entityProps = otherDbSetModel.Entity.Properties;
        foreach (var entityProp in entityProps)
        {
            var propertyName = entityProp.Name;
            //if (!ReflectionHelper.HasPublicGetter(entityProp)) continue;
            //if (!ReflectionHelper.HasPublicSetter(entityProp)) continue;
            if (entityProp.IsReadOnly) continue;

            //if (ReflectionHelper.HasNavigationProperties(entityProp.PropertyType) &&
            //    ReflectionHelper.IsIEntity(entityProp.PropertyType))
            //{
            //    GenerateForeignKeyUsageCode_GenerateForEntity(dbSetModel, entityProp.PropertyType, dbSetName, applicationDbContextTypeFullName,
            //        ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex,
            //        doneTypes, baseProperty + $".{entityProp.Name}", basePropertyNull + $".{entityProp.Name}?");
            //    continue;
            //}
            //if (!ReflectionHelper.IsNavigationEntityProperty(entityProp)) continue;

            if (entityProp.NavigationItem == null || entityProp.NavigationDbSet == null) continue;
            if (entityProp.NavigationDbSet.Entity.Properties.Any(a => a.NavigationItem != null))
            {
                GenerateForeignKeyUsageCode_GenerateForEntity(
                    dbContextModel, dbSetModel, entityProp.NavigationDbSet,
                    ref codeRemoveIfFound, ref codeExceptionIfFound, ref listIndex,
                    doneTypes, baseProperty + $".{entityProp.Name}", basePropertyNull + $".{entityProp.Name}?");
                continue;
            }



            //var foreignType = ReflectionHelper.GetILazyType(entityProp);

            //var foreignKeyName = $"{propertyName}Id";
            ////if (ReflectionHelper.HasForeignKeyAttribute(entityProp))
            ////    foreignKeyName = ReflectionHelper.GetForeignKeyAttributeName(entityProp);
            //if (entityProp.HasForeignKeyAttribute)
            //    foreignKeyName = entityProp.ForeignKeyAttributeName;

            var foreignType = entityProp.NavigationDbSet;
            var foreignKeyName = entityProp.NavigationItem.Name;

            if (foreignType != dbSetModel) continue;

            //var foreignProperty = otherDbSetModel.GetProperties()
            //    .FirstOrDefault(a => a.Name == foreignKeyName)
            //    ?? throw new Exception($"ZipDatabase Exception: Foreign key property {foreignKeyName} not found on {dbSetModel.Name}.");

            var foreignProperty = entityProp.NavigationItem;

            var defaultvalue = foreignProperty.IsNullable ? "null" : "0";

            var otherDbSetName = otherDbSetModel.Name;

            listIndex++;
            codeRemoveIfFound += $@"
                        var list{otherDbSetName}{listIndex} = db.{otherDbSetName}.Where(a => a{basePropertyNull}.{foreignKeyName} == item.Id);
                        foreach (var item{listIndex} in list{otherDbSetName}{listIndex})
                        {{
                            res = true;
                            item{listIndex}{baseProperty}.{foreignKeyName} = {defaultvalue};
                        }}";
            codeExceptionIfFound += $@"
                        if (db.{otherDbSetName}.Any(a => a{basePropertyNull}.{foreignKeyName} == item.Id)) 
                            throw new Exception(
                                ""Cannot delete {dbSetModel.Entity.FullName}, id #"" + item.Id + 
                                "", from {dbSetModel.Name}. {dbContextModel.FullName}.{otherDbSetName}{baseProperty}.{foreignKeyName} has a reference towards it. Please remove the reference."");";
        }
    }

    private static string GenerateExtendCode(DbContextModel dbContextModel, DbSetModel dbSetModel, string methodName, DbContext dbContext)
    {
        var className = dbSetModel.Type.Name;
        var fullClassName = dbSetModel.Type.FullName!;

        var lazyCode = string.Empty;

        var foreignEntityCollectionNotNullType = typeof(LazyEntityCollectionNotNull<,>);
        var foreignEntityCollectionNotNullFullName = foreignEntityCollectionNotNullType.FullName!.Split('`').First();

        var foreignEntityCollectionNullType = typeof(LazyEntityCollectionNull<,>);
        var foreignEntityCollectionNullFullName = foreignEntityCollectionNullType.FullName!.Split('`').First();

        var foreignEntityLazyNotNullType = typeof(LazyEntityNotNull<,>);
        var foreignEntityLazyNotNullFullName = foreignEntityLazyNotNullType.FullName!.Split('`').First();

        var foreignEntityLazyNullType = typeof(LazyEntityNull<,>);
        var foreignEntityLazyNullFullName = foreignEntityLazyNullType.FullName!.Split('`').First();

        var entityFactoryType = typeof(NavigatorFactory<>);
        var entityFactoryFullName = entityFactoryType.FullName!.Split('`').First();

        var dbContextType = typeof(DbContext);
        var dbContextTypeFullName = dbContextType.FullName;

        var navigatorCollectionType = typeof(NavigatorCollection);
        var navigatorCollectionTypeFullName = navigatorCollectionType.FullName;
        var navigatorCollectionTypeMethod = navigatorCollectionType.GetMethods().First().Name;

        var applicationDbContextType = dbContext.GetType();
        var applicationDbContextTypeFullName = applicationDbContextType.FullName;

        var props = dbSetModel.Entity.Properties;
        foreach (var prop in props)
        {
            var propertyName = prop.Name;
            //if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            //if (!ReflectionHelper.HasPublicSetter(prop)) continue;
            //if (ReflectionHelper.HasNavigationProperties(prop.PropertyType))
            //{
            //    lazyCode += $@"
            //    if (item.{propertyName} != null)
            //    {{
            //        var {propertyName}EntityFactory = {navigatorCollectionTypeFullName}.{navigatorCollectionTypeMethod}<{prop.PropertyType.FullName}>(db);
            //        {propertyName}EntityFactory.SetNavigationProperties(item.{propertyName}, db);
            //    }}";
            //    continue;
            //}
            if (prop.IsReadOnly) continue;
            //if (prop.NavigationDbSet != null &&
            //    prop.NavigationDbSet.Entity.Properties.Any(a => a.NavigationDbSet != null))
            //{
            //    lazyCode += $@"
            //        if (item.{propertyName} != null)
            //        {{
            //            var {propertyName}EntityFactory = {navigatorCollectionTypeFullName}.{navigatorCollectionTypeMethod}<{prop.Entity.FullName}>(db);
            //            {propertyName}EntityFactory.SetNavigationProperties(item.{propertyName}, db);
            //        }}";
            //    continue;
            //}

            //if (!ReflectionHelper.IsNavigationProperty(prop)) continue;
            if (prop.NavigationDbSet == null) continue;

            //if (ReflectionHelper.IsNavigationCollectionProperty(prop))
            if (prop.NavigationList != null)
            {
                GenerateExtendCode_NavigationLists(dbContextModel, dbSetModel, className, fullClassName, ref lazyCode, foreignEntityCollectionNotNullFullName, foreignEntityCollectionNullFullName, applicationDbContextType, prop, propertyName);
            }
            //else if (ReflectionHelper.IsNavigationEntityProperty(prop))
            else if (prop.NavigationItem != null)
            {
                GenerateExtendCode_NavigationItems(dbContextModel, dbSetModel, fullClassName, ref lazyCode, foreignEntityLazyNotNullFullName, foreignEntityLazyNullFullName, applicationDbContextType, prop, propertyName);
            }
        }

        return $@"
                public static void {methodName}({fullClassName} item, {dbContextTypeFullName} objDb)
                {{
                    var db = objDb as {applicationDbContextTypeFullName};
                    if (db == null) throw new Exception(""dbContext is not of type {applicationDbContextTypeFullName}"");
                    {lazyCode}
                }}
                ";
    }
    private static void GenerateExtendCode_NavigationItems(
        DbContextModel dbContextModel, DbSetModel dbSetModel, string fullClassName, ref string lazyCode,
        string foreignEntityLazyNotNullFullName, string foreignEntityLazyNullFullName,
        Type applicationDbContextType, EntityPropertyModel entityProp, string propertyName)
    {
        //var foreignType = ReflectionHelper.GetILazyType(prop);
        //var foreignKeyName = $"{propertyName}Id";
        //if (ReflectionHelper.HasForeignKeyAttribute(prop))
        //{
        //    foreignKeyName = ReflectionHelper.GetForeignKeyAttributeName(prop);
        //}
        if (entityProp.NavigationDbSet == null) return;
        if (entityProp.NavigationItem == null) return;
        if (entityProp.PrimaryKey == null) return;
        var foreignType = entityProp.NavigationDbSet;
        var foreignKeyName = entityProp.NavigationItem.Name;


        //var foreignProperty = dbSetModel.GetProperties()
        //    .FirstOrDefault(a => a.Name == foreignKeyName)
        //    ?? throw new Exception($"ZipDatabase Exception: Foreign key property {foreignKeyName} not found on {dbSetModel.Name}.");

        //var lazyPropertyOnApplicationDbContext = applicationDbContextType.GetProperties()
        //    .Where(a => ReflectionHelper.IsDbSet(a))
        //    .FirstOrDefault(a => ReflectionHelper.GetDbSetType(a) == foreignType);
        //if (lazyPropertyOnApplicationDbContext == null) return;

        var foreignProperty = entityProp.NavigationItem;
        var lazyPropertyOnApplicationDbContext = entityProp.NavigationDbSet;

        var lazyPropertyOnApplicationDbContextName = lazyPropertyOnApplicationDbContext.Name;

        var primaryKeyName = entityProp.PrimaryKey.Name;
        var foreignKeyTypeName = entityProp.NavigationItem.TypeSimpleName;

        //if (ReflectionHelper.IsNulleble(foreignProperty))
        if (foreignProperty.IsNullable)
        {
            lazyCode += $@"
                    if (item.{propertyName} != null && 
                        item.{propertyName} is not {foreignEntityLazyNullFullName}<{foreignType.Entity.FullName}, {fullClassName}> &&
                        item.{propertyName}.Value != null)
                    {{
                        var subitem = item.{propertyName}.Value;
                        db.{lazyPropertyOnApplicationDbContextName}.Attach(subitem);
                        if (item.{foreignKeyName} != subitem.Id)
                            item.{foreignKeyName} = subitem.Id;
                    }}

                    if (item.{propertyName} == null ||
                        item.{propertyName} is not {foreignEntityLazyNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>)
                    {{
                        item.{propertyName} = new {foreignEntityLazyNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>(
                            db.{lazyPropertyOnApplicationDbContextName},
                            item,
                            (primary) => primary.{primaryKeyName},
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = ({foreignKeyTypeName})value; }});
                    }}";
        }
        else
        {
            lazyCode += $@"
                    if (item.{propertyName} != null && 
                        item.{propertyName} is not {foreignEntityLazyNotNullFullName}<{foreignType.Entity.FullName}, {fullClassName}> &&
                        item.{propertyName}.Value != null)
                    {{
                        var subitem = item.{propertyName}.Value;
                        if (item.{foreignKeyName} != subitem.Id)
                            item.{foreignKeyName} = subitem.Id;
                        db.{lazyPropertyOnApplicationDbContextName}.Attach(subitem);
                    }}

                    if (item.{propertyName} == null ||
                        item.{propertyName} is not {foreignEntityLazyNotNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>)
                    {{
                        item.{propertyName} = new {foreignEntityLazyNotNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>(
                            db.{lazyPropertyOnApplicationDbContextName},
                            item,
                            (primary) => primary.{primaryKeyName},
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = ({foreignKeyTypeName})value; }});
                    }}";
        }
    }
    private static void GenerateExtendCode_NavigationLists(
        DbContextModel dbContextModel, DbSetModel dbSetModel, string className, string fullClassName, ref string lazyCode,
        string foreignEntityCollectionNotNullFullName, string foreignEntityCollectionNullFullName,
        Type applicationDbContextType, EntityPropertyModel entityProp, string propertyName)
    {
        //var foreignType = ReflectionHelper.GetIEnumerableType(prop);
        //var foreignKeyName = $"{className}Id";
        //if (ReflectionHelper.HasForeignKeyAttribute(prop))
        //{
        //    foreignKeyName = ReflectionHelper.GetForeignKeyAttributeName(prop);
        //}

        if (entityProp.NavigationDbSet == null) return;
        if (entityProp.NavigationList == null) return;
        if (entityProp.PrimaryKey == null) return;
        if (entityProp.NavigationDbSet.Entity.PrimaryKey == null) return;
        var foreignType = entityProp.NavigationDbSet;
        var foreignKeyName = entityProp.NavigationList.Name;

        //var foreignProperty = foreignType.GetProperties()
        //    .FirstOrDefault(a => a.Name == foreignKeyName)
        //    ?? throw new Exception($"ZipDatabase Exception: Foreign key property {foreignKeyName} not found on {foreignType.FullName}.");

        //var foreignPropertyOnApplicationDbContext = applicationDbContextType.GetProperties()
        //    .Where(a => ReflectionHelper.IsDbSet(a))
        //    .FirstOrDefault(a => ReflectionHelper.GetDbSetType(a) == foreignType)
        //    ?? throw new Exception($"ZipDatabase Exception: DbSet<{foreignType.Name}> not found on {applicationDbContextType.Name}.");

        //if (!ReflectionHelper.IsIEntity(dbSetModel))
        //    throw new Exception(
        //        $"ZipDatabase Exception: Type '{dbSetModel.FullName}' does not implement IEntity interface, though is used to filter in the " +
        //        $"{foreignType.Name} entities with '{foreignKeyName}'. Type {dbSetModel.Name} needs a primary key " +
        //        $"('public long Id {{ get; set; }}' property) to filter in Entities (you can copy it from the " +
        //        $"parent entity '{foreignType}').");


        var foreignProperty = entityProp.NavigationList;
        var foreignPropertyOnApplicationDbContext = entityProp.NavigationDbSet;
        var foreignPropertyOnApplicationDbContextName = foreignPropertyOnApplicationDbContext.Name;

        var primaryKeyName = entityProp.PrimaryKey.Name;
        var foreignKeyTypeName = entityProp.NavigationList.TypeSimpleName;
        var foreignPrimaryKeyName = entityProp.NavigationDbSet.Entity.PrimaryKey.Name;

        //if (ReflectionHelper.IsNulleble(foreignProperty))
        if (foreignProperty.IsNullable)
        {
            lazyCode += $@"
                    if (item.{propertyName} != null &&
                        item.{propertyName} is not {foreignEntityCollectionNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>)
                    {{
                        foreach(var subitem in item.{propertyName})
                        {{
                            if (subitem.{foreignKeyName} != item.Id)
                                subitem.{foreignKeyName} = item.Id;
                            db.{foreignPropertyOnApplicationDbContextName}.Attach(subitem);
                        }}
                    }}
                    if (item.{propertyName} == null ||
                        item.{propertyName} is not {foreignEntityCollectionNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>)
                    {{
                        item.{propertyName} = new {foreignEntityCollectionNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>(
                            db.{foreignPropertyOnApplicationDbContextName},
                            item,
                            (primary) => primary.{primaryKeyName},
                            (foreign) => foreign.{foreignPrimaryKeyName},
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = ({foreignKeyTypeName})value; }});
                    }}";
        }
        else
        {
            lazyCode += $@"
                    if (item.{propertyName} != null &&
                        item.{propertyName} is not {foreignEntityCollectionNotNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>)
                    {{
                        foreach(var subitem in item.{propertyName})
                        {{
                            if (subitem.{foreignKeyName} != item.Id)
                                subitem.{foreignKeyName} = item.Id;
                            db.{foreignPropertyOnApplicationDbContextName}.Attach(subitem);
                        }}
                    }}
                    if (item.{propertyName} == null ||
                        item.{propertyName} is not {foreignEntityCollectionNotNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>)
                    {{
                        item.{propertyName} = new {foreignEntityCollectionNotNullFullName}<{foreignType.Entity.FullName}, {fullClassName}>(
                            db.{foreignPropertyOnApplicationDbContextName},
                            item,
                            (primary) => primary.{primaryKeyName},
                            (foreign) => foreign.{foreignPrimaryKeyName},
                            (foreign) => foreign.{foreignKeyName},
                            (foreign, value) => {{ foreign.{foreignKeyName} = ({foreignKeyTypeName})value; }});
                    }}";
        }
    }
}