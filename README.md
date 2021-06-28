# pkg-doctor
包体医生，Unity 及 Unreal 游戏包体优化工具。

# 下载发布版本
https://github.com/taptap/pkg-doctor/releases

# 分析 Unity 游戏

## 生成 pkg-doctor.exe
- 安装 [FBX SDK 2020.1](https://www.autodesk.com/content/dam/autodesk/www/adn/fbx/2020-1/fbx20201_fbxsdk_vs2017_win.exe)
- 打开 AssetStudio\AssetStudio.sln
- 选择 Release 模式
- 生成 AssetStudio\AssetStudioGUI\bin\Release\pkg-doctor.exe

## 分析 Unity 游戏 apk 或 ipa

> pkg-unity.bat /path/to/game.apk
> pkg-unity.bat /path/to/game.

## 分析 Unity 游戏资源文件夹

> pkg-unity.bat /path/to/game/data/

# 分析 Unreal 游戏

## 生成 pkg-doctor.exe
- 进入 *Engine\Source\Programs* 目录
- mklink /D UnrealPakViewer /path/to/pkg-doctor/UnrealPakViewer
- 重新生成解决方案编译

## 分析 Unreal 游戏 apk 或 ipa

## 分析 Unreal 游戏资源文件夹

