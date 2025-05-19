using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceMaster
{
    internal class PlayerinfoTranslations
    {
        public static string TranslatePlayerstats(string mystats) {
            string id;

            switch (mystats) // 또는 id가 "數值_" 포함하면 제거한 PKey 기준으로
            {
            case "體力": mystats = "체력"; break;
            case "內力": mystats = "내력"; break;
            case "輕功": mystats = "경공"; break;
            case "刀劍": mystats = "도검"; break;
            case "暗器": mystats = "암기"; break;
            case "拳掌": mystats = "권장"; break;
            case "學問": mystats = "학문"; break;
            case "嘴力": mystats = "구력"; break;
            case "抗毒": mystats = "항독"; break;
            case "抗麻": mystats = "항마"; break;
            case "性情": mystats = "성정"; break;
            case "處世": mystats = "처세"; break;
            case "道德": mystats = "도덕"; break;
            case "修養": mystats = "수양"; break;
            case "銀兩": mystats = "은량"; break;
            case "心理衛生": mystats = "심상"; break;

            // --- 스탯 창 및 AssetStudioGUI 목록 기반 (나무위키에 직접 언급 없거나 다른 섹션) ---
            case "門派名聲": mystats = "문파의 명성"; break;
            case "向心力": mystats = "향심"; break;
            case "命運": mystats = "운명"; break;

            case "陰陽內功": mystats = "음양"; break;
            case "心上人": mystats = "심계"; break;
            case "廚藝": mystats = "주예"; break;
            case "棋力": mystats = "기력"; break;
            case "行動次數": mystats = "행동 횟수"; break;
            case "個人貢獻度": mystats = "공헌도"; break;

            case "門派規模": mystats = "문파 규모"; break;
            case "門派資產": mystats = "문파 자산"; break;

            default: // 이 switch문의 default는 DevNote를 그대로 사용하는 것이었음
                id = $"{mystats}(미번역)";
            break;
            }
        return mystats;
        }
    }
}