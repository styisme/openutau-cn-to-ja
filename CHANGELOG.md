# Changelog

本项目的重要变更都记录在此文件。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [Semantic Versioning 2.0.0](https://semver.org/lang/zh-CN/)。

> **版本号说明**：v2.1.7 及以前的历史版本号未严格遵循 SemVer
> （例如 v2.1.7 按 SemVer 应为 MINOR 递增，即 v2.2.0）。
> 历史版本与 tag 不重写，自 **v2.2.0** 起严格执行 SemVer 2.0.0。
> 历史条目在没有确切发布日期时不标注日期。
> 
> **`## [Latest] X.Y.Z`**：CI 构建安装包时读取的唯一版本来源。
>   发新版时把 `[Latest]` 头衔移到新条目上，旧条目改为 `## X.Y.Z`。
>   两个 `[Latest]` 同时存在时，CI 只取文件中靠上的那一个。

## [Latest] 2.3.0

### Added

- 数字读法扩展：支持日期（`2024-01-15`）、时间（`12:30`）、小数（`3.14`）、
  百分比（`50%`）、千位逗号（`1,000`）
- 补充 `NumberReadingTests` 单元测试

## 2.2.2

### Fixed

- `LightToneChars` 误把感叹词（啊/呀/哦/咯/嘛/啦/哎/哇）当作轻声处理，
  导致「哦」等字无声。现在只保留语法助词（的/了/着/呢/吧/吗）。

## 2.2.1

### Fixed

- 安装向导显示旧版本号 2.1.7 的问题

## 2.2.0

### Changed

- 项目结构重组：源码移至 `src/`，脚本移至 `build/`，文档移至 `docs/`
- 历史 DLL / 安装包 / docx 移出 Git，改由 GitHub Release 管理
- README 全面清理，版本历史迁移至本文件

### Added

- xunit 单元测试项目与 `openutau-cn-to-ja.slnx` 解决方案
- GitHub Actions CI：编译 → 测试 → Inno Setup 安装包（`build/fetch-deps.ps1` 自动拉取依赖）
- CONTRIBUTING.md（Conventional Commits + SemVer + 发版检查单）
- `zh2ja.yaml` 模板补充 `use_wildcard` 键
- 全部标准音节的 `ParsePinyin` 回归测试

### Fixed

- 修复 `yuan` 拼音解析返回 null 导致该音节无声的缺陷（`Finals` 表补 `van` 项，输出 ゆえん）

## [2.1.7] - 2026-10-02

### Added

- 通配符 `*` 过渡音素支持（Defoko 等 CV 音源），可通过 `use_wildcard` 开关控制

## [2.1.6]

### Fixed

- 修复 `nasal_mode=none` 不生效的 bug

### Changed

- short 模式改用 KOtoJA 风格「末尾固定毫秒」逻辑（默认 120ms / 有下音符 180ms）
- 默认 `nasal_mode` 改为 `short`

### Added

- 新增 `nasal_ms` 配置
- 提供 Inno Setup 安装包

## [2.1.5]

### Added

- `nasal_mode` 三档开关（none / short / full）

## [2.1.4]

### Changed

- 尝试 position 后移压缩鼻音（效果有限）

## [2.1.3]

### Fixed

- ou 韵母 o+u → o+o（避免像 who）

### Changed

- 鼻音默认省略

## [2.1.2]

### Fixed

- 修复 yu 键重复
- dya/dyu/dyo 改 じゃ 系

### Added

- 新增 `disable_vcv`

## [2.1.1]

### Added

- 内置 RomajiToKana 映射表

### Fixed

- 修复拗音拆分

## [2.1]

### Changed

- FullPinyinMap 重构为音节级数组

## [2.0]

### Fixed

- 修复拼音轻声误判

### Changed

- 精简多音字表

## [1.3]

### Added

- 声调时长 / 轻声 / 叠字 / 一不变调 / 别名覆盖 / C4 UI

## [1.2]

### Added

- 标点 / 数字 / 儿化 / YAML 配置 / 别名缓存

## [1.1]

### Added

- 完整拼音优先 + 多音节输入

## [1.0]

### Added

- 首版：拼音 → 假名

[Unreleased]: https://github.com/styisme/openutau-cn-to-ja/compare/v2.2.2...HEAD
[2.2.2]: https://github.com/styisme/openutau-cn-to-ja/releases/tag/v2.2.2
[2.2.1]: https://github.com/styisme/openutau-cn-to-ja/releases/tag/v2.2.1
[2.2.0]: https://github.com/styisme/openutau-cn-to-ja/releases/tag/v2.2.0
[2.1.7]: https://github.com/styisme/openutau-cn-to-ja/releases/tag/v2.1.7