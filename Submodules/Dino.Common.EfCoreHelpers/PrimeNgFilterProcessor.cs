using Dino.Common.EfCoreHelpers.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Dino.Common.EfCoreHelpers
{
    /// <summary>
    /// Generic processor for applying PrimeNG table filters and sorting to IQueryable
    /// </summary>
    public static class PrimeNgFilterProcessor
    {
        /// <summary>
        /// Apply PrimeNG filters to an IQueryable
        /// </summary>
        public static IQueryable<T> ApplyFilters<T>(
            this IQueryable<T> query,
            Dictionary<string, PrimeNgFilterMetadata>? filters,
            string? globalFilter = null,
            string[]? globalFilterFields = null,
            int globalFilterNavigationDepth = 1)
        {
            if (filters == null || !filters.Any())
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");

            foreach (var filter in filters)
            {
                var fieldName = filter.Key;
                var metadata = filter.Value;

                // Get normalized constraints (handles both array and object formats)
                var constraints = metadata.GetConstraints();
                
                if (!constraints.Any())
                    continue;

                Expression? filterExpression = null;

                if (constraints.Count == 1)
                {
                    // Single constraint - simpler expression
                    var constraint = constraints[0];
                    var mode = constraint.MatchMode?.ToLower();
                    if (constraint.Value != null || mode == "isnull" || mode == "isnotnull")
                    {
                        filterExpression = BuildFilterExpression(parameter, fieldName, constraint.Value, constraint.MatchMode ?? "equals");
                    }
                }
                else
                {
                    // Multiple constraints - combine with AND/OR
                    var constraintExpressions = new List<Expression>();

                    foreach (var constraint in constraints)
                    {
                        var mode = constraint.MatchMode?.ToLower();
                        if (constraint.Value == null && mode != "isnull" && mode != "isnotnull")
                            continue;

                        var expr = BuildFilterExpression(parameter, fieldName, constraint.Value, constraint.MatchMode ?? "equals");
                        if (expr != null)
                            constraintExpressions.Add(expr);
                    }

                    if (constraintExpressions.Any())
                    {
                        // Combine constraints based on operator (AND/OR)
                        var isAnd = (metadata.Operator ?? "and").ToLower() == "and";
                        filterExpression = constraintExpressions.Aggregate(
                            (accumulated, next) => isAnd
                                ? Expression.AndAlso(accumulated, next)
                                : Expression.OrElse(accumulated, next)
                        );
                    }
                }

                if (filterExpression != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
                    query = query.Where(lambda);
                }
            }

            // Apply global filter if specified
            if (!string.IsNullOrWhiteSpace(globalFilter))
            {
                query = ApplyGlobalFilter(query, globalFilter, globalFilterFields, globalFilterNavigationDepth);
            }

            return query;
        }

        /// <summary>
        /// Apply PrimeNG sorting to an IQueryable
        /// </summary>
        public static IQueryable<T> ApplySorting<T>(
            this IQueryable<T> query,
            List<PrimeNgSortMetadata>? multiSortMeta,
            string? sortField = null,
            int? sortOrder = null)
        {
            // Use multiSortMeta if available, otherwise fall back to single sort
            var sortList = multiSortMeta?.Where(s => !string.IsNullOrEmpty(s.Field)).ToList();

            if (sortList == null || !sortList.Any())
            {
                if (!string.IsNullOrEmpty(sortField))
                {
                    sortList = new List<PrimeNgSortMetadata>
                    {
                        new PrimeNgSortMetadata { Field = sortField, Order = sortOrder ?? 1 }
                    };
                }
            }

            if (sortList == null || !sortList.Any())
                return query;

            IOrderedQueryable<T>? orderedQuery = null;

            for (int i = 0; i < sortList.Count; i++)
            {
                var sort = sortList[i];
                var propertyInfo = GetPropertyInfo(typeof(T), sort.Field!);
                if (propertyInfo == null)
                    continue;

                var parameter = Expression.Parameter(typeof(T), "x");
                var propertyAccess = BuildPropertyAccess(parameter, sort.Field!);
                var orderByExpression = Expression.Lambda(propertyAccess, parameter);

                var isDescending = sort.Order == -1;

                if (i == 0)
                {
                    // First sort
                    orderedQuery = isDescending
                        ? InvokeOrderBy(query, orderByExpression, "OrderByDescending")
                        : InvokeOrderBy(query, orderByExpression, "OrderBy");
                }
                else
                {
                    // Subsequent sorts (ThenBy)
                    orderedQuery = isDescending
                        ? InvokeThenBy(orderedQuery!, orderByExpression, "ThenByDescending")
                        : InvokeThenBy(orderedQuery!, orderByExpression, "ThenBy");
                }
            }

            return orderedQuery ?? query;
        }

        /// <summary>
        /// Build a filter expression for a specific field, value, and match mode
        /// </summary>
        private static Expression? BuildFilterExpression(ParameterExpression parameter, string fieldName, object? value, string matchMode)
        {
            try
            {
                var propertyAccess = BuildPropertyAccess(parameter, fieldName);
                if (propertyAccess == null)
                    return null;

                var propertyType = propertyAccess.Type;
                var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                // Handle special operators that don't need values
                var mode = matchMode.ToLower();
                if (mode == "isnull")
                {
                    return Expression.Equal(propertyAccess, Expression.Constant(null, propertyType));
                }
                if (mode == "isnotnull")
                {
                    return Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyType));
                }
                if (mode == "isempty" && underlyingType == typeof(string))
                {
                    return Expression.OrElse(
                        Expression.Equal(propertyAccess, Expression.Constant(null, propertyType)),
                        Expression.Equal(propertyAccess, Expression.Constant(string.Empty))
                    );
                }
                if (mode == "isnotempty" && underlyingType == typeof(string))
                {
                    return Expression.AndAlso(
                        Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyType)),
                        Expression.NotEqual(propertyAccess, Expression.Constant(string.Empty))
                    );
                }

                // Handle "in" operator with array values
                if (mode == "in")
                {
                    if (value == null) return null;
                    
                    var (values, containsNull) = ExtractArrayValues(value, underlyingType);
                    // Only return null if we have neither values nor null
                    if ((values == null || !values.Any()) && !containsNull)
                        return null;

                    return BuildInExpression(propertyAccess, values ?? new List<object>(), propertyType, underlyingType, containsNull);
                }

                // For other operators, convert value to the correct type
                if (value == null)
                    return null;

                // Check if we're doing string operations on numeric types
                var isStringOperation = mode == "contains" || mode == "startswith" || mode == "endswith" || mode == "notcontains";
                var isNumericType = IsNumericType(underlyingType);
                
                object? convertedValue;
                Type targetType;
                
                if (isStringOperation && isNumericType)
                {
                    // Keep value as string for string operations on numeric fields
                    convertedValue = value.ToString();
                    targetType = typeof(string);
                }
                else
                {
                    // Convert to target type for normal operations
                    try
                    {
                        convertedValue = ConvertValue(value, underlyingType);
                    }
                    catch
                    {
                        return null; // Conversion failed
                    }
                    targetType = underlyingType;
                }

                if (convertedValue == null)
                    return null;

                // Handle nullable properties
                Expression propertyForComparison = propertyAccess;
                Expression? hasValueCheck = null;

                if (Nullable.GetUnderlyingType(propertyType) != null)
                {
                    var hasValueProperty = propertyType.GetProperty("HasValue")!;
                    var valueProperty = propertyType.GetProperty("Value")!;

                    hasValueCheck = Expression.Property(propertyAccess, hasValueProperty);
                    propertyForComparison = Expression.Property(propertyAccess, valueProperty);
                }

                var constantValue = Expression.Constant(convertedValue, targetType);
                var comparison = BuildComparisonExpression(propertyForComparison, constantValue, mode, isStringOperation && isNumericType ? underlyingType : targetType);

                if (comparison == null)
                    return null;

                // Combine with HasValue check if nullable
                return hasValueCheck != null
                    ? Expression.AndAlso(hasValueCheck, comparison)
                    : comparison;
            }
            catch
            {
                return null; // Skip filter on any error
            }
        }

        /// <summary>
        /// Build comparison expression based on match mode
        /// </summary>
        private static Expression? BuildComparisonExpression(Expression property, Expression value, string matchMode, Type type)
        {
            var mode = matchMode.ToLower();

            // For string operations on non-string types (e.g., numeric types), convert to string first
            var isStringOperation = mode == "contains" || mode == "startswith" || mode == "endswith" || mode == "notcontains";
            
            if (isStringOperation && type != typeof(string) && IsNumericType(type))
            {
                // Convert numeric property to string: property.ToString()
                var toStringMethod = type.GetMethod("ToString", Type.EmptyTypes)!;
                var propertyAsString = Expression.Call(property, toStringMethod);
                
                // Convert the search value to string if it isn't already
                var valueAsString = value.Type == typeof(string) ? value : Expression.Call(value, type.GetMethod("ToString", Type.EmptyTypes)!);
                
                return mode switch
                {
                    "contains" => Expression.Call(propertyAsString, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, valueAsString),
                    "startswith" => Expression.Call(propertyAsString, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, valueAsString),
                    "endswith" => Expression.Call(propertyAsString, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, valueAsString),
                    "notcontains" => Expression.Not(Expression.Call(propertyAsString, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, valueAsString)),
                    _ => null
                };
            }

            // String operations on string types
            if (type == typeof(string))
            {
                return mode switch
                {
                    "contains" => Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, value),
                    "startswith" => Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, value),
                    "endswith" => Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, value),
                    "equals" => Expression.Equal(property, value),
                    "notequals" => Expression.NotEqual(property, value),
                    "notcontains" => Expression.Not(Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, value)),
                    _ => Expression.Equal(property, value)
                };
            }

            // Date operations
            if (type == typeof(DateTime))
            {
                return mode switch
                {
                    "dateis" => Expression.Equal(Expression.Property(property, "Date"), Expression.Property(value, "Date")),
                    "dateisnot" => Expression.NotEqual(Expression.Property(property, "Date"), Expression.Property(value, "Date")),
                    "datebefore" => Expression.LessThan(property, value),
                    "dateafter" => Expression.GreaterThan(property, value),
                    "equals" => Expression.Equal(property, value),
                    "notequals" => Expression.NotEqual(property, value),
                    _ => Expression.Equal(property, value)
                };
            }

            // Numeric and other operations
            return mode switch
            {
                "equals" => Expression.Equal(property, value),
                "notequals" => Expression.NotEqual(property, value),
                "lt" => Expression.LessThan(property, value),
                "lte" => Expression.LessThanOrEqual(property, value),
                "gt" => Expression.GreaterThan(property, value),
                "gte" => Expression.GreaterThanOrEqual(property, value),
                _ => Expression.Equal(property, value)
            };
        }

        /// <summary>
        /// Build an "in" expression for array values
        /// </summary>
        private static Expression BuildInExpression(Expression property, List<object> values, Type propertyType, Type underlyingType, bool containsNull = false)
        {
            Expression propertyForComparison = property;
            Expression? hasValueCheck = null;
            Expression? nullCheck = null;

            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                var hasValueProperty = propertyType.GetProperty("HasValue")!;
                var valueProperty = propertyType.GetProperty("Value")!;

                hasValueCheck = Expression.Property(property, hasValueProperty);
                propertyForComparison = Expression.Property(property, valueProperty);
                
                // If array contains null, we need to check for !HasValue
                if (containsNull)
                {
                    nullCheck = Expression.Not(hasValueCheck);
                }
            }

            // Build OR expression for all non-null values
            Expression? orExpression = null;
            
            if (values.Any())
            {
                foreach (var value in values)
                {
                    var constantValue = Expression.Constant(value, underlyingType);
                    var equalExpression = Expression.Equal(propertyForComparison, constantValue);

                    orExpression = orExpression == null
                        ? equalExpression
                        : Expression.OrElse(orExpression, equalExpression);
                }

                // Combine with HasValue check for non-null values
                if (hasValueCheck != null && orExpression != null)
                {
                    orExpression = Expression.AndAlso(hasValueCheck, orExpression);
                }
            }

            // Combine null check with value checks
            if (nullCheck != null && orExpression != null)
            {
                // Either null OR one of the values
                return Expression.OrElse(nullCheck, orExpression);
            }
            else if (nullCheck != null)
            {
                // Only checking for null
                return nullCheck;
            }
            else if (orExpression != null)
            {
                // Only checking for values (no null)
                return orExpression;
            }

            return Expression.Constant(false);
        }

        /// <summary>
        /// Apply global filter across specified fields (or all string fields if none specified)
        /// </summary>
        private static IQueryable<T> ApplyGlobalFilter<T>(IQueryable<T> query, string filterText, string[]? fields, int navigationDepth = 1)
        {
            if (string.IsNullOrWhiteSpace(filterText))
                return query;

            // If no fields specified, discover all string properties
            if (fields == null || !fields.Any())
            {
                fields = GetAllStringPropertyNames(typeof(T), navigationDepth);
                if (!fields.Any())
                    return query; // No string properties to search
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? combinedCondition = null;

            foreach (var field in fields)
            {
                try
                {
                    var propertyAccess = BuildPropertyAccess(parameter, field);
                    if (propertyAccess == null || propertyAccess.Type != typeof(string))
                        continue;

                    // Build null checks for the entire property path (including navigation properties)
                    var nullChecks = BuildPropertyPathNullChecks(parameter, field);
                    
                    // Build the contains check for the final string property
                    var finalPropertyNullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                    var filterConstant = Expression.Constant(filterText);
                    var contains = Expression.Call(propertyAccess, containsMethod, filterConstant);
                    
                    // Combine: navigation != null AND property != null AND property.Contains(filter)
                    Expression condition = Expression.AndAlso(finalPropertyNullCheck, contains);
                    
                    // Add null checks for navigation properties
                    if (nullChecks != null)
                    {
                        condition = Expression.AndAlso(nullChecks, condition);
                    }

                    combinedCondition = combinedCondition == null
                        ? condition
                        : Expression.OrElse(combinedCondition, condition);
                }
                catch
                {
                    // Skip invalid fields
                    continue;
                }
            }

            if (combinedCondition != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedCondition, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        /// <summary>
        /// Build null checks for all navigation properties in a property path
        /// For example, "Assignee.FirstName" needs to check "Assignee != null"
        /// </summary>
        private static Expression? BuildPropertyPathNullChecks(Expression parameter, string propertyPath)
        {
            var parts = propertyPath.Split('.');
            if (parts.Length <= 1)
                return null; // No navigation properties to check
            
            Expression? combinedNullChecks = null;
            Expression currentExpression = parameter;
            
            // Check all parts except the last one (which is the final property)
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var property = currentExpression.Type.GetProperty(parts[i]);
                if (property == null)
                    return null;
                
                currentExpression = Expression.Property(currentExpression, property);
                
                // Build null check for this navigation property
                var nullCheck = Expression.NotEqual(currentExpression, Expression.Constant(null, currentExpression.Type));
                
                combinedNullChecks = combinedNullChecks == null
                    ? nullCheck
                    : Expression.AndAlso(combinedNullChecks, nullCheck);
            }
            
            return combinedNullChecks;
        }

        /// <summary>
        /// Get all string property names from a type, including navigation properties
        /// </summary>
        /// <param name="type">The entity type to inspect</param>
        /// <param name="maxNavigationDepth">Maximum depth for navigation properties (0 = only direct properties, 1 = one level of navigation, etc.)</param>
        private static string[] GetAllStringPropertyNames(Type type, int maxNavigationDepth = 1)
        {
            var stringProperties = new List<string>();
            var processedTypes = new HashSet<Type>(); // Prevent infinite recursion
            
            GetStringPropertiesRecursive(type, "", stringProperties, processedTypes, maxDepth: maxNavigationDepth, currentDepth: 0);
            
            return stringProperties.ToArray();
        }

        /// <summary>
        /// Recursively get string properties, including from navigation properties
        /// </summary>
        private static void GetStringPropertiesRecursive(
            Type type, 
            string prefix, 
            List<string> result, 
            HashSet<Type> processedTypes, 
            int maxDepth, 
            int currentDepth)
        {
            if (currentDepth > maxDepth || !processedTypes.Add(type))
                return; // Prevent infinite recursion
            
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;
                var propertyPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                // Add direct string properties
                if (propType == typeof(string))
                {
                    result.Add(propertyPath);
                }
                // Navigate into complex types (but not collections, primitives, or system types)
                else if (currentDepth < maxDepth && 
                         propType.IsClass && 
                         !propType.IsArray &&
                         !propType.IsGenericType && // Skip List<>, ICollection<>, etc.
                         !propType.IsPrimitive &&
                         !propType.Namespace?.StartsWith("System") == true &&
                         propType != typeof(string) &&
                         propType != typeof(DateTime) &&
                         propType != typeof(DateTimeOffset) &&
                         propType != typeof(TimeSpan) &&
                         propType != typeof(Guid))
                {
                    // Recurse into navigation property
                    GetStringPropertiesRecursive(propType, propertyPath, result, processedTypes, maxDepth, currentDepth + 1);
                }
            }
        }

        /// <summary>
        /// Build property access expression (handles nested properties)
        /// </summary>
        private static Expression BuildPropertyAccess(Expression parameter, string propertyPath)
        {
            var properties = propertyPath.Split('.');
            Expression propertyAccess = parameter;

            foreach (var propertyName in properties)
            {
                var propertyInfo = propertyAccess.Type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new ArgumentException($"Property '{propertyName}' not found on type '{propertyAccess.Type.Name}'");

                propertyAccess = Expression.Property(propertyAccess, propertyInfo);
            }

            return propertyAccess;
        }

        /// <summary>
        /// Get PropertyInfo for a property path (handles nested properties)
        /// </summary>
        private static PropertyInfo? GetPropertyInfo(Type type, string propertyPath)
        {
            var properties = propertyPath.Split('.');
            Type currentType = type;
            PropertyInfo? propertyInfo = null;

            foreach (var propertyName in properties)
            {
                propertyInfo = currentType.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    return null;

                currentType = propertyInfo.PropertyType;
            }

            return propertyInfo;
        }

        /// <summary>
        /// Convert value to target type
        /// </summary>
        private static object? ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            // Handle Newtonsoft.Json JToken types
            if (value is Newtonsoft.Json.Linq.JToken jToken)
            {
                // Check if JToken represents null
                if (jToken.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                {
                    return null;
                }
                
                // Convert JToken to actual value
                value = jToken.ToObject(targetType) ?? jToken.ToString();
            }

            var valueType = value.GetType();
            
            // Already correct type
            if (valueType == targetType)
                return value;

            // Handle enums
            if (targetType.IsEnum)
            {
                if (value is string strValue)
                    return Enum.Parse(targetType, strValue, ignoreCase: true);
                return Enum.ToObject(targetType, value);
            }

            // Handle type conversion
            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Extract array values from various input formats
        /// Returns a tuple of (values list, containsNull flag)
        /// </summary>
        private static (List<object>? values, bool containsNull) ExtractArrayValues(object value, Type targetType)
        {
            if (value == null)
                return (null, false);

            var values = new List<object>();
            var containsNull = false;

            // Handle different array formats
            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item == null)
                    {
                        containsNull = true;
                    }
                    else
                    {
                        try
                        {
                            var converted = ConvertValue(item, targetType);
                            if (converted != null)
                            {
                                values.Add(converted);
                            }
                            else
                            {
                                // ConvertValue returned null (e.g., JToken null)
                                containsNull = true;
                            }
                        }
                        catch
                        {
                            // Skip invalid values
                        }
                    }
                }
            }
            else
            {
                // Single value - treat as array of one
                try
                {
                    var converted = ConvertValue(value, targetType);
                    if (converted != null)
                    {
                        values.Add(converted);
                    }
                    else
                    {
                        // ConvertValue returned null (e.g., JToken null)
                        containsNull = true;
                    }
                }
                catch
                {
                    return (null, false);
                }
            }

            return (values.Any() ? values : null, containsNull);
        }

        /// <summary>
        /// Invoke OrderBy or OrderByDescending dynamically
        /// </summary>
        private static IOrderedQueryable<T> InvokeOrderBy<T>(IQueryable<T> query, LambdaExpression orderByExpression, string methodName)
        {
            var methodCall = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), orderByExpression.ReturnType },
                query.Expression,
                Expression.Quote(orderByExpression)
            );

            return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(methodCall);
        }

        /// <summary>
        /// Invoke ThenBy or ThenByDescending dynamically
        /// </summary>
        private static IOrderedQueryable<T> InvokeThenBy<T>(IOrderedQueryable<T> query, LambdaExpression orderByExpression, string methodName)
        {
            var methodCall = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), orderByExpression.ReturnType },
                query.Expression,
                Expression.Quote(orderByExpression)
            );

            return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(methodCall);
        }

        /// <summary>
        /// Check if a type is numeric
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        /// <summary>
        /// Apply the complete PrimeNG pipeline: Filters → Count → Sorting → Pagination
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">Base query to filter</param>
        /// <param name="filters">PrimeNG filter metadata</param>
        /// <param name="sortMeta">PrimeNG sorting metadata</param>
        /// <param name="page">Zero-based page number</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="globalFilter">Global filter string</param>
        /// <param name="globalFilterFields">Fields to search in for global filter</param>
        /// <returns>Paged result with items and total count</returns>
        public static async Task<PrimeNgPagedResult<T>> ApplyPrimeNgPipelineAsync<T>(
            this IQueryable<T> query,
            Dictionary<string, PrimeNgFilterMetadata>? filters,
            List<PrimeNgSortMetadata>? sortMeta,
            int page,
            int pageSize,
            string? globalFilter = null,
            string[]? globalFilterFields = null,
            int globalFilterNavigationDepth = 1) where T : class
        {
            // Step 1: Apply filters
            query = ApplyFilters(query, filters, globalFilter, globalFilterFields, globalFilterNavigationDepth);

            // Step 2: Get total count before pagination
            var total = await query.CountAsync();

            // Step 3: Apply sorting
            query = ApplySorting(query, sortMeta);

            // Step 4: Apply pagination
            query = query.Skip(page * pageSize).Take(pageSize);

            // Step 5: Execute query and get items
            var items = await query.ToListAsync();

            return new PrimeNgPagedResult<T>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Apply the complete PrimeNG pipeline: Filters → Count → Sorting → Pagination
        /// Overload accepting PrimeNgTableRequest
        /// </summary>
        public static async Task<PrimeNgPagedResult<T>> ApplyPrimeNgPipelineAsync<T>(
            this IQueryable<T> query,
            PrimeNgTableRequest request,
            string[]? globalFilterFields = null,
            int globalFilterNavigationDepth = 1) where T : class
        {
            return await ApplyPrimeNgPipelineAsync(
                query,
                request.Filters,
                request.MultiSortMeta,
                request.Page,
                request.PageSize,
                request.GlobalFilter,
                globalFilterFields,
                globalFilterNavigationDepth
            );
        }
    }
}


