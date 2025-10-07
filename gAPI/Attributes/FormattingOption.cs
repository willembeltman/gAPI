
namespace gAPI.Attributes
{
    public enum FormattingOption
    {
        ToString, // Default

        HH_mm,
        HH_mm_SS,

        yyyy,
        yyyy_MM,
        yyyy_MM_dd,
        yyyy_MM_dd_HH_mm,
        yyyy_MM_dd_HH_mm_SS,

        MM,
        MM_dd,
        MM_dd_yyyy,
        MM_dd_yyyy_HH_mm,
        MM_dd_yyyy_HH_mm_SS,

        dd,
        dd_MM,
        dd_MM_yyyy,
        dd_MM_yyyy_HH_mm,
        dd_MM_yyyy_HH_mm_SS,

        F0, F1, F2, F3, F4, F5, F6
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