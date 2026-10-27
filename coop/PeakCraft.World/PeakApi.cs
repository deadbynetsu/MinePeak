using System;
using System.Reflection;
using UnityEngine;

namespace PeakCraft.World;

internal static class PeakApi
{
	private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	private static Func<object?, bool>? dead;

	private static Func<object?, bool>? inPauseMenu;

	private static Func<object?, object?>? pauseMenu;

	private static MethodInfo? halt;

	private static object?[]? haltArgs;

	private static bool haltSearched;

	public static bool IsDead(CharacterData? data)
	{
		if (data == null)
		{
			return false;
		}
		if (dead == null)
		{
			dead = BoolMember(typeof(CharacterData), "dead", "isDead", "Dead", "IsDead");
		}
		return dead(data);
	}

	public static bool InPauseMenu(GUIManager? gui)
	{
		if (inPauseMenu == null)
		{
			inPauseMenu = BoolMember(typeof(GUIManager), "InPauseMenu", "inPauseMenu");
		}
		if (inPauseMenu(gui))
		{
			return true;
		}
		if (gui == null)
		{
			return false;
		}
		if (pauseMenu == null)
		{
			pauseMenu = Member(typeof(GUIManager), "pauseMenu");
		}
		if (pauseMenu(gui) is GameObject gameObject && gameObject != null)
		{
			return gameObject.activeInHierarchy;
		}
		return false;
	}

	public static void HaltBodyVelocity(CharacterRagdoll? ragdoll)
	{
		if (ragdoll == null)
		{
			return;
		}
		if (!haltSearched)
		{
			haltSearched = true;
			MethodInfo[] methods = typeof(CharacterRagdoll).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (!(methodInfo.Name != "HaltBodyVelocity") && !methodInfo.IsStatic)
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					haltArgs = new object[parameters.Length];
					for (int j = 0; j < parameters.Length; j++)
					{
						Type parameterType = parameters[j].ParameterType;
						ref object? reference = ref haltArgs[j];
						object obj;
						if (parameters[j].HasDefaultValue)
						{
							obj = parameters[j].DefaultValue;
						}
						else
						{
							obj = (parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null);
						}
						reference = obj;
					}
					halt = methodInfo;
					break;
				}
			}
			if (halt == null)
			{
				Plugin.Log.LogWarning((object)"compat: this PEAK has no CharacterRagdoll.HaltBodyVelocity; the body is not stopped on hand-over");
			}
		}
		if (halt == null)
		{
			return;
		}
		try
		{
			halt.Invoke(ragdoll, haltArgs);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("compat: HaltBodyVelocity failed: " + (ex.InnerException?.Message ?? ex.Message)));
			halt = null;
		}
	}

	private static Func<object?, bool> BoolMember(Type type, params string[] names)
	{
		Func<object?, object?> get = Member(type, names);
		return (object? target) =>
		{
			object obj = get(target);
			bool flag = default;
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		};
	}

	private static Func<object?, object?> Member(Type type, params string[] names)
	{
		foreach (string name in names)
		{
			MethodInfo getter = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true);
			if (getter != null && getter.GetParameters().Length == 0)
			{
				bool isStatic = getter.IsStatic;
				return (object? target) => Safe(() =>
				{
					object result = ((!isStatic) ? ((target != null) ? getter.Invoke(target, null) : null) : getter.Invoke(null, null));
					return result;
				});
			}
			FieldInfo f = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (!(f != null))
			{
				continue;
			}
			bool isStatic2 = f.IsStatic;
			return (object? target) => Safe(() =>
			{
				object result = ((!isStatic2) ? ((target != null) ? f.GetValue(target) : null) : f.GetValue(null));
				return result;
			});
		}
		Plugin.Log.LogWarning((object)("compat: this PEAK has no " + type.Name + "." + names[0] + "; using a fallback"));
		return (object? _) => (object?)null;
	}

	private static object? Safe(Func<object?> read)
	{
		try
		{
			return read();
		}
		catch
		{
			return null;
		}
	}
}
