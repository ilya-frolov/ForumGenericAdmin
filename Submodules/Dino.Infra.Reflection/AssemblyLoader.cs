using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dino.Infra.Reflection
{
	public class AssemblyLoader : MarshalByRefObject
	{
		private Assembly _assembly;

		public override object InitializeLifetimeService()
		{
			return null;
		}

		public void LoadAssembly(string path)
		{
			_assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
		}

		public void LoadAssembly(byte[] assembly)
		{
			_assembly = Assembly.Load(assembly);
		}

		public List<string> GetTypeNames()
		{
			return _assembly.GetTypes().Select(x => x.FullName).ToList();
		}

		public object ExecuteStaticMethod(string typeName, string methodName, params object[] parameters)
		{
			Type type = _assembly.GetType(typeName);
			// TODO: this won't work if there are overloads available
			MethodInfo method = type.GetMethod(
				methodName,
				BindingFlags.Static | BindingFlags.Public);
			return method.Invoke(null, parameters);
		}

		public object ExecuteMethod(string typeName, string methodName, params object[] parameters)
		{
			Type type = _assembly.GetType(typeName);
			// TODO: this won't work if there are overloads available
			MethodInfo method = type.GetMethod(
				methodName,
				BindingFlags.Public);
			return method.Invoke(null, parameters);
		}

		public object CreateInstance(string typeName)
		{
			return _assembly.CreateInstance(typeName);
		}
	}
}