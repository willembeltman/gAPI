using System;
using System.Linq;
using System.Linq.Expressions;

namespace gAPI.Helpers
{
    public static class ApplyOrderByExtention
    {
        public static IQueryable<T> ApplyOrderBy<T>(this IQueryable<T> source, string[]? orderby = null)
        {
            if (orderby == null || orderby.Length == 0)
                return source;

            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var (prop, index) in orderby.Select((p, i) => (p, i)))
            {
                var trimmed = prop.Trim();
                bool desc = false;

                if (trimmed.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
                {
                    desc = true;
                    trimmed = trimmed.Substring(0, trimmed.Length - 5).Trim();
                }
                else if (trimmed.EndsWith(" asc", StringComparison.OrdinalIgnoreCase))
                {
                    desc = false;
                    trimmed = trimmed.Substring(0, trimmed.Length - 4).Trim();
                }

                var param = Expression.Parameter(typeof(T), "x");

                // Support voor nested properties zoals "Company.Name"
                Expression property = param;
                foreach (var member in trimmed.Split('.'))
                {
                    property = Expression.PropertyOrField(property, member);
                }

                var lambda = Expression.Lambda(property, param);

                string methodName;
                if (index == 0)
                    methodName = desc ? "OrderByDescending" : "OrderBy";
                else
                    methodName = desc ? "ThenByDescending" : "ThenBy";

                var resultExp = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), property.Type },
                    index == 0 ? source.Expression : orderedQuery?.Expression ?? throw new Exception("even checken aub"),
                    Expression.Quote(lambda));

                orderedQuery = (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExp);
            }

            return orderedQuery ?? source;
        }

    }
}