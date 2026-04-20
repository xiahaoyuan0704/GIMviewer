<div align="center">
<br>
<h1>GimViewer </h1>
<br>
效果图<br><br>

![输入图片说明](doc/image/gim%20viewer%20v1.0%20beta.png)


使用必须遵守国家法律法规，⛔不允许非法项目使用，后果自负❗

🚩特色功能

| 支持功能 | 功能描述                                                                    | 完成程度 |
|------|-------------------------------------------------------------------------|------|
| 基本图元 | 几何图形构建 | ✅    |
| IFC | 解析IFC格式模型                | ✅    |
| STL   | 解析STL格式模型                                       | ✅    |
| 部件属性   | 部件参数详细介绍                                                        | ✅    |
| 树形层级   | 树形结构展示模型层级                        | ✅    |
| 场景环境   | 可对场景模拟真实天气环境                       | ✅    |






## 🍓依赖版本

| 依赖                         | 版本         |
|----------------------------|------------|
| ProBuilder Mesh                | 5.0.7     |
| STL                | 1.0.0  |
| Xbim       |5.1 |
|Unistorm|-|

# 贡献力量

- 大家可以贡献代码维护开源项目

# 其它说明

- 商业合作 893500765@qq.com

# Windows 本地运行指南（源码方式）

> 当前仓库是 Unity 工程源码，不是可直接双击运行的现成 EXE。

1. 安装 **Unity Hub**（Windows）。
2. 在 Unity Hub 的 `Installs` 里安装编辑器版本 **2022.1.23f1**（建议与项目版本保持一致）。
3. 在 Unity Hub 点击 `Open`，选择本仓库根目录（包含 `Assets/`、`ProjectSettings/` 的目录）。
4. 等待 Unity 首次导入资源完成后，打开主场景（优先 `Assets/GIMViewer/Scenes/Main.unity`）。
5. 点击顶部 `▶ Play` 先在编辑器中运行，验证功能是否正常。
6. 若要生成给其他 Windows 电脑直接运行的程序：
   - 打开 `File > Build Settings...`
   - 选择平台 `PC, Mac & Linux Standalone`
   - `Target Platform` 设为 `Windows`
   - 勾选（或确认）场景列表里包含 `Assets/GIMViewer/Scenes/Main.unity`
   - 点击 `Build`，输出目录中会生成 `.exe` 和同名数据文件夹

## 常见问题（Windows）

- **Q：这个项目能直接在 Unity Hub 里运行吗？**  
  **A：可以。** Unity Hub 只是“项目管理入口”，实际运行是在 Unity Editor 里完成。按上面的步骤用 Hub 打开项目后，在编辑器里点 `Play` 就能运行。

- **Q：一定要装 Visual Studio 吗？**  
  **A：不一定。**  
  - 只想打开场景、运行、打包：通常只装 Unity Hub + 对应 Unity Editor 就够了。  
  - 需要修改 C# 脚本：建议安装任意 C# IDE（Visual Studio、JetBrains Rider、VS Code + C# 插件都可以），用于代码编辑和调试，属于“推荐”而不是“运行必需”。

- **Q：Unity Hub 导入项目时报“需要 Editor / No Editor installed”怎么办？**  
  **A：这是因为你本机还没装（或没装对版本）Unity Editor。按下面处理：**  
  1. 在 Unity Hub 打开 `Installs` → `Install Editor`。  
  2. 安装与项目一致的版本：`2022.1.23f1`（见 `ProjectSettings/ProjectVersion.txt`）。  
  3. 安装完成后回到 `Projects`，重新 `Add/Open` 本项目目录。  
  4. 如果 Hub 列表里没有该版本，在 `Install Editor` 里进入 `Archive` 安装历史版本。  
  5. 若仍提示找不到 Editor：在 Hub `Installs` 里确认该版本路径有效，必要时用 `Locate` 指向已安装的 `Unity.exe`。  
  6. Windows 建议把项目放在纯英文路径（例如 `D:\\UnityProjects\\GIMviewer`），避免中文/特殊字符路径导致导入异常。

- **Q：安装 Editor 时提示 `Download failed` / `Validation failed` 怎么办？**  
  **A：通常是下载包损坏、网络中断或缓存异常。可按顺序尝试：**  
  1. 在 Unity Hub 里取消当前安装任务，退出 Hub 后重新打开再安装一次。  
  2. 更换稳定网络（尽量避免代理频繁切换、公司网络限流、断流 Wi-Fi）。  
  3. 在 Hub 设置中清理下载缓存后重试（不同版本 Hub 入口名称可能是 `Troubleshooting` / `Advanced` / `Clear cache`）。  
  4. 以管理员身份运行 Unity Hub，再次安装。  
  5. 改用 **Unity Archive** 页面下载对应版本安装包，再在 Hub 里 `Locate` 本地已安装的 `Unity.exe`。  
  6. 检查磁盘剩余空间与杀毒软件拦截（安装过程被拦截也会触发校验失败）。  
  7. 若仍失败，先安装同大版本邻近小版本（2022.1.x）验证环境，再回装 `2022.1.23f1`。

- **Q：Editor 已安装，但 Unity Hub 点“添加项目/导入项目”选中 `GIMviewer` 文件夹后没反应？**  
  **A：这通常是“路径/权限/Hub 索引”问题，不是项目本身坏了。按下面排查：**  
  1. 确认你选的是**项目根目录**（里面直接能看到 `Assets`、`ProjectSettings`、`Packages`）。  
  2. 把项目移动到英文短路径（如 `D:\\UnityProjects\\GIMviewer`），避免中文、空格、超长路径。  
  3. 用管理员身份启动 Unity Hub，再点一次 `Add`。  
  4. 在 Hub 里先 `Open` 其他已知正常项目，确认 Hub 自身可用后再回到本项目。  
  5. 关闭 Hub 后删除项目根目录下临时锁文件（如 `Temp/`、`Library/`，若存在），再重新打开 Hub 导入。  
  6. 直接双击 `ProjectSettings/ProjectVersion.txt` 确认目标版本仍是 `2022.1.23f1`，并在 Hub `Installs` 中确认该版本状态为可用。  
  7. 还是没反应：查看日志定位原因：  
     - Hub 日志：`%AppData%\\UnityHub\\logs\\info-log.json`  
     - Editor 日志：`%UserProfile%\\AppData\\Local\\Unity\\Editor\\Editor.log`  
     把最后 50 行报错发出来即可继续定位。

- **Q：项目提示需要 `2022.1.23f1`，但我本机是 `6000.x`，官网又只看到 `2022.3.x`，怎么办？**  
  **A：这是“主版本不一致”导致的。建议按优先级处理：**  
  1. **首选**：安装 `2022.1.23f1`（与项目完全一致，兼容风险最低）。  
  2. **次选**：如果确实拿不到 `2022.1.23f1`，用 `2022.3 LTS` 打开项目也常见可行，但请先复制项目做备份再升级。  
  3. 不建议直接用 `6000.x` 首次打开老项目（跨代跨度太大，包和渲染设置更容易触发升级问题）。  
  4. 用 `2022.3 LTS` 打开时，如果弹出 API/包升级提示，先同意升级；首次导入后检查 Console 报错，再决定是否继续。  
  5. 如果你只想“先跑起来”，优先目标是：`2022.3 LTS` 能无红错进入 `Main.unity` 并点击 `Play`。  
  6. 若升级后出现大量报错，再回到备份项目，逐项处理包版本（ProBuilder、URP、第三方插件）后再尝试。

- **Q：我已经打开 Unity 了，但场景是空的，看不到“土建总图”模型怎么办？**  
  **A：你很可能打开了空场景（如 `Untitled`）。请按这个顺序：**  
  1. 在 `Project` 面板打开 `Assets/GIMViewer/Scenes/Main.unity`（不要停留在 `Untitled`）。  
  2. 点击 `Play` 进入运行模式。  
  3. 在运行界面点击“打开/加载 GIM”按钮，选择模型文件。  
  4. 若文件选择器里看不到你的文件，可先复制一份并改扩展名为 `.gim` 再试（例如把 `土建总图.INTEG.RAW` 复制为 `土建总图.gim`）。  
  5. 加载后用鼠标滚轮缩放、右键旋转视角，或在层级树中点击节点定位。  

## 兼容性提示

- 本项目依赖中包含 IFC/STL 解析能力，详情见上方“依赖版本”表。
- 代码中包含 Windows 平台文件选择器实现（`UniversalFileBrowserWindows`），说明 Windows 是主要支持平台之一。

## 二次开发说明（可以改功能吗？）

可以。这个仓库就是 Unity + C# 源码工程，适合二次开发。常见可扩展方向：

1. **属性解析扩展**  
   - `GimParser` 在解析 CBM/DEV/PHM/MOD 时会挂载属性并构建层级。  
   - 目前代码里已有多处 `TODO`（例如 `SYSTEMNAME*`、`BASEFAMILY*`、逻辑模型等），这些就是可继续完善的入口。  
2. **模型解析/生成扩展**  
   - 已有 IFC、STL、GIM 解析链路，可在 `ParseIfc`、`ParseModFile` 或几何构建分支里新增图元类型、LOD、材质策略。  
3. **交互与编辑扩展**  
   - 现有点击取属性、树形层级、拖拽层级、FBX 导出、飞行/行走切换等功能都在源码里，可继续加“批量改属性、模型编辑、构件替换、测量标注”等。

### 当前代码里已经实现的主要功能（对应文件）

- **GIM 文件解压 + 解析主流程**：`GimParser.ParseGim()`（7z 解压、进入解析协程、解析结束事件）。  
- **CBM/DEV/PHM/MOD 分层解析**：`ParseCbmFile`、`ParseDevFile`、`ParsePhmFile`、`ParseModFile`。  
- **IFC 解析入口**：`ParseIfc` + `ifc2xbim` 工具链。  
- **属性面板展示**：点击模型后读取 `GimLoadItem.Items` 并动态生成属性 UI 行。  
- **GIMViewer 场景属性查看**：`GimParser` 会把 `BASEFAMILY` 属性文件解析到 `GimLoadItem.Items`，运行时点击模型可在右侧面板查看属性。  
- **打开 GIM / 导出 FBX**：`Controller` 中调用文件对话框与 `FBXExporter.ExportGameObjAtRuntime`。  
- **浏览模式和环境控制**：`GimUIController` 里有飞行/行走模式切换、天气/时间 UI 入口与树形层级菜单。

# GIMViewer1
