// =====================================================================
// ChineseToJapanesePhonemizer v2.2.1
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

            Log.Information($"[ZH to JA v2.2.1] UI compatibility: {(UiCompat.Available ? "available" : "NOT available")}");

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

                Log.Information($"[ZH to JA v2.2.1] Loaded config from {path} (nasal_mode={nasalMode}, nasal_ms={nasalMs}, use_wildcard={useWildcard})");
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
