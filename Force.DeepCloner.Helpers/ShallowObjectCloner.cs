using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Force.DeepCloner.Helpers;

/// <summary>
/// Internal class but due implementation restriction should be public
/// </summary>
public abstract class ShallowObjectCloner
{
	private class ShallowSafeObjectCloner : ShallowObjectCloner
	{
		private static readonly Func<object, object> _cloneFunc;

		static ShallowSafeObjectCloner()
		{
			MethodInfo methodInfo = typeof(object).GetPrivateMethod("MemberwiseClone");
			ParameterExpression p = Expression.Parameter(typeof(object));
			ShallowSafeObjectCloner._cloneFunc = Expression.Lambda<Func<object, object>>(Expression.Call(p, methodInfo), new ParameterExpression[1] { p }).Compile();
		}

		protected override object DoCloneObject(object obj)
		{
			return ShallowSafeObjectCloner._cloneFunc(obj);
		}
	}

	private static readonly ShallowObjectCloner _unsafeInstance;

	private static ShallowObjectCloner _instance;

	/// <summary>
	/// Abstract method for real object cloning
	/// </summary>
	protected abstract object DoCloneObject(object obj);

	/// <summary>
	/// Performs real shallow object clone
	/// </summary>
	public static object CloneObject(object obj)
	{
		return ShallowObjectCloner._instance.DoCloneObject(obj);
	}

	internal static bool IsSafeVariant()
	{
		return ShallowObjectCloner._instance is ShallowSafeObjectCloner;
	}

	static ShallowObjectCloner()
	{
		ShallowObjectCloner._instance = new ShallowSafeObjectCloner();
		ShallowObjectCloner._unsafeInstance = ShallowObjectCloner._instance;
	}

	/// <summary>
	/// Purpose of this method is testing variants
	/// </summary>
	internal static void SwitchTo(bool isSafe)
	{
		DeepClonerCache.ClearCache();
		if (isSafe)
		{
			ShallowObjectCloner._instance = new ShallowSafeObjectCloner();
		}
		else
		{
			ShallowObjectCloner._instance = ShallowObjectCloner._unsafeInstance;
		}
	}
}
