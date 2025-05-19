using HarmonyLib;
using System;
using System.Reflection;

#nullable disable
namespace DiceMaster;

public static class ReflectionExtensions
{
  public static T GetFieldValue<T>(this object instance, string fieldname)
  {
    try
    {
      return (T) AccessTools.Field(instance.GetType(), fieldname)?.GetValue(instance);
    }
    catch (Exception ex)
    {
      DicePlugin.LogError((object) $"Error getting field {fieldname} from {instance.GetType().Name}: {ex}");
      return default (T);
    }
  }

  public static T GetClassFieldValue<T>(this Type type, string fieldname)
  {
    return (T) AccessTools.Field(type, fieldname)?.GetValue((object) null);
  }

  public static void SetPrivateField(this object instance, string fieldname, object value)
  {
    AccessTools.Field(instance.GetType(), fieldname)?.SetValue(instance, value);
  }

  public static T CallPrivateMethod<T>(
    this object instance,
    string methodname,
    params object[] param)
  {
    return (T) AccessTools.Method(instance.GetType(), methodname, (Type[]) null, (Type[]) null).Invoke(instance, param);
  }

  public static void CallPrivateMethod(
    this object instance,
    string methodname,
    params object[] param)
  {
    AccessTools.Method(instance.GetType(), methodname, (Type[]) null, (Type[]) null)?.Invoke(instance, param);
  }

  public static void SetPrivateProperty(this object instance, string propertyname, object value)
  {
    AccessTools.Property(instance.GetType(), propertyname)?.SetValue(instance, value);
  }

  public static T GetPrivateProperty<T>(this object instance, string propertyname)
  {
    return (T) AccessTools.Property(instance.GetType(), propertyname)?.GetValue(instance);
  }

  public static T ShallowCopy<T>(this T source) where T : class, new()
  {
    if ((object) source == null)
      return default (T);
    Type type = source.GetType();
    T obj1 = new T();
    foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    {
      object obj2 = field.GetValue((object) source);
      field.SetValue((object) obj1, obj2);
    }
    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    {
      if (property.CanRead && property.CanWrite)
      {
        object obj3 = property.GetValue((object) source);
        property.SetValue((object) obj1, obj3);
      }
    }
    return obj1;
  }
}
