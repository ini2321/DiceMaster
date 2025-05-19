using Microsoft.CSharp;
using Mortal.Core;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#nullable disable
namespace DiceMaster.LuaParse;

public class Action
{
  public ActionType Type { get; set; }

  public bool IsConditional { get; set; }

  public string Content { get; set; }

  public string Description { get; private set; }

  public Action(ActionType type, string content, bool isConditional = false)
  {
    this.Type = type;
    this.Content = content;
    this.IsConditional = isConditional;
    this.Description = this.ParseAction();
  }

  private string ParseAction()
  {
    string action;
    switch (this.Type)
    {
      case ActionType.ModifyAction:
        action = this.ParseModifyAction();
        break;
      case ActionType.BattleScene:
        action = this.Content.Contains("Combat") ? "결투" : "난전";
        break;
      default:
        action = string.Empty;
        break;
    }
    return action;
  }

  public static string ToLiteral(string input) //특수문자 로그 출력용 함수
  {
    if (string.IsNullOrEmpty(input))
        return input;

    var literal = new StringBuilder(input.Length + 2);
    foreach (var c in input)
    {
        switch (c)
        {
            case '\n': literal.Append(@"\n"); break;
            case '\r': literal.Append(@"\r"); break;
            case '\t': literal.Append(@"\t"); break;
            case '\\': literal.Append(@"\\"); break;
            case '"': literal.Append("\\\""); break;
            // 필요한 다른 이스케이프 시퀀스 추가
            default:
                literal.Append(c);
                break;
        }
    }
    // literal.Append("\"");
    return literal.ToString();
}

    private string ParseModifyAction()
  {
    Match match = Regex.Match(this.Content, "statmodifymanager\\.(\\w+)\\(([^)]*)\\)");
    string actionType = match.Groups[1].Value;
    string parameters = match.Groups[2].Value;
    return !match.Success ? string.Empty : this.GetActionDescription(actionType, parameters);
  }

  private string GetActionDescription(string actionType, string parameters)
  {
    string[] array = ((IEnumerable<string>) parameters.Split(',')).Select<string, string>((Func<string, string>) (p => p.Trim().Trim('"'))).ToArray<string>();
    if (array.Length < 1)
      return string.Empty;
    string id1 = array[0];
    string str;
    if (actionType != null)
    {
      switch (actionType.Length)
      {
        case 6:
          if (actionType == "Player")
          {
            GameStatType result;
            str = DataHelper.TryParseEnum<GameStatType>(id1, out result) ? result.ToString() : id1;
            goto label_29;
          }
          break;
        case 7:
          switch (actionType[3])
          {
            case 'B':
              if (actionType == "AddBook")
              {
                str = DataHelper.GetItemName((GameItemType) 1, id1);
                goto label_29;
              }
              break;
            case 'F':
              switch (actionType)
              {
                case "AddFlag":
                  str = DataHelper.GetFlagName(id1);
                  goto label_29;
                case "SetFlag":
                  str = DataHelper.GetFlagName(id1);
                  goto label_29;
              }
              break;
            case 'M':
              if (actionType == "AddMisc")
              {
                str = DataHelper.GetItemName((GameItemType) 2, id1);
                goto label_29;
              }
              break;
          }
          break;
        case 9:
          switch (actionType[0])
          {
            case 'A':
              if (actionType == "AddTalent")
              {
                str = DataHelper.GetTalentName(id1);
                goto label_29;
              }
              break;
            case 'C':
              if (actionType == "Character")
              {
                RelationshipStatType result;
                str = DataHelper.TryParseEnum<RelationshipStatType>(id1, out result) ? result.ToString() : id1;
                goto label_29;
              }
              break;
          }
          break;
        case 10:
          switch (actionType[0])
          {
            case 'A':
              if (actionType == "AddSpecial")
              {
                str = DataHelper.GetItemName((GameItemType) 3, id1);
                goto label_29;
              }
              break;
            case 'R':
              if (actionType == "RemoveMisc")
              {
                str = DataHelper.GetItemName((GameItemType) 2, id1);
                goto label_29;
              }
              break;
          }
          break;
        case 15:
          if (actionType == "ModifyEnemyTeam")
          {
            str = DataHelper.GetTeamName(id1);
            goto label_29;
          }
          break;
        case 19:
          if (actionType == "UpdateSetPlayerStat")
          {
            GameStatType result;
            str = DataHelper.TryParseEnum<GameStatType>(id1, out result) ? result.ToString() : id1;
            goto label_29;
          }
          break;
      }
    }
    str = id1;
label_29:
    string id2 = str;
    if (array.Length == 1)
      return !(actionType == "RemoveMisc") ? string.Empty : "버림:" + id2;
    int result1;
    return !int.TryParse(array[1], out result1) ? string.Empty : this.GetDescription(actionType, id2, result1);
  }

  private string GetDescription(string actionType, string id, int value)
  {
    string str = value > 0 ? $"+{value}" : $"{value}";
    string description;
    if (actionType != null)
    {
      switch (actionType.Length)
      {
        case 6:
          if (actionType == "Player")
          {
            id = PlayerinfoTranslations.TranslatePlayerstats(id);
            description = RichText.ColorText($"{id} {str}");
            goto label_24;
          }
          break;
        case 7:
          switch (actionType[3])
          {
            case 'B':
              if (actionType == "AddBook")
              {
                description = $"{id} x{value}";
                goto label_24;
              }
              break;
            case 'F':
              switch (actionType)
              {
                case "AddFlag":
                  id = FlagTranslations.TranslateDevNote(id, value);
                  description = string.IsNullOrEmpty(id) ? string.Empty : $"{id} {value}";
                  goto label_24;
                case "SetFlag":
                  //DicePlugin.LogInfo($"{ToLiteral(id)},{value}");
                  id = FlagTranslations.TranslateDevNote(id, value);
                  description = string.IsNullOrEmpty(id) ? string.Empty : $"활성:{id}{value}";
                  goto label_24;
              }
              break;
            case 'M':
              if (actionType == "AddMisc")
              {
                description = $"{id} x{value}";
                goto label_24;
              }
              break;
          }
          break;
        case 9:
          switch (actionType[0])
          {
            case 'A':
              if (actionType == "AddTalent")
              {
                description = $"기능 {id} lv{value}";
                goto label_24;
              }
              break;
            case 'C': // actionType의 첫 글자가 'C'인 경우
                if (actionType == "Character")
                {
                    // ... (id 변수가 준비되었다고 가정) ...
                    id = CharacterTranslations.TranslateCharacter(id);
                    description = RichText.ColorText($"{id} 호감도 {str}"); // "好感 " -> " 호감 " (공백 유의)
                    goto label_24; // 이 goto는 디컴파일러가 생성한 것이므로, 실제로는 switch문의 break나 다른 제어 흐름일 수 있음
                                    // 만약 이 goto가 없다면, 이 if 블록 다음에 break;가 올 것입니다.
                }
                break; // actionType == "Character" 조건이 false일 경우의 break
                    }
          break;
        case 10:
          if (actionType == "AddSpecial")
          {
            description = $"{id} x{value}";
            goto label_24;
          }
          break;
        case 15:
          if (actionType == "ModifyEnemyTeam")
          {
            description = RichText.ColorText($"{id}단결력 {str}");
            goto label_24;
          }
          break;
        case 19:
          if (actionType == "UpdateSetPlayerStat")
          {
            id = PlayerinfoTranslations.TranslatePlayerstats(id);
            description = $"활성:{id} {value}";
            goto label_24;
          }
          break;
      }
    }
    description = string.Empty;
label_24:
    return description;
  }
}
