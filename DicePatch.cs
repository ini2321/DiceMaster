using HarmonyLib;
using Mortal.Story;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#nullable disable
namespace DiceMaster;

internal static class DicePatch
{
    // Token: 0x06000019 RID: 25 RVA: 0x00002EF0 File Offset: 0x000010F0
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DiceMenuDialog), "Awake")]
    public static void DiceMenuDialog_Awake_Postfix(DiceMenuDialog __instance)
    {
        if (DicePlugin.Instance.DictInputPrefab != null)
        {
            GameObject gameObject = Object.Instantiate<GameObject>(DicePlugin.Instance.DictInputPrefab, __instance.transform, false);
            gameObject.transform.localPosition = new Vector3(850f, -230f, 0f);
            DicePatch._curInputField = gameObject.GetComponentInChildren<InputField>();
            DicePatch._curInputField.GetComponentInChildren<InputField>().onValueChanged.AddListener(delegate (string value)
            {
                int num;
                if (int.TryParse(value, out num))
                {
                    DicePatch._diceValue = num;
                }
            });
            DicePatch._curInputField.text = "0";
            DicePatch._diceValue = 0;
        }
    }

    // Token: 0x0600001A RID: 26 RVA: 0x00002117 File Offset: 0x00000317
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DiceMenuDialog), "OnEnable")]
    public static void DiceMenuDialog_OnEnable_Postfix(DiceMenuDialog __instance)
    {
        if (DicePatch._curInputField != null)
        {
            DicePatch._curInputField.text = "0";
            DicePatch._diceValue = 0;
        }
    }

    // Token: 0x0600001B RID: 27 RVA: 0x00002F9C File Offset: 0x0000119C
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CheckPointManager), "Dice")]
    public static void CheckPointManager_Dice_Prefix(ref int random)
    {
        if (DicePatch._diceValue == 0 || DicePlugin.Instance.DictInputPrefab == null)
        {
            return;
        }
        DiceMenuDialog diceMenuDialog = Object.FindObjectOfType<DiceMenuDialog>();
        if (diceMenuDialog != null)
        {
            diceMenuDialog.SetPrivateField("_currentRandomValue", DicePatch._diceValue);
        }
        random = DicePatch._diceValue;
    }

    // Token: 0x0600001C RID: 28 RVA: 0x0000213B File Offset: 0x0000033B
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DiceMenuDialog), "CheckRevolution")]
    public static bool DiceMenuDialog_CheckRevolution_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }

    // Token: 0x0400001B RID: 27
    private static int _diceValue;

    // Token: 0x0400001C RID: 28
    private static InputField _curInputField;
}