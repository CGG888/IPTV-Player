# IPTV Player 发行版说明

## 简介
- 目标：产出可分发的 Windows 安装包（Inno Setup）与可选的便携包（Portable）。
- 特性：内置 GitHub 元信息（仓库、Issues、Releases、版本号），安装向导中展示许可与第三方声明。

## 系统要求
- 操作系统：Windows 10/11（x64）
- .NET 运行时：随安装包一并提供（自包含发布），无需额外安装
- 硬件：支持 d3d11 的显卡以获得硬件解码

## 发行物组成
- 安装包：Output\IPTV_Player_Setup_{版本号}.exe
- 便携包（可选）：bin\Release\net8.0-windows\win-x64\publish\ 目录整体压缩为 zip
- 许可材料：
  - LICENSE.txt（MIT）
  - THIRD-PARTY-NOTICES.txt（第三方声明）

## 版本号与元信息
- 版本来源：优先取 `git describe --tags`，否则退回 `setup.iss` 中的 `MyAppVersion`。
- 元信息注入：
  - Git 仓库主页、Issues、Releases 链接在安装向导“欢迎/完成”页展示
  - 完成页提供“打开 GitHub 仓库主页”的可选复选框

## 构建步骤
### 推荐：一键脚本
1. 双击或运行 `build_release.bat`
2. 行为：
   - 清理旧发布目录
   - dotnet publish 自包含产物到 `bin\Release\net8.0-windows\win-x64\publish\`
   - 从 Git 提取 `origin` 与 `describe`，注入为环境变量
   - 检测 `iscc`，如存在则自动编译 `setup.iss` 生成安装包至 `Output\`
3. 结束后检查：
   - `Output\IPTV_Player_Setup_{版本号}.exe`
   - `publish\` 目录用于便携包打包

### 手动：无 iscc 的情况下
1. 执行 `build_release.bat` 仅完成 `dotnet publish` 与元数据注入
2. 使用 Inno Setup IDE 打开 `setup.iss`，点击 Compile 生成安装包

## 安装包校验
1. 欢迎页与完成页应显示：
   - 项目主页（GitHub Repo）
   - Issues 链接与反馈说明
   - 当前版本号（来自 `git describe` 或 `MyAppVersion`）
2. 完成页勾选“打开 GitHub 仓库主页”，点击完成应在默认浏览器打开仓库页面
3. 安装目录 `{app}\licenses\` 下应包含：
   - LICENSE.txt
   - THIRD-PARTY-NOTICES.txt

## 运行验证
1. 首次启动 IPTV Player
2. 在“设置”填写 EPG 与台标库地址（可选，台标模板支持 `{name}`）
3. 加载 M3U 源并播放，检查：
   - 解码方式为 d3d11va（可在设置中关闭）
   - 频道切换、EPG 展示、回放入口可用（若频道/EPG 数据具备）

## libmpv-2.dll 获取与放置
- 仓库不包含 `libmpv-2.dll`（文件体积大且为运行时依赖），发布阶段引入
- 开发/调试：将 `libmpv-2.dll` 放在 `bin\Debug\net8.0-windows\` 或相应 Release 输出目录
- 发布：置于 `publish\` 目录，安装脚本会打包
- 许可：`libmpv` 采用 LGPL 2.1+，用户可以替换该 DLL；详见 THIRD-PARTY-NOTICES.txt

## GitHub Releases 发布流程
1. 打 Tag：`git tag vX.Y.Z && git push origin vX.Y.Z`
2. 打开 GitHub -> Releases -> Draft a new release
3. 标题：`IPTV Player vX.Y.Z`；正文：变更摘要、已知问题、注意事项
4. 附件：
   - Output\IPTV_Player_Setup_{版本号}.exe
   - Portable zip（可选）
5. 发布后验证下载与安装完整性

## 已知问题与注意事项
- 大文件限制：请勿将 `libmpv-2.dll` 加入仓库；已在 `.gitignore` 中忽略
- 音轨探测：极少数频道可能出现“有画无声”，可在后续版本提供可调探测参数
- 若安装包需签名：在编译完成后对 exe 进行代码签名（可集成到发布流程）

## 许可证
- 主项目：MIT（见 LICENSE.txt）
- 第三方组件与声明：见 THIRD-PARTY-NOTICES.txt

