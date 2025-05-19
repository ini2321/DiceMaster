using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceMaster
{
    internal class CharacterTranslations
    {
        public static string TranslateCharacter(string mycharacter)
        {
            string id = mycharacter;

            switch (mycharacter)
            {
                case "三師兄": id = "삼사형"; break;
                case "二師兄": id = "이사형"; break;
                case "四師兄": id = "사사형"; break;
                case "夏侯蘭": id = "하후란"; break;
                case "大師兄": id = "대사형"; break;
                case "瑞笙": id = "서생"; break; // 또는 "예생"
                case "虞小梅": id = "우소매"; break;
                case "龍湘": id = "용상"; break;
                case "上官螢": id = "상관형"; break; // 확인된 ID
                case "劉顎": id = "유악"; break;
                case "劉顎_趙活_車軒轅_王二壯": id = "유악, 조활, 차헌원, 왕이장"; break;
                case "劉顎_趙活_車軒轅_王二壯_最高": id = "유악, 조활, 차헌원, 왕이장 (최고)"; break;
                case "南宮深": id = "남궁심"; break;
                case "南宮淺": id = "남궁천"; break;
                case "宋悲": id = "송비"; break;
                case "小師妹": id = "소사매"; break;
                case "尹志平": id = "윤지평"; break;
                case "崆峒四姝": id = "공동사주"; break; // 또는 "공동 네 아가씨"
                case "掌門人": id = "장문인"; break;
                case "最高唐門師兄": id = "최고 당문 사형"; break;
                case "李富貴": id = "이부귀"; break;
                case "王二壯": id = "왕이장"; break;
                case "申屠龍": id = "신도룡"; break;
                case "福韞": id = "복온"; break;     // 확인된 ID
                case "葉雲裳": id = "엽운상"; break;
                case "葉雲萊": id = "엽운래"; break;
                case "解無塵": id = "해무진"; break;   // 확인된 ID
                case "趙活": id = "조활"; break;
                case "車軒轅": id = "차헌원"; break;
                case "郁竹": id = "욱죽"; break;
                case "魏菊": id = "위국"; break;
                case "龍洲": id = "용주"; break;

                default:
                    id = mycharacter; // 일치하는 case가 없으면 원본 ID (중국어) 표시
                                      // DicePlugin.LogInfo($"GetDescription: Unhandled character ID '{processedId}' for Character actionType."); // 필요시 로그
                break;
            }
            return id;
        }
    }
}
