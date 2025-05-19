using BepInEx;
using Mortal.Core;
using Mortal.Story;
using OBB.Framework.Data;
using OBB.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#nullable disable
namespace DiceMaster;

public static class DataHelper
{
  public static string ExtractConditions(ConditionResultItem condition, out bool isInPeriod)
  {
    try
    {
      List<string> values = new List<string>();
      isInPeriod = true;
      foreach (StatCompareItem instance in condition.GetFieldValue<List<StatCompareItem>>("_items"))
      {
        StatGroupVariable fieldValue1 = instance.GetFieldValue<StatGroupVariable>("_value1");
        StatGroupVariable fieldValue2 = instance.GetFieldValue<StatGroupVariable>("_value2");
        string compareOperator = DataHelper.GetCompareOperator(instance.GetFieldValue<StatCompareType>("_compareOp"));
        if (fieldValue1.name.Contains("時間_") && !instance.Check())
          isInPeriod = false;
        string str1 = DataHelper.FormatConditionValName(fieldValue1);
        string str2 = DataHelper.FormatConditionValName(fieldValue2);
        string text = $"{str1}({fieldValue1.GetResult()}) {compareOperator} {str2}({fieldValue2.GetResult()})";
        string str3 = instance.Check() ? RichText.Green(text) : RichText.Red(text);
        values.Add(str3);
      }
      return string.Join($" {DataHelper.GetLogicOperator(condition.GetFieldValue<LogicOperatorType>("_logicOp"))} ", (IEnumerable<string>) values);
    }
    catch (Exception ex)
    {
      DicePlugin.LogError((object) ("Error extracting conditions: " + ex.Message));
      isInPeriod = false;
      return string.Empty;
    }
  }

  private static string FormatConditionValName(StatGroupVariable variable)
  {
    string name = ((UnityEngine.Object) variable).name;
    if (name.StartsWith("旗標_"))
    {
      List <StatValueReference> fieldValue = variable.GetFieldValue<List<StatValueReference>>("_value");
      if (fieldValue.Count > 0)
        return DataHelper.GetFlagName(fieldValue[0].GetFieldValue<FlagData>("_flag"));
    }
    return name.Replace("數值_", "").Replace("時間_", "").Replace("常數_", "");
  }

    public static string ParseSwitchCheckpoint(string checkpoint)
    {
        CheckPointManager instance1 = CheckPointManager.Instance;

        if (instance1 == null) // Object.op_Equality 수정
            return string.Empty;

        StringBuilder stringBuilder = new StringBuilder();

        // 수정: _switchList 필드의 실제 타입인 SwitchResultConfig[]를 T에 명시
        SwitchResultConfig[] fieldValue = instance1.GetFieldValue<SwitchResultConfig[]>("_switchList");
        SwitchResultData instance2 = null;

        if (fieldValue != null) // null 체크 추가
        {
            for (int index = 0; index < fieldValue.Length; ++index)
            {
                instance2 = fieldValue[index].GetData(checkpoint);
                if (instance2 != null) // Object.op_Inequality 수정
                    break;
            }
        }

        if (instance2 == null) // Object.op_Equality 수정
            return string.Empty;

        // 수정: _items 필드의 실제 타입인 List<ConditionResultItem>을 T에 명시
        var itemsCollection = instance2.GetFieldValue<List<ConditionResultItem>>("_items");
        if (itemsCollection != null) // null 체크 추가
        {
            foreach (ConditionResultItem condition in itemsCollection)
                stringBuilder.AppendLine("    " + DataHelper.ExtractConditions(condition, out bool _));
        }
        return stringBuilder.ToString();
    }

    public static Dictionary<MissionCheckData, (string Key, string Description)> GetMissions()
    {
        Dictionary<MissionCheckData, (string, string)> missions = new Dictionary<MissionCheckData, (string, string)>();
        MissionManagerData instance1 = MissionManagerData.Instance;
        SubMissionsData fieldValue1 = instance1?.GetFieldValue<SubMissionsData>("_subMissions"); // C# 6.0 이상이면 null 조건부 연산자로 더 간결하게 가능

        if (fieldValue1 == null)
            return missions;

        // ((CollectionData<MissionData>) fieldValue1).List 부분 수정:
        // fieldValue1 (SubMissionsData 타입)은 CollectionData<MissionData>를 상속하고,
        // CollectionData<MissionData>에는 public List<MissionData> List 속성이 있으므로,
        // fieldValue1.List 로 직접 접근합니다. 캐스팅이 필요 없습니다.
        var missionDataList = fieldValue1.List;
        if (missionDataList == null) // _list가 초기화되지 않았을 가능성도 고려 (보통은 생성자에서 new List<T>() 함)
        {
            // DicePlugin.LogWarning("_subMissions.List is null"); // 필요시 로그
            return missions;
        }

        foreach (MissionData instance2 in missionDataList) // 이제 오류 없이 작동해야 함
        {
            // ... 나머지 GetMissions 메소드 로직 ...
            string fieldValue2 = instance2.GetFieldValue<string>("_desc");
            string str1 = Utility.IsNullOrWhiteSpace(fieldValue2) ? instance2.Name : fieldValue2;

            string currentKey = instance2.CurrentKey;
            if (!(currentKey == "clear"))
            {
                MissionCheckData byKey = instance2.GetByKey(currentKey);
                if (byKey != null && DataHelper.IsValidMission(byKey))
                {
                    string fieldValue3 = byKey.GetFieldValue<string>("_desc");
                    string str2 = Utility.IsNullOrWhiteSpace(fieldValue3) ? byKey.name : fieldValue3;

                    string str3 = $"{str1} - {str2}";
                    string str4 = byKey.Position.ToString();
                    string periodText;
                    bool isInPeriod = DataHelper.GetMissionPeriod(byKey, out periodText);
                    if (isInPeriod)
                    {
                        List<string> conditions = DataHelper.ExtractConditions(byKey, out isInPeriod);
                        if (isInPeriod)
                        {
                            StringBuilder stringBuilder = new StringBuilder();
                            // 중국어 문자열 번역은 나중에
                            //DicePlugin.LogInfo($"{LuaParse.Action.ToLiteral(str4)}");
                            str4 = LocationTranslations.TranslateLocation(str4);
                            stringBuilder.AppendLine("위치 :" + str4);
                            stringBuilder.AppendLine("시간 : " + periodText);
                            stringBuilder.AppendLine("조건 :");
                            foreach (string str5 in conditions)
                                stringBuilder.AppendLine("    " + str5);
                            missions[byKey] = (str3, stringBuilder.ToString());
                        }
                    }
                }
            }
        }
        return missions;
    }

    private static bool IsValidMission(MissionCheckData checkData)
  {
    bool fieldValue = checkData.GetFieldValue<bool>("_dayCheck");
    bool flag1 = checkData.Position > 0;
    bool flag2 = checkData.GetFieldValue<ConditionResultItem[]>("_conditions").Length != 0;
    return DataHelper.LoadLuaScript(checkData.StartScript) != string.Empty && fieldValue | flag1 | flag2;
  }

  private static List<string> ExtractConditions(MissionCheckData checkData, out bool isInPeriod)
  {
    List<string> conditions = new List<string>();
    ConditionResultItem[] fieldValue = checkData.GetFieldValue<ConditionResultItem[]>("_conditions");
    isInPeriod = true;
    foreach (ConditionResultItem condition in fieldValue)
    {
      bool isInPeriod1;
      conditions.Add(DataHelper.ExtractConditions(condition, out isInPeriod1));
      if (!isInPeriod1)
        isInPeriod = false;
    }
    return conditions;
  }

    public static bool GetMissionPeriod(MissionCheckData checkData, out string periodText)
    {
        if (!checkData.GetFieldValue<bool>("_dayCheck"))
        {
            periodText = "무제한"; // 나중에 "무제한" 등으로 번역
            return true;
        }

        GameTime gameTime = PlayerStatManagerData.Instance?.GameTime; // null 조건부 연산자 유지
        TimeCheckType timeCheckTypeValue = checkData.GetFieldValue<TimeCheckType>("_timeCheckType");

        if (timeCheckTypeValue == TimeCheckType.特定)
        {
            GameTime specificTimeValue = checkData.GetFieldValue<GameTime>("_specificTime");
            // "第", "年", "月" 등은 나중에 번역
            periodText = $"第{specificTimeValue.YearText}年{specificTimeValue.MonthText}月{specificTimeValue.StageText}";
            // GameTime.op_Equality(gameTime, specificTimeValue) -> gameTime == specificTimeValue
            return gameTime == specificTimeValue;
        }
        else if (timeCheckTypeValue == TimeCheckType.區間)
        {
            GameTime minTimeValue = checkData.GetFieldValue<GameTime>("_minTime");
            GameTime maxTimeValue = checkData.GetFieldValue<GameTime>("_maxTime");
            // "第", "年", "月" 등은 나중에 번역
            periodText = $"第{minTimeValue.YearText}年{minTimeValue.MonthText}月{minTimeValue.StageText} - 第{maxTimeValue.YearText}年{maxTimeValue.MonthText}月{maxTimeValue.StageText}";
            // GameTime.op_GreaterThanOrEqual(gameTime, minTimeValue) -> gameTime >= minTimeValue
            // GameTime.op_LessThanOrEqual(gameTime, maxTimeValue) -> gameTime <= maxTimeValue
            return gameTime >= minTimeValue && gameTime <= maxTimeValue;
        }
        else // TimeCheckType.週期 또는 기타 값 (기본값 처리)
        {
            periodText = "무제한"; // 나중에 "무제한" 등으로 번역
            return true;
        }
    }

    public static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct
    {
        // result = default(TEnum); // 이 라인은 더 이상 필요하지 않으며, 오히려 out 매개변수 규칙에 어긋날 수 있습니다.
        // out 매개변수는 호출된 메소드 내에서 반드시 할당되어야 합니다.

        // EnumUtils.TryParseByStringValue가 out 매개변수를 기대하므로, out 키워드를 사용합니다.
        return EnumUtils.TryParseByStringValue<TEnum>(value, out result);
    }

    public static string GetItemName(GameItemType itemType, string id)
  {
    IGameItem igameItem = ItemDatabase.Instance.GetItem(itemType, id);
    return igameItem == null ? string.Empty : LocalizationManager.Instance.LocaleResolver.GetString(igameItem.GetNameKey());
  }

    public static string GetTalentName(string id)
    {
        PlayerTalentData playerTalentData = PlayerStatManagerData.Instance.Talents.Get(id);
        // Object.op_Equality((Object) playerTalentData, (Object) null) 부분을
        // playerTalentData == null 로 변경합니다.
        DicePlugin.LogInfo($"{playerTalentData.GetIdKey()}");
        return playerTalentData == null ? string.Empty : LocalizationManager.Instance.LocaleResolver.GetString(playerTalentData.GetIdKey());
    }

    public static string GetStoryName(string id)
  {
    return LocalizationManager.Instance.LocaleResolver.GetString("LegendInfo/" + id);
  }

    public static string GetTeamName(string id)
    {
        BattleTeamStat battleTeamStat = PlayerStatManagerData.Instance.EnemyTeam.Get(id);
        // Object.op_Equality((Object) battleTeamStat, (Object) null) 부분을
        // battleTeamStat == null 로 변경합니다.
        return battleTeamStat == null ? string.Empty : LocalizationManager.Instance.LocaleResolver.GetString(battleTeamStat.GetNameKey());
    }

    public static string GetEventName(PositionEventData eventData)
  {
    string text = eventData.GetFieldValue<StoryMappingItem>("_config")?.name;
    string fieldValue = eventData.GetFieldValue<string>("_devNote");
    if (!Utility.IsNullOrWhiteSpace(fieldValue))
      text = fieldValue;
    return RichText.Golden(text);
  }

  public static string LoadLuaScript(string scriptName)
  {
    return Resources.Load<TextAsset>("story/chinesetraditional/" + scriptName.ToLower())?.text ?? string.Empty;
  }

  public static string GetCompareOperator(StatCompareType compareType)
  {
    string compareOperator;
    switch ((int) compareType)
    {
      case 0:
        compareOperator = "=";
        break;
      case 1:
        compareOperator = "!=";
        break;
      case 2:
        compareOperator = "<";
        break;
      case 3:
        compareOperator = "<=";
        break;
      case 4:
        compareOperator = ">";
        break;
      case 5:
        compareOperator = ">=";
        break;
      default:
        compareOperator = "unknown";
        break;
    }
    return compareOperator;
  }

    public static string GetLogicOperator(LogicOperatorType logicOp)
    {

        if (logicOp == LogicOperatorType.OR)
        {
            return "또는"; 
        }
        else if (logicOp == LogicOperatorType.AND)
        {
            return "그리고";
        }
        else
        {
            return "unknown";
        }
    }

    public static string GetFlagName(string id)
  {
    return DataHelper.GetFlagName(MissionManagerData.Instance.GetFlag(id));
  }

    public static string GetFlagName(FlagData flagData)
    {
        // Object.op_Equality((Object) flagData, (Object) null) 부분을
        // flagData == null 로 변경합니다.
        if (flagData == null)
            return string.Empty;

        string devNote = flagData.DevNote;
        if (devNote != null && devNote.Contains("\n")) // devNote가 null일 가능성도 고려 (안전장치)
            devNote = devNote.Split('\n')[0];

        return !string.IsNullOrEmpty(devNote) ? devNote : string.Empty;
    }
}
