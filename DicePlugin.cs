using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using DiceMaster.Config;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

#nullable disable
namespace DiceMaster;

[BepInPlugin("DiceMaster", "DiceMaster", "1.0.0")]
public class DicePlugin : BaseUnityPlugin
{
    // Token: 0x17000001 RID: 1
    // (get) Token: 0x06000003 RID: 3 RVA: 0x00002067 File Offset: 0x00000267
    // (set) Token: 0x06000004 RID: 4 RVA: 0x0000206E File Offset: 0x0000026E
    public static DicePlugin Instance { get; private set; }

    // Token: 0x06000005 RID: 5 RVA: 0x000025EC File Offset: 0x000007EC
    private void Awake()
    {
        DicePlugin.Instance = this;
        DicePlugin._logger = base.Logger;
        ConfigManager.Init();
        try
        {
            if (ConfigManager.EnableDiceController.Value)
            {
                DicePlugin._harmony.PatchAll(typeof(DicePatch));
            }
            DicePlugin._harmony.PatchAll(typeof(DebugPatch));
            if (ConfigManager.EnableOptionInfo.Value)
            {
                DicePlugin._harmony.PatchAll(typeof(OptionInfoPatch));
            }
        }
        catch
        {
            DicePlugin.LogError("Failed to patch");
        }
        GameObject gameObject;
        if (AssetBundleHelper.LoadAsset<GameObject>("DiceInputField", "Assets.mortaldice", out gameObject, false))
        {
            this.DictInputPrefab = gameObject;
        }
        else
        {
            DicePlugin.LogError("Failed to load asset");
        }
        float num = (float)Screen.width;
        float num2 = (float)Screen.height;
        this.scaleFactor = Mathf.Min(num / 1920f, num2 / 1080f);
        this.logWindowRect = new Rect(20f * this.scaleFactor, 20f * this.scaleFactor, 750f * this.scaleFactor, 750f * this.scaleFactor);
    }

    // Token: 0x06000006 RID: 6 RVA: 0x0000270C File Offset: 0x0000090C
    private void SetGUI()
    {
        this.headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(this.baseFontSize * this.scaleFactor),
            // fontStyle = (FontStyle)1, // 이전 수정안 (오류는 해결되지만, 가독성 낮음)
            fontStyle = FontStyle.Bold, // 더 좋은 수정안 (문서 및 enum 멤버 직접 사용)
                                        // (파일 상단에 using UnityEngine; 가 있으므로 FontStyle만 써도 됨)
            alignment = TextAnchor.MiddleCenter, // 이 부분은 이전 논의에서 확인됨
            richText = true
        };
        this.logEntryStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(16f * this.scaleFactor),
            richText = true
        };
        this.descriptionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(this.baseFontSize * this.scaleFactor),
            richText = true,
            wordWrap = true
        };
        this.buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = (int)(this.baseFontSize * this.scaleFactor),
            fixedWidth = this.logWindowRect.width * 0.2f
        };
        this.darkBackgroundStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
                {
                    background = this.MakeTex(600, 400, new Color(0.1f, 0.1f, 0.1f, 1f))
                }
        };
        this.lightBackgroundStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
                {
                    background = this.MakeTex(600, 400, new Color(0.2f, 0.2f, 0.2f, 1f))
                }
        };
    }

    // Token: 0x06000007 RID: 7 RVA: 0x00002890 File Offset: 0x00000A90
    private void Update()
    {
        if (Screen.width != this.lastScreenSize.x || Screen.height != this.lastScreenSize.y)
        {
            this.lastScreenSize.x = Screen.width;
            this.lastScreenSize.y = Screen.height;
            this.OnScreenSizeChanged((float)this.lastScreenSize.x, (float)this.lastScreenSize.y);
        }
        if (Input.GetKeyDown(ConfigManager.LogMenuToggleKey.Value))
        {
            this.showLogWindow = !this.showLogWindow;
        }
    }

    // Token: 0x06000008 RID: 8 RVA: 0x00002920 File Offset: 0x00000B20
    private void OnDestroy()
    {
        Harmony harmony = DicePlugin._harmony;
        if (harmony == null)
        {
            return;
        }
        harmony.UnpatchSelf();
    }

    // Token: 0x06000009 RID: 9 RVA: 0x00002940 File Offset: 0x00000B40
    private void OnGUI()
    {
        if (!this.isInitialized)
        {
            this.SetGUI();
            this.isInitialized = true;
        }
        if (GUI.skin != null)
        {
            GUI.skin.label.fontSize = (int)(this.baseFontSize * this.scaleFactor);
        }
        if (this.showLogWindow)
        {
            this.logWindowRect = GUILayout.Window(1, this.logWindowRect, new GUI.WindowFunction(this.DrawLogWindow), "Dice Log Output", Array.Empty<GUILayoutOption>());
        }
    }

    // Token: 0x0600000A RID: 10 RVA: 0x00002076 File Offset: 0x00000276
    public void AddOptionEntry(string key, string description = "")
    {
        key = RichText.Golden(key);
        this.optionEntries.Add(new ValueTuple<string, string>(key, description));
    }

    // Token: 0x0600000B RID: 11 RVA: 0x00002092 File Offset: 0x00000292
    public void AddSwitchEntry(string key, string description)
    {
        key = RichText.Golden(key);
        this.switchEntries.Add(new ValueTuple<string, string>(key, description));
    }

    // Token: 0x0600000C RID: 12 RVA: 0x000020AE File Offset: 0x000002AE
    public void AddEventEntry(PositionEventData eventData, string key, string description)
    {
        this.eventEntries.Add(new ValueTuple<PositionEventData, string, string>(eventData, key, description));
    }

    // Token: 0x0600000D RID: 13 RVA: 0x000020C3 File Offset: 0x000002C3
    public void AddMissionEntry(MissionCheckData missionData, string key, string description)
    {
        this.missionEntries.Add(new ValueTuple<MissionCheckData, string, string>(missionData, key, description));
    }

    // Token: 0x0600000E RID: 14 RVA: 0x000029BC File Offset: 0x00000BBC
    private void DrawLogWindow(int windowID)
    {
        GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
        foreach (string text in this.tabs)
        {
            if (GUILayout.Toggle(this.currentTab == text, text, Array.Empty<GUILayoutOption>()))
            {
                this.currentTab = text;
            }
        }
        GUILayout.EndHorizontal();
        this.logScrollPosition = GUILayout.BeginScrollView(this.logScrollPosition, Array.Empty<GUILayoutOption>());
        string text2 = this.currentTab;
        if (!(text2 == "OPTIONS"))
        {
            if (!(text2 == "SWITCH"))
            {
                if (!(text2 == "EVENTS"))
                {
                    if (text2 == "MISSIONS")
                    {
                        this.DrawMissionEntries(this.missionEntries);
                    }
                }
                else
                {
                    this.DrawEventEntries(this.eventEntries);
                }
            }
            else
            {
                this.DrawEntries(this.switchEntries);
            }
        }
        else
        {
            this.DrawEntries(this.optionEntries);
        }
        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    // Token: 0x0600000F RID: 15 RVA: 0x00002AA4 File Offset: 0x00000CA4

    private void DrawEntries(IEnumerable<ValueTuple<string, string>> entries)
    {
        bool flag = true; // 번갈아가며 다른 배경 스타일을 적용하기 위한 플래그

        foreach (ValueTuple<string, string> entryTuple in entries) // 변수명을 entryTuple로 변경
        {
            string entryKey = entryTuple.Item1;         // Item1을 의미 있는 변수명으로 받음
            string entryDescription = entryTuple.Item2; // Item2를 의미 있는 변수명으로 받음

            GUIStyle currentBackgroundStyle = flag ? this.darkBackgroundStyle : this.lightBackgroundStyle;
            flag = !flag; // 다음 항목을 위해 플래그 반전

            GUILayout.BeginVertical(currentBackgroundStyle, Array.Empty<GUILayoutOption>());
            GUILayout.Label(entryKey, this.logEntryStyle, Array.Empty<GUILayoutOption>());
            GUILayout.Label(entryDescription, this.descriptionStyle, Array.Empty<GUILayoutOption>());
            GUILayout.EndVertical();
        }
    }

    // Token: 0x06000010 RID: 16 RVA: 0x00002B3C File Offset: 0x00000D3C
    private void DrawEventEntries(IEnumerable<(PositionEventData EventData, string Key, string Description)> entries)
    {
        bool flag = true;
        foreach (var (eventData, key, description) in entries) // 변수명은 원하는대로
        {
            GUIStyle currentBackgroundStyle = flag ? this.darkBackgroundStyle : this.lightBackgroundStyle;
            flag = !flag;

            GUILayout.BeginVertical(currentBackgroundStyle, Array.Empty<GUILayoutOption>());
            GUILayout.Label(key, this.logEntryStyle, Array.Empty<GUILayoutOption>());
            GUILayout.Label(description, this.descriptionStyle, Array.Empty<GUILayoutOption>());
            if (GUILayout.Button("设置事件", this.buttonStyle, Array.Empty<GUILayoutOption>())) // "设置事件" -> "이벤트 설정" 등으로 번역
            {
                OptionInfoPatch.TargetEvent = eventData;
            }
            GUILayout.EndVertical();
        }
    }

    // Token: 0x06000011 RID: 17 RVA: 0x00002C00 File Offset: 0x00000E00
    // 파라미터에서 [TupleElementNames(...)] 속성만 제거
    private void DrawMissionEntries(IEnumerable<ValueTuple<MissionCheckData, string, string>> entries)
    {
        bool flag = true; // 번갈아가며 다른 배경 스타일을 적용하기 위한 플래그

        foreach (ValueTuple<MissionCheckData, string, string> entryTuple in entries) // 변수명을 entryTuple로 변경
        {
            MissionCheckData missionDataItem = entryTuple.Item1; // Item1을 의미 있는 변수명으로 받음
            string entryKey = entryTuple.Item2;              // Item2를 의미 있는 변수명으로 받음
            string entryDescription = entryTuple.Item3;      // Item3를 의미 있는 변수명으로 받음

            GUIStyle currentBackgroundStyle = flag ? this.darkBackgroundStyle : this.lightBackgroundStyle;
            flag = !flag; // 다음 항목을 위해 플래그 반전

            GUILayout.BeginVertical(currentBackgroundStyle, Array.Empty<GUILayoutOption>());
            GUILayout.Label(entryKey, this.logEntryStyle, Array.Empty<GUILayoutOption>());
            GUILayout.Label(entryDescription, this.descriptionStyle, Array.Empty<GUILayoutOption>());

            // "设置事件" 버튼 텍스트는 나중에 번역 필요
            if (GUILayout.Button("设置事件", this.buttonStyle, Array.Empty<GUILayoutOption>()))
            {
                OptionInfoPatch.TargetMission = missionDataItem;
                // MissionManagerData.Instance.ActiveSubMissions.Add(entryKey, missionDataItem);
                // 위 라인은 Dictionary에 이미 해당 키가 있을 경우 예외를 발생시킬 수 있습니다.
                // 좀 더 안전하게는 다음과 같이 처리할 수 있습니다:
                if (MissionManagerData.Instance != null && MissionManagerData.Instance.ActiveSubMissions != null)
                {
                    if (!MissionManagerData.Instance.ActiveSubMissions.ContainsKey(entryKey))
                    {
                        MissionManagerData.Instance.ActiveSubMissions.Add(entryKey, missionDataItem);
                    }
                    else
                    {
                        // 이미 키가 존재할 경우 어떻게 처리할지 결정 (예: 업데이트 또는 로그)
                        // DicePlugin.LogWarning($"Mission key '{entryKey}' already exists in ActiveSubMissions. Updating.");
                        MissionManagerData.Instance.ActiveSubMissions[entryKey] = missionDataItem; // 기존 값 덮어쓰기
                    }
                }
            }
            GUILayout.EndVertical();
        }
    }

    // Token: 0x06000012 RID: 18 RVA: 0x00002CD4 File Offset: 0x00000ED4
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] array = new Color[width * height];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = col;
        }
        Texture2D texture2D = new Texture2D(width, height);
        texture2D.SetPixels(array);
        texture2D.Apply();
        return texture2D;
    }

    // Token: 0x06000013 RID: 19 RVA: 0x000020D8 File Offset: 0x000002D8
    public void ClearLogEntries()
    {
        this.optionEntries.Clear();
        this.switchEntries.Clear();
        this.eventEntries.Clear();
        this.missionEntries.Clear();
    }

    // Token: 0x06000014 RID: 20 RVA: 0x00002D14 File Offset: 0x00000F14
    public static void LogInfo(object o)
    {
        ManualLogSource logger = DicePlugin._logger;
        bool flag;
        BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(3, 2, out flag);
        if (flag)
        {
            bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[");
            bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>(DateTime.Now.ToString(DateTimeFormatInfo.InvariantInfo));
            bepInExInfoLogInterpolatedStringHandler.AppendLiteral("] ");
            bepInExInfoLogInterpolatedStringHandler.AppendFormatted<object>(o);
        }
        logger.LogInfo(bepInExInfoLogInterpolatedStringHandler);
    }

    // Token: 0x06000015 RID: 21 RVA: 0x00002D70 File Offset: 0x00000F70
    public static void LogError(object o)
    {
        ManualLogSource logger = DicePlugin._logger;
        bool flag;
        BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(3, 2, out flag);
        if (flag)
        {
            bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[");
            bepInExErrorLogInterpolatedStringHandler.AppendFormatted<string>(DateTime.Now.ToString(DateTimeFormatInfo.InvariantInfo));
            bepInExErrorLogInterpolatedStringHandler.AppendLiteral("] ");
            bepInExErrorLogInterpolatedStringHandler.AppendFormatted<object>(o);
        }
        logger.LogError(bepInExErrorLogInterpolatedStringHandler);
    }

    // Token: 0x06000018 RID: 24 RVA: 0x00002E80 File Offset: 0x00001080
    private void OnScreenSizeChanged(float width, float height)
    {
        float num = (float)Screen.width;
        float num2 = (float)Screen.height;
        this.scaleFactor = Mathf.Min(num / 1920f, num2 / 1080f);
        this.logWindowRect = new Rect(20f * this.scaleFactor, 20f * this.scaleFactor, 750f * this.scaleFactor, 750f * this.scaleFactor);
    }

    // Token: 0x04000003 RID: 3
    public GameObject DictInputPrefab;

    // Token: 0x04000004 RID: 4
    private static ManualLogSource _logger;

    // Token: 0x04000005 RID: 5
    private static Harmony _harmony = new Harmony("DiceMaster");

    private List<(string Key, string Description)> optionEntries = new();
    private List<(string Key, string Description)> switchEntries = new();
    private List<(PositionEventData EventData, string Key, string Description)> eventEntries = new();
    private List<(MissionCheckData MissionData, string Key, string Description)> missionEntries = new();

    // Token: 0x0400000A RID: 10
    private Vector2 logScrollPosition;

    // Token: 0x0400000B RID: 11
    private Rect logWindowRect = new Rect(20f, 20f, 750f, 750f);

    // Token: 0x0400000C RID: 12
    private bool showLogWindow = true;

    // Token: 0x0400000D RID: 13
    private float baseFontSize = 14f;

    // Token: 0x0400000E RID: 14
    private float scaleFactor = 1f;

    // Token: 0x0400000F RID: 15
    private GUIStyle headerStyle;

    // Token: 0x04000010 RID: 16
    private GUIStyle logEntryStyle;

    // Token: 0x04000011 RID: 17
    private GUIStyle descriptionStyle;

    // Token: 0x04000012 RID: 18
    private GUIStyle buttonStyle;

    // Token: 0x04000013 RID: 19
    private GUIStyle darkBackgroundStyle;

    // Token: 0x04000014 RID: 20
    private GUIStyle lightBackgroundStyle;

    // Token: 0x04000015 RID: 21
    private bool isInitialized;

    // Token: 0x04000016 RID: 22
    private string currentTab = "OPTIONS";

    // Token: 0x04000017 RID: 23
    private string[] tabs = new string[] { "OPTIONS", "SWITCH", "EVENTS", "MISSIONS" };

    // Token: 0x04000018 RID: 24
    private Vector2Int lastScreenSize;

    // Token: 0x02000005 RID: 5
    public static class Constants
    {
        // Token: 0x04000019 RID: 25
        public const string BundleRes = "Assets.mortaldice";

        // Token: 0x0400001A RID: 26
        public const string AssetName = "DiceInputField";
    }
}