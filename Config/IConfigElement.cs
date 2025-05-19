using System;

#nullable disable
namespace DiceMaster.Config;

public interface IConfigElement
{
  string Name { get; }

  string Description { get; }

  Type ElementType { get; }

  object BoxedValue { get; set; }

  object DefaultValue { get; }

  object GetLoaderConfigValue();

  void RevertToDefaultValue();

  Action OnValueChangedNotify { get; set; }
}
