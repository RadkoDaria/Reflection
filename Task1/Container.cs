using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Task1.DoNotChange;

namespace Task1
{
    public class Container
    {
        readonly Dictionary<Type, Type> registeredTypes = new Dictionary<Type, Type>();

		public void AddAssembly(Assembly assembly)
		{
			Assembly currAssembly = assembly;

			var types = currAssembly.ExportedTypes;

			var exportTypes = types.Where(t => t.CustomAttributes.Any(a => a.AttributeType == typeof(ExportAttribute)))
				.Select(t => new {
					type = t,
					baseType = (Type)t.CustomAttributes
							.FirstOrDefault(a => a.AttributeType == typeof(ExportAttribute))
							.ConstructorArguments
							.FirstOrDefault(c => c.ArgumentType == typeof(Type)).Value
				}).ToList();
			exportTypes.ForEach(t => {
				if (t.baseType == null)
					AddType(t.type);
				else
					AddType(t.type, t.baseType);
			});

			types.Where(t => t.GetProperties().Any(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(ImportAttribute)))).ToList().ForEach(t => AddType(t));
			types.Where(t => t.CustomAttributes.Any(a => a.AttributeType == typeof(ImportConstructorAttribute))).ToList().ForEach(t => AddType(t));

		}


	

		public void AddType(Type type)
		{
			if (!registeredTypes.ContainsKey(type))
				registeredTypes.Add(type, type);
			else
			{
				throw new ContainerException("Duplicating");
			}
		}

		public void AddType(Type type, Type baseType)
		{
			if (!registeredTypes.ContainsKey(type))
			{
				registeredTypes.Add(baseType, type);
				registeredTypes.Add(type, type);
			}
			else
            {
				throw new ContainerException("Duplicating");
            }

		}

		public object Get(Type type)
		{

			if (!registeredTypes.ContainsKey(type))
			{
				throw new ContainerException($"Can't create the instance of {type.FullName}. No mapping for the type!");
			}
			var realType = registeredTypes[type];
			if (realType.IsAbstract) throw new ContainerException(string.Format("Тип ({0}) является абстрактным. Объекты с таким типом нельзя создавать", type.Name));

			object injectedObject = null;
			var constrs = realType.GetConstructors().Where(x => x.GetParameters().Length > 0);
			if (constrs != null && realType.GetCustomAttributes(typeof(ImportConstructorAttribute), true).Length >= 1)
			{
				var constInfo = constrs.First();
				ParameterInfo[] ps = constInfo.GetParameters();
				object[] paramets = new object[ps.Length];
				for (int i = 0; i < ps.Length; i++)
				{
					paramets[i] = Get(ps[i].ParameterType);
				}

				injectedObject = (object)Activator.CreateInstance(realType, paramets);
				return injectedObject;
			}

			var propsInfo = realType.GetProperties().Where(p => p.GetCustomAttribute(typeof(ImportAttribute), true) != null);
			if (propsInfo != null)
			{
				injectedObject = (object)Activator.CreateInstance(realType);
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