using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#nullable disable
namespace DiceMaster.LuaParse;

public class LuaParser
{
  private static readonly Lazy<LuaParser> instance = new Lazy<LuaParser>((Func<LuaParser>) (() => new LuaParser()));
  private static readonly Regex indexMarkerRegex = new Regex("^\\s*--index:(O(_\\w+)+)", RegexOptions.Multiline);
  private static readonly Regex endBlockRegex = new Regex("\\b(elseif|end)");
  private static readonly Regex switchRegex = new Regex("checkpointmanager\\.Switch\\(([^)]*)\\)");
  private static readonly Regex positionRegex = new Regex("checkpointmanager\\.Position\\(([^)]*)\\)");
  private static readonly Regex modifyRegex = new Regex("\\s*statmodifymanager\\.(?!GetDisplayTime)\\w+\\(.*?\\)");
  private static readonly Regex battleScenesRegex = new Regex("luamanager\\.ChangeScene\\(\"(Combat|Battle)\"");
  private static readonly Regex scriptActionsRegex = new Regex("luamanager\\.SetNextScript");
  public Dictionary<string, List<Action>> OptionResult;
  private Dictionary<string, List<Action>> luaModification;
  private Dictionary<string, List<string>> luaOptions;
  private Dictionary<string, List<string>> luaSwitchCheck;
  private Dictionary<string, List<string>> luaPositionWork;

  private LuaParser()
  {
    this.OptionResult = new Dictionary<string, List<Action>>();
    this.luaModification = new Dictionary<string, List<Action>>();
    this.luaOptions = new Dictionary<string, List<string>>();
    this.luaSwitchCheck = new Dictionary<string, List<string>>();
    this.luaPositionWork = new Dictionary<string, List<string>>();
  }

  public static LuaParser Instance => LuaParser.instance.Value;

  public Dictionary<string, List<Action>> ExtractOptionActionsFromLua(
    string friendlyName,
    string luaScript = "")
  {
    Dictionary<string, List<Action>> optionActionsFromLua = new Dictionary<string, List<Action>>();
    List<string> stringList;
    if (this.luaOptions.TryGetValue(friendlyName, out stringList))
    {
      foreach (string key in stringList)
        optionActionsFromLua.Add(key, this.OptionResult[key]);
      return optionActionsFromLua;
    }
    this.luaOptions[friendlyName] = new List<string>();
    MatchCollection matchCollection = LuaParser.indexMarkerRegex.Matches(luaScript);
    for (int i = 0; i < matchCollection.Count; ++i)
    {
      string key = matchCollection[i].Groups[1].Value;
      this.luaOptions[friendlyName].Add(key);
      if (this.OptionResult.ContainsKey(key))
      {
        optionActionsFromLua[key] = this.OptionResult[key];
      }
      else
      {
        int startIndex = matchCollection[i].Index + matchCollection[i].Length;
        int val1 = luaScript.Length;
        if (i + 1 < matchCollection.Count)
        {
          val1 = Math.Min(val1, matchCollection[i + 1].Index);
        }
        else
        {
          Match match = LuaParser.endBlockRegex.Match(luaScript.Substring(startIndex));
          if (match.Success)
            val1 = startIndex + match.Index;
        }
        string blockContent = luaScript.Substring(startIndex, val1 - startIndex);
        List<Action> actionList = new List<Action>();
        this.AddActionsToList(blockContent, LuaParser.modifyRegex, ActionType.ModifyAction, actionList);
        this.AddActionsToList(blockContent, LuaParser.battleScenesRegex, ActionType.BattleScene, actionList);
        optionActionsFromLua[key] = actionList;
        this.OptionResult[key] = actionList;
      }
    }
    return optionActionsFromLua;
  }

  private void AddActionsToList(
    string blockContent,
    Regex regex,
    ActionType actionType,
    List<Action> actionList)
  {
    actionList.AddRange(regex.Matches(blockContent).Cast<Match>().Select<Match, Action>((Func<Match, Action>) (m => new Action(actionType, m.Value.Trim()))));
  }

  public List<string> ExtractSwitchCheckFromLua(string friendlyName, string luaScript = "")
  {
    if (this.luaSwitchCheck.ContainsKey(friendlyName))
      return this.luaSwitchCheck[friendlyName];
    List<string> matches = this.ExtractMatches(luaScript, LuaParser.switchRegex);
    this.luaSwitchCheck[friendlyName] = matches;
    return matches;
  }

  public List<string> ExtractPositionWorkFromLua(string friendlyName, string luaScript = "")
  {
    if (this.luaPositionWork.ContainsKey(friendlyName))
      return this.luaPositionWork[friendlyName];
    List<string> matches = this.ExtractMatches(luaScript, LuaParser.positionRegex);
    this.luaPositionWork[friendlyName] = matches;
    return matches;
  }

  private List<string> ExtractMatches(string luaScript, Regex regex)
  {
    List<string> matches = new List<string>();
    foreach (Match match in regex.Matches(luaScript))
    {
      string str = match.Groups[1].Value.Trim().Trim('"');
      if (!matches.Contains(str))
        matches.Add(str);
    }
    return matches;
  }

  public List<Action> ExtractActionsFromLua(string luaScriptName)
  {
    if (this.luaModification.ContainsKey(luaScriptName))
      return this.luaModification[luaScriptName];
    string[] strArray = DataHelper.LoadLuaScript(luaScriptName).Split(new string[3]
    {
      "\r\n",
      "\r",
      "\n"
    }, StringSplitOptions.None);
    List<Action> actionsFromLua = new List<Action>();
    bool isConditional = false;
    foreach (string str in strArray)
    {
      string input = str.Trim();
      if (input.StartsWith("if") || input.StartsWith("elseif"))
        isConditional = true;
      else if (input.StartsWith("end"))
        isConditional = false;
      foreach (Match match in LuaParser.modifyRegex.Matches(input))
        actionsFromLua.Add(new Action(ActionType.ModifyAction, match.Value, isConditional));
      foreach (Match match in LuaParser.battleScenesRegex.Matches(input))
        actionsFromLua.Add(new Action(ActionType.BattleScene, match.Value, isConditional));
    }
    this.luaModification[luaScriptName] = actionsFromLua;
    return actionsFromLua;
  }

  public static string Description(List<Action> actions)
  {
    return string.Join("|", (IEnumerable<string>) actions.Select<Action, string>((Func<Action, string>) (action => action.Description)).Where<string>((Func<string, bool>) (description => !string.IsNullOrEmpty(description))).ToList<string>());
  }
}
