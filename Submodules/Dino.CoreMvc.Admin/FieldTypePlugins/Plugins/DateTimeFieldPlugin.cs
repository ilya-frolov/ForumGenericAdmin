using System;
using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for date and time field types
    /// </summary>
    public class DateTimeFieldPlugin : BaseFieldTypePlugin<AdminFieldDateTimeAttribute, object>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "DateTime";

        public DateTimeFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Validates a date/time field value
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(object value, PropertyInfo property)
        {
            var fieldAttribute = property.GetCustomAttribute<AdminFieldDateTimeAttribute>();
            
            // Skip validation if null
            if (value == null)
                return base.Validate(null, property);
            
            var errorMessages = new List<string>();
            
            // Validate based on the field type
            switch (fieldAttribute.DateType)
            {
                case DateTimePickerType.Date:
                case DateTimePickerType.DateTime:
                    if (!(value is DateTime) && !(value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(value.GetType()) == typeof(DateTime)))
                    {
                        errorMessages.Add($"Field '{property.Name}' must be a valid date");
                    }
                    break;
                    
                case DateTimePickerType.Time:
                    if (!(value is TimeSpan) && !(value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(value.GetType()) == typeof(TimeSpan)))
                    {
                        errorMessages.Add($"Field '{property.Name}' must be a valid time");
                    }
                    break;
            }
            
            // If there are validation errors, return them
            if (errorMessages.Count > 0)
                return (false, errorMessages);
                
            // Otherwise, delegate to base validation
            return base.Validate(value, property);
        }

        /// <summary>
        /// Prepares a typed value for database storage
        /// </summary>
        protected override object PrepareTypedValueForDb(object value, PropertyInfo property)
        {
            if (value == null)
                return null;
                
            var fieldAttribute = property.GetCustomAttribute<AdminFieldDateTimeAttribute>();
            
            // Handle based on the field type
            switch (fieldAttribute.DateType)
            {
                case DateTimePickerType.Date:
                    // For date-only fields, ensure time component is set to midnight
                    if (value is DateTime dateValue)
                    {
                        return dateValue.Date;
                    }
                    break;
                    
                case DateTimePickerType.Time:
                    // For time-only fields, ensure we just store the time component
                    if (value is TimeSpan timeSpan)
                    {
                        return timeSpan;
                    }
                    else if (value is DateTime dateTimeForTime)
                    {
                        return dateTimeForTime.TimeOfDay;
                    }
                    break;
                    
                case DateTimePickerType.DateTime:
                    // Full date and time, no special handling needed
                    if (value is DateTime dateTime)
                    {
                        return dateTime;
                    }
                    break;
            }
            
            // If we couldn't handle it specifically, return as is
            return value;
        }

        /// <summary>
        /// Prepares a database value for model use
        /// </summary>
        protected override object PrepareTypedValueForModel(object dbValue, PropertyInfo property)
        {
            if (dbValue == null)
                return null;
                
            var fieldAttribute = property.GetCustomAttribute<AdminFieldDateTimeAttribute>();
            
            // Handle based on the property's type and the field attribute type
            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                // For DateTime properties
                if (dbValue is DateTime dtValue)
                {
                    // If it's a Date field, ensure time is midnight
                    if (fieldAttribute.DateType == DateTimePickerType.Date)
                    {
                        return dtValue.Date;
                    }
                    return dtValue;
                }
                
                // Try to parse from string
                if (dbValue is string strValue && DateTime.TryParse(strValue, out DateTime parsedDate))
                {
                    if (fieldAttribute.DateType == DateTimePickerType.Date)
                    {
                        return parsedDate.Date;
                    }
                    return parsedDate;
                }
            }
            else if (property.PropertyType == typeof(TimeSpan) || property.PropertyType == typeof(TimeSpan?))
            {
                // For TimeSpan properties
                if (dbValue is TimeSpan tsValue)
                {
                    return tsValue;
                }
                
                // If the DB value is a DateTime, extract the TimeOfDay
                if (dbValue is DateTime dtForTime)
                {
                    return dtForTime.TimeOfDay;
                }
                
                // Try to parse from string
                if (dbValue is string strTimeValue && TimeSpan.TryParse(strTimeValue, out TimeSpan parsedTime))
                {
                    return parsedTime;
                }
            }
            
            // If we couldn't handle it specifically, rely on base implementation
            return base.PrepareTypedValueForModel(dbValue, property);
        }
    }
} 