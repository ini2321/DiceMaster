using DiceMaster.LuaParse;
using Mortal.Core;
using Mortal.Story;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#nullable disable
namespace DiceMaster;

public class PositionEventCache
{
  private static readonly Lazy<PositionEventCache> instance = new Lazy<PositionEventCache>((Func<PositionEventCache>) (() => new PositionEventCache()));
  private readonly Dictionary<string, List<PositionEventData>> positionEventDataCache;
  private readonly Dictionary<PositionEventData, string> eventKeyCache;
  private readonly Dictionary<PositionEventData, string> eventDescCache;
  private readonly Dictionary<PositionEventData, string> scriptPrefixCache;

  private PositionEventCache()
  {
    this.positionEventDataCache = new Dictionary<string, List<PositionEventData>>();
    this.eventKeyCache = new Dictionary<PositionEventData, string>();
    this.eventDescCache = new Dictionary<PositionEventData, string>();
    this.scriptPrefixCache = new Dictionary<PositionEventData, string>();
  }

  public static PositionEventCache Instance => PositionEventCache.instance.Value;

  public List<PositionEventData> GetPositionEvents(string position)
  {
    List<PositionEventData> positionEvents;
    if (!this.positionEventDataCache.TryGetValue(position, out positionEvents))
    {
      positionEvents = this.LoadPositionEvents(position);
      this.positionEventDataCache[position] = positionEvents;
    }
    return positionEvents;
  }

  public string GetPositionEventName(PositionEventData eventData)
  {
    string eventKey;
    if (!this.eventKeyCache.TryGetValue(eventData, out eventKey))
    {
      eventKey = this.GenerateEventKey(eventData);
      this.eventKeyCache[eventData] = eventKey;
    }
    return eventKey;
  }

  public string GetPositionEventDesc(PositionEventData eventData)
  {
    string staticDesc;
    if (!this.eventDescCache.TryGetValue(eventData, out staticDesc))
    {
      staticDesc = this.GenerateStaticDesc(eventData);
      this.eventDescCache[eventData] = staticDesc;
    }
    return $"{this.GenerateDynamicDesc(eventData)}\n{staticDesc}";
  }

  public int GetPositionEventRate(PositionEventData eventData) => eventData.GetRate();

  public List<PositionEventData> GetPositionEventsSortedByRate(string position)
  {
    return this.GetPositionEvents(position).OrderByDescending<PositionEventData, int>(new Func<PositionEventData, int>(this.GetPositionEventRate)).ToList<PositionEventData>();
  }

    private List<PositionEventData> LoadPositionEvents(string position)
    {
        List<PositionEventData> positionEventDataList = new List<PositionEventData>();
        CheckPointManager instance1 = CheckPointManager.Instance;

        if (instance1 == null) // Object 모호성 수정
            return positionEventDataList;

        // 1. _position 필드의 타입은 PositionResultConfig
        PositionResultConfig positionConfig = instance1.GetFieldValue<PositionResultConfig>("_position");
        if (positionConfig == null)
            return positionEventDataList;

        // 2. PositionResultConfig는 CollectionData<PositionResultData>를 상속하므로,
        //    public List<PositionResultData> List 속성을 가짐.
        var positionResults = positionConfig.List;
        if (positionResults == null)
            return positionEventDataList;

        // 람다식 내 Object 모호성 수정 (p.name 직접 사용)
        PositionResultData instance2 = positionResults.FirstOrDefault<PositionResultData>(p => p.name == position);

        if (instance2 == null) // Object 모호성 수정
            return positionEventDataList;

        // 3. _scriptPrefix 필드의 타입은 string
        string fieldValue = instance2.GetFieldValue<string>("_scriptPrefix");

        // 4. PositionResultData는 CollectionData<PositionEventData>를 상속하므로,
        //    public List<PositionEventData> List 속성을 가짐.
        var eventDataItems = instance2.List;
        if (eventDataItems == null)
            return positionEventDataList;

        foreach (PositionEventData key in eventDataItems)
        {
            this.scriptPrefixCache[key] = fieldValue;
            positionEventDataList.Add(key);
        }
        return positionEventDataList;
    }

    private string GenerateEventKey(PositionEventData eventData)
  {
    return $"{this.positionEventDataCache.FirstOrDefault<KeyValuePair<string, List<PositionEventData>>>((Func<KeyValuePair<string, List<PositionEventData>>, bool>) (p => p.Value.Contains(eventData))).Key}事件：{DataHelper.GetEventName(eventData)}";
  }

  private string GenerateStaticDesc(PositionEventData eventData)
  {
    StringBuilder stringBuilder = new StringBuilder();
    stringBuilder.AppendLine("可能的结果：");
    string str1;
    if (!this.scriptPrefixCache.TryGetValue(eventData, out str1))
      throw new InvalidOperationException("Script prefix not found for event data");
    List<DiceMaster.LuaParse.Action> actionsFromLua = LuaParser.Instance.ExtractActionsFromLua($"{str1}_{eventData.ResultType}");
    List<DiceMaster.LuaParse.Action> list1 = actionsFromLua.Where<DiceMaster.LuaParse.Action>((Func<DiceMaster.LuaParse.Action, bool>) (a => !a.IsConditional)).ToList<DiceMaster.LuaParse.Action>();
    List<DiceMaster.LuaParse.Action> list2 = actionsFromLua.Where<DiceMaster.LuaParse.Action>((Func<DiceMaster.LuaParse.Action, bool>) (a => a.IsConditional)).ToList<DiceMaster.LuaParse.Action>();
    string str2 = LuaParser.Description(list1);
    string str3 = LuaParser.Description(list2);
    stringBuilder.AppendLine("    一定触发：" + str2);
    stringBuilder.AppendLine("    某些条件下：" + str3);
    return stringBuilder.ToString();
  }

  private string GenerateDynamicDesc(PositionEventData eventData)
  {
    StringBuilder stringBuilder = new StringBuilder();
    int fieldValue1 = eventData.GetFieldValue<int>("_defaultRate");
    int rate = eventData.GetRate();
    stringBuilder.AppendLine($"概率：{rate}({fieldValue1})");
    int num = eventData.GetFieldValue<bool>("_toggleCondition") ? 1 : 0;
    ConditionResultItem fieldValue2 = eventData.GetFieldValue<ConditionResultItem>("_activeCondition");
    bool isInPeriod;
    if (num != 0 && fieldValue2 != null)
      stringBuilder.AppendLine("激活条件：" + DataHelper.ExtractConditions(fieldValue2, out isInPeriod));
    stringBuilder.AppendLine("判定条件：");
    foreach (PositionEventRateItem positionEventRateItem in eventData.GetFieldValue<PositionEventRateItem[]>("_eventRateItem"))
    {
      string conditions = DataHelper.ExtractConditions(positionEventRateItem.Condition, out isInPeriod);
      string str = positionEventRateItem.Rate > 0 ? $"+{positionEventRateItem.Rate}" : positionEventRateItem.Rate.ToString();
      stringBuilder.AppendLine($"    {conditions}：{str}");
    }
    return stringBuilder.ToString();
  }
}
