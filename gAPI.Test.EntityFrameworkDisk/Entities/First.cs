using gAPI.EntityFrameworkDisk;
using System.ComponentModel.DataAnnotations;
#nullable disable
public class First
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    // PrimaryKey != null, wijst naar Second.Id
    // ForeignKey != null, wijst naar First.Second
    public long? SecondId { get; set; }

    // PrimaryKey != null, wijst naar Second.Id
    // NavigationDbSet != null, wijst naar Second
    // NavigationItem != null, wijst naar First.SecondId
    public virtual ILazy<Second> Second { get; set; }
}












//public class ApplicationDbContext : DbContext
//{
//    public virtual DbSet<Test> Tests { get; set; }
//    public virtual DbSet<OtherTest> OtherTests { get; set; }

//}

//public class Test
//{
//    [Key]
//    public int Id { get; set; }
//    public string? Name { get; set; }
//    public int? OtherTestId { get; set; }

//    [ForeignKey(nameof(OtherTestId))]
//    public virtual OtherTest? OtherTest { get; set; }
//}
//public class OtherTest
//{
//    [Key]
//    public int Id { get; set; }
//    public string? Name { get; set; }
//}











//public static class EntityDefinitionCollection
//{
//    public static readonly Dictionary<Type, EntityDefinition> EntityDefinitions = new Dictionary<Type, EntityDefinition>();

//    public static EntityDefinition GetOrCreate<T>()
//        where T : class
//    {
//        var entityType = typeof(T);
//        if (EntityDefinitions.TryGetValue(entityType, out var entityDefinition))
//        {
//            return entityDefinition;
//        }
//        else
//        {
//            var newEntityDefinition = new EntityDefinition(entityType);
//            EntityDefinitions[entityType] = newEntityDefinition;
//            return newEntityDefinition;
//        }
//    }
//}
//public class EntityDefinition
//{
//    public Type ElementType { get; }
//    public PropertyInfo? KeyProperty { get; }
//    public PropertyInfo[] ForeignKeyProperties { get; }
//    public PropertyInfo[] ValueProperties { get; }

//    public EntityDefinition(Type elementType)
//    {
//        ElementType = elementType;
//        KeyProperty = ElementType
//            .GetProperties()
//            .FirstOrDefault(p => p.GetCustomAttributes(typeof(KeyAttribute), true).Any());

//        ForeignKeyProperties = ElementType
//            .GetProperties()
//            .Where(p =>
//                p.GetCustomAttributes(typeof(ForeignKeyAttribute), true).Any())
//            .ToArray();

//        ValueProperties = ElementType
//            .GetProperties()
//            .Where(p =>
//                p.GetCustomAttributes(typeof(ForeignKeyAttribute), true).Any() == false &&
//                p.GetCustomAttributes(typeof(KeyAttribute), true).Any() == false)
//            .ToArray();
//    }
//}

//public class DbSet<T>
//    where T : class
//{
//    private readonly EntityDefinition EntityDefinition = 
//        EntityDefinitionCollection.GetOrCreate<T>();

//    public IQueryable<T> Where(Expression<Func<T, bool>> predicate)
//    {
//        var visitor = new PropertyUsageVisitor();
//        visitor.Visit(predicate);

//        if (visitor.UsedProperties.Count == 1 &&
//            visitor.UsedProperties.First() == "Id")
//        {

//        }

//        return null; //Cache.AsQueryable().Where(predicate);
//    }
//}

//public class PropertyUsageVisitor : ExpressionVisitor
//{
//    public HashSet<string> UsedProperties { get; } = new HashSet<string>();

//    protected override Expression VisitMember(MemberExpression node)
//    {
//        if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
//        {
//            UsedProperties.Add(node.Member.Name);
//        }

//        return base.VisitMember(node);
//    }
//}