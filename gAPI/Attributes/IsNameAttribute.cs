using System;

namespace gAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IsNameAttribute : Attribute
    {
        public IsNameAttribute()
        {
            FormattingOption = FormattingOption.ToString;
        }
        public IsNameAttribute(FormattingOption formattingOption)
        {
            FormattingOption = formattingOption;
        }
        public IsNameAttribute(string start, FormattingOption formattingOption, string? end = null) : this(formattingOption)
        {
            Start = start;
            End = end;
        }

        public string? Start { get; }
        public FormattingOption FormattingOption { get; }
        public string? End { get; }

        public string? StringFormat
        {
            get
            {
                switch (FormattingOption)
                {
                    case FormattingOption.dd:
                        return "dd";
                    case FormattingOption.dd_MM:
                        return "dd-MM";
                    case FormattingOption.dd_MM_yyyy:
                        return "dd-MM-yyyy";
                    case FormattingOption.dd_MM_yyyy_HH_mm:
                        return "dd-MM-yyyy HH:mm";
                    case FormattingOption.dd_MM_yyyy_HH_mm_SS:
                        return "dd-MM-yyyy HH:mm:SS";
                    case FormattingOption.HH_mm:
                        return "HH:mm";
                    case FormattingOption.HH_mm_SS:
                        return "HH:mm:SS";
                    case FormattingOption.MM:
                        return "MM";
                    case FormattingOption.MM_dd:
                        return "MM-dd";
                    case FormattingOption.MM_dd_yyyy:
                        return "MM-dd-yyyy";
                    case FormattingOption.MM_dd_yyyy_HH_mm:
                        return "MM-dd-yyyy HH:mm";
                    case FormattingOption.MM_dd_yyyy_HH_mm_SS:
                        return "MM-dd-yyyy HH:mm:SS";
                    case FormattingOption.yyyy:
                        return "yyyy";
                    case FormattingOption.yyyy_MM:
                        return "yyyy-MM";
                    case FormattingOption.yyyy_MM_dd:
                        return "yyyy-MM-dd";
                    case FormattingOption.yyyy_MM_dd_HH_mm:
                        return "yyyy-MM-dd HH:mm";
                    case FormattingOption.yyyy_MM_dd_HH_mm_SS:
                        return "yyyy-MM-dd HH:mm:SS";
                    case FormattingOption.F0:
                        return "F0";
                    case FormattingOption.F1:
                        return "F1";
                    case FormattingOption.F2:
                        return "F2";
                    case FormattingOption.F3:
                        return "F3";
                    case FormattingOption.F4:
                        return "F4";
                    case FormattingOption.F5:
                        return "F5";
                    case FormattingOption.F6:
                        return "F6";
                    default:
                        return null;
                }
            }
        }

        public string Format(string full)
        {
            var yyyy = $"({full}.Year)";
            var MM = $"(\"0\" + {full}.Month).Substring((\"0\" + {full}.Month).Length - 2)";
            var dd = $"(\"0\" + {full}.Day).Substring((\"0\" + {full}.Day).Length - 2)";
            var HH = $"(\"0\" + {full}.Hour).Substring((\"0\" + {full}.Hour).Length - 2)";
            var mm = $"(\"0\" + {full}.Minute).Substring((\"0\" + {full}.Minute).Length - 2)";
            var SS = $"(\"0\" + {full}.Second).Substring((\"0\" + {full}.Second).Length - 2)";

            switch (FormattingOption)
            {
                case FormattingOption.ToString:
                    return $"(\"{Start}\" + {full} + \"{End}\")"; // default property

                // ---- Dates ----
                case FormattingOption.yyyy:
                    return $"(\"{Start}\" + {yyyy} + \"{End}\")";
                case FormattingOption.MM:
                    return $"(\"{Start}\" + {MM} + \"{End}\")";
                case FormattingOption.dd:
                    return $"(\"{Start}\" + {dd} + \"{End}\")";
                case FormattingOption.HH_mm:
                    return $"(\"{Start}\" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")";
                case FormattingOption.HH_mm_SS:
                    return $"(\"{Start}\" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")";

                case FormattingOption.yyyy_MM:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM })} + \"{End}\")";
                case FormattingOption.yyyy_MM_dd:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM, dd })} + \"{End}\")";
                case FormattingOption.yyyy_MM_dd_HH_mm:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM, dd })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")";
                case FormattingOption.yyyy_MM_dd_HH_mm_SS:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM, dd })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")";

                case FormattingOption.MM_dd:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd })} + \"{End}\")";
                case FormattingOption.MM_dd_yyyy:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd, yyyy })} + \"{End}\")";
                case FormattingOption.MM_dd_yyyy_HH_mm:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")";
                case FormattingOption.MM_dd_yyyy_HH_mm_SS:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")";

                case FormattingOption.dd_MM:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM })} + \"{End}\")";
                case FormattingOption.dd_MM_yyyy:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM, yyyy })} + \"{End}\")";
                case FormattingOption.dd_MM_yyyy_HH_mm:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")";
                case FormattingOption.dd_MM_yyyy_HH_mm_SS:
                    return $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")";

                // ---- Numeric rounding ----
                case FormattingOption.F0:
                    return $"(\"{Start}\" + Math.Round({full}, 0) + \"{End}\")";
                case FormattingOption.F1:
                    return $"(\"{Start}\" + Math.Round({full}, 1) + \"{End}\")";
                case FormattingOption.F2:
                    return $"(\"{Start}\" + Math.Round({full}, 2) + \"{End}\")";
                case FormattingOption.F3:
                    return $"(\"{Start}\" + Math.Round({full}, 3) + \"{End}\")";
                case FormattingOption.F4:
                    return $"(\"{Start}\" + Math.Round({full}, 4) + \"{End}\")";
                case FormattingOption.F5:
                    return $"(\"{Start}\" + Math.Round({full}, 5) + \"{End}\")";
                case FormattingOption.F6:
                    return $"(\"{Start}\" + Math.Round({full}, 6) + \"{End}\")";

                default:
                    return full;
            }
        }

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