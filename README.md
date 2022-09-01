## 修改

- 升级AssetStudio为最新
- 升级为[FBX SDK 2020.2.1](https://damassets.autodesk.net/content/dam/autodesk/www/adn/fbx/2020)（AssetStudio需要）
- 编译器版本为Vs2022（AssetStudio需要）

# pkg-doctor

包体医生，Unity 及 Unreal 游戏包体优化工具。

# 分析 Unity 游戏包体

## 下载预编译版本
https://github.com/taptap/pkg-doctor/releases

## 也可手动生成 pkg-doctor.exe
- 安装 [FBX SDK 2020.1](https://www.autodesk.com/content/dam/autodesk/www/adn/fbx/2020-1/fbx20201_fbxsdk_vs2017_win.exe)
- 打开 AssetStudio\AssetStudio.sln
- 选择 Release 模式
- 生成 AssetStudio\AssetStudioGUI\bin\Release\pkg-doctor.exe

## 分析 Unity 游戏 apk 或 ipa

> pkg-doctor.exe /path/to/game.apk

> pkg-doctor.exe /path/to/game.ipa

## 分析 Unity 游戏资源文件夹

> pkg-doctor.exe /path/to/game/data/

# 分析 Unreal 游戏包体 [开发 ing]

## 生成 pkg-doctor.exe
- 进入 *Engine\Source\Programs* 目录
- mklink /D UnrealPakViewer /path/to/pkg-doctor/UnrealPakViewer
- 重新生成解决方案编译
