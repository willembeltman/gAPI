using gAPI.Test.EntityFrameworkDisk.Entities;

namespace gAPI.Test.EntityFrameworkDisk;

internal class Program
{
    private static void Main()
    {
        var db = new ApplicationDbContext();
        var second = new Second()
        {
            Name = "Second",
            Description = "Second"
        };
        db.Seconds.Add(second);
        db.SaveChanges();

        var first1 = new First()
        {
            Name = "first 1",
            Description = "first 1",
            SecondId = second.Id
        };
        db.Firsts.Add(first1);
        var first2 = new First()
        {
            Name = "first 2",
            Description = "first 2",
            SecondId = second.Id
        };
        db.Firsts.Add(first2);
        db.SaveChanges();
    }
    //private static void Main(string[] args)
    //{
    //    var sw = Stopwatch.StartNew();
    //    var db = new ApplicationDbContext();
    //    var test1 = new Test() { Name = "1" };
    //    var test2 = new Test() { Name = "2" };
    //    var test3 = new Test() { Name = "3" };

    //    var str = $"Initialize: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //    Console.WriteLine(str);
    //    sw.Restart();

    //    for (var i = 0; i < 1; i++)
    //    {
    //        //db.Clear(); 

    //        //str = $"Clear: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //        //Console.WriteLine(str);
    //        //sw.Restart();

    //        db.Tests.Add(test1);
    //        db.Tests.Add(test2);
    //        db.Tests.Add(test3);

    //        //str = $"New + Adds: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //        //Console.WriteLine(str);
    //        //sw.Restart();

    //        //db.SaveChanges();

    //        //str = $"SaveChanges: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //        //Console.WriteLine(str);
    //        //sw.Restart();
    //    }

    //    str = $"Run: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //    Console.WriteLine(str);
    //    sw.Restart();

    //    test2.Name = "TEST";
    //    db.SaveChanges();

    //    str = $"SaveChanges: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //    Console.WriteLine(str);
    //    sw.Restart();

    //    //var test1 = new Test() { Name = "1" };
    //    //var test2 = new Test() { Name = "2" };
    //    //var test3 = new Test() { Name = "3" };
    //    //var range = new Test[]
    //    //{
    //    //    test1, 
    //    //    test2,
    //    //    test3 
    //    //};
    //    //var db = DiskSetCollection.GetOrCreate<Test>();

    //    //var str = $"Initialize: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //    //Console.WriteLine(str);
    //    //sw.Restart();

    //    //for (var i = 0; i < 5; i++)
    //    //{
    //    //    db.Clear();

    //    //    str = $"Pass #{i} Clear: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //    //    Console.WriteLine(str);
    //    //    sw.Restart();

    //    //    db.AddRange(range);

    //    //    str = $"Pass #{i} AddRange: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //    //    Console.WriteLine(str);
    //    //    sw.Restart();

    //    //    var testQuery1 = db.FirstOrDefault(a => a.Id == 1);
    //    //    var testQuery2 = db.Where(a => a.Id == 1).ToArray();
    //    //    var testQuery3 = db.SingleOrDefault(a => a.Id == 1 && a.Name == "1");
    //    //    var testQuery4 = db.Any(a => a.Id == 1 && a.Name == "1");
    //    //    var testQuery5 = db.FirstOrDefault(a => a.Id == 1 && a.Name == "2");
    //    //    var testQuery6 = db.Where(a => a.Id == 1 && a.Name == "2").ToArray();

    //    //    str = $"Pass #{i} Queries: {sw.Elapsed.TotalMilliseconds:F2}ms";
    //    //    Console.WriteLine(str);
    //    //    sw.Restart();

    //    //    db.RemoveRange(range);

    //    //    str = $"Pass #{i} RemoveRange: {sw.Elapsed.TotalMilliseconds:F2}ms count: {db.Count}";
    //    //    Console.WriteLine(str);
    //    //    sw.Restart();
    //    //}
    //}
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