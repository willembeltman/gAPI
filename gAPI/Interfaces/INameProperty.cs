namespace gAPI.Interfaces
{
    public interface INameProperty
    {
        string Name { get; }
    }
}



//public class CompanyEntity
//{
//    [Key]
//    public int Id { get; set; }
//    [IsName("Company: ", FormattingOption.ToString)]
//    public string Name { get; set; }
//    [IsName(" (est ", FormattingOption.yyyy, ")")]
//    public DateTime DateStarted { get; set; }
//}

//public class UserEntity
//{
//    [Key]
//    public int Id { get; set; }
//    public int CompanyId { get; set; }
//    public virtual CompanyEntity Company { get; set; }
//    [IsName]
//    public string Name { get; set; }

//}

//public class UserDto
//{
//    public int Id { get; set; }
//    public int CompanyId { get; set; }
//    public string CompanyName { get; set; }
//    public string Name { get; set; }
//}

//// *** Generated ***
//public static class UserEntityMapper
//{
//    public static Expression<Func<UserEntity, UserDto>> ProjectToDto
//    {
//        get
//        {
//            return a => new UserDto
//            {
//                Id = a.Id,
//                CompanyId = a.CompanyId,
//                CompanyName = "Company: " + a.Company.Name + " " + " (est " + a.Company.DateStarted.Year + ")"
//            };
//        }
//    }
//}
//// *** Generated ***