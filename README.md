以下是一份专为你的“AI 辅助生成的 3D 无尽圆柱赛道赛车”项目定制的 `README.md`，你可以直接复制到仓库根目录，并替换掉括号内的占位内容。

---

```markdown
# 🏎️ Endless Cylinder Racer – AI 从零生成的 3D 无尽赛车

> 一款使用 **Unity + C#** 开发的 3D 无尽赛车游戏。  
> 所有代码与场景由 **AI（Claude + Unity MCP）通过自然语言指令从零生成**，未使用任何预制脚本。

![Gameplay Screenshot](screenshot.png) <!-- 可替换为你的游戏截图 -->

---

## 🎮 游戏简介

玩家操控一辆赛车，在一个**动态生成的圆柱赛道内表面**无尽飞驰。赛道地形由**柏林噪声**实时生成，每次运行都独一无二。躲避障碍物、穿过得分门，坚持越久分数越高！

### ✨ 核心特性

- **无尽圆柱赛道**：圆柱形赛道无限延伸，顶点基于柏林噪声产生起伏，视觉永不重复。
- **动态生成**：一次只保留两段赛道，旧的自动销毁，新的即时生成，实现真正无限循环。
- **完整车辆物理**：自定义 WheelCollider 控制、车轮动画、稳定性辅助、尘土粒子特效。
- **智能摄像机**：第三人称跟随，平滑入场，转弯时拥有电影级阻尼感。
- **障碍物与闸门**：随机生成的动态障碍（碰撞即结束）与得分门（穿过 +1 分）。
- **游戏管理器**：计时、计分、本地最高分记录、游戏结束重玩。
- **主菜单与背景音乐**：简洁的启动界面，跨场景不中断的背景音乐。

---

## 🧠 AI 开发说明

本项目是 **“AI 驱动游戏开发”** 的实验作品，全程使用 **VSCode + Unity MCP（Model Context Protocol）** 让 AI 直接读取场景、创建脚本、配置组件。开发者只需给出自然语言指令，AI 即可完成编写代码、搭建场景、关联引用等所有工作。

> 示例指令：  
> “创建一个 WorldGenerator 脚本，使用柏林噪声生成圆柱赛道，支持无限循环。”  
> “在场景中创建 Canvas，添加分数文本和时间文本，并挂载 GameManager。”

整个项目约 **95% 的代码由 AI 生成**，包括：
- `WorldGenerator.cs`
- `Car.cs`
- `CameraFollow.cs`
- `GameManager.cs`
- `Obstacle.cs` / `Gate.cs`
- `MainMenu.cs` / `Music.cs`

你可以在 [Commit 历史] 中看到每一步的 AI 对话记录（如果有的话）。

> 工具链： `Unity 2021.3` · `VSCode` · `Unity MCP` · `Claude 3 / Copilot`

---

## 🚀 快速开始

### 环境要求
- Unity 2021.3 或更高版本（内置渲染管线）
- .NET Framework 4.x（默认）
- 任意支持 WebGL / PC 的平台

### 运行游戏
1. 克隆仓库：
   ```bash
   git clone https://github.com/你的用户名/EndlessCylinderRacer.git
   ```
2. 使用 Unity Hub 打开项目文件夹。
3. 打开 `Scenes/MainMenu` 场景，点击 Play 或直接进入 `Scenes/Game` 场景。
4. 使用**鼠标点击屏幕左右半区** 或 **A/D 键（←→）** 控制赛车左右变道。

---

## 🎛️ 核心参数调整

所有主要参数均在 Unity Inspector 中公开，方便调整游戏体验：

| 脚本 | 关键参数 | 作用 |
|------|---------|------|
| `WorldGenerator` | `dimensions.y` / `scale` | 控制单段赛道长度 |
| `WorldGenerator` | `waveHeight` | 地形起伏高度 |
| `WorldGenerator` | `globalSpeed` | 赛道卷动速度（难度） |
| `Car` | `rotateSpeed` / `rotationAngle` | 车辆转向灵敏度 |
| `CameraFollow` | `distance` / `height` | 摄像机视角 |

---

## 📂 项目结构

```
Assets/
├── Scripts/
│   ├── WorldGenerator.cs      # 圆柱赛道动态生成
│   ├── BasicMovement.cs       # 赛道片段移动
│   ├── Car.cs                 # 玩家车辆控制
│   ├── CameraFollow.cs        # 第三人称相机
│   ├── GameManager.cs         # 游戏主逻辑与UI
│   ├── Obstacle.cs            # 障碍物
│   ├── Gate.cs                # 得分门
│   ├── CarGameOverTrigger.cs  # 翻车检测
│   ├── MainMenu.cs            # 主菜单
│   └── Music.cs               # 背景音乐单例
├── Scenes/
│   ├── MainMenu.unity
│   └── Game.unity
├── Prefabs/
│   ├── PlayerCar.prefab
│   ├── Obstacle.prefab
│   └── Gate.prefab
└── Materials/
```

---

## 🛠️ 自定义与扩展

- **增加障碍物种类**：在 `WorldGenerator` 的 `obstacles` 数组里拖入新 Prefab，即可随机生成。
- **添加得分特效**：修改 `GameManager.UpdateScore()`，使用 `scoreEffect` 触发器播放动画。
- **实现 AI 对手**：该项目原生支持添加 ML‑Agents 训练的智能赛车（详见 `AIAgent` 分支，如果有的话）。
- **更换赛道纹理**：替换 `WorldGenerator` 中的 `meshMaterial`。

---

## 📸 截图

（此处可粘贴游戏截图、GIF 动图或视频链接）

---

## 🤝 贡献

本项目旨在展示 **AI 辅助游戏开发** 的可能性。欢迎提交 Issue 或 PR 来完善 AI 生成代码的健壮性、添加新特性。

---

## 📄 许可证

本项目采用 MIT 许可证。详见 `LICENSE` 文件。

---

## ⭐ 给个 Star 吧

如果这个“AI 从零生成的游戏”让你感兴趣，不妨给个 Star ⭐ 支持一下，让更多人看到 AI 在游戏开发中的潜力！
```

---

**使用建议**：
1. 将 `screenshot.png` 替换为你的游戏实际截图（放在仓库根目录）。
2. 将 `https://github.com/你的用户名/EndlessCylinderRacer.git` 改为你的仓库链接。
3. 如果你在开发过程中真的保留了 AI 对话记录，可以在 AI 开发说明部分附上链接或截图，非常加分。
4. 如果后续添加了 ML‑Agents 的 AI 对手，可以额外开一个分支并在 README 中提及。

这份 README 既清晰说明了游戏玩法，又突出了“纯 AI 生成”的技术亮点，非常适合放在 GitHub 上展示。
