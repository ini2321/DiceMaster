
using System;

#nullable disable
namespace DiceMaster.Config;

public class ConfigElement<T> : IConfigElement
{
  public Action<T> OnValueChanged;
  private T m_value;

  public string Name { get; }

  public string Description { get; }

  public Type ElementType => typeof (T);

  public Action OnValueChangedNotify { get; set; }

  public object DefaultValue { get; }

  public T Value
  {
    get => this.m_value;
    set => this.SetValue(value);
  }

  object IConfigElement.BoxedValue
  {
    get => (object) this.m_value;
    set => this.SetValue((T) value);
  }

  public ConfigElement(string name, string description, T defaultValue)
  {
    this.Name = name;
    this.Description = description;
    this.m_value = defaultValue;
    this.DefaultValue = (object) defaultValue;
    ConfigManager.RegisterConfigElement<T>(this);
  }

  private void SetValue(T value)
  {
    if (object.Equals((object) this.m_value, (object) value))
      return;
    this.m_value = value;
    ConfigManager.SetConfigValue<T>(this, value);
    Action<T> onValueChanged = this.OnValueChanged;
    if (onValueChanged != null)
      onValueChanged(value);
    Action valueChangedNotify = this.OnValueChangedNotify;
    if (valueChangedNotify != null)
      valueChangedNotify();
    ConfigManager.SaveConfig();
  }

  public object GetLoaderConfigValue() => (object) ConfigManager.GetConfigValue<T>(this);

  public void RevertToDefaultValue() => this.Value = (T) this.DefaultValue;
}
