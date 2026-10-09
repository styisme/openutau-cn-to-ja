// =====================================================================
// Zh2JaCore - 纯逻辑与数据表
// 从 ChineseToJapanesePhonemizer 抽出，不依赖 OpenUtau 运行时，
// 供单元测试（tests/ChineseToJapanesePhonemizer.Tests）直接调用。
//
// v2.2.2 修复：LightToneChars 移除感叹词（啊呀哦咯嘛啦哎哇），
//              修复「哦」等字被误判为轻声导致无声的问题。
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenUtau.Plugin.Builtin {

    internal static class Zh2JaCore {

        // ------------------------------------------------------------
        // 数据表
        // ------------------------------------------------------------
        // =============================================================
        // Romaji → Kana 映射表（内置）
        // =============================================================
        internal static readonly Dictionary<string, string> RomajiToKana = new() {
            { "a", "あ" }, { "i", "い" }, { "u", "う" }, { "e", "え" }, { "o", "お" },
            { "n", "ん" }, { "N", "ん" },

            { "ba", "ば" }, { "bi", "び" }, { "bu", "ぶ" }, { "be", "べ" }, { "bo", "ぼ" },
            { "bya", "びゃ" }, { "byu", "びゅ" }, { "byo", "びょ" },

            { "pa", "ぱ" }, { "pi", "ぴ" }, { "pu", "ぷ" }, { "pe", "ぺ" }, { "po", "ぽ" },
            { "pya", "ぴゃ" }, { "pyu", "ぴゅ" }, { "pyo", "ぴょ" },

            { "ma", "ま" }, { "mi", "み" }, { "mu", "む" }, { "me", "め" }, { "mo", "も" },
            { "mya", "みゃ" }, { "myu", "みゅ" }, { "myo", "みょ" },

            { "fa", "ふぁ" }, { "fi", "ふぃ" }, { "fu", "ふ" }, { "fe", "ふぇ" }, { "fo", "ふぉ" },
            { "ha", "は" }, { "hi", "ひ" }, { "hu", "ふ" }, { "he", "へ" }, { "ho", "ほ" },
            { "hya", "ひゃ" }, { "hyu", "ひゅ" }, { "hyo", "ひょ" },

            { "da", "だ" }, { "di", "でぃ" }, { "du", "どぅ" }, { "de", "で" }, { "do", "ど" },
            { "dya", "じゃ" }, { "dyu", "じゅ" }, { "dyo", "じょ" },

            { "ta", "た" }, { "ti", "てぃ" }, { "tu", "とぅ" }, { "te", "て" }, { "to", "と" },
            { "tya", "ちゃ" }, { "tyu", "ちゅ" }, { "tyo", "ちょ" },

            { "na", "な" }, { "ni", "に" }, { "nu", "ぬ" }, { "ne", "ね" }, { "no", "の" },
            { "nya", "にゃ" }, { "nyu", "にゅ" }, { "nyo", "にょ" },

            { "ra", "ら" }, { "ri", "り" }, { "ru", "る" }, { "re", "れ" }, { "ro", "ろ" },
            { "rya", "りゃ" }, { "ryu", "りゅ" }, { "ryo", "りょ" },

            { "ga", "が" }, { "gi", "ぎ" }, { "gu", "ぐ" }, { "ge", "げ" }, { "go", "ご" },
            { "gya", "ぎゃ" }, { "gyu", "ぎゅ" }, { "gyo", "ぎょ" },

            { "ka", "か" }, { "ki", "き" }, { "ku", "く" }, { "ke", "け" }, { "ko", "こ" },
            { "kya", "きゃ" }, { "kyu", "きゅ" }, { "kyo", "きょ" },

            { "za", "ざ" }, { "zi", "じ" }, { "zu", "ず" }, { "ze", "ぜ" }, { "zo", "ぞ" },
            { "ja", "じゃ" }, { "ji", "じ" }, { "ju", "じゅ" }, { "je", "じぇ" }, { "jo", "じょ" },

            { "sa", "さ" }, { "si", "す" }, { "su", "す" }, { "se", "せ" }, { "so", "そ" },
            { "sha", "しゃ" }, { "shi", "し" }, { "shu", "しゅ" }, { "she", "しぇ" }, { "sho", "しょ" },

            { "tsa", "つぁ" }, { "tsi", "つぃ" }, { "tsu", "つ" }, { "tse", "つぇ" }, { "tso", "つぉ" },
            { "cha", "ちゃ" }, { "chi", "ち" }, { "chu", "ちゅ" }, { "che", "ちぇ" }, { "cho", "ちょ" },

            { "ya", "や" }, { "yu", "ゆ" }, { "ye", "いぇ" }, { "yo", "よ" },

            { "wa", "わ" }, { "wi", "うぃ" }, { "wu", "う" }, { "we", "うぇ" }, { "wo", "うぉ" },

            { "va", "ヴぁ" }, { "vi", "ヴぃ" }, { "vu", "ヴ" }, { "ve", "ヴぇ" }, { "vo", "ヴぉ" },
        };

        internal static readonly string[] Initials = {
            "zh", "ch", "sh",
            "b", "p", "m", "f", "d", "t", "n", "l",
            "g", "k", "h", "j", "q", "x", "r", "z", "c", "s",
            "y", "w"
        };

        internal static readonly string[] Finals = {
            "iang", "iong", "uang", "ueng",
            "ang", "eng", "ing", "ong", "ian", "iao", "uan", "uai", "van",
            "ai", "ei", "ao", "ou", "an", "en", "in", "un", "er",
            "ia", "ie", "iu", "ua", "uo", "ui", "ue", "ve", "vn",
            "a", "o", "e", "i", "u", "v"
        };

        internal static readonly Dictionary<string, string> InitialMap = new() {
            { "b", "b" }, { "p", "p" }, { "m", "m" }, { "f", "h" },
            { "d", "d" }, { "t", "t" }, { "n", "n" }, { "l", "r" },
            { "g", "g" }, { "k", "k" }, { "h", "h" },
            { "j", "j" }, { "q", "ch" }, { "x", "sh" },
            { "zh", "j" }, { "ch", "ch" }, { "sh", "sh" }, { "r", "r" },
            { "z", "z" }, { "c", "ts" }, { "s", "s" },
            { "y", "y" }, { "w", "w" }
        };

        // 【v2.2.2 修复】仅保留真正的语法助词（无声调、应轻声化）。
        // 感叹词「啊呀哦咯嘛啦哎哇」有实际声调，不应被当作轻声处理，
        // 否则会出现「哦」等字无声的 bug。
        internal static readonly HashSet<string> LightToneChars = new() {
            "的", "了", "着", "呢", "吧", "吗",
        };

        internal static readonly Dictionary<string, string[]> DefaultFullPinyinMap = new() {
            // 零声母
            { "a",   new[]{"a"} },        { "ai",  new[]{"a", "i"} },
            { "an",  new[]{"a", "n"} },   { "ang", new[]{"a", "n"} },
            { "ao",  new[]{"a", "o"} },   { "o",   new[]{"o"} },
            { "ou",  new[]{"o", "o"} },   { "e",   new[]{"e"} },
            { "en",  new[]{"e", "n"} },   { "eng", new[]{"e", "n"} },
            { "er",  new[]{"a"} },        { "i",   new[]{"i"} },
            { "u",   new[]{"u"} },        { "v",   new[]{"yu"} },

            // b
            { "ba",   new[]{"ba"} },          { "bo",   new[]{"bo"} },
            { "bai",  new[]{"ba", "i"} },     { "bei",  new[]{"be", "i"} },
            { "bao",  new[]{"ba", "o"} },     { "ban",  new[]{"ba", "n"} },
            { "ben",  new[]{"be", "n"} },     { "bang", new[]{"ba", "n"} },
            { "beng", new[]{"be", "n"} },     { "bi",   new[]{"bi"} },
            { "bie",  new[]{"bi", "e"} },     { "biao", new[]{"bya", "o"} },
            { "bian", new[]{"bya", "n"} },    { "bin",  new[]{"bi", "n"} },
            { "bing", new[]{"bi", "n"} },     { "bu",   new[]{"bu"} },

            // p
            { "pa",   new[]{"pa"} },          { "po",   new[]{"po"} },
            { "pai",  new[]{"pa", "i"} },     { "pei",  new[]{"pe", "i"} },
            { "pao",  new[]{"pa", "o"} },     { "pou",  new[]{"po", "o"} },
            { "pan",  new[]{"pa", "n"} },     { "pen",  new[]{"pe", "n"} },
            { "pang", new[]{"pa", "n"} },     { "peng", new[]{"pe", "n"} },
            { "pi",   new[]{"pi"} },          { "pie",  new[]{"pi", "e"} },
            { "piao", new[]{"pya", "o"} },    { "pian", new[]{"pya", "n"} },
            { "pin",  new[]{"pi", "n"} },     { "ping", new[]{"pi", "n"} },
            { "pu",   new[]{"pu"} },

            // m
            { "ma",   new[]{"ma"} },          { "mo",   new[]{"mo"} },
            { "me",   new[]{"me"} },          { "mai",  new[]{"ma", "i"} },
            { "mei",  new[]{"me", "i"} },     { "mao",  new[]{"ma", "o"} },
            { "mou",  new[]{"mo", "o"} },     { "man",  new[]{"ma", "n"} },
            { "men",  new[]{"me", "n"} },     { "mang", new[]{"ma", "n"} },
            { "meng", new[]{"me", "n"} },     { "mi",   new[]{"mi"} },
            { "mie",  new[]{"mi", "e"} },     { "miao", new[]{"mya", "o"} },
            { "miu",  new[]{"myu"} },         { "mian", new[]{"mya", "n"} },
            { "min",  new[]{"mi", "n"} },     { "ming", new[]{"mi", "n"} },
            { "mu",   new[]{"mu"} },

            // f
            { "fa",   new[]{"fa"} },          { "fo",   new[]{"fo"} },
            { "fei",  new[]{"fe", "i"} },     { "fou",  new[]{"fo", "o"} },
            { "fan",  new[]{"fa", "n"} },     { "fen",  new[]{"fe", "n"} },
            { "fang", new[]{"fa", "n"} },     { "feng", new[]{"fe", "n"} },
            { "fu",   new[]{"fu"} },

            // d
            { "da",   new[]{"da"} },          { "de",   new[]{"de"} },
            { "dai",  new[]{"da", "i"} },     { "dei",  new[]{"de", "i"} },
            { "dao",  new[]{"da", "o"} },     { "dou",  new[]{"do", "o"} },
            { "dan",  new[]{"da", "n"} },     { "den",  new[]{"de", "n"} },
            { "dang", new[]{"da", "n"} },     { "deng", new[]{"de", "n"} },
            { "di",   new[]{"di"} },          { "dia",  new[]{"ja"} },
            { "die",  new[]{"di", "e"} },     { "diao", new[]{"dya", "o"} },
            { "diu",  new[]{"ju"} },          { "dian", new[]{"dya", "n"} },
            { "ding", new[]{"di", "n"} },     { "dong", new[]{"do", "n"} },
            { "du",   new[]{"du"} },          { "duo",  new[]{"du", "o"} },
            { "dui",  new[]{"du", "i"} },     { "duan", new[]{"du", "a", "n"} },
            { "dun",  new[]{"du", "n"} },

            // t
            { "ta",   new[]{"ta"} },          { "te",   new[]{"te"} },
            { "tai",  new[]{"ta", "i"} },     { "tao",  new[]{"ta", "o"} },
            { "tou",  new[]{"to", "o"} },     { "tan",  new[]{"ta", "n"} },
            { "tang", new[]{"ta", "n"} },     { "teng", new[]{"te", "n"} },
            { "ti",   new[]{"ti"} },          { "tie",  new[]{"ti", "e"} },
            { "tiao", new[]{"tya", "o"} },    { "tian", new[]{"tya", "n"} },
            { "ting", new[]{"ti", "n"} },     { "tong", new[]{"to", "n"} },
            { "tu",   new[]{"tu"} },          { "tuo",  new[]{"tu", "o"} },
            { "tui",  new[]{"tu", "i"} },     { "tuan", new[]{"tu", "a", "n"} },
            { "tun",  new[]{"tu", "n"} },

            // n
            { "na",   new[]{"na"} },          { "ne",   new[]{"ne"} },
            { "nai",  new[]{"na", "i"} },     { "nei",  new[]{"ne", "i"} },
            { "nao",  new[]{"na", "o"} },     { "nou",  new[]{"no", "o"} },
            { "nan",  new[]{"na", "n"} },     { "nen",  new[]{"ne", "n"} },
            { "nang", new[]{"na", "n"} },     { "neng", new[]{"ne", "n"} },
            { "ni",   new[]{"ni"} },          { "nie",  new[]{"ni", "e"} },
            { "niao", new[]{"nya", "o"} },    { "niu",  new[]{"nyu"} },
            { "nian", new[]{"nya", "n"} },    { "nin",  new[]{"ni", "n"} },
            { "niang",new[]{"nya", "n"} },    { "ning", new[]{"ni", "n"} },
            { "nong", new[]{"no", "n"} },     { "nu",   new[]{"nu"} },
            { "nuo",  new[]{"nu", "o"} },     { "nuan", new[]{"nu", "a", "n"} },
            { "nv",   new[]{"nyu"} },         { "nve",  new[]{"nyu", "e"} },

            // l → r
            { "la",   new[]{"ra"} },          { "le",   new[]{"re"} },
            { "lai",  new[]{"ra", "i"} },     { "lei",  new[]{"re", "i"} },
            { "lao",  new[]{"ra", "o"} },     { "lou",  new[]{"ro", "o"} },
            { "lan",  new[]{"ra", "n"} },     { "lang", new[]{"ra", "n"} },
            { "leng", new[]{"re", "n"} },     { "li",   new[]{"ri"} },
            { "lia",  new[]{"rya"} },         { "lie",  new[]{"ri", "e"} },
            { "liao", new[]{"rya", "o"} },    { "liu",  new[]{"ryu"} },
            { "lian", new[]{"rya", "n"} },    { "lin",  new[]{"ri", "n"} },
            { "liang",new[]{"rya", "n"} },    { "ling", new[]{"ri", "n"} },
            { "long", new[]{"ro", "n"} },     { "lu",   new[]{"ru"} },
            { "luo",  new[]{"ru", "o"} },     { "luan", new[]{"ru", "a", "n"} },
            { "lun",  new[]{"ru", "n"} },     { "lv",   new[]{"ryu"} },
            { "lve",  new[]{"ryu", "e"} },

            // g
            { "ga",   new[]{"ga"} },          { "ge",   new[]{"ge"} },
            { "gai",  new[]{"ga", "i"} },     { "gei",  new[]{"ge", "i"} },
            { "gao",  new[]{"ga", "o"} },     { "gou",  new[]{"go", "o"} },
            { "gan",  new[]{"ga", "n"} },     { "gen",  new[]{"ge", "n"} },
            { "gang", new[]{"ga", "n"} },     { "geng", new[]{"ge", "n"} },
            { "gong", new[]{"go", "n"} },     { "gu",   new[]{"gu"} },
            { "gua",  new[]{"gu", "a"} },     { "guo",  new[]{"gu", "o"} },
            { "guai", new[]{"gu", "a", "i"} },{ "gui",  new[]{"gu", "i"} },
            { "guan", new[]{"gu", "a", "n"} },{ "guang",new[]{"gu", "a", "n"} },
            { "gun",  new[]{"gu", "n"} },

            // k
            { "ka",   new[]{"ka"} },          { "ke",   new[]{"ke"} },
            { "kai",  new[]{"ka", "i"} },     { "kei",  new[]{"ke", "i"} },
            { "kao",  new[]{"ka", "o"} },     { "kou",  new[]{"ko", "o"} },
            { "kan",  new[]{"ka", "n"} },     { "ken",  new[]{"ke", "n"} },
            { "kang", new[]{"ka", "n"} },     { "keng", new[]{"ke", "n"} },
            { "kong", new[]{"ko", "n"} },     { "ku",   new[]{"ku"} },
            { "kua",  new[]{"ku", "a"} },     { "kuo",  new[]{"ku", "o"} },
            { "kuai", new[]{"ku", "a", "i"} },{ "kui",  new[]{"ku", "i"} },
            { "kuan", new[]{"ku", "a", "n"} },{ "kuang",new[]{"ku", "a", "n"} },
            { "kun",  new[]{"ku", "n"} },

            // h
            { "ha",   new[]{"ha"} },          { "he",   new[]{"he"} },
            { "hai",  new[]{"ha", "i"} },     { "hei",  new[]{"he", "i"} },
            { "hao",  new[]{"ha", "o"} },     { "hou",  new[]{"ho", "o"} },
            { "han",  new[]{"ha", "n"} },     { "hen",  new[]{"he", "n"} },
            { "hang", new[]{"ha", "n"} },     { "heng", new[]{"he", "n"} },
            { "hong", new[]{"ho", "n"} },     { "hu",   new[]{"fu"} },
            { "hua",  new[]{"fa"} },          { "huo",  new[]{"fu", "o"} },
            { "huai", new[]{"fa", "i"} },     { "hui",  new[]{"fu", "i"} },
            { "huan", new[]{"fa", "n"} },     { "huang",new[]{"fa", "n"} },
            { "hun",  new[]{"fu", "n"} },

            // j
            { "ji",   new[]{"ji"} },          { "jia",  new[]{"ja"} },
            { "jie",  new[]{"je"} },          { "jiao", new[]{"ja", "o"} },
            { "jiu",  new[]{"ju"} },          { "jian", new[]{"je", "n"} },
            { "jin",  new[]{"ji", "n"} },     { "jiang",new[]{"ja", "n"} },
            { "jing", new[]{"ji", "n"} },     { "jiong",new[]{"jo", "n"} },
            { "ju",   new[]{"ju"} },          { "jue",  new[]{"ju", "e"} },
            { "juan", new[]{"ju", "e", "n"} },{ "jun",  new[]{"ju", "n"} },

            // q
            { "qi",   new[]{"chi"} },         { "qia",  new[]{"cha"} },
            { "qie",  new[]{"che"} },         { "qiao", new[]{"cha", "o"} },
            { "qiu",  new[]{"chu"} },         { "qian", new[]{"che", "n"} },
            { "qin",  new[]{"chi", "n"} },    { "qiang",new[]{"cha", "n"} },
            { "qing", new[]{"chi", "n"} },    { "qiong",new[]{"cho", "n"} },
            { "qu",   new[]{"chu"} },         { "que",  new[]{"chu", "e"} },
            { "quan", new[]{"chu", "e", "n"} },{ "qun", new[]{"chu", "n"} },

            // x
            { "xi",   new[]{"shi"} },         { "xia",  new[]{"sha"} },
            { "xie",  new[]{"she"} },         { "xiao", new[]{"sha", "o"} },
            { "xiu",  new[]{"shu"} },         { "xian", new[]{"she", "n"} },
            { "xin",  new[]{"shi", "n"} },    { "xiang",new[]{"sha", "n"} },
            { "xing", new[]{"shi", "n"} },    { "xiong",new[]{"sho", "n"} },
            { "xu",   new[]{"shu"} },         { "xue",  new[]{"shu", "e"} },
            { "xuan", new[]{"shu", "e", "n"} },{ "xun", new[]{"shu", "n"} },

            // zh
            { "zha",  new[]{"ja"} },          { "zhe",  new[]{"je"} },
            { "zhi",  new[]{"ji"} },          { "zhai", new[]{"ja", "i"} },
            { "zhao", new[]{"ja", "o"} },     { "zhou", new[]{"jo", "o"} },
            { "zhan", new[]{"ja", "n"} },     { "zhen", new[]{"je", "n"} },
            { "zhang",new[]{"ja", "n"} },     { "zheng",new[]{"je", "n"} },
            { "zhong",new[]{"jo", "n"} },     { "zhu",  new[]{"ju"} },
            { "zhua", new[]{"ju", "a"} },     { "zhuo", new[]{"ju", "o"} },
            { "zhuai",new[]{"ju", "a", "i"} },{ "zhui", new[]{"ju", "i"} },
            { "zhuan",new[]{"ju", "a", "n"} },{ "zhuang",new[]{"ju", "a", "n"} },
            { "zhun", new[]{"ju", "n"} },

            // ch
            { "cha",  new[]{"cha"} },         { "che",  new[]{"che"} },
            { "chi",  new[]{"chi"} },         { "chai", new[]{"cha", "i"} },
            { "chao", new[]{"cha", "o"} },    { "chou", new[]{"cho", "o"} },
            { "chan", new[]{"cha", "n"} },    { "chen", new[]{"che", "n"} },
            { "chang",new[]{"cha", "n"} },    { "cheng",new[]{"che", "n"} },
            { "chong",new[]{"cho", "n"} },    { "chu",  new[]{"chu"} },
            { "chua", new[]{"chu", "a"} },    { "chuo", new[]{"chu", "o"} },
            { "chuai",new[]{"chu", "a", "i"} },{ "chui", new[]{"chu", "i"} },
            { "chuan",new[]{"chu", "a", "n"} },{ "chuang",new[]{"chu", "a", "n"} },
            { "chun", new[]{"chu", "n"} },

            // sh
            { "sha",  new[]{"sha"} },         { "she",  new[]{"she"} },
            { "shi",  new[]{"shi"} },         { "shai", new[]{"sha", "i"} },
            { "shao", new[]{"sha", "o"} },    { "shou", new[]{"sho", "o"} },
            { "shan", new[]{"sha", "n"} },    { "shen", new[]{"she", "n"} },
            { "shang",new[]{"sha", "n"} },    { "sheng",new[]{"she", "n"} },
            { "shu",  new[]{"shu"} },         { "shua", new[]{"shu", "a"} },
            { "shuo", new[]{"shu", "o"} },    { "shuai",new[]{"shu", "a", "i"} },
            { "shui", new[]{"shu", "i"} },    { "shuan",new[]{"shu", "a", "n"} },
            { "shuang",new[]{"shu", "a", "n"} },{ "shun",new[]{"shu", "n"} },

            // r
            { "re",   new[]{"re"} },          { "ri",   new[]{"ri"} },
            { "rao",  new[]{"ra", "o"} },     { "rou",  new[]{"ro", "o"} },
            { "ran",  new[]{"ra", "n"} },     { "ren",  new[]{"re", "n"} },
            { "rang", new[]{"ra", "n"} },     { "reng", new[]{"re", "n"} },
            { "rong", new[]{"ro", "n"} },     { "ru",   new[]{"ru"} },
            { "ruo",  new[]{"ru", "o"} },     { "rui",  new[]{"ru", "i"} },
            { "ruan", new[]{"ru", "a", "n"} },{ "run",  new[]{"ru", "n"} },

            // z
            { "za",   new[]{"za"} },          { "ze",   new[]{"ze"} },
            { "zi",   new[]{"ji"} },          { "zai",  new[]{"za", "i"} },
            { "zao",  new[]{"za", "o"} },     { "zou",  new[]{"zo", "o"} },
            { "zan",  new[]{"za", "n"} },     { "zen",  new[]{"ze", "n"} },
            { "zang", new[]{"za", "n"} },     { "zeng", new[]{"ze", "n"} },
            { "zong", new[]{"zo", "n"} },     { "zu",   new[]{"zu"} },
            { "zuo",  new[]{"zu", "o"} },     { "zui",  new[]{"zu", "i"} },
            { "zuan", new[]{"zu", "a", "n"} },{ "zun",  new[]{"zu", "n"} },

            // c
            { "ca",   new[]{"tsa"} },         { "ce",   new[]{"tse"} },
            { "ci",   new[]{"tsu"} },         { "cai",  new[]{"tsa", "i"} },
            { "cao",  new[]{"tsa", "o"} },    { "cou",  new[]{"tso", "o"} },
            { "can",  new[]{"tsa", "n"} },    { "cen",  new[]{"tse", "n"} },
            { "cang", new[]{"tsa", "n"} },    { "ceng", new[]{"tse", "n"} },
            { "cong", new[]{"tso", "n"} },    { "cu",   new[]{"tsu"} },
            { "cuo",  new[]{"tsu", "o"} },    { "cui",  new[]{"tsu", "i"} },
            { "cuan", new[]{"tsu", "a", "n"} },{ "cun", new[]{"tsu", "n"} },

            // s
            { "sa",   new[]{"sa"} },          { "se",   new[]{"se"} },
            { "si",   new[]{"su"} },          { "sai",  new[]{"sa", "i"} },
            { "sao",  new[]{"sa", "o"} },     { "sou",  new[]{"so", "o"} },
            { "san",  new[]{"sa", "n"} },     { "sen",  new[]{"se", "n"} },
            { "sang", new[]{"sa", "n"} },     { "seng", new[]{"se", "n"} },
            { "song", new[]{"so", "n"} },     { "su",   new[]{"su"} },
            { "suo",  new[]{"su", "o"} },     { "sui",  new[]{"su", "i"} },
            { "suan", new[]{"su", "a", "n"} },{ "sun",  new[]{"su", "n"} },

            // y
            { "yi",   new[]{"i"} },           { "ya",   new[]{"ya"} },
            { "ye",   new[]{"ye"} },          { "yao",  new[]{"ya", "o"} },
            { "you",  new[]{"yo", "o"} },     { "yan",  new[]{"ya", "n"} },
            { "yin",  new[]{"i", "n"} },      { "yang", new[]{"ya", "n"} },
            { "ying", new[]{"i", "n"} },      { "yong", new[]{"yo", "n"} },
            { "yu",   new[]{"yu"} },          { "yue",  new[]{"yu", "e"} },
            { "yuan", new[]{"e", "n"} },      { "yun",  new[]{"yu", "n"} },

            // w
            { "wu",   new[]{"u"} },           { "wa",   new[]{"wa"} },
            { "wo",   new[]{"wo"} },          { "wai",  new[]{"wa", "i"} },
            { "wei",  new[]{"we", "i"} },     { "wan",  new[]{"wa", "n"} },
            { "wen",  new[]{"we", "n"} },     { "wang", new[]{"wa", "n"} },
            { "weng", new[]{"we", "n"} },
        };

        internal static readonly Dictionary<string, string> DefaultPolyphoneMap = new() {
            { "chang da", "zhang da" }, { "chang jia", "zhang jia" },
            { "chang bei", "zhang bei" }, { "chang zi", "zhang zi" },
            { "chang guan", "zhang guan" }, { "sheng chang", "sheng zhang" },
            { "le qu", "yue qu" }, { "le dui", "yue dui" },
            { "le qi", "yue qi" }, { "le pu", "yue pu" },
            { "le zhang", "yue zhang" }, { "yin le", "yin yue" },
            { "le jie", "liao jie" }, { "da fu", "dai fu" },
            { "zhong xin", "chong xin" }, { "zhong fu", "chong fu" },
            { "zhong qing", "chong qing" }, { "huan shi", "hai shi" },
            { "zhe ji", "zhao ji" }, { "gan huo", "gan huo" },
        };

        internal static readonly HashSet<char> Punctuation = new() {
            '，', '。', '！', '？', '、', '；', '：', '“', '”', '‘', '’',
            '（', '）', '《', '》', '【', '】', '…', '—', '～', '·',
            ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')',
            '[', ']', '{', '}', '<', '>', '/', '\\', '|', '`',
        };

        internal static readonly HashSet<char> LongMarks = new() { '~', '～', '—', 'ー' };

        internal static readonly Dictionary<char, (char baseChar, int tone)> ToneMarks = new() {
            { 'ā', ('a', 1) }, { 'á', ('a', 2) }, { 'ǎ', ('a', 3) }, { 'à', ('a', 4) },
            { 'ē', ('e', 1) }, { 'é', ('e', 2) }, { 'ě', ('e', 3) }, { 'è', ('e', 4) },
            { 'ī', ('i', 1) }, { 'í', ('i', 2) }, { 'ǐ', ('i', 3) }, { 'ì', ('i', 4) },
            { 'ō', ('o', 1) }, { 'ó', ('o', 2) }, { 'ǒ', ('o', 3) }, { 'ò', ('o', 4) },
            { 'ū', ('u', 1) }, { 'ú', ('u', 2) }, { 'ǔ', ('u', 3) }, { 'ù', ('u', 4) },
            { 'ü', ('v', 0) }, { 'Ü', ('v', 0) },
            { 'ǖ', ('v', 1) }, { 'ǘ', ('v', 2) }, { 'ǚ', ('v', 3) }, { 'ǜ', ('v', 4) },
            { 'ê', ('e', 0) }, { 'Ê', ('e', 0) },
            { 'ế', ('e', 2) }, { 'ề', ('e', 4) }, { 'ể', ('e', 3) },
            { 'ễ', ('e', 3) }, { 'ệ', ('e', 4) },
        };

        internal static readonly string[] DigitPinyin = {
            "ling", "yi", "er", "san", "si", "wu", "liu", "qi", "ba", "jiu"
        };


        // ------------------------------------------------------------
        // 文本处理（标点 / 数字 / 声调符号 / 长音）
        // ------------------------------------------------------------
        internal static (string, int) ParseToneMarks(string s) {
            var sb = new StringBuilder(s.Length);
            int tone = 0;
            foreach (char c in s) {
                if (ToneMarks.TryGetValue(c, out var m)) {
                    sb.Append(m.baseChar);
                    tone = m.tone;
                } else {
                    sb.Append(c);
                }
            }
            string result = sb.ToString();

            if (result.Length >= 2) {
                char last = result[^1];
                char prev = result[^2];
                if (char.IsDigit(last) && char.IsLetter(prev)) {
                    int t = last - '0';
                    if (t >= 1 && t <= 5) {
                        tone = t;
                        result = result[..^1];
                    }
                }
            }
            return (result, tone);
        }

        internal static string ReplacePunctuation(string s) {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) {
                if (Punctuation.Contains(c)) sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        internal static string ReplaceDigits(string s) {
            if (!s.Any(char.IsDigit)) return s;
            var sb = new StringBuilder();
            var digits = new List<int>();
            foreach (char c in s) {
                if (char.IsDigit(c)) digits.Add(c - '0');
                else {
                    if (digits.Count > 0) {
                        sb.Append(ConvertDigitSequence(digits));
                        sb.Append(' ');
                        digits.Clear();
                    }
                    sb.Append(c);
                }
            }
            if (digits.Count > 0) sb.Append(ConvertDigitSequence(digits));
            return sb.ToString();
        }

        internal static string ConvertDigitSequence(List<int> digits) {
            if (digits.Count == 1) return DigitPinyin[digits[0]];
            long num = 0;
            foreach (var d in digits) num = num * 10 + d;
            if (num == 10) return "shi";
            if (num < 20) return "shi " + DigitPinyin[num - 10];
            if (num < 100) {
                int t = (int)(num / 10);
                int o = (int)(num % 10);
                return o == 0
                    ? DigitPinyin[t] + " shi"
                    : DigitPinyin[t] + " shi " + DigitPinyin[o];
            }
            return string.Join(" ", digits.Select(d => DigitPinyin[d]));
        }

        internal static string StripLongMarks(string s) {
            return new string(s.Where(c => !LongMarks.Contains(c)).ToArray()).Trim();
        }

        internal static string StripDigits(string s) =>
            new string(s.Where(c => !char.IsDigit(c)).ToArray());

        internal static bool ContainsJapaneseKana(string s) {
            foreach (char c in s) {
                if ((c >= 0x3040 && c <= 0x309F) ||
                    (c >= 0x30A0 && c <= 0x30FF) ||
                    (c >= 0xFF66 && c <= 0xFF9D))
                    return true;
            }
            return false;
        }


        // ------------------------------------------------------------
        // 拼音解析
        // ------------------------------------------------------------
        internal static string[] ParsePinyin(string pinyin) {
            pinyin = new string(pinyin.Where(c => c >= 'a' && c <= 'z').ToArray()).ToLowerInvariant();
            if (string.IsNullOrEmpty(pinyin)) return null;

            if (pinyin == "n" || pinyin == "ng") return new[] { "N" };
            if (pinyin == "zh") pinyin = "zhi";
            else if (pinyin == "ch") pinyin = "chi";
            else if (pinyin == "sh") pinyin = "shi";

            if (pinyin.StartsWith("y")) {
                string rest = pinyin.Substring(1);
                if (rest == "i" || rest == "in" || rest == "ing") return new[] { rest };
                if (rest.StartsWith("u")) {
                    string after = "v" + rest.Substring(1);
                    if (Finals.Contains(after)) return new[] { after };
                    return null;
                }
                if (Finals.Contains(rest)) return new[] { "y", rest };
                return null;
            }

            if (pinyin.StartsWith("w")) {
                string rest = pinyin.Substring(1);
                if (rest == "u") return new[] { "u" };
                if (Finals.Contains(rest)) return new[] { "w", rest };
                return null;
            }

            if (Finals.Contains(pinyin)) return new[] { pinyin };

            foreach (var init in Initials) {
                if (pinyin.StartsWith(init)) {
                    string remaining = pinyin.Substring(init.Length);
                    if (Finals.Contains(remaining))
                        return new[] { init, remaining };
                }
            }
            return null;
        }


        // ------------------------------------------------------------
        // 假名转换
        // ------------------------------------------------------------
        internal static List<string> GenerateMoras(string[] cc, string final, Dictionary<string, string[]> fullPinyinMap) {
            var moras = new List<string>();

            string rawInitial = (cc != null && cc.Length > 0) ? cc[0] : "";
            string fullPinyin = rawInitial + final;

            if (fullPinyinMap.TryGetValue(fullPinyin, out var mapped)) {
                foreach (var m in mapped) {
                    moras.Add(ToKana(m));
                }
                return moras;
            }

            string cons = "";
            if (cc != null && cc.Length > 0)
                cons = InitialMap.TryGetValue(cc[0], out var jc) ? jc : cc[0];

            var vowelSeq = GetJapaneseVowelSequence(final);
            if (vowelSeq.Count == 0) return moras;

            if (!string.IsNullOrEmpty(cons)) {
                string firstV = vowelSeq[0];
                vowelSeq.RemoveAt(0);

                if (firstV == "N") {
                    moras.Add(ToKana(cons + "u"));
                    moras.Add("ん");
                } else {
                    moras.Add(ToKana(cons + firstV));
                }
            }

            foreach (var v in vowelSeq) {
                if (v == "N") moras.Add("ん");
                else moras.Add(ToKana(v));
            }

            return moras;
        }

        internal static List<string> GetJapaneseVowelSequence(string pinyinFinal) {
            switch (pinyinFinal) {
                case "a": return new List<string> { "a" };
                case "o": return new List<string> { "o" };
                case "e": return new List<string> { "e" };
                case "i": return new List<string> { "i" };
                case "u": return new List<string> { "u" };
                case "v": return new List<string> { "yu" };
                case "er": return new List<string> { "a" };

                case "ai": return new List<string> { "a", "i" };
                case "ei": return new List<string> { "e", "i" };
                case "ao": return new List<string> { "a", "o" };
                case "ou": return new List<string> { "o", "o" };

                case "an": return new List<string> { "a", "N" };
                case "en": return new List<string> { "e", "N" };
                case "in": return new List<string> { "i", "N" };
                case "un": return new List<string> { "u", "N" };
                case "ang": return new List<string> { "a", "N" };
                case "eng": return new List<string> { "e", "N" };
                case "ing": return new List<string> { "i", "N" };
                case "ong": return new List<string> { "o", "N" };
                case "ian": return new List<string> { "i", "e", "N" };
                case "iang": return new List<string> { "i", "a", "N" };
                case "iong": return new List<string> { "i", "o", "N" };
                case "uan": return new List<string> { "u", "a", "N" };
                case "uang": return new List<string> { "u", "a", "N" };
                case "ueng": return new List<string> { "u", "e", "N" };
                case "van": return new List<string> { "yu", "e", "N" };
                case "vn": return new List<string> { "yu", "N" };

                case "ia": return new List<string> { "i", "a" };
                case "ie": return new List<string> { "i", "e" };
                case "iu": return new List<string> { "i", "u" };
                case "iao": return new List<string> { "i", "a", "o" };

                case "ua": return new List<string> { "u", "a" };
                case "uo": return new List<string> { "u", "o" };
                case "ui": return new List<string> { "u", "i" };
                case "uai": return new List<string> { "u", "a", "i" };

                case "ue": return new List<string> { "yu", "e" };
                case "ve": return new List<string> { "yu", "e" };

                default: return new List<string> { pinyinFinal };
            }
        }

        internal static string ToKana(string romaji) {
            if (string.IsNullOrEmpty(romaji)) return romaji;
            if (RomajiToKana.TryGetValue(romaji, out var kana)) return kana;
            return romaji;
        }

        internal static string ConvertToJapaneseVowel(string pinyinFinal) {
            if (string.IsNullOrEmpty(pinyinFinal)) return "-";
            if (pinyinFinal == "-" || pinyinFinal == "R") return pinyinFinal;

            var seq = GetJapaneseVowelSequence(pinyinFinal);
            if (seq.Count > 0) {
                var last = seq.Last();
                if (last == "N") return "n";
                return last.Last().ToString();
            }
            return pinyinFinal.Last().ToString();
        }

        internal static string GetLastJapaneseVowel(string mora) {
            if (string.IsNullOrEmpty(mora)) return "-";
            if (mora.EndsWith("ん")) return "n";
            if (mora.EndsWith("ゃ")) return "a";
            if (mora.EndsWith("ゅ")) return "u";
            if (mora.EndsWith("ょ")) return "o";

            char last = mora.Last();
            if ("あかさたなはまやらわがざだばぱ".Contains(last)) return "a";
            if ("いきしちにひみりぎじぢびぴ".Contains(last)) return "i";
            if ("うくすつぬふむゆるぐずづぶぷ".Contains(last)) return "u";
            if ("えけせてねへめれげぜでべぺ".Contains(last)) return "e";
            if ("おこそとのほもよろごぞどぼぽ".Contains(last)) return "o";
            return "-";
        }

    }
}