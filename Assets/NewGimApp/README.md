# NewGimApp (Starter)

这是一个新的独立软件起步模块，目标是复用 GIMViewer 的 `.gim` 解析思路，先实现：

1. 导入 `.gim`
2. 解压并读取 `CBM/project.cbm`
3. 递归解析 `SUBSYSTEM` 树
4. 读取 `BASEFAMILY` 属性到 `Dictionary<string,string>`

## 包含脚本

- `GimAppModels.cs`：节点和文档模型。
- `GimAppParser.cs`：核心解析器（不依赖特定 UI 逻辑）。
- `GimAppDemoController.cs`：演示入口（调用文件选择并打印解析摘要）。
- `GimAppTreeViewController.cs`：树形层级 + 属性面板绑定器。

## 使用方式（Unity）

1. 新建空物体，挂载 `GimAppDemoController`。
2. 给任意 UI Button 绑定 `GimAppDemoController.ImportGim()`。
3. 新建空物体，挂载 `GimAppTreeViewController` 并绑定：
   - `demoController` -> 上面的 `GimAppDemoController`
   - `treeView` -> Battlehub `TreeView`
   - `propertiesContent` -> 属性列表容器（Content）
   - `propertyRowPrefab` -> 包含 `key` / `value` 子节点的行预制体
4. Play 后点击按钮，导入 `.gim`。
5. 选择树节点后，属性面板会显示 `BASEFAMILY` 解析结果。

## 下一步建议

- 增加 DEV/PHM/MOD/STL 几何解析封装。
- 把 `GimNode` 扩展为可直接绑定到树形控件的 ViewModel。
- 增加模型几何渲染（MeshFilter/MeshRenderer）和选中高亮。
