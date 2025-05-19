using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceMaster
{
    internal class LocationTranslations
    {
        public static string TranslateLocation(string locationKeyWithoutYcPrefix) // 함수로 넘어오는 값은 이미 "養成_"가 제거된 상태
        {
            string baseLocationKey = locationKeyWithoutYcPrefix;
            bool isClosed = false;

            if (baseLocationKey.StartsWith("關閉_"))
            {
                isClosed = true;
                baseLocationKey = baseLocationKey.Substring("關閉_".Length); // "關閉_" 제거
            }

            string translatedName;
            bool isTranslated = true;

            switch (baseLocationKey)
            {
                case "伙房":
                    translatedName = "부엌"; break;
                case "大門":
                    translatedName = "대문"; break;
                case "女弟子房":
                    translatedName = "여제자방"; break;
                case "弟子房":
                    translatedName = "남제자방"; break;
                case "後山":
                    translatedName = "뒷산"; break;
                case "正心堂":
                    translatedName = "정심당"; break;
                case "煉丹房":
                    translatedName = "연단방"; break;
                case "秘密": 
                    translatedName = "비밀 장소"; break;
                case "練武場":
                    translatedName = "연무장"; break;
                case "校場":
                    translatedName = "연무장"; break;
                case "校場_夜晚":
                    translatedName = "연무장(밤)";
                    break;
                case "校場_早上":
                    translatedName = "연무장(아침)";
                    break;
                case "校場_白天":
                    translatedName = "연무장(낮)";
                    break;
                case "校場_黄昏":
                    translatedName = "연무장(저녁)";
                    break;
                case "講經堂":
                    translatedName = "강경당"; break;
                case "鍛冶場":
                    translatedName = "대장간"; break;
                case "無":
                    translatedName = "없음"; break;
                default:
                    translatedName = baseLocationKey; // 번역 실패 시 "關閉_"만 제거된 키 사용
                    isTranslated = false;
                break;
            }

            if (!isTranslated)
            {
                // "關閉_"가 있었고 baseLocationKey가 미번역이면, 
                // 예를 들어 locationKeyWithoutYcPrefix가 "關閉_未知地點"이었다면
                // baseLocationKey는 "未知地點"이 되고, 여기에 태그가 붙음.
                translatedName += "(장소 미번역)";
            }

            if (isClosed)
            {
                translatedName += " (닫힘)";
            }

            return translatedName;
        }
    }
}
