using gAPI.Enums;
using System;

namespace gAPI.Attributes;

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
            return FormattingOption switch
            {
                FormattingOption.dd => "dd",
                FormattingOption.dd_MM => "dd-MM",
                FormattingOption.dd_MM_yyyy => "dd-MM-yyyy",
                FormattingOption.dd_MM_yyyy_HH_mm => "dd-MM-yyyy HH:mm",
                FormattingOption.dd_MM_yyyy_HH_mm_SS => "dd-MM-yyyy HH:mm:SS",
                FormattingOption.HH_mm => "HH:mm",
                FormattingOption.HH_mm_SS => "HH:mm:SS",
                FormattingOption.MM => "MM",
                FormattingOption.MM_dd => "MM-dd",
                FormattingOption.MM_dd_yyyy => "MM-dd-yyyy",
                FormattingOption.MM_dd_yyyy_HH_mm => "MM-dd-yyyy HH:mm",
                FormattingOption.MM_dd_yyyy_HH_mm_SS => "MM-dd-yyyy HH:mm:SS",
                FormattingOption.yyyy => "yyyy",
                FormattingOption.yyyy_MM => "yyyy-MM",
                FormattingOption.yyyy_MM_dd => "yyyy-MM-dd",
                FormattingOption.yyyy_MM_dd_HH_mm => "yyyy-MM-dd HH:mm",
                FormattingOption.yyyy_MM_dd_HH_mm_SS => "yyyy-MM-dd HH:mm:SS",
                FormattingOption.F0 => "F0",
                FormattingOption.F1 => "F1",
                FormattingOption.F2 => "F2",
                FormattingOption.F3 => "F3",
                FormattingOption.F4 => "F4",
                FormattingOption.F5 => "F5",
                FormattingOption.F6 => "F6",
                _ => null,
            };
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

        return FormattingOption switch
        {
            FormattingOption.ToString => $"(\"{Start}\" + {full} + \"{End}\")",// default property
            // ---- Dates ----
            FormattingOption.yyyy => $"(\"{Start}\" + {yyyy} + \"{End}\")",
            FormattingOption.MM => $"(\"{Start}\" + {MM} + \"{End}\")",
            FormattingOption.dd => $"(\"{Start}\" + {dd} + \"{End}\")",
            FormattingOption.HH_mm => $"(\"{Start}\" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")",
            FormattingOption.HH_mm_SS => $"(\"{Start}\" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")",
            FormattingOption.yyyy_MM => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM })} + \"{End}\")",
            FormattingOption.yyyy_MM_dd => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM, dd })} + \"{End}\")",
            FormattingOption.yyyy_MM_dd_HH_mm => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM, dd })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")",
            FormattingOption.yyyy_MM_dd_HH_mm_SS => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { yyyy, MM, dd })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")",
            FormattingOption.MM_dd => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd })} + \"{End}\")",
            FormattingOption.MM_dd_yyyy => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd, yyyy })} + \"{End}\")",
            FormattingOption.MM_dd_yyyy_HH_mm => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")",
            FormattingOption.MM_dd_yyyy_HH_mm_SS => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { MM, dd, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")",
            FormattingOption.dd_MM => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM })} + \"{End}\")",
            FormattingOption.dd_MM_yyyy => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM, yyyy })} + \"{End}\")",
            FormattingOption.dd_MM_yyyy_HH_mm => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm })} + \"{End}\")",
            FormattingOption.dd_MM_yyyy_HH_mm_SS => $"(\"{Start}\" + {string.Join(" + \"-\" + ", new string[] { dd, MM, yyyy })} + \" \" + {string.Join(" + \":\" + ", new string[] { HH, mm, SS })} + \"{End}\")",
            // ---- Numeric rounding ----
            FormattingOption.F0 => $"(\"{Start}\" + Math.Round({full}, 0) + \"{End}\")",
            FormattingOption.F1 => $"(\"{Start}\" + Math.Round({full}, 1) + \"{End}\")",
            FormattingOption.F2 => $"(\"{Start}\" + Math.Round({full}, 2) + \"{End}\")",
            FormattingOption.F3 => $"(\"{Start}\" + Math.Round({full}, 3) + \"{End}\")",
            FormattingOption.F4 => $"(\"{Start}\" + Math.Round({full}, 4) + \"{End}\")",
            FormattingOption.F5 => $"(\"{Start}\" + Math.Round({full}, 5) + \"{End}\")",
            FormattingOption.F6 => $"(\"{Start}\" + Math.Round({full}, 6) + \"{End}\")",
            _ => full,
        };
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