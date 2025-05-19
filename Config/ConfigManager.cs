using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;

#nullable disable
namespace DiceMaster.Config;

public static class ConfigManager
{
  internal static readonly Dictionary<string, IConfigElement> ConfigElements = new Dictionary<string, IConfigElement>();
  private const string SEC_NAME = "DiceMaster";
  public static ConfigElement<bool> EnableDiceController;
  public static ConfigElement<bool> EnableOptionInfo;
  public static ConfigElement<KeyCode> LogMenuToggleKey;
  public static ConfigElement<KeyCode> SpeedUpKey;

  private static ConfigFile Config => DicePlugin.Instance.Config;

  public static void Init()
  {
    ConfigManager.CreateConfigElements();
    ConfigManager.LoadConfig();
  }

  internal static void RegisterConfigElement<T>(ConfigElement<T> configElement)
  {
    ConfigEntry<T> entry = ConfigManager.Config.Bind<T>("DiceMaster", configElement.Name, configElement.Value, configElement.Description);
    entry.SettingChanged += (EventHandler) ((o, e) => configElement.Value = entry.Value);
    ConfigManager.ConfigElements.Add(configElement.Name, (IConfigElement) configElement);
  }

    public static T GetConfigValue<T>(ConfigElement<T> element)
    {
        ConfigEntry<T> configEntry; // out 매개변수로 사용될 것이므로 여기서 초기화할 필요 없음
        if (!ConfigManager.Config.TryGetEntry<T>("DiceMaster", element.Name, out configEntry)) // ref를 out으로 변경
        {
            // Config 항목을 찾지 못한 경우, 예외를 던지거나 기본값을 반환할 수 있습니다.
            // 현재 코드는 예외를 던지도록 되어 있습니다.
            throw new Exception($"Could not get config entry '{element.Name}'");
        }
        // TryGetEntry가 true를 반환했다면, configEntry에는 유효한 값이 할당되어 있습니다.
        return configEntry.Value;
    }

    public static void SetConfigValue<T>(ConfigElement<T> element, T value)
    {
        ConfigEntry<T> configEntry; // out 매개변수로 사용될 것이므로 여기서 초기화할 필요 없음
        if (ConfigManager.Config.TryGetEntry<T>("DiceMaster", element.Name, out configEntry)) // ref를 out으로 변경
        {
            // TryGetEntry가 true를 반환했다면, configEntry에 유효한 ConfigEntry<T> 객체가 할당됨.
            configEntry.Value = value;
        }
        else
        {
            DicePlugin.LogInfo((object)$"Could not get config entry '{element.Name}' to set value."); // 로그 메시지 약간 수정 (선택 사항)
        }
    }

    public static void LoadConfig()
  {
    foreach (KeyValuePair<string, IConfigElement> configElement in ConfigManager.ConfigElements)
    {
      ConfigDefinition configDefinition = new ConfigDefinition("DiceMaster", configElement.Key);
      if (ConfigManager.Config.ContainsKey(configDefinition))
      {
        ConfigEntryBase configEntryBase = ConfigManager.Config[configDefinition];
        if (configEntryBase != null)
          configElement.Value.BoxedValue = configEntryBase.BoxedValue;
      }
    }
  }

  public static void SaveConfig() => ConfigManager.Config.Save();

  private static void CreateConfigElements()
  {
    ConfigManager.EnableDiceController = new ConfigElement<bool>("EnableDiceController", "Enable dice controller", true);
    ConfigManager.EnableOptionInfo = new ConfigElement<bool>("EnableOptionInfo", "Enable option info", true);
    ConfigManager.LogMenuToggleKey = new ConfigElement<KeyCode>("LogMenuToggleKey", "Log menu toggle key", (KeyCode) 9);
    ConfigManager.SpeedUpKey = new ConfigElement<KeyCode>("SpeedUpKey", "Speed up key", (KeyCode) 306);
  }
}
