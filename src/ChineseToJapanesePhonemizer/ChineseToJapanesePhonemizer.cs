// =====================================================================
// ChineseToJapanesePhonemizer v2.3.0
// 让日语音源唱中文歌词的 OpenUtau 音素器
//
// 版本历史：
//   v1.0  初始版本
//   v1.1  加入完整拼音优先 + 多音节输入
//   v1.2  加入标点/数字/儿化/YAML/别名缓存
//   v1.3  加入一不变调/轻声/叠字/声调时长/别名覆盖/拼音简写/C4 UI
//   v2.0  修复拼音轻声误判；精简多音字表；支持显式轻声 zhe5
//   v2.1  重构 FullPinyinMap 为音节级数组
//   v2.1.1 内置 RomajiToKana；修复拗音拆分
//   v2.1.2 修复 yu 键重复；dya/dyu/dyo 改 じゃ 系；新增 disable_vcv
//   v2.1.3 ou 韵母 o+u → o+o
//   v2.1.4 尝试 position 后移压缩鼻音（效果有限）
//   v2.1.5 nasal_mode 三档（none/short/full），默认 none
//   v2.1.6 修复 nasal_mode=none 不生效的 bug；short 模式改用 KOtoJA
//          风格的「末尾固定毫秒」逻辑（默认 120ms / 有下音符 180ms）；
//          默认改为 short
//   v2.1.7 加入通配符 * 过渡音素支持（Defoko 等 CV 音源），
//          可通过 use_wildcard 开关控制
//   v2.2.0 项目结构重组；引入测试/CI/SemVer；修复 yuan 无声 bug
//   v2.2.1 修复安装向导版本号未同步问题
//   v2.2.2 修复 LightToneChars 误把感叹词当轻声，导致「哦」等字无声
//   v2.3.0 数字读法扩展：日期/时间/小数/百分比/千位逗号
//
// 依赖：OpenUtau v0.1.570+ / .NET 10 / YamlDotNet / Serilog
// =====================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;
using YamlDotNet.Serialization;

namespace OpenUtau.Plugin.Builtin {

    // =================================================================
    // YAML 配置结构
    // =================================================================
    public class Zh2JaConfig {
        [YamlMember(Alias = "full_pinyin")]
        public Dictionary<string, List<string>> FullPinyin { get; set; } = new();

        [YamlMember(Alias = "polyphones")]
        public Dictionary<string, string> Polyphones { get; set; } = new();

        [YamlMember(Alias = "alias_overrides")]
        public Dictionary<string, List<string>> AliasOverrides { get; set; } = new();

        [YamlMember(Alias = "ask_polyphone")]
        public bool AskPolyphone { get; set; } = false;

        [YamlMember(Alias = "polyphone_decisions")]
        public Dictionary<string, bool> PolyphoneDecisions { get; set; } = new();

        [YamlMember(Alias = "disable_tone3")]
        public bool DisableTone3 { get; set; } = false;

        [YamlMember(Alias = "disable_light_tone")]
        public bool DisableLightTone { get; set; } = false;

        [YamlMember(Alias = "disable_tone_timing")]
        public bool DisableToneTiming { get; set; } = false;

        [YamlMember(Alias = "disable_double_char")]
        public bool DisableDoubleChar { get; set; } = false;

        [YamlMember(Alias = "disable_vcv")]
        public bool DisableVcv { get; set; } = false;

        // 鼻音韵尾处理模式：
        //   none  → 完全省略「ん」
        //   short → 「ん」只占音符末尾 nasal_ms 毫秒（默认，推荐）
        //   full  → 保留完整采样
        [YamlMember(Alias = "nasal_mode")]
        public string NasalMode { get; set; } = "short";

        // short 模式下「ん」占音符末尾的毫秒数（有下一个音符时会自动 ×1.5）
        [YamlMember(Alias = "nasal_ms")]
        public int NasalMs { get; set; } = 120;

        // 是否启用通配符 * 过渡音素（对 Defoko 等 CV 音源有效）
        [YamlMember(Alias = "use_wildcard")]
        public bool UseWildcard { get; set; } = true;
    }

    // =================================================================
    // 音素器主类
    // =================================================================
    [Phonemizer("Chinese to Japanese Phonemizer", "ZH to JA", "Deepseek", language: "ZH")]
    public class ChineseToJapanesePhonemizer : SyllableBasedPhonemizer {

        private static class UiCompat {
            private static int _state = -1;
            private static readonly object _lock = new();

            public static bool Available {
                get {
                    if (_state >= 0) return _state == 1;
                    lock (_lock) {
                        if (_state >= 0) return _state == 1;
                        _state = Check() ? 1 : 0;
                        return _state == 1;
                    }
                }
            }

            private static bool Check() {
                try {
                    if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
                    IntPtr h = LoadLibraryW("user32.dll");
                    if (h == IntPtr.Zero) return false;
                    FreeLibrary(h);
                    if (GetProcessWindowStation() == IntPtr.Zero) return false;
                    return true;
                } catch { return false; }
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr LoadLibraryW(string name);
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool FreeLibrary(IntPtr h);
            [DllImport("user32.dll")]
            private static extern IntPtr GetProcessWindowStation();
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private readonly Dictionary<string, string[]> fullPinyinMap = new();
        private readonly Dictionary<string, string> polyphoneMap = new();
        private readonly Dictionary<string, List<string>> aliasOverrides = new();
        private readonly Dictionary<(string, int), bool> otoCache = new();
        private readonly Dictionary<int, int> noteTones = new();
        private readonly Dictionary<int, bool> lightToneFlags = new();
        private readonly Dictionary<string, bool> polyphoneDecisions = new();

        private bool askPolyphone = false;
        private bool disableTone3 = false;
        private bool disableLightTone = false;
        private bool disableToneTiming = false;
        private bool disableDoubleChar = false;
        private bool disableVcv = false;
        private string nasalMode = "short";
        private int nasalMs = 120;
        private bool useWildcard = true;

        private USinger currentSinger;

        public ChineseToJapanesePhonemizer() {
            this.vowels = Zh2JaCore.Finals;
            this.consonants = Zh2JaCore.Initials;
            foreach (var kv in Zh2JaCore.DefaultFullPinyinMap) fullPinyinMap[kv.Key] = kv.Value;
            foreach (var kv in Zh2JaCore.DefaultPolyphoneMap) polyphoneMap[kv.Key] = kv.Value;
        }

        protected override string[] GetVowels() => Zh2JaCore.Finals;
        protected override string[] GetConsonants() => Zh2JaCore.Initials;
        protected override string GetDictionaryName() => null;

        public override void SetSinger(USinger singer) {
            base.SetSinger(singer);
            this.currentSinger = singer;
            if (singer == null || !singer.Loaded) return;

            Log.Information($"[ZH to JA v2.3.0] UI compatibility: {(UiCompat.Available ? "available" : "NOT available")}");

            TryLoadConfig(Path.Combine(PluginDir, "zh2ja.yaml"));
            TryLoadConfig(Path.Combine(singer.Location, "zh2ja.yaml"));
            otoCache.Clear();
        }

        private void TryLoadConfig(string path) {
            if (!File.Exists(path)) return;
            try {
                var text = File.ReadAllText(path);
                var cfg = Core.Yaml.DefaultDeserializer.Deserialize<Zh2JaConfig>(text);
                if (cfg == null) return;

                if (cfg.FullPinyin != null) {
                    foreach (var kv in cfg.FullPinyin) {
                        if (kv.Value != null) fullPinyinMap[kv.Key] = kv.Value.ToArray();
                    }
                }
                if (cfg.Polyphones != null) {
                    foreach (var kv in cfg.Polyphones) polyphoneMap[kv.Key] = kv.Value;
                }
                if (cfg.AliasOverrides != null) {
                    foreach (var kv in cfg.AliasOverrides) {
                        if (kv.Value != null) aliasOverrides[kv.Key] = kv.Value;
                    }
                }
                if (cfg.PolyphoneDecisions != null) {
                    foreach (var kv in cfg.PolyphoneDecisions) polyphoneDecisions[kv.Key] = kv.Value;
                }

                askPolyphone = cfg.AskPolyphone;
                disableTone3 = cfg.DisableTone3;
                disableLightTone = cfg.DisableLightTone;
                disableToneTiming = cfg.DisableToneTiming;
                disableDoubleChar = cfg.DisableDoubleChar;
                disableVcv = cfg.DisableVcv;
                nasalMode = cfg.NasalMode ?? "short";
                nasalMs = cfg.NasalMs;
                if (nasalMs < 30) nasalMs = 30;
                if (nasalMs > 500) nasalMs = 500;
                useWildcard = cfg.UseWildcard;

                Log.Information($"[ZH to JA v2.3.0] Loaded config from {path} (nasal_mode={nasalMode}, nasal_ms={nasalMs}, use_wildcard={useWildcard})");
            } catch (Exception e) {
                Log.Error(e, $"Failed to load zh2ja config: {path}");
            }
        }

        public override void SetUp(Note[][] groups, UProject project, UTrack track) {
            noteTones.Clear();
            lightToneFlags.Clear();

            PreprocessNotes(groups);
            BaseChinesePhonemizer.RomanizeNotes(groups);
            FixPolyphones(groups);
            base.SetUp(groups, project, track);
        }

        private void PreprocessNotes(Note[][] groups) {
            foreach (var group in groups) {
                if (group == null || group.Length == 0) continue;
                var note = group[0];
                string lyric = note.lyric;
                if (string.IsNullOrEmpty(lyric)) continue;

                string trimmed = lyric.Trim();
                if (trimmed == "-" || trimmed == "R" || trimmed == "+") continue;

                lyric = Zh2JaCore.ReplacePunctuation(lyric);

                var (baseLyric, tone) = Zh2JaCore.ParseToneMarks(lyric);
                lyric = baseLyric;

                bool isExplicitLight = (tone == 5);
                bool hasExplicitTone = (tone >= 1 && tone <= 4);
                bool isLightChar = Zh2JaCore.LightToneChars.Contains(lyric);

                if (!disableLightTone && !hasExplicitTone && (isExplicitLight || isLightChar)) {
                    lightToneFlags[note.position] = true;
                }

                if (tone >= 1 && tone <= 4) {
                    noteTones[note.position] = tone;
                }

                lyric = Zh2JaCore.ReplaceDigits(lyric);
                lyric = Zh2JaCore.StripLongMarks(lyric);

                group[0] = new Note {
                    lyric = lyric,
                    phoneticHint = note.phoneticHint,
                    tone = note.tone,
                    position = note.position,
                    duration = note.duration,
                    phonemeAttributes = note.phonemeAttributes,
                };
            }
        }

        private void FixPolyphones(Note[][] groups) {
            var lyrics = new List<string>();
            var refs = new List<(int gi, int ni)>();

            for (int i = 0; i < groups.Length; i++) {
                if (groups[i].Length == 0) continue;
                var note = groups[i][0];
                if (!string.IsNullOrEmpty(note.phoneticHint)) lyrics.Add("__skip__");
                else lyrics.Add(note.lyric.ToLowerInvariant());
                refs.Add((i, 0));
            }

            for (int i = 0; i < lyrics.Count; i++) {
                if (lyrics[i] == "__skip__") continue;

                for (int len = Math.Min(4, lyrics.Count - i); len >= 2; len--) {
                    bool hasSkip = false;
                    for (int j = 0; j < len; j++) {
                        if (lyrics[i + j] == "__skip__") { hasSkip = true; break; }
                    }
                    if (hasSkip) continue;

                    string pattern = string.Join(" ", lyrics.Skip(i).Take(len));
                    if (!polyphoneMap.TryGetValue(pattern, out var replacement)) continue;

                    bool useCorrection;
                    if (polyphoneDecisions.TryGetValue(pattern, out bool cached)) {
                        useCorrection = cached;
                    } else if (askPolyphone) {
                        useCorrection = AskPolyphoneUser(pattern,
                            lyrics.Skip(i).Take(len).ToArray(),
                            replacement.Split(' '));
                        polyphoneDecisions[pattern] = useCorrection;
                    } else {
                        polyphoneDecisions[pattern] = true;
                        useCorrection = true;
                    }

                    if (!useCorrection) continue;

                    var parts = replacement.Split(' ');
                    if (parts.Length != len) break;

                    for (int j = 0; j < len; j++) {
                        var (gi, ni) = refs[i + j];
                        var oldNote = groups[gi][ni];
                        groups[gi][ni] = new Note {
                            lyric = parts[j],
                            phoneticHint = oldNote.phoneticHint,
                            tone = oldNote.tone,
                            position = oldNote.position,
                            duration = oldNote.duration,
                            phonemeAttributes = oldNote.phonemeAttributes,
                        };
                        lyrics[i + j] = parts[j];
                    }
                    i += len - 1;
                    break;
                }
            }

            FixYiBuToneChange(lyrics, groups, refs);

            if (!disableDoubleChar) {
                for (int i = 0; i < lyrics.Count - 1; i++) {
                    if (lyrics[i] == "__skip__" || string.IsNullOrEmpty(lyrics[i])) continue;
                    if (lyrics[i] == lyrics[i + 1]) {
                        var (gi, ni) = refs[i + 1];
                        lightToneFlags[groups[gi][ni].position] = true;
                    }
                }
            }

            for (int i = 0; i < lyrics.Count - 1; i++) {
                if (lyrics[i] == "__skip__") continue;
                if (lyrics[i] == "er") continue;
                if (lyrics[i + 1] != "er") continue;
                if (!IsSingleSyllable(lyrics[i])) continue;

                var (gi, ni) = refs[i + 1];
                var oldNote = groups[gi][ni];
                groups[gi][ni] = new Note {
                    lyric = "",
                    phoneticHint = null,
                    tone = oldNote.tone,
                    position = oldNote.position,
                    duration = oldNote.duration,
                    phonemeAttributes = oldNote.phonemeAttributes,
                };
                lyrics[i + 1] = "";
            }
        }

        private void FixYiBuToneChange(List<string> lyrics, Note[][] groups, List<(int gi, int ni)> refs) {
            for (int i = 0; i < lyrics.Count - 1; i++) {
                if (lyrics[i] == "__skip__") continue;

                string cur = Zh2JaCore.StripDigits(lyrics[i]);
                if (cur != "yi" && cur != "bu") continue;

                int nextPos = groups[refs[i + 1].gi][refs[i + 1].ni].position;
                bool nextIs4th = noteTones.TryGetValue(nextPos, out int t) && t == 4;
                if (!nextIs4th) continue;

                string newLyric = cur + "2";
                var (gi, ni) = refs[i];
                var oldNote = groups[gi][ni];
                groups[gi][ni] = new Note {
                    lyric = newLyric,
                    phoneticHint = oldNote.phoneticHint,
                    tone = oldNote.tone,
                    position = oldNote.position,
                    duration = oldNote.duration,
                    phonemeAttributes = oldNote.phonemeAttributes,
                };
                lyrics[i] = newLyric;
                noteTones[oldNote.position] = 2;
            }
        }

        private bool IsSingleSyllable(string lyric) => Zh2JaCore.ParsePinyin(lyric) != null;

        private bool AskPolyphoneUser(string pattern, string[] original, string[] corrected) {
            if (!UiCompat.Available) return true;

            try {
                string origStr = string.Join(" ", original);
                string corrStr = string.Join(" ", corrected);

                string msg =
                    $"检测到多音字：\n\n" +
                    $"  原始识别：{origStr}\n" +
                    $"  建议修正：{corrStr}\n\n" +
                    $"是否采用建议读音？\n\n" +
                    $"「是」= 使用修正读音\n" +
                    $"「否」= 保持原始识别";

                int result = MessageBoxW(IntPtr.Zero, msg, "ZH to JA - 多音字确认",
                    0x4 | 0x20 | 0x40000);

                return result == 6;
            } catch {
                return true;
            }
        }

        public override Result Process(Note[] notes, Note? prev, Note? next,
                                       Note? prevNeighbour, Note? nextNeighbour,
                                       Note[] prevNeighbours) {
            if (string.IsNullOrEmpty(notes[0].lyric))
                return new Result { phonemes = new Phoneme[0] };

            string rawLyric = notes[0].lyric;
            if (!string.IsNullOrEmpty(notes[0].phoneticHint))
                rawLyric = notes[0].phoneticHint;

            var parts = rawLyric.Split(new[] { ' ', ',', '，', '、', '/', '|' },
                StringSplitOptions.RemoveEmptyEntries);

            Result result;
            if (parts.Length > 1)
                result = ProcessMultipleSyllables(parts, notes, prevNeighbour);
            else
                result = base.Process(notes, prev, next, prevNeighbour, nextNeighbour, prevNeighbours);

            ShortenNasalTail(result, notes, next.HasValue);

            int notePos = notes[0].position;
            if (!disableLightTone && lightToneFlags.TryGetValue(notePos, out bool isLight) && isLight)
                ApplyLightTone(result, notes);

            if (!disableToneTiming && noteTones.TryGetValue(notePos, out int tone)
                && tone >= 1 && tone <= 4)
                ApplyToneTiming(result, notes, tone);

            return result;
        }

        private void ApplyLightTone(Result result, Note[] notes) {
            if (result.phonemes == null || result.phonemes.Length < 2) return;
            int totalDuration = notes.Sum(n => n.duration);
            int last = result.phonemes.Length - 1;
            int lastPos = result.phonemes[last].position;
            int currentDur = totalDuration - lastPos;
            int newDur = Math.Max(25, (int)(currentDur * 0.6));
            int newPos = totalDuration - newDur;

            if (newPos > 0 && newPos < totalDuration) {
                var p = result.phonemes[last];
                p.position = newPos;
                result.phonemes[last] = p;
            }
        }

        private void ApplyToneTiming(Result result, Note[] notes, int tone) {
            if (result.phonemes == null || result.phonemes.Length < 2) return;
            int totalDuration = notes.Sum(n => n.duration);
            int last = result.phonemes.Length - 1;

            float ratio = tone switch {
                1 => 1.00f, 2 => 0.95f, 3 => 1.10f, 4 => 0.75f, _ => 1.00f,
            };

            int lastPos = result.phonemes[last].position;
            int currentDur = totalDuration - lastPos;
            int newDur = Math.Max(25, (int)(currentDur * ratio));
            int newPos = totalDuration - newDur;

            if (newPos > 0 && newPos < totalDuration) {
                var p = result.phonemes[last];
                p.position = newPos;
                result.phonemes[last] = p;
            }
        }

        // =============================================================
        // v2.1.6 鼻音韵尾处理（KOtoJA 风格：末尾固定毫秒）
        // =============================================================
        private void ShortenNasalTail(Result result, Note[] notes, bool hasNext) {
            if (result.phonemes == null || result.phonemes.Length == 0) return;
            if (nasalMode == "full") return;

            int totalDuration = notes.Sum(n => n.duration);
            if (totalDuration <= 0) return;

            for (int i = result.phonemes.Length - 1; i >= 0; i--) {
                var p = result.phonemes[i];
                if (string.IsNullOrEmpty(p.phoneme)) continue;

                string ph = p.phoneme.ToLowerInvariant();
                bool isNasalTail = ph == "ん" || ph == "ン" || ph == "n" ||
                                   ph.EndsWith("ん") || ph.EndsWith("ン");
                if (!isNasalTail) break;

                if (result.phonemes.Length == 1) break;

                if (nasalMode == "none") {
                    var np = p;
                    np.position = totalDuration;
                    result.phonemes[i] = np;
                    continue;
                }

                int targetMs = hasNext ? (int)(nasalMs * 1.5) : nasalMs;
                int targetStart = totalDuration - targetMs;
                int minStart = totalDuration / 2;
                if (targetStart < minStart) targetStart = minStart;

                if (targetStart <= p.position) continue;

                var np2 = p;
                np2.position = targetStart;
                result.phonemes[i] = np2;
            }
        }

        protected override string[] GetSymbols(Note note) {
            if (!string.IsNullOrEmpty(note.phoneticHint))
                return note.phoneticHint.Split(
                    new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string lyric = note.lyric;
            if (string.IsNullOrEmpty(lyric)) return null;
            if (lyric == "-" || lyric == "R" || lyric == "+") return new[] { lyric };

            if (Zh2JaCore.ContainsJapaneseKana(lyric))
                return lyric.Split(
                    new[] { ' ', ',', '，', '、' },
                    StringSplitOptions.RemoveEmptyEntries);

            return Zh2JaCore.ParsePinyin(lyric);
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            var phonemes = new List<string>();
            if (string.IsNullOrEmpty(syllable.v)) return phonemes;

            var moras = Zh2JaCore.GenerateMoras(syllable.cc, syllable.v, fullPinyinMap);
            if (moras.Count == 0) return phonemes;

            string prevJaVowel = Zh2JaCore.ConvertToJapaneseVowel(syllable.prevV);
            string prevMora = null;

            for (int i = 0; i < moras.Count; i++) {
                string mora = moras[i];
                string alias = null;

                // 1. 优先尝试带 * 的过渡音素
                if (useWildcard && !disableVcv) {
                    if (i == 0) {
                        if (!string.IsNullOrEmpty(prevJaVowel) && prevJaVowel != "-") {
                            alias = PickAlias(syllable.vowelTone,
                                $"{prevJaVowel} * {mora}", $"{prevJaVowel} *{mora}",
                                $"{prevJaVowel}* {mora}", $"{prevJaVowel}*{mora}",
                                $"{prevJaVowel} {mora}", $"{prevJaVowel}{mora}",
                                $"{prevJaVowel} *", $"* {mora}",
                                mora);
                        }
                    } else {
                        if (!string.IsNullOrEmpty(prevMora)) {
                            string prevVowel = Zh2JaCore.GetLastJapaneseVowel(prevMora);
                            if (!string.IsNullOrEmpty(prevVowel) && prevVowel != "-") {
                                alias = PickAlias(syllable.vowelTone,
                                    $"{prevVowel} * {mora}", $"{prevVowel} *{mora}",
                                    $"{prevVowel}* {mora}", $"{prevVowel}*{mora}",
                                    $"{prevVowel} {mora}", $"{prevVowel}{mora}",
                                    $"{prevVowel} *", $"* {mora}",
                                    mora);
                            }
                        }
                    }
                }

                // 2. 回退到标准逻辑
                if (string.IsNullOrEmpty(alias) || alias == mora) {
                    if (i == 0) {
                        if (disableVcv || string.IsNullOrEmpty(prevJaVowel) || prevJaVowel == "-") {
                            alias = PickAlias(syllable.vowelTone, $"- {mora}", $"-{mora}", mora);
                        } else {
                            alias = PickAlias(syllable.vowelTone, $"{prevJaVowel} {mora}", $"{prevJaVowel}{mora}", mora);
                        }
                    } else {
                        alias = PickAlias(syllable.vowelTone, mora);
                    }
                }

                phonemes.Add(alias);
                prevMora = mora;
            }
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            var phonemes = new List<string>();
            string prevV = Zh2JaCore.ConvertToJapaneseVowel(ending.prevV);
            if (string.IsNullOrEmpty(prevV)) return phonemes;

            if (ending.cc != null && ending.cc.Length > 0) {
                foreach (var c in ending.cc) {
                    if (string.IsNullOrEmpty(c)) continue;
                    string cons = Zh2JaCore.InitialMap.TryGetValue(c, out var jc) ? jc : c;

                    if (HasOtoCached($"{prevV} {cons}", ending.tone)) {
                        phonemes.Add($"{prevV} {cons}");
                    } else if (HasOtoCached($"{prevV}{cons}", ending.tone)) {
                        phonemes.Add($"{prevV}{cons}");
                    }
                }
            }

            if (phonemes.Count == 0) {
                if (HasOtoCached($"{prevV} R", ending.tone)) {
                    phonemes.Add($"{prevV} R");
                } else if (HasOtoCached($"{prevV} -", ending.tone)) {
                    phonemes.Add($"{prevV} -");
                }
            }

            return phonemes;
        }

        private Result ProcessMultipleSyllables(string[] parts, Note[] notes, Note? prevNeighbour) {
            var phonemes = new List<Phoneme>();
            int totalDuration = notes.Sum(n => n.duration);
            int perSyllable = totalDuration / parts.Length;

            string prevV = "-";
            if (prevNeighbour.HasValue) {
                string prevLyric = prevNeighbour.Value.lyric;
                if (!string.IsNullOrEmpty(prevNeighbour.Value.phoneticHint))
                    prevLyric = prevNeighbour.Value.phoneticHint;
                var prevParts = prevLyric.Split(
                    new[] { ' ', ',', '，', '、' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (prevParts.Length > 0) {
                    var parsedPrev = Zh2JaCore.ParsePinyin(prevParts.Last());
                    if (parsedPrev != null && parsedPrev.Length > 0)
                        prevV = Zh2JaCore.ConvertToJapaneseVowel(parsedPrev[parsedPrev.Length - 1]);
                }
            }

            int cursor = 0;
            for (int i = 0; i < parts.Length; i++) {
                var parsed = Zh2JaCore.ParsePinyin(parts[i]);
                if (parsed == null) continue;

                string[] cc = parsed.Length > 1
                    ? parsed.Take(parsed.Length - 1).ToArray()
                    : new string[0];
                string v = parsed[parsed.Length - 1];
                if (string.IsNullOrEmpty(v)) continue;

                var moras = Zh2JaCore.GenerateMoras(cc, v, fullPinyinMap);
                if (moras.Count == 0) continue;

                int moraStep = Math.Max(1, perSyllable / moras.Count);

                for (int j = 0; j < moras.Count; j++) {
                    string alias;
                    if (j == 0) {
                        if (disableVcv || string.IsNullOrEmpty(prevV) || prevV == "-")
                            alias = PickAlias(notes[0].tone,
                                $"- {moras[j]}", $"-{moras[j]}", moras[j]);
                        else
                            alias = PickAlias(notes[0].tone,
                                $"{prevV} {moras[j]}", $"{prevV}{moras[j]}", moras[j]);
                    } else {
                        alias = PickAlias(notes[0].tone, moras[j]);
                    }

                    phonemes.Add(new Phoneme { phoneme = alias, position = cursor });
                    cursor += moraStep;
                }

                prevV = Zh2JaCore.ConvertToJapaneseVowel(v);
            }

            return new Result { phonemes = phonemes.ToArray() };
        }

        private string PickAlias(int tone, params string[] candidates) {
            foreach (var c in candidates) {
                if (HasOtoCached(c, tone)) return c;

                foreach (var variant in ExpandAliasVariants(c)) {
                    if (variant != c && HasOtoCached(variant, tone)) return variant;
                }
            }
            return candidates.Last();
        }

        private IEnumerable<string> ExpandAliasVariants(string alias) {
            if (aliasOverrides.Count == 0) yield break;

            string bare = alias;
            string prefix = "";
            int lastSpace = alias.LastIndexOf(' ');
            if (lastSpace >= 0 && lastSpace < alias.Length - 1) {
                bare = alias.Substring(lastSpace + 1);
                prefix = alias.Substring(0, lastSpace + 1);
            }

            if (aliasOverrides.TryGetValue(bare, out var alts)) {
                foreach (var alt in alts) yield return prefix + alt;
            }
        }

        private bool HasOtoCached(string alias, int tone) {
            var key = (alias, tone);
            if (!otoCache.TryGetValue(key, out bool result)) {
                result = HasOto(alias, tone);
                otoCache[key] = result;
            }
            return result;
        }
    }
}