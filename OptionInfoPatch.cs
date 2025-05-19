using BepInEx;
using DiceMaster.LuaParse;
using Fungus;
using HarmonyLib;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


#nullable disable
namespace DiceMaster;

internal static class OptionInfoPatch
{
  private static string _originalDialogKey = string.Empty;
  private static PositionController _positionCtrl = (PositionController) null;
  public static PositionEventData TargetEvent = (PositionEventData) null;
  public static MissionCheckData TargetMission = (MissionCheckData) null;

  [HarmonyPrefix]
  [HarmonyPatch(typeof (LuaEnvironment), "LoadLuaFunction")]
  public static void LuaEnvironment_LoadLuaFunction_Prefix(string luaString, string friendlyName)
  {
    DicePlugin.Instance.ClearLogEntries();
    foreach (KeyValuePair<string, List<DiceMaster.LuaParse.Action>> keyValuePair in LuaParser.Instance.ExtractOptionActionsFromLua(friendlyName, luaString))
      DicePlugin.Instance.AddOptionEntry(keyValuePair.Key, LuaParser.Description(keyValuePair.Value));
    foreach (string str in LuaParser.Instance.ExtractSwitchCheckFromLua(friendlyName, luaString))
    {
      string switchCheckpoint = DataHelper.ParseSwitchCheckpoint(str);
      DicePlugin.Instance.AddSwitchEntry(str, switchCheckpoint);
    }
    OptionInfoPatch.TargetEvent = (PositionEventData) null;
    foreach (string position in LuaParser.Instance.ExtractPositionWorkFromLua(friendlyName, luaString))
    {
      foreach (PositionEventData eventData in PositionEventCache.Instance.GetPositionEventsSortedByRate(position))
      {
        string positionEventName = PositionEventCache.Instance.GetPositionEventName(eventData);
        string positionEventDesc = PositionEventCache.Instance.GetPositionEventDesc(eventData);
        DicePlugin.Instance.AddEventEntry(eventData, positionEventName, positionEventDesc);
      }
    }
    foreach (KeyValuePair<MissionCheckData, (string Key, string Description)> mission in DataHelper.GetMissions())
      DicePlugin.Instance.AddMissionEntry(mission.Key, mission.Value.Key, mission.Value.Description);
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof (LeanLocalizationResolver), "GetStoryText")]
  public static void LeanLocalizationResolver_GetStoryText_Postfix(string key, ref string __result)
  {
    List<DiceMaster.LuaParse.Action> actions;
    if (!LuaParser.Instance.OptionResult.TryGetValue(key, out actions))
      return;
    string input = LuaParser.Description(actions);
    if (Utility.IsNullOrWhiteSpace(input))
      return;
    string str = Regex.Replace(input, "<.*?>", string.Empty);
    if (str.Length > 30)
      str = str.Substring(0, 29) + "...";
    __result = $"{__result} <size=18>({str})</size>";
  }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PositionController), "OnPositionEnter")]
    public static void PositionController_OnPositionEnter_Prefix(PositionController __instance)
    {
        PositionType fieldValue = __instance.GetFieldValue<PositionType>("_position"); // <PositionType>은 실제 타입으로 확인 필요

        // CS0104 오류 수정
        if (OptionInfoPatch._positionCtrl != null)
        {
            // HandleDialogChange의 원래 시그니처에 맞게 호출
            // 이전 코드에서는 suffix 인자 없이 호출하는 부분이 있었는데,
            // HandleDialogChange(PositionController instance, bool addSuffix, string suffix = "") 라면
            // addSuffix가 false일 때 suffix를 전달할 필요가 없을 수도 있지만, 명확성을 위해 빈 문자열 전달
            OptionInfoPatch.HandleDialogChange(OptionInfoPatch._positionCtrl, false, "");
        }

        // CS0165 오류 수정
        string missionKeyOutput; // 변수명 변경 및 초기화 불필요 (out이므로)
        if (!OptionInfoPatch.HasTriggerSubMissions(fieldValue, out missionKeyOutput))
        {
            return;
        }
        // "存在事件："는 나중에 번역
        OptionInfoPatch.HandleDialogChange(__instance, true, "발생사건：" + missionKeyOutput);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PositionController), "OnPositionClick")]
    public static void PositionController_OnPositionClick_Prefix()
    {
        if (OptionInfoPatch._positionCtrl == null) // _positionCtrl이 null이면 바로 반환
        {
            return;
        }

        // _positionCtrl이 null이 아닐 경우에만 HandleDialogChange 호출
        // HandleDialogChange의 원래 시그니처에 맞게 suffix 인자 전달
        OptionInfoPatch.HandleDialogChange(OptionInfoPatch._positionCtrl, false, "");
    }

  [HarmonyPrefix]
  [HarmonyPatch(typeof (ConditionResultData), "Execute")]
  public static void PrefixExecuteConditionResult(ConditionResultData __instance)
  {
    OptionInfoPatch.LogCheckpoint("触发剧情检查点:", (IEnumerable<ConditionResultItem>) __instance.GetFieldValue<List<ConditionResultItem>>("_items"));
  }

  [HarmonyPrefix]
  [HarmonyPatch(typeof (MissionCheckData), "Check")]
  public static void PrefixCheckMission(MissionCheckData __instance)
  {
    ConditionResultItem[] fieldValue = __instance.GetFieldValue<ConditionResultItem[]>("_conditions");
    StringBuilder stringBuilder = new StringBuilder("触发任务检查点:");
    StringBuilder logBuilder = stringBuilder;
    OptionInfoPatch.AppendConditions((IEnumerable<ConditionResultItem>) fieldValue, logBuilder);
    if (__instance.GetFieldValue<bool>("_dayCheck"))
    {
      string periodText;
      DataHelper.GetMissionPeriod(__instance, out periodText);
      if (!Utility.IsNullOrWhiteSpace(periodText))
        stringBuilder.AppendLine("    时限：" + periodText);
    }
    OptionInfoPatch.PrintToNarrativeLog(stringBuilder.ToString());
  }

  [HarmonyPrefix]
  [HarmonyPatch(typeof (SwitchResultData), "Execute")]
  public static void PrefixExecuteSwitchResult(SwitchResultData __instance)
  {
    OptionInfoPatch.LogCheckpoint("触发剧情分支检查点:", (IEnumerable<ConditionResultItem>) __instance.GetFieldValue<List<ConditionResultItem>>("_items"));
  }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PositionEventData), "GetRate")]
    public static void PositionEventData_GetRate_Postfix(
      PositionEventData __instance, // Harmony Postfix에서 원본 객체 인스턴스는 __instance로 전달됨
      ref int __result)
    {
        // Object.op_Equality를 사용하여 두 객체를 비교하는 것은
        // 두 객체가 같은 참조를 가리키는지, 또는 값 타입이라면 값이 같은지 확인하려는 의도.
        // OptionInfoPatch.TargetEvent와 __instance (메소드가 호출된 PositionEventData 객체)가
        // 같은 객체인지 비교합니다.
        //
        // ! (A == B) 는 (A != B) 와 동일합니다.
        if (OptionInfoPatch.TargetEvent != __instance) // 수정된 부분
            return;

        __result = 99999;
        OptionInfoPatch.TargetEvent = null; // (PositionEventData) 캐스팅은 null 할당 시 불필요
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MissionCheckData), "Check")]
    public static void MissionCheckData_Postfix(MissionCheckData __instance, ref bool __result)
    {
        // ! (A == B) 는 (A != B) 와 동일합니다.
        if (OptionInfoPatch.TargetMission != __instance) // 수정된 부분
            return;

        __result = true;
        OptionInfoPatch.TargetMission = null; // 수정된 부분: (MissionCheckData) 캐스팅 제거
    }

    private static void HandleDialogChange(
    PositionController instance,
    bool addSuffix,
    string suffix = "")
  {
    DicePlugin.LogInfo((object) $"HandleDialogChange called, addSuffix: {addSuffix}, suffix: {suffix}");
    try
    {
      PositionDialogItem dialog = instance.GetFieldValue<FreePositionData>("_positionData").GetDialog();
      string fieldValue = dialog.GetFieldValue<string>("_dialog");
      if (addSuffix)
      {
        OptionInfoPatch._originalDialogKey = fieldValue;
        OptionInfoPatch._positionCtrl = instance;
        string str = LocalizationManager.Instance.LocaleResolver.GetString("Position/Dialog/" + fieldValue);
        if (string.IsNullOrEmpty(str))
          str = fieldValue;
        dialog.SetPrivateField("_dialog", (object) $"{str}({suffix})");
      }
      else
      {
        dialog.SetPrivateField("_dialog", (object) OptionInfoPatch._originalDialogKey);
        OptionInfoPatch._positionCtrl = (PositionController) null;
        OptionInfoPatch._originalDialogKey = string.Empty;
      }
    }
    catch (Exception ex)
    {
      DicePlugin.LogError((object) ex);
    }
  }

  private static bool HasTriggerSubMissions(PositionType position, out string missionKey)
  {
    foreach (MissionCheckData missionCheckData in MissionManagerData.Instance.ActiveSubMissions.Values)
    {
      if (missionCheckData.Position == position)
      {
        missionKey = missionCheckData.Key;
        return true;
      }
    }
    missionKey = string.Empty;
    return false;
  }

  private static void LogCheckpoint(string header, IEnumerable<ConditionResultItem> items)
  {
    StringBuilder logBuilder = new StringBuilder(header);
    OptionInfoPatch.AppendConditions(items, logBuilder);
    OptionInfoPatch.PrintToNarrativeLog(logBuilder.ToString());
  }

  private static void AppendConditions(
    IEnumerable<ConditionResultItem> items,
    StringBuilder logBuilder)
  {
    foreach (ConditionResultItem condition in items)
    {
      string conditions = DataHelper.ExtractConditions(condition, out bool _);
      if (!Utility.IsNullOrWhiteSpace(conditions))
        logBuilder.AppendLine("    " + conditions);
    }
  }

    private static void PrintToNarrativeLog(string message)
    {
        string str = Regex.Replace(message, "^\\s*$\\n|\\r", string.Empty, RegexOptions.Multiline).Trim();
        NarrativeLog narrativeLog = FungusManager.Instance.NarrativeLog;

        // 수정된 부분:
        // !Object.op_Inequality(...) 는 Object.op_Equality(...)와 같고,
        // 이는 narrativeLog == null 과 동일한 의미입니다.
        if (narrativeLog == null)
            return;

        NarrativeLogEntry narrativeLogEntry = new NarrativeLogEntry()
        {
            name = "DiceMaster",
            text = $"<size=18>{str}</size>"
        };
        narrativeLog.AddLine(narrativeLogEntry);
    }
}
