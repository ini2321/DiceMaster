#nullable disable
namespace DiceMaster;

public class RichText
{
  public static string Green(string text) => $"<color=#A3B86C>{text}</color>";

  public static string Red(string text) => $"<color=#CD594A>{text}</color>";

  public static string Golden(string text) => $"<color=#BCA136>{text}</color>";

  public static string SeparateLine(string text)
  {
    int num = 68 - text.Length;
    return new string('═', num / 2) + text + new string('═', num / 2);
  }

  public static string ColorText(string text)
  {
    return !text.Contains("+") ? RichText.Red(text) : RichText.Green(text);
  }
}
