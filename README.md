# NOI 乱斗

> 第一题已经拿下，第二题毫无思路。金牌就在考场尽头，而今天，你决定把脑洞开得比时间复杂度还大。

《NOI 乱斗》是一款发生在虚构 NOI 考场中的第三人称 3D 潜行喜剧原型。你需要在监考老师的视线之间寻找线索、制造一点可控的混乱，并在时限内拼出第二题的关键思路。

本项目由 **X4CE** 于 **2026 年** 开始开发，以 MIT License 开源。

## 实际画面

![NOI 乱斗主菜单](Docs/Screenshots/title.png)

![考场探索画面](Docs/Screenshots/game.png)

## 当前可玩内容

- 第三人称自由移动、奔跑、跳跃、蹲伏和镜头环绕
- 3D 考场场景，包含机位、讲台、白板、奖项展示柜与可探索侧区
- 两名会巡逻、注视、调查噪音的监考老师
- 怀疑度系统：奔跑、靠近监考和可疑行为都会提高怀疑
- 三条可收集线索与目标笔记本
- 广播、咖啡机、空调、灯光、时钟、海报等多类互动物件
- 可对话的邻座选手
- 金牌结局、被抓结局与隐藏结局
- 程序化视觉、音效和中文 UI，不依赖外部美术资源

## 环境

- Unity **2022.3.62f3c1**
- Built-in Render Pipeline
- 输入使用 Unity 旧版 Input Manager，无需安装额外输入包

使用 Unity Hub 打开项目根目录，运行 `Assets/Scenes/Main.unity`，点击 Play。

如果场景尚未生成，可以在 Unity 中执行：

`NOI Chaos > Rebuild Main Scene`

在本机生成 macOS 可执行版本：

`NOI Chaos > Build macOS App`

构建输出位于 `Builds/macOS/`。该目录已被 Git 忽略，不会进入源码仓库。

也可以使用命令行生成：

```bash
UNITY="/Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics -quit \
  -projectPath "/path/to/NOI Chaos" \
  -executeMethod NOIChaos.Editor.ProjectBootstrapper.SetupProject \
  -logFile "Logs/setup.log"
```

## 操作

| 操作 | 按键 |
| --- | --- |
| 移动 | `WASD` |
| 观察 | 鼠标 |
| 奔跑 | `Shift` |
| 跳跃 | `Space` |
| 蹲伏 | `C` |
| 交互 | `E` |
| 任务笔记本 | `Tab` |
| 暂停 | `Esc` |

## 设计原则

本作是夸张化的虚构校园喜剧。游戏中的“歪招”最终服务于寻找解题灵感，而不是鼓励现实考试作弊。现实中的 NOI 请遵守竞赛规则，尊重每一位选手。

## 目录

```text
Assets/
  Editor/                 一键场景与项目设置
  Scenes/                 主场景
  Scripts/
    AI/                   监考巡逻与视野
    Core/                 游戏状态、流程、音频
    Player/               第三人称角色与镜头
    UI/                   标题、HUD、笔记本和结局界面
    World/                程序化考场和交互物
```

## 参与开发

欢迎提交 Issue 和 Pull Request。开始前请先阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 版权

Copyright © 2026 X4CE. Licensed under the [MIT License](LICENSE).
