using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dino.Infra.Reflection
{
	public static class ReflectionHelpers
    {
        #region GetAssemblyTypesWithoutLoading

        //public static List<string> GetAssemblyTypesWithoutLoading(byte[] assemblyData)
        //   {
        //    List<string> types = null;

        //    AppDomain appDomain = null;
        //    try
        //    {
        //	    var loader = CreateDomainAndAssemblyLoader("Tmp" + DateTime.Now, out appDomain);

        //	    loader.LoadAssembly(assemblyData);

        //	    types = loader.GetTypeNames();
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //    finally
        //    {
        //	    if (appDomain != null)
        //	    {
        //		    AppDomain.Unload(appDomain);
        //	    }
        //    }

        //    return types;
        //   }

        #endregion

        #region CreateDomainAndAssemblyLoader

        //public static AssemblyLoader CreateDomainAndAssemblyLoader(string domainName, out AppDomain appDomain)
        //{
        //	AssemblyLoader loader = null;

        //	appDomain = null;
        //	try
        //	{
        //		// Construct and initialize settings for a second AppDomain.
        //		var ads = new AppDomainSetup
        //		{
        //			ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
        //			DisallowBindingRedirects = false,
        //			DisallowCodeDownload = true,
        //			ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
        //			PrivateBinPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath
        //		};

        //		appDomain = AppDomain.CreateDomain(domainName, null, ads);

        //		// Loader lives in another AppDomain
        //		loader = (AssemblyLoader)appDomain.CreateInstanceAndUnwrap(typeof(AssemblyLoader).Assembly.FullName,
        //																   typeof(AssemblyLoader).FullName);
        //	}
        //	catch (Exception ex)
        //	{
        //		if (appDomain != null)
        //		{
        //			AppDomain.Unload(appDomain);
        //		}
        //	}

        //	return loader;
        //}

        #endregion

        #region GetMemberInfo

        public static MemberInfo GetMemberInfo(LambdaExpression lambdaExpression)
        {
            return GetMemberInfo(lambdaExpression.Body);
        }

        public static MemberInfo GetMemberInfo(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentException("");
            }

            if (expression is MemberExpression)
            {
                // Reference type property or field
                var memberExpression = (MemberExpression)expression;
                return memberExpression.Member;
            }

            if (expression is MethodCallExpression)
            {
                // Reference type method
                var methodCallExpression = (MethodCallExpression)expression;
                return methodCallExpression.Method;
            }

            if (expression is UnaryExpression)
            {
                // Property, field of method returning value type
                var unaryExpression = (UnaryExpression)expression;
                return GetMemberInfo(unaryExpression);
            }

            throw new ArgumentException("");
        }

        private static MemberInfo GetMemberInfo(UnaryExpression unaryExpression)
        {
            if (unaryExpression.Operand is MethodCallExpression)
            {
                var methodExpression = (MethodCallExpression)unaryExpression.Operand;
                return methodExpression.Method;
            }

            return ((MemberExpression)unaryExpression.Operand).Member;
        }

        #endregion

        #region GetMemberName

        public static string GetMemberName<T>(Expression<Func<T, object>> propertyLambda)
		{
			return GetMemberName((LambdaExpression)propertyLambda);
		}

		public static string GetMemberName(LambdaExpression lambdaExpression)
		{
			return GetMemberName(lambdaExpression.Body);
		}

		public static string GetMemberName(Expression expression)
		{
			if (expression == null)
			{
				throw new ArgumentException("");
			}

			if (expression is MemberExpression)
			{
				// Reference type property or field
				var memberExpression = (MemberExpression)expression;
				return memberExpression.Member.Name;
			}

			if (expression is MethodCallExpression)
			{
				// Reference type method
				var methodCallExpression = (MethodCallExpression)expression;
				return methodCallExpression.Method.Name;
			}

			if (expression is UnaryExpression)
			{
				// Property, field of method returning value type
				var unaryExpression = (UnaryExpression)expression;
				return GetMemberName(unaryExpression);
			}

			throw new ArgumentException("");
		}

		private static string GetMemberName(UnaryExpression unaryExpression)
		{
			if (unaryExpression.Operand is MethodCallExpression)
			{
				var methodExpression = (MethodCallExpression)unaryExpression.Operand;
				return methodExpression.Method.Name;
			}

			return ((MemberExpression)unaryExpression.Operand).Member.Name;
		}

		#endregion

		#region GetMemberAttribute

		public static T GetMemberAttribute<T>(LambdaExpression lambdaExpression) where T : Attribute
		{
			return GetMemberAttribute<T>(lambdaExpression.Body);
		}

		public static T GetMemberAttribute<T>(Expression expression) where T : Attribute
		{
			if (expression == null)
			{
				throw new ArgumentException("");
			}

			if (expression is MemberExpression)
			{
				// Reference type property or field
				var memberExpression = (MemberExpression)expression;
				return memberExpression.Member.GetCustomAttribute<T>();
			}

			if (expression is MethodCallExpression)
			{
				// Reference type method
				var methodCallExpression = (MethodCallExpression)expression;
				return methodCallExpression.Method.GetCustomAttribute<T>();
			}

			if (expression is UnaryExpression)
			{
				// Property, field of method returning value type
				var unaryExpression = (UnaryExpression)expression;
				return GetMemberAttribute<T>(unaryExpression);
			}

			throw new ArgumentException("");
		}

		private static T GetMemberAttribute<T>(UnaryExpression unaryExpression) where T : Attribute
		{
			if (unaryExpression.Operand is MethodCallExpression)
			{
				var methodExpression = (MethodCallExpression)unaryExpression.Operand;
				return methodExpression.Method.GetCustomAttribute<T>();
			}

			return ((MemberExpression)unaryExpression.Operand).Member.GetCustomAttribute<T>();
		}

		#endregion

		#region HasProperty

		/// <summary>
		/// Checks if a property exists.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="propertyName">The property's name.</param>
		/// <returns>Does the property exists.</returns>
		public static bool HasProperty(this object obj, string propertyName)
		{
			return obj.GetType().HasProperty(propertyName);
		}

		/// <summary>
		/// Checks if a property exists.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="propertyName">The property's name.</param>
		/// <returns>Does the property exists.</returns>
		public static bool HasProperty(this Type type, string propertyName)
		{
			return type.GetProperty(propertyName) != null;
		}

		#endregion

		#region SetPropertyValueByName

		/// <summary>
		/// Sets a property by its name.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="propertyName">The property's name.</param>
		/// <param name="value">The new value of the property.</param>
		public static void SetPropertyValueByName(this object obj, string propertyName, object value)
		{
			Type type = obj.GetType();
			PropertyInfo prop = type.GetProperty(propertyName);
			prop.SetValue(obj, value, null);
		}

		#endregion

        #region GetPropertyValueByName

        /// <summary>
        /// Gets a property's value by its name.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyName">The property's name.</param>
        /// <returns>The property's value.</returns>
        public static object GetPropertyValueByName(this object obj, string propertyName)
        {
            Type type = obj.GetType();
            PropertyInfo prop = type.GetProperty(propertyName);
            if (prop == null)
            {
                return null;
            }
            return prop.GetValue(obj, null);
        }

        /// <summary>
        /// Tries to get a property's value by its name.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyName">The property's name.</param>
        /// <param name="value">The property's value if found, otherwise null.</param>
        /// <returns>True if the property is found and the value is retrieved, false otherwise.</returns>
        public static bool TryGetPropertyValueByName(this object obj, string propertyName, out object value)
        {
            value = null;

            if (obj == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            Type type = obj.GetType();
            PropertyInfo prop = type.GetProperty(propertyName);

            if (prop == null)
            {
                return false;
            }

            value = prop.GetValue(obj, null);
            return true;
        }

        /// <summary>
        /// Gets a property's value by its name.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyName">The property's name.</param>
        /// <returns>The property's value.</returns>
        public static dynamic GetDynamicPropertyValueByName(this object obj, string propertyName)
        {
            Type type = obj.GetType();
            PropertyInfo prop = type.GetProperty(propertyName);
            return prop.GetValue(obj, null);
        }

        #endregion

        #region GetItemPropertyLambdaExpression

        public static Expression<Func<T, object>> GetItemPropertyLambdaExpression<T>(string propertyName) where T : class
        {
            var param = Expression.Parameter(typeof(T));
            var body = Expression.Convert(Expression.Property(param, propertyName), typeof(object));
            var expression = Expression.Lambda<Func<T, object>>(body, param);

            return expression;
        }

        #endregion

        #region GetRealTypeWithNullable

        public static Type GetRealTypeWithNullable(this object obj)
        {
            var type = obj.GetType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Nullable.GetUnderlyingType(type);
            }

            return type;
        }

        #endregion

        #region GetTypeByName

        /// <summary>
        /// Gets a Type by its name, first looking in the calling assembly, then searching all loaded assemblies
        /// </summary>
        /// <param name="typeName">The name of the type to find</param>
        /// <returns>The Type if found, null otherwise</returns>
        public static Type GetTypeByName(string typeName)
        {
            // Get the assembly that called this method
            var callingAssembly = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly;

            // Try to find type in calling assembly first
            var type = callingAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == typeName);

            // If not found, search all loaded assemblies
            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == typeName);
            }

            return type;
        }

        #endregion

        #region IsOfTypeOrInherits

        /// <summary>
        /// Checks if an object is of a specific type or inherits from it.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="type">The type to check against.</param>
        /// <returns>True if the object is of the specified type or inherits from it, false otherwise.</returns>
        public static bool IsOfTypeOrInherits(this object obj, Type type)
        {
            if (obj == null)
                return false;

            var objType = obj.GetType();
            return objType == type || objType.IsSubclassOf(type);
        }

        /// <summary>
        /// Checks if an object is of a specific type or inherits from it.
        /// </summary>
        /// <typeparam name="T">The type to check against.</typeparam>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is of the specified type or inherits from it, false otherwise.</returns>
        public static bool IsOfTypeOrInherits<T>(this object obj)
        {
            return IsOfTypeOrInherits(obj, typeof(T));
        }

        #endregion
    }
}
