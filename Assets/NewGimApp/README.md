# NewGimApp (Starter)

这是一个新的独立软件起步模块，目标是复用 GIMViewer 的 `.gim` 解析思路，当前已实现：

1. 导入 `.gim`
2. 解压并读取 `CBM/project.cbm`
3. 递归解析 `SUBSYSTEM` 树
4. 解析 `OBJECTMODELPOINTER -> DEV`，并递归解析 `SUBDEVICE`
5. 解析 `SOLIDMODELS` 的 PHM/MOD/STL 文件引用
6. 读取 `BASEFAMILY` 属性到 `Dictionary<string,string>`

## 包含脚本

- `GimAppModels.cs`：节点和文档模型。
- `GimAppParser.cs`：核心解析器（不依赖特定 UI 逻辑）。
- `GimAppDemoController.cs`：演示入口（调用文件选择并打印解析摘要）。
- `GimAppTreeViewController.cs`：树形层级 + 属性面板绑定器。
- `GimAppSceneRenderer.cs`：把解析节点转成场景对象，并提供选中高亮。

## 使用方式（Unity）

1. 新建空物体，挂载 `GimAppDemoController`。
2. 给任意 UI Button 绑定 `GimAppDemoController.ImportGim()`。
3. 新建空物体，挂载 `GimAppTreeViewController` 并绑定：
   - `demoController` -> 上面的 `GimAppDemoController`
   - `sceneRenderer` -> `GimAppSceneRenderer`
   - `treeView` -> Battlehub `TreeView`
   - `propertiesContent` -> 属性列表容器（Content）
   - `propertyRowPrefab` -> 包含 `key` / `value` 子节点的行预制体
4. 新建空物体，挂载 `GimAppSceneRenderer` 并绑定 `demoController`。
5. Play 后点击按钮，导入 `.gim`。
6. 选择树节点后：
   - 属性面板会显示解析属性；
   - 关联场景对象会高亮；
   - STL 节点会生成实际 Mesh，PHM/MOD 先用占位体（Cube）渲染。

## 下一步建议

- 增加 PHM/MOD 的真实几何解析（当前为占位体渲染）。
- 把 `GimNode` 扩展为可直接绑定到树形控件的 ViewModel（减少中间对象转换）。
- 优化大模型渲染性能（实例化、合批、LOD）。
