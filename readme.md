# ChineseToJapanesePhonemizer

> 让日语音源唱中文 —— OpenUtau 音素器插件

[![Version](https://img.shields.io/badge/version-2.1.6-blue.svg)](#版本历史)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-lightgrey.svg)](#系统要求)
[![OpenUtau](https://img.shields.io/badge/OpenUtau-v0.1.570%2B-green.svg)](#系统要求)
[![License](https://img.shields.io/badge/license-MIT-orange.svg)](#许可证)

一个 OpenUtau 音素器插件，让**日语音源（CV / VCV / CVVC）直接演唱中文歌词**，无需手动把汉字翻译成日语假名。

```
中文歌词  →  拼音  →  日语 mora 序列  →  匹配音源别名  →  渲染歌声
```

你只需要在音符里输入中文汉字，插件会自动完成后续所有转换。

---

## ✨ 特性

- **输入即唱** — 一个音符一个汉字，插件自动转换
- **完整拼音优先** — 400+ 常用拼音直接映射到日语谐音（如 `hua → ふぁ`、`chi → ち`）
- **内置 Romaji → 假名映射表** — 不依赖 WanaKanaNet，拗音（みゃ / きゅ / しょ）转换 100% 正确
- **多音节输入** — 一个音符里用空格分隔多个拼音，自动分配时值
- **中文韵律支持** — 一/不变调、轻声压缩、叠字缩短、声调时长微调
- **多音字修正** — n-gram 上下文匹配，支持用户确认弹窗
- **鼻音韵尾三档处理** — `none` / `short` / `full`，解决弱音源「ん」过长问题
- **YAML 配置** — 用户可自定义完整拼音映射、多音字规则、别名覆盖
- **标点/数字自动处理** — 标点转空格，数字转拼音（2024 → er ling er si），儿化音合并

---

## 📋 系统要求

| 项目 | 要求 |
|---|---|
| OpenUtau 版本 | **v0.1.570 或更高**（官方 win-x64 编译版） |
| 操作系统 | Windows 10 / 11 (x64) |
| 音源 | 任何日语音源（CV、VCV、CVVC 均支持） |
| .NET 运行时 | .NET 10 Desktop Runtime（最新 OpenUtau 官方版已自带） |

> ⚠️ 本插件针对 OpenUtau 官方的 .NET 10 版本编译。如果你使用的是旧版 OpenUtau（.NET 8 时代），插件将无法加载，请先升级 OpenUtau。

---

## 📦 安装

### 方式一：使用安装包（推荐）

1. 下载最新的 `MyZHtoJAPlugin-v2.1.6-Setup.exe`
2. **完全关闭 OpenUtau**（任务管理器确认无 `OpenUtau.exe` 进程）
3. 双击安装包，按提示下一步
4. 安装程序会自动探测 OpenUtau 的 Plugins 目录
5. 完成后可选择自动启动 OpenUtau

### 方式二：手动安装

1. 下载 `MyZHtoJAPlugin2.1.6.dll`（在 `dll单文件版本（包括历史版本）/` 目录）
2. 完全关闭 OpenUtau
3. 复制 DLL 到以下位置之一：
   ```
   C:\Users\<你的用户名>\Documents\OpenUtau\Plugins\MyZHtoJAPlugin.dll
   ```
   或（便携版）：
   ```
   <OpenUtau安装目录>\Plugins\MyZHtoJAPlugin.dll
   ```
4. 启动 OpenUtau，在音轨设置里找到音素器 **"ZH to JA"**

> 💡 **提示**：如果 OpenUtau 正在运行，DLL 会被锁定，复制会失败或 DLL 消失。请务必先完全退出。

---

## 🎤 使用方法

### 1. 选择音素器

在音轨设置面板里，把**音素器（Phonemizer）**切换为 `ZH to JA`。

### 2. 直接输入中文

在钢琴卷帘中双击音符，直接输入汉字：

```
音符1: 你
音符2: 好
音符3: 世
音符4: 界
```

**规则：一个音符 = 一个汉字。** 不要把一句话塞进同一个音符。

### 3. 特殊符号

| 符号 | 作用 |
|---|---|
| `-` | 起始标记（放在乐句第一个音符） |
| `R` | 尾音收尾 |
| `+` | 延音 |
| `吸入` | 换气声 |

### 4. 多音字处理

遇到多音字（如「长」「行」「重」）读错时：

- **方法一**：直接写拼音。把「长」改成 `chang` 或 `zhang`
- **方法二**：右键音符 → 音素提示（Phonetic Hint） → 输入拼音

### 5. 鼻音韵尾（ん）控制

中文的 `an / en / ang / eng / in / ing / ong` 等韵母会生成「ん」。不同音源的「ん」采样长度差异很大，插件提供三档模式：

| 模式 | 行为 | 适用场景 |
|---|---|---|
| `none` | 完全不生成「ん」 | 想要干净、不带鼻音 |
| `short`（默认） | 「ん」只占音符末尾固定毫秒 | 保留鼻音但不拖长 |
| `full` | 保留完整「ん」采样 | 想要明显鼻音 |

> ⚠️ **重要**：「ん」的物理时长由音源 `oto.ini` 决定，音素器**无法改变采样本身长度**。
> - 普通音源（ん ≈ 50ms）→ `short` 效果明显
> - **弱音源 / 气声音源**（ん ≥ 150ms）→ `short` 效果有限，仍会偏长
>
> 此时请改用**分音符**，这是唯一 100% 精确的方案：
> ```
> 音符1: a     ← 主元音
> 音符2: n     ← 单独的「ん」，音符长度决定实际时长
> ```

### 6. 渲染

填完词后，点击 **渲染（Render）** 按钮。

> ⚠️ **更换 DLL 或修改 YAML 后必做**：
> 1. 完全退出 OpenUtau
> 2. 清空 `Documents\OpenUtau\Cache\` 里的内容
> 3. 重新启动并渲染
>
> 不清缓存会导致 OpenUtau 继续播放旧音素器生成的音频，你改了参数也听不到效果。

---

## ⚙️ 配置（zh2ja.yaml）

插件会自动读取以下位置的 `zh2ja.yaml`：

- `Documents\OpenUtau\Plugins\zh2ja.yaml`（全局配置）
- `<音源目录>\zh2ja.yaml`（该音源专属，优先级更高）

没有这个文件时使用内置默认值。完整示例：

```yaml
# ===== 基础开关 =====
nasal_mode: short       # none / short / full
nasal_ms: 120           # short 模式下「ん」占音符末尾毫秒
disable_vcv: false      # true = 禁用 VCV 过渡
disable_light_tone: false
disable_tone_timing: false
disable_double_char: false

# ===== 自定义完整拼音映射 =====
# full_pinyin:
#   hua: [fa]
#   chi: [chi]
#   an: [a]          # 例：让「安」不带鼻音

# ===== 多音字修正 =====
# polyphones:
#   "chang da": "zhang da"     # 长大
#   "le qu": "yue qu"          # 乐趣

# ===== 别名覆盖（弱音源用短别名）=====
# alias_overrides:
#   "ん": ["ん -", "n -", "n"]
```

---

## 🐛 常见问题

<details>
<summary><b>Q1：音素器列表里找不到 "ZH to JA"？</b></summary>

1. 确认 DLL 位置正确：`Documents\OpenUtau\Plugins\`
2. 确认文件名完整：不能是 `MyZHtoJAPlugin.dll.txt` 或带 `(1)` 后缀
3. 查看日志：菜单栏「帮助」→「显示日志」，搜索 `ZH to JA`
4. 重启 OpenUtau
</details>

<details>
<summary><b>Q2：改了 YAML / 换了 DLL，效果没变化？</b></summary>

OpenUtau 的缓存机制。必须：
1. 完全关闭 OpenUtau
2. 清空 `Documents\OpenUtau\Cache\` 里的内容
3. 重新启动并渲染
</details>

<details>
<summary><b>Q3：渲染时报 <code>Oto not found for "ゃ"</code>？</b></summary>

正常现象。「ゃ」是拗音（みゃ / きゃ / しゃ）的小写后缀，不是独立 mora。OpenUtau 会做边界探测，把「みゃ」拆成「み」和「ゃ」尝试查找 OTO。**警告可忽略，声音正常播放。**
</details>

<details>
<summary><b>Q4：DLL 复制进去后消失了？</b></summary>

- OpenUtau 正在运行，锁定了文件
- 复制的是与官方内置插件同名的文件，被自动清理

**解决**：完全关闭 OpenUtau（任务管理器确认）→ 使用独立文件名 `MyZHtoJAPlugin.dll` → 重新复制。
</details>

<details>
<summary><b>Q5：某些字发音很奇怪？</b></summary>

1. 换读法：用拼音代替汉字
2. 音素提示：右键音符 → 音素提示 → 手动指定发音
3. 换音源测试
4. 反馈问题：把出问题的字和音源型号反馈给作者
</details>

<details>
<summary><b>Q6：ん 听起来太长怎么办？</b></summary>

先确认 `zh2ja.yaml` 的 `nasal_mode`。如果已经是 `short` 但还长，说明**音源「ん」采样本身就很长**（常见于弱音源）。此时：
- 改成 `nasal_mode: none`，直接省略鼻音
- 分两个音符写 `a` + `n`，用音符长度精确控制
- 调小 `nasal_ms`（如 60），能榨出一点效果
</details>

---

## 📝 版本历史

| 版本 | 主要改动 |
|---|---|
| **v2.1.6** | 修复 `nasal_mode=none` 不生效 bug；`short` 改用 KOtoJA 风格「末尾固定毫秒」；默认 `short`；新增 `nasal_ms` 配置；提供 Inno Setup 安装包 |
| v2.1.5 | `nasal_mode` 三档开关（none / short / full） |
| v2.1.4 | 尝试 position 后移压缩鼻音（效果有限） |
| v2.1.3 | `ou` 韵母 `o+u → o+o`（避免像 who）；鼻音默认省略 |
| v2.1.2 | 修复 `yu` 键重复；`dya/dyu/dyo` 改 `じゃ` 系；新增 `disable_vcv` |
| v2.1.1 | 内置 `RomajiToKana`；修复拗音拆分 |
| v2.1 | `FullPinyinMap` 重构为音节级数组 |
| v2.0 | 修复拼音轻声误判；精简多音字表 |
| v1.3 | 声调/轻声/叠字/一不变调/别名覆盖/C4 UI |
| v1.2 | 标点/数字/儿化/YAML/别名缓存 |
| v1.1 | 完整拼音优先 + 多音节输入 |
| **v1.0** | 首版：拼音 → 假名 |

完整开发日志见 [`开发日志.docx`](开发/开发日志.docx)。

---

## 📂 仓库结构

```
ChineseToJapanesePhonemizer/
├── README.md
├── MyZHtoJAPlugin-v2.1.6-Setup.exe          # 安装包（推荐）
├── 使用说明.docx                             # 图文教程
├── dll单文件版本（包括历史版本）/
│   ├── MyZHtoJAPlugin1.0.dll
│   ├── MyZHtoJAPlugin2.1.dll
│   ├── MyZHtoJAPlugin2.1.1.dll
│   └── MyZHtoJAPlugin2.1.6.dll              # 最新
└── 开发/                                     # 源码
    ├── ChineseToJapanesePhonemizer.cs        # 主源码
    ├── MyZHtoJAPlugin.csproj
    ├── build.ps1                             # 一键编译脚本
    ├── installer.iss                         # Inno Setup 安装脚本
    ├── generate_manual.py                    # 文档生成脚本
    ├── zh2ja.yaml                            # 默认配置模板
    └── LICENSE.txt
```

---

## 🔨 从源码编译

```powershell
cd 开发
.\build.ps1
```

脚本会自动：
1. 检查依赖文件（`OpenUtau.Core.dll` 等）
2. 生成 csproj（如缺失）
3. 执行 `dotnet build -c Release`
4. 输出编译好的 DLL 路径

需要 .NET 10 SDK 和 OpenUtau 的参考 DLL（从 OpenUtau 安装目录复制）。

### 打包安装器

```powershell
.\build.ps1 -Package
```

需要预先安装 [Inno Setup 6](https://jrsoftware.org/isdl.php)。

---

## 🙏 致谢

- [OpenUtau](https://github.com/stakira/OpenUtau) — 开源歌声合成软件
- [ENtoJAPhonemizer](https://github.com/stakira/OpenUtau) — 架构参考（作者 TUBS、Cadlaxa）
- [KOtoJAPhonemizer](https://github.com/stakira/OpenUtau) — 鼻音 position 处理参考（作者 Lotte V）
- 所有测试和反馈的用户

---

## 📄 许可证

本项目使用 [MIT License](开发/LICENSE.txt)。

```
Copyright (c) 2026 Deepseek

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
```

---

**祝调教愉快！🎤**