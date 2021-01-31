using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Task1.DoNotChange;

namespace Task1
{
    public class Container
    {
		Dictionary<Type, Type> registeredTypes = new Dictionary<Type, Type>();

		public void AddAssembly(Assembly assembly)
		{
			Assembly currAssembly = Assembly.GetExecutingAssembly();

			var types = currAssembly.ExportedTypes;
			foreach (var type in types)
			{
				// types that have [Import] attribute
				if (type.GetProperties().Length >= 1)
				{
					foreach (var r in type.GetProperties())
					{
						foreach (var att in r.GetCustomAttributes(typeof(ImportAttribute), true))
						{
							AddType(r.PropertyType);
						}
					}
				}
				// [ImportConstructor] attributes
				if (type.GetCustomAttributes(typeof(ImportConstructorAttribute), true).Length >= 1)
				{
					var constructorImport = type.GetCustomAttributes(typeof(ImportConstructorAttribute), true);
					foreach (var ci in constructorImport)
					{
						var ctor = type.GetConstructors().Where(y => y.GetParameters().Length > 0).First();
						AddType(type);
						foreach (var par in ctor.GetParameters())
						{
							AddType(par.ParameterType);
						}

					}
				}
				// export attributes
				if (type.GetCustomAttributes(typeof(ExportAttribute), true).Length >= 1)
				{
					foreach (var exportAttribute in type.GetCustomAttributes(typeof(ExportAttribute), true))
					{
						AddType(type);
					}
				}
			}


		}

		public void AddType(Type type)
		{
			if (!registeredTypes.ContainsKey(type))
				registeredTypes.Add(type, type);
		}

		public void AddType(Type type, Type baseType)
		{
			if (!registeredTypes.ContainsKey(type))
				registeredTypes.Add(type, baseType);
		}

		public object Get(Type type)
		{

			if (!registeredTypes.ContainsKey(type))
			{
				throw new ContainerException($"Can't create the instance of {type.FullName}. No mapping for the type!");
			}
			if (type.IsAbstract) throw new ContainerException(string.Format("Тип ({0}) является абстрактным. Объекты с таким типом нельзя создавать", type.Name));

			object injectedObject = null;
			var constrs = type.GetConstructors().Where(x => x.GetParameters().Length > 0);
			if (constrs != null && type.GetCustomAttributes(typeof(ImportConstructorAttribute), true).Length >= 1)
			{
				var constInfo = constrs.First();
				ParameterInfo[] ps = constInfo.GetParameters();
				object[] paramets = new object[ps.Length];
				for (int i = 0; i < ps.Length; i++)
				{
					paramets[i] = Get(ps[i].ParameterType);
				}

				injectedObject = (object)Activator.CreateInstance(type, paramets);
				return injectedObject;
			}

			var propsInfo = type.GetProperties().Where(p => p.GetCustomAttribute(typeof(ImportAttribute), true) != null);
			if (propsInfo != null)
			{
				injectedObject = (object)Activator.CreateInstance(type);
				foreach (var p in propsInfo)
				{
					if (registeredTypes.ContainsKey(p.PropertyType))
					{
						var propInstance = Get(p.PropertyType);
						p.SetValue(injectedObject, propInstance);
					}
				}
			}

			return injectedObject;
		}

		public T Get<T>()
		{
			return (T)Get(typeof(T));
		}
	}
}