using Dino.Common;
using Dino.Common.Helpers;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Models;
using Dino.Infra.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;
using Dino.CoreMvc.Admin.Logic;
using Dino.CoreMvc.Admin.Attributes.Permissions;
using Dino.CoreMvc.Admin.Helpers;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.CoreMvc.Admin.Models.Exceptions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dino.CoreMvc.Admin.Controllers
{
    public abstract partial class DinoAdminBaseEntityController<TModel, TEFEntity, TIdType>
    {
        #region List Def

        protected abstract Task<ListDef> CreateListDef(string refId);


        protected virtual async Task<List<ListBannerConfig<dynamic>>> ConfigureBannerCaller(string refId)
        {
            return await ConfigureBanners(refId, null, null, null);
        }

        protected virtual async Task<List<ListBannerConfig<dynamic>>> ConfigureBanners(string refId, string id, string modelTypeName = null, string entityTypeName = null)
        {
            // Default implementation returns empty list - override in derived controllers to provide banners
            return new List<ListBannerConfig<dynamic>>();
        }

        /// <summary>
        /// Virtual method for configuring inline form actions that appear above the table.
        /// Override this method in derived controllers to provide custom form actions.
        /// </summary>
        /// <param name="refId">The reference ID if applicable</param>
        /// <returns>List of inline form actions</returns>
        protected virtual async Task<List<InlineFormAction>> CreateInlineFormActions(string refId)
        {
            // Default implementation returns empty list - override in derived controllers to provide actions
            return new List<InlineFormAction>();
        }

        protected virtual async Task<DynamicFormStructure<dynamic>> ConfigureBanner(string refId, string id, string modelTypeName = null, string entityTypeName = null)
        {
            var Assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes());
            // Use reflection to create DynamicFormStructure with specified types

            var modelType = modelTypeName != null ? Assemblies.FirstOrDefault(p => p.Name == modelTypeName) : typeof(TModel);
            var entityType = entityTypeName != null ? Assemblies.FirstOrDefault(p => p.Name == entityTypeName) : typeof(TEFEntity);

            // Use reflection to call the typed method with runtime types
            var method = GetType().GetMethod(nameof(ConfigureBannerTyped), BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                // Fallback: create empty structure
                var fallbackType = typeof(DynamicFormStructure<>).MakeGenericType(modelType);
                return (DynamicFormStructure<dynamic>)Activator.CreateInstance(fallbackType);
            }
            var genericMethod = method.MakeGenericMethod(modelType, entityType);

            var task = (Task)genericMethod.Invoke(this, new object[] { refId, id });
            await task;

            var result = task.GetType().GetProperty("Result").GetValue(task);

            // Create a new DynamicFormStructure<dynamic> and copy the properties
            var dynamicStructure = new DynamicFormStructure<dynamic>();
            var resultType = result.GetType();

            dynamicStructure.Structure = (FormNodeContainer)resultType.GetProperty("Structure").GetValue(result);
            dynamicStructure.InputOptions = (Dictionary<string, List<ListDef.SelectOption>>)resultType.GetProperty("InputOptions").GetValue(result);
            dynamicStructure.ForeignTypes = (Dictionary<string, FormNodeContainer>)resultType.GetProperty("ForeignTypes").GetValue(result);
            dynamicStructure.Model = resultType.GetProperty("Model").GetValue(result);

            return dynamicStructure;
        }

        /// <summary>
        /// Virtual method for configuring the banner displayed above the list.
        /// Override this method in derived controllers to customize the banner.
        /// </summary>
        /// <param name="refId">The reference ID if applicable</param>
        protected virtual async Task<DynamicFormStructure<TFormModel>> ConfigureBannerTyped<TFormModel, TFormEntity>(string refId = null, string id = null)
            where TFormModel : class
            where TFormEntity : class
        {
            try
            {
                // Create or retrieve the model instance FIRST
                dynamic model = null;

                // If ID is provided, load the existing entity and map to model
                TEFEntity entity = null;
                if (!string.IsNullOrEmpty(id))
                {
                    entity = await GetEntityById(id);

                    if (entity != null)
                    {
                        var adminModelType = GetAdminModelType(id, null, entity);

                        model = ModelMappingExtensions.ToAdminModelFromTypes(entity, adminModelType, entity.GetType(), MappingContext).Model;
                        //model = entity.ToAdminModel<TModel, TEFEntity>(MappingContext).Model;
                    }
                    else
                    {
                        Logger.LogError($"Entity with ID {id} not found");
                        return new DynamicFormStructure<TFormModel>();
                    }
                }
                else
                {
                    // Create a new instance with default values
                    model = Activator.CreateInstance<TFormModel>();
                    // Note: ApplyDefaultValues expects TModel, so we skip it for different types
                    if (typeof(TFormModel) == typeof(TModel))
                    {
                        ApplyDefaultValues((TModel)(object)model);
                    }
                }

                // Create the structure container AFTER we have the model
                var formStructure = new DynamicFormStructure<TFormModel>
                {
                    ModelType = typeof(TFormModel).Name,
                    EntityType = typeof(TFormEntity).Name,
                    InputOptions = new Dictionary<string, List<ListDef.SelectOption>>(),
                    Model = (TFormModel)model, // Set the model immediately
                    ForeignTypes = new Dictionary<string, FormNodeContainer>() // Add dictionary for foreign types
                };

                // Build the structure and set values in a single pass
                formStructure.Structure = await BuildModelStructureWithValues(model, formStructure.InputOptions,
                    typeof(TFormModel),
                    true, null, null, formStructure.ForeignTypes);

                return formStructure;
            }
            catch (MissingEndContainerException ex)
            {
                Logger.LogError(ex, "Error generating form structure");
                return new DynamicFormStructure<TFormModel>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating form structure");
                return new DynamicFormStructure<TFormModel>();
            }
        }

        public async Task<JsonResult> GetListDefinition(string refId)
        {
            var listDef = await CreateListDef(refId);

            // Add parent context to title if refId is provided
            if (!string.IsNullOrEmpty(refId))
            {
                var referenceColumn = GetModelPropertyWithAttribute<ParentReferenceColumnAttribute>();
                if (referenceColumn != null)
                {
                    var selectAttr = referenceColumn.GetCustomAttribute<AdminFieldSelectAttribute>();
                    if (selectAttr != null && selectAttr.SourceType == SelectSourceType.DbType)
                    {
                        try
                        {
                            var parentEntityType = selectAttr.OptionsSource as Type;
                            var displayPropertyName = selectAttr.NameFieldOrMethod;
                            var valuePropertyName = selectAttr.ValueFieldOrMethod;

                            if (parentEntityType != null && !string.IsNullOrEmpty(displayPropertyName) && !string.IsNullOrEmpty(valuePropertyName))
                            {
                                var dbSet = DbContext.GetType().GetMethod("Set", Type.EmptyTypes)?.MakeGenericMethod(parentEntityType)?.Invoke(DbContext, null);
                                if (dbSet != null)
                                {
                                    var queryable = dbSet as IQueryable;
                                    var parentEntity = await queryable?.Cast<object>()
                                        .Where(e => EF.Property<object>(e, valuePropertyName).ToString() == refId)
                                        .FirstOrDefaultAsync();

                                    if (parentEntity != null)
                                    {
                                        var displayProperty = parentEntityType.GetProperty(displayPropertyName);
                                        var displayValue = displayProperty?.GetValue(parentEntity)?.ToString();

                                        if (!string.IsNullOrEmpty(displayValue))
                                        {
                                            listDef.Title = $"{listDef.Title} - {displayValue}";
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, $"Error adding parent context to list title for refId '{refId}'");
                        }
                    }
                }
            }

            // Initialize InputOptions as dictionary
            listDef.InputOptions = new Dictionary<string, List<ListDef.SelectOption>>();

            // Configure banners if needed  
            var banners = await ConfigureBannerCaller(refId);
            if (banners != null && banners.Any())
            {
                listDef.Banners = banners;
            }

            // Configure inline form actions
            var inlineFormActions = await CreateInlineFormActions(refId);
            if (inlineFormActions != null && inlineFormActions.Any())
            {
                listDef.InlineFormActions = inlineFormActions;
            }

            // ReservedAdminPropertiesType

            // If no columns data was defined we get all column with list settings
            if (listDef.Columns.IsNullOrEmpty())
            {
                listDef.Columns = new List<ListColumnInfo>();

                var properties = typeof(TModel).GetProperties();
                foreach (var currProperty in properties)
                {
                    var settings = currProperty.GetCustomAttribute<ListSettingsAttribute>();
                    if (settings != null)
                    {
                        var columnInfo = new ListColumnInfo<TModel>(currProperty, properties);
                        listDef.Columns.Add(columnInfo);

                        // Check for SelectType attributes (itself, or inherited from it) and add to InputOptions.
                        var selectAttr = currProperty.GetCustomAttributes().FirstOrDefault(attr =>
                            attr.IsOfTypeOrInherits(typeof(AdminFieldSelectAttribute)));

                        if (selectAttr != null)
                        {
                            var selectTypeAttr = (AdminFieldSelectAttribute)selectAttr;
                            var optionsKey = $"{selectTypeAttr.SourceType}_{selectTypeAttr.OptionsSource}";

                            // Set the InputOptionsKey for this column
                            columnInfo.InputOptionsKey = optionsKey;

                            // Only process if we haven't cached these options yet
                            if (!listDef.InputOptions.ContainsKey(optionsKey))
                            {
                                listDef.InputOptions[optionsKey] = await GetSelectOptions(selectTypeAttr);
                            }
                        }

                        // Check for SortIndex attribute
                        if (currProperty.GetCustomAttribute<SortIndexAttribute>() != null)
                        {
                            columnInfo.IsSortIndex = true;
                        }
                    }
                }

                // Order the columns
                listDef.Columns = listDef.Columns.OrderBy(x => x.PropertyOrderIndex).ToList();
            }

            // Parse actions
            await ParseActions(listDef, listDef.Actions);
            await ParseActions(listDef, listDef.SelfActions);

            // Apply permissions from attribute to ListDef
            listDef.AllowAdd &= await CheckPermission(PermissionType.Add, null, false);
            listDef.AllowEdit &= await CheckPermission(PermissionType.Edit, null, false);
            listDef.AllowDelete &= await CheckPermission(PermissionType.Delete, null, false);

            bool canExport = await CheckPermission(PermissionType.Export, null, false);

            if (!canExport)
            {
                listDef.AllowedExportFormats = ExportFormat.None;
            }

            if (!await CheckPermission(PermissionType.Archive, null, false))
            {
                listDef.ShowArchive = false;
            }

            return CreateJsonResponse(result: true, data: listDef, error: null);
        }

        /// <summary>
        /// Parse actions and build the structure for confirmation dialogs
        /// </summary>
        /// <param name="listDef">The list definition</param>
        /// <param name="actions">The action list</param>
        private async Task ParseActions(ListDef listDef, List<ListAction> actions)
        {
            if (actions.IsNotNullOrEmpty())
            {
                foreach (var currAction in actions)
                {
                    if (currAction.ConfirmationDialog != null)
                    {
                        if (currAction.ConfirmationDialog.FormStructureType != null)
                        {
                            var model = Activator.CreateInstance(currAction.ConfirmationDialog.FormStructureType);

                            // Build the structure and set values in a single pass
                            currAction.ConfirmationDialog.Structure = await BuildModelStructureWithValues(model, listDef.InputOptions);
                        }
                    }
                }
            }
        }

        #endregion

        #region List

        [HttpPost]
        public virtual async Task<JsonResult> List([FromBody] ListRetrieveParams listParams,
            [FromQuery] bool showArchive = false, [FromQuery] bool showDeleted = false, [FromQuery] string refId = null)
        {
            if (!await CheckPermission(PermissionType.View, refId))
            {
                return CreateJsonResponse(false, null, "You do not have permission to view this list.", false);
            }

            var listData = await CreateListData(refId, showArchive, showDeleted, listParams);
            return CreateJsonResponse(true, listData, null, true);
        }

        protected virtual async Task<ListData<TModel>> CreateListData(string refId, bool? showArchive, bool? showDeleted, ListRetrieveParams listParams)
        {
            // Check if archive filtering is applicable
            var modelType = typeof(TModel);
            var hasArchiveProperty = modelType.GetProperties()
                .Any(p => p.GetCustomAttribute<ArchiveIndicatorAttribute>() != null);

            if (!hasArchiveProperty)
            {
                showArchive = null;
            }

            var itemsQueryable = GetFilteredData(refId, showArchive, showDeleted, listParams);
            var totalRecords = await itemsQueryable.CountAsync();

            // Apply sorting
            itemsQueryable = ApplySorting(itemsQueryable, listParams);

            // Apply paging
            var items = await itemsQueryable
                .Skip(listParams.PageIndex * listParams.PageSize)
                .Take(listParams.PageSize)
                .ToListAsync();

            // TODO: Check if there's a good way to map only the properties we need, to reduce mapping time. For complex model it's a lot.
            var convertedItems = items.SelectList(x => x.ToAdminModel<TModel, TEFEntity>(MappingContext));
            var adminItems = convertedItems.SelectList(x => x.Model);

            // Errors.
            Dictionary<string, List<DinoAdminConvertError>> conversionErrors =
                new Dictionary<string, List<DinoAdminConvertError>>();
            foreach (var itemErrors in convertedItems.Select(x => x.ValidationErrors))
            {
                foreach (var error in itemErrors)
                {
                    conversionErrors.AddOrSet(error.Key, error.Value);
                }
            }

            return new ListData<TModel>
            {
                Items = adminItems,
                RecordsTotal = totalRecords,
                RecordsFiltered = items.Count,
                ConversionErrors = conversionErrors
            };
        }

        #region GetFilteredData

        protected virtual IQueryable<TEFEntity> GetFilteredData(string refId, bool? showArchive, bool? showDeleted, ListRetrieveParams listParams)
        {
            var query = DbContext.Set<TEFEntity>().AsQueryable();

            // Apply archive filter if applicable
            showArchive = showArchive.HasValue ? showArchive.Value : false;
            var modelArchiveProperty = GetModelPropertyWithAttribute<ArchiveIndicatorAttribute>();

            // If the refId is not null or empty, we need to filter by the reference column and use the advanced filters to do so.
            if (refId.IsNotNullOrEmpty())
            {
                var referenceColumn = GetModelPropertyWithAttribute<ParentReferenceColumnAttribute>();
                if (referenceColumn != null)
                {
                    listParams.AdvancedFilters.Add(new AdvancedListFilter
                    {
                        PropertyName = referenceColumn.Name,
                        Rules = new List<FilterRule>
                        {
                            new FilterRule
                            {
                                Operator = FilterOperator.Equals,
                                Value = refId
                            }
                        }
                    });
                }
            }

            if (modelArchiveProperty != null)
            {
                var parameter = Expression.Parameter(typeof(TEFEntity), "x");
                var property = Expression.Property(parameter, modelArchiveProperty.Name);
                var value = Expression.Constant(showArchive.Value); // We want Archived == showArchive.Value
                var equals = Expression.Equal(property, value);
                var lambda = Expression.Lambda<Func<TEFEntity, bool>>(equals, parameter);

                query = query.Where(lambda); // Show only items matching the archive state
            }

            // Apply delete filter if applicable
            showDeleted = showDeleted.HasValue ? showDeleted.Value : false;
            var modelDeleteProperty = GetModelPropertyWithAttribute<DeletionIndicatorAttribute>();

            if (modelDeleteProperty != null)
            {
                var parameter = Expression.Parameter(typeof(TEFEntity), "x");
                var property = Expression.Property(parameter, modelDeleteProperty.Name);
                var value = Expression.Constant(showDeleted.Value); // We want IsDeleted == showDeleted.Value
                var equals = Expression.Equal(property, value);
                var lambda = Expression.Lambda<Func<TEFEntity, bool>>(equals, parameter);

                query = query.Where(lambda); // Show only items matching the delete state
            }

            // Apply text search filter
            if (!string.IsNullOrEmpty(listParams.Filter))
            {
                query = ApplyTextFilter(query, listParams.Filter);
            }

            // Apply advanced filters
            if (listParams.AdvancedFilters != null && listParams.AdvancedFilters.Any())
            {
                query = ApplyAdvancedFilters(query, listParams.AdvancedFilters);
            }

            return query;
        }

        #endregion

        #region ApplyTextFilter

        protected virtual IQueryable<TEFEntity> ApplyTextFilter(IQueryable<TEFEntity> query, string filterText)
        {
            var stringProperties = typeof(TEFEntity).GetProperties()
                .Where(p => p.PropertyType == typeof(string));

            if (!stringProperties.Any())
                return query;

            var parameter = Expression.Parameter(typeof(TEFEntity), "x");
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var filterConstant = Expression.Constant(filterText);

            var conditions = stringProperties.Select(prop =>
            {
                var property = Expression.Property(parameter, prop);
                var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
                var contains = Expression.Call(property, containsMethod, filterConstant);
                return Expression.AndAlso(nullCheck, contains);
            });

            var combinedCondition = conditions.Aggregate(Expression.OrElse);
            var lambda = Expression.Lambda<Func<TEFEntity, bool>>(combinedCondition, parameter);

            return query.Where(lambda);
        }

        #endregion

        #region ApplyAdvancedFilters

        protected virtual IQueryable<TEFEntity> ApplyAdvancedFilters(IQueryable<TEFEntity> query, List<AdvancedListFilter> filters)
        {
            foreach (var filter in filters)
            {
                var propertyInfo = typeof(TEFEntity).GetProperty(filter.PropertyName.FromCamelCase());
                if (propertyInfo == null) continue;

                var parameter = Expression.Parameter(typeof(TEFEntity), "x");
                var property = Expression.Property(parameter, propertyInfo);

                if (!filter.Rules.Any()) continue;

                // Create expressions for all rules
                var expressions = filter.Rules
                    .Select(rule => CreateFilterExpression(parameter, property, rule, propertyInfo.PropertyType))
                    .Where(expr => expr != null)
                    .ToList();

                if (!expressions.Any()) continue;

                // Combine expressions based on matchAll flag
                Expression combinedExpression;
                if (filter.MatchAll)
                {
                    // AND all expressions together
                    combinedExpression = expressions.Aggregate(Expression.AndAlso);
                }
                else
                {
                    // OR all expressions together
                    combinedExpression = expressions.Aggregate(Expression.OrElse);
                }

                var lambda = Expression.Lambda<Func<TEFEntity, bool>>(combinedExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        #endregion

        #region CreateFilterExpression

        protected virtual Expression CreateFilterExpression(ParameterExpression parameter, MemberExpression property, FilterRule rule, Type propertyType)
        {
            // Handle null/empty checks first as they don't need value conversion
            switch (rule.Operator)
            {
                case FilterOperator.IsNull:
                    return Expression.Equal(property, Expression.Constant(null));
                case FilterOperator.IsNotNull:
                    return Expression.NotEqual(property, Expression.Constant(null));
                case FilterOperator.IsEmpty when propertyType == typeof(string):
                    return Expression.OrElse(
                        Expression.Equal(property, Expression.Constant(null)),
                        Expression.Equal(property, Expression.Constant(string.Empty))
                    );
                case FilterOperator.IsNotEmpty when propertyType == typeof(string):
                    return Expression.AndAlso(
                        Expression.NotEqual(property, Expression.Constant(null)),
                        Expression.NotEqual(property, Expression.Constant(string.Empty))
                    );
                case FilterOperator.In:
                    if (string.IsNullOrEmpty(rule.Value)) return null;

                    try
                    {
                        var values = rule.Value.Split(',')
                            .Select(v => v.Trim())
                            .Select(v => propertyType.IsEnum
                                ? Enum.Parse(propertyType, v)
                                : Convert.ChangeType(v, propertyType))
                            .ToList();

                        if (!values.Any()) return null;

                        var listType = typeof(List<>).MakeGenericType(propertyType);
                        var list = Activator.CreateInstance(listType);
                        var addMethod = listType.GetMethod("Add");
                        foreach (var value in values)
                        {
                            addMethod.Invoke(list, new[] { value });
                        }

                        var containsMethodInfo = listType.GetMethod("Contains");
                        var listConstant = Expression.Constant(list);
                        return Expression.Call(listConstant, containsMethodInfo, property);
                    }
                    catch
                    {
                        return null;
                    }
            }

            // For other operators that need values, convert the value
            object valueObj = null;
            if (!string.IsNullOrEmpty(rule.Value))
            {
                try
                {
                    if (propertyType.IsEnum)
                    {
                        valueObj = Enum.Parse(propertyType, rule.Value);
                    }
                    else if (propertyType.IsGenericType &&
                             propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(propertyType);
                        valueObj = Convert.ChangeType(rule.Value, underlyingType);
                    }
                    else
                    {
                        valueObj = Convert.ChangeType(rule.Value, propertyType);
                    }
                }
                catch
                {
                    return null; // Invalid value conversion, skip this filter
                }
            }

            var constant = Expression.Constant(valueObj, propertyType);

            switch (rule.Operator)
            {
                case FilterOperator.Equals:
                    return Expression.Equal(property, constant);

                case FilterOperator.NotEquals:
                    return Expression.NotEqual(property, constant);

                case FilterOperator.Gt:
                    return Expression.GreaterThan(property, constant);

                case FilterOperator.Gte:
                    return Expression.GreaterThanOrEqual(property, constant);

                case FilterOperator.Lt:
                    return Expression.LessThan(property, constant);

                case FilterOperator.Lte:
                    return Expression.LessThanOrEqual(property, constant);

                case FilterOperator.Contains when propertyType == typeof(string):
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    return Expression.Call(property, containsMethod, constant);

                case FilterOperator.NotContains when propertyType == typeof(string):
                    var notContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var notContainsCall = Expression.Call(property, notContainsMethod, constant);
                    return Expression.Not(notContainsCall);

                case FilterOperator.StartsWith when propertyType == typeof(string):
                    var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
                    return Expression.Call(property, startsWithMethod, constant);

                case FilterOperator.EndsWith when propertyType == typeof(string):
                    var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
                    return Expression.Call(property, endsWithMethod, constant);

                case FilterOperator.DateIs when propertyType == typeof(DateTime) || propertyType == typeof(DateTime?):
                    return Expression.Equal(property, constant);

                case FilterOperator.DateIsNot when propertyType == typeof(DateTime) || propertyType == typeof(DateTime?):
                    return Expression.NotEqual(property, constant);

                case FilterOperator.DateIsBefore when propertyType == typeof(DateTime) || propertyType == typeof(DateTime?):
                    return Expression.LessThan(property, constant);

                case FilterOperator.DateIsAfter when propertyType == typeof(DateTime) || propertyType == typeof(DateTime?):
                    return Expression.GreaterThan(property, constant);


                case FilterOperator.Between:
                    if (string.IsNullOrEmpty(rule.Value2)) return null;

                    object value2Obj;
                    try
                    {
                        value2Obj = propertyType.IsEnum
                            ? Enum.Parse(propertyType, rule.Value2)
                            : Convert.ChangeType(rule.Value2, propertyType);
                    }
                    catch
                    {
                        return null;
                    }

                    var constant2 = Expression.Constant(value2Obj, propertyType);
                    return Expression.AndAlso(
                        Expression.GreaterThanOrEqual(property, constant),
                        Expression.LessThanOrEqual(property, constant2)
                    );

                default:
                    return null;
            }
        }

        #endregion

        #region ApplySorting

        protected virtual IQueryable<TEFEntity> ApplySorting(IQueryable<TEFEntity> query, ListRetrieveParams listParams)
        {
            if (listParams.SortColumns == null || !listParams.SortColumns.Any())
            {
                // Look for property with SortIndex attribute as default sort
                var sortIndexProperty = typeof(TEFEntity).GetProperties()
                    .FirstOrDefault(p => p.GetCustomAttribute<SortIndexAttribute>() != null);

                if (sortIndexProperty != null)
                {
                    return query.OrderByPropertyName(sortIndexProperty.Name);
                }

                // If no sort index, return as is
                return query;
            }

            var isFirstSort = true;
            IOrderedQueryable<TEFEntity> orderedQuery = null;

            foreach (var sortColumn in listParams.SortColumns)
            {
                var propertyName = sortColumn.PropertyName.FromCamelCase();
                var isAscending = sortColumn.Direction == SortDirection.Ascending;

                if (isFirstSort)
                {
                    orderedQuery = isAscending
                        ? query.OrderByPropertyName(propertyName)
                        : query.OrderByDescendingPropertyName(propertyName);
                    isFirstSort = false;
                }
                else
                {
                    orderedQuery = isAscending
                        ? orderedQuery.ThenByPropertyName(propertyName)
                        : orderedQuery.ThenByDescendingPropertyName(propertyName);
                }
            }

            return orderedQuery ?? query;
        }

        #endregion

        #endregion

        #region Select Input Values Retrieval

        private async Task<List<ListDef.SelectOption>> GetSelectOptions(AdminFieldSelectAttribute selectTypeAttr)
        {
            var options = new List<ListDef.SelectOption>();

            switch (selectTypeAttr.SourceType)
            {
                case SelectSourceType.Function:
                    // Get function from controller
                    var methodInfo = this.GetType().GetMethod((string)selectTypeAttr.OptionsSource);
                    if (methodInfo != null)
                    {
                        var result = methodInfo.Invoke(this, null);
                        if (result is Dictionary<int, string> dict)
                        {
                            options.AddRange(dict.Select(kvp => new ListDef.SelectOption { Value = kvp.Key, Display = kvp.Value }));
                        }
                        else if (result is IEnumerable<ListDef.SelectOption> selectOptions)
                        {
                            options.AddRange(selectOptions);
                        }
                    }
                    break;

                case SelectSourceType.Enum:
                    // Try direct type lookup first
                    var enumType = (Type)selectTypeAttr.OptionsSource;

                    if (enumType != null)
                    {
                        foreach (var value in Enum.GetValues(enumType))
                        {
                            var name = value.ToString();
                            var displayAttr = enumType.GetField(name).GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                            var displayName = displayAttr?.Description ?? name;
                            options.Add(new ListDef.SelectOption { Value = value, Display = displayName });
                        }
                    }
                    break;

                case SelectSourceType.DbType:
                    var entityType = (Type)selectTypeAttr.OptionsSource;
                    if (entityType != null && DbContext != null)
                    {
                        // Get the DbSet for the entity type
                        var dbSetProperty = DbContext.GetType().GetProperties()
                            .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                               p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                               p.PropertyType.GetGenericArguments()[0] == entityType);

                        if (dbSetProperty != null)
                        {
                            // Get name and value fields/properties
                            var nameField = !string.IsNullOrEmpty(selectTypeAttr.NameFieldOrMethod)
                                ? selectTypeAttr.NameFieldOrMethod
                                : "Name"; // Default to "Name" if not specified

                            var valueField = !string.IsNullOrEmpty(selectTypeAttr.ValueFieldOrMethod)
                                ? selectTypeAttr.ValueFieldOrMethod
                                : "Id";  // Default to "Id" if not specified

                            // Get all entities
                            var dbSet = dbSetProperty.GetValue(DbContext);
                            var entities = await ((IQueryable<object>)dbSet).ToListAsync();

                            // Map to SelectOptions using reflection
                            foreach (var entity in entities)
                            {
                                var nameValue = entity.GetType().GetProperty(nameField)?.GetValue(entity)?.ToString();
                                var idValue = entity.GetType().GetProperty(valueField)?.GetValue(entity);

                                if (nameValue != null && idValue != null)
                                {
                                    options.Add(new ListDef.SelectOption
                                    {
                                        Value = idValue,
                                        Display = nameValue
                                    });
                                }
                            }

                            // Order by display name
                            options = options.OrderBy(o => o.Display).ToList();
                        }
                    }
                    break;
            }

            // Process name/value field methods if specified
            if (!string.IsNullOrEmpty(selectTypeAttr.NameFieldOrMethod))
            {
                var nameMethod = this.GetType().GetMethod(selectTypeAttr.NameFieldOrMethod);
                if (nameMethod != null)
                {
                    var processedOptions = nameMethod.Invoke(this, new object[] { options }) as List<ListDef.SelectOption>;
                    if (processedOptions != null)
                    {
                        options = processedOptions;
                    }
                }
            }

            if (!string.IsNullOrEmpty(selectTypeAttr.ValueFieldOrMethod))
            {
                var valueMethod = this.GetType().GetMethod(selectTypeAttr.ValueFieldOrMethod);
                if (valueMethod != null)
                {
                    var processedOptions = valueMethod.Invoke(this, new object[] { options }) as List<ListDef.SelectOption>;
                    if (processedOptions != null)
                    {
                        options = processedOptions;
                    }
                }
            }

            return options;
        }

        #endregion

        #region Reorder

        [HttpPost]
        public virtual async Task<JsonResult> SaveReorder([FromBody] List<TIdType> entityIds)
        {
            if (!await CheckPermission(PermissionType.Edit))
            {
                return CreateJsonResponse(false, null, "You do not have permission to reorder entities.");
            }

            // Get the sort index property
            var sortIndexProperty = GetModelPropertyWithAttribute<SortIndexAttribute>();
            if (sortIndexProperty == null)
            {
                return CreateJsonResponse(false, null, "Sort index property not found");
            }

            var efSortIndexProperty = typeof(TEFEntity).GetProperty(sortIndexProperty.Name);
            if (efSortIndexProperty == null)
            {
                return CreateJsonResponse(false, null, "Sort index property not found in entity");
            }

            var result = false;

            var query = GetEntitiesByIds(entityIds);

            // Create the lambda expression to convert the entities to a dictionary
            var parameter = Expression.Parameter(typeof(TEFEntity), "x");
            var keySelector = Expression.Lambda<Func<TEFEntity, TIdType>>(Expression.Convert(Expression.Property(parameter, "Id"), typeof(TIdType)), parameter);
            var elementSelector = Expression.Lambda<Func<TEFEntity, TEFEntity>>(parameter, parameter);

            // Execute the query and convert the entities to a dictionary
            var entities = await query.ToDictionaryAsync(keySelector.Compile(), elementSelector.Compile());

            if (entities.Any())
            {
                // Get the minimum order index using reflection
                var parameterExpr = Expression.Parameter(typeof(TEFEntity), "y");
                var propertyExpr = Expression.Property(parameterExpr, efSortIndexProperty);
                var lambdaExpr = Expression.Lambda<Func<TEFEntity, int>>(propertyExpr, parameterExpr);
                var currOrderIndex = entities.Values.Min(lambdaExpr.Compile());

                foreach (var currEntityId in entityIds)
                {
                    var currEntity = entities[currEntityId];
                    efSortIndexProperty.SetValue(currEntity, currOrderIndex);

                    currOrderIndex++;
                }

                await DbContext.SaveChangesAsync();

                // Cache.
                await OnSortChangedForCache(entityIds);

                result = true;
            }

            return CreateJsonResponse(result, null, null);
        }

        #endregion

        #region DeleteAll

        [HttpDelete]
        public virtual async Task<JsonResult> DeleteAll(string refId = null)
        {
            if (!await CheckPermission(PermissionType.Delete, refId))
            {
                return CreateJsonResponse(false, null, "You do not have permission to delete all items.", false);
            }

            try
            {
                IQueryable<TEFEntity> entitiesToDeleteQuery = DbContext.Set<TEFEntity>();

                if (!string.IsNullOrEmpty(refId))
                {
                    PropertyInfo referenceColumnProperty = GetModelPropertyWithAttribute<ParentReferenceColumnAttribute>();
                    if (referenceColumnProperty != null)
                    {
                        entitiesToDeleteQuery = entitiesToDeleteQuery.Where(e => EF.Property<string>(e, referenceColumnProperty.Name) == refId);
                    }
                    else
                    {
                        Logger.LogWarning($"DeleteAll called with refId '{refId}' but no ParentReferenceColumnAttribute found on model {typeof(TModel).Name}. No refId filtering applied.");
                    }
                }

                var itemsToRemove = await entitiesToDeleteQuery.ToListAsync();

                if (itemsToRemove.Any())
                {
                    DbContext.Set<TEFEntity>().RemoveRange(itemsToRemove);
                    await DbContext.SaveChangesAsync();
                    return CreateJsonResponse(true, null, $"{itemsToRemove.Count} item(s) deleted successfully.", false);
                }
                else
                {
                    return CreateJsonResponse(true, null, "No items found to delete.", false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting all items");
                return CreateJsonResponse(false, null, $"Error deleting all items: {ex.Message}", false);
            }
        }

        #endregion


        #region Delete

        [HttpPost]
        public virtual async Task<JsonResult> Delete([FromBody] DeleteRequest request)
        {
            if (!await CheckPermission(PermissionType.Delete))
            {
                return CreateJsonResponse(false, null, "You do not have permission to delete entities.");
            }

            try
            {
                if (request == null || request.Id == null)
                {
                    return CreateJsonResponse(false, null, "Invalid delete request");
                }

                var entity = await DbContext.Set<TEFEntity>().FindAsync(request.Id);
                if (entity == null)
                {
                    return CreateJsonResponse(false, null, "Entity not found");
                }

                // Check if entity has IsDeleted property
                var deleteProperty = typeof(TEFEntity).GetProperty("IsDeleted");
                if (deleteProperty != null)
                {
                    // Soft delete - set IsDeleted to true
                    deleteProperty.SetValue(entity, true);
                    await DbContext.SaveChangesAsync();

                }
                else
                {
                    // Delete form DB, with option to overload.
                    await DeleteEntityFromDb(entity);

                    await DbContext.SaveChangesAsync();
                }

                await OnEntityDeletedForCache(request.Id);

                return CreateJsonResponse(true, null, null);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "Error deleting entity");
                return CreateJsonResponse(false, null, "Error deleting entity: " + ex.Message);
            }
        }

        public virtual async Task DeleteEntityFromDb(TEFEntity entity)
        {
            // Hard delete - remove from database
            DbContext.Set<TEFEntity>().Remove(entity);
        }

        public class DeleteRequest
        {
            public TIdType? Id { get; set; }
        }

        #endregion

        #region ArchiveAll

        [HttpPost]
        public virtual async Task<JsonResult> ArchiveAll(string refId = null)
        {
            if (!await CheckPermission(PermissionType.Delete, refId))
            {
                return CreateJsonResponse(false, null, "You do not have permission to archive all items.", false);
            }

            try
            {
                // Check if entity has Archived property first
                var archiveProperty = typeof(TEFEntity).GetProperty("Archived");
                if (archiveProperty == null)
                {
                    return CreateJsonResponse(false, null, "Entity does not support archiving.", false);
                }

                IQueryable<TEFEntity> entitiesToArchiveQuery = DbContext.Set<TEFEntity>();

                // Filter out already archived items
                entitiesToArchiveQuery = entitiesToArchiveQuery.Where(e => !EF.Property<bool>(e, "Archived"));

                if (!string.IsNullOrEmpty(refId))
                {
                    PropertyInfo referenceColumnProperty = GetModelPropertyWithAttribute<ParentReferenceColumnAttribute>();
                    if (referenceColumnProperty != null)
                    {
                        entitiesToArchiveQuery = entitiesToArchiveQuery.Where(e => EF.Property<string>(e, referenceColumnProperty.Name) == refId);
                    }
                    else
                    {
                        Logger.LogWarning($"ArchiveAll called with refId '{refId}' but no ParentReferenceColumnAttribute found on model {typeof(TModel).Name}. No refId filtering applied.");
                    }
                }

                var itemsToArchive = await entitiesToArchiveQuery.ToListAsync();

                if (!itemsToArchive.Any())
                {
                    return CreateJsonResponse(true, null, "No items found to archive.", false);
                }

                // Archive all items (no need to check individually since we filtered them already)
                foreach (var item in itemsToArchive)
                {
                    archiveProperty.SetValue(item, true);
                }

                await DbContext.SaveChangesAsync();
                return CreateJsonResponse(true, null, $"{itemsToArchive.Count} item(s) archived successfully.", false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error archiving all items");
                return CreateJsonResponse(false, null, $"Error archiving all items: {ex.Message}", false);
            }
        }

        #endregion

        #region Archive

        [HttpPost]
        public virtual async Task<JsonResult> Archive([FromBody] ArchiveRequest request)
        {
            if (!await CheckPermission(PermissionType.Delete))
            {
                return CreateJsonResponse(false, null, "You do not have permission to archive entities.");
            }

            try
            {
                if (request == null || request.Id == null)
                {
                    return CreateJsonResponse(false, null, "Invalid archive request");
                }

                var entity = await DbContext.Set<TEFEntity>().FindAsync(request.Id);
                if (entity == null)
                {
                    return CreateJsonResponse(false, null, "Entity not found");
                }

                // Check if entity has Archived property
                var archiveProperty = typeof(TEFEntity).GetProperty("Archived");
                if (archiveProperty == null)
                {
                    return CreateJsonResponse(false, null, "Entity does not support archiving.");
                }

                // Check if already archived
                var currentValue = archiveProperty.GetValue(entity);
                if (currentValue is bool isArchived && isArchived)
                {
                    return CreateJsonResponse(true, null, "Item is already archived.");
                }

                // Set archived to true
                archiveProperty.SetValue(entity, true);
                await DbContext.SaveChangesAsync();

                return CreateJsonResponse(true, null, "Item archived successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, "Error archiving entity");
                return CreateJsonResponse(false, null, "Error archiving entity: " + ex.Message);
            }
        }

        public class ArchiveRequest
        {
            public TIdType? Id { get; set; }
        }

        #endregion
    }
}
