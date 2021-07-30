# FluentCore
![](https://img.shields.io/badge/license-MIT-green)
![](https://img.shields.io/github/repo-size/Xcube-Studio/FluentCore)
![](https://img.shields.io/github/stars/Xcube-Studio/FluentCore)
![](https://img.shields.io/github/commit-activity/y/Xcube-Studio/FluentCore)

一个高效的模块化Minecraft启动器核心
---------------------------------------------------------

### 简介
一个由C#编写的跨平台模块化Minecraft启动核心

+ 支持跨平台 (Windows/Linux上的调试已通过)
+ Minecraft游戏核心的查找
+ Minecraft的参数生成、启动封装 (支持启动最近更新的Forge-1.17.1)
+ 对Offline/Yggdrasil验证的支持
+ 支持AuthlibInjector并搭配Yggdrasil验证器实现外置登录
+ 支持多线程补全Assets、Libraries、游戏主Jar
+ 支持[Bmclapi](https://bmclapidoc.bangbang93.com/)下载源的调用
  + 在此感谢bangbang93提供镜像站服务 如果您支持我们 可以 [赞助Bmclapi](https://afdian.net/@bangbang93)

本项目依赖及运行环境:

+ [.NET 5 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0)
+ [Newtonsoft.Json](https://www.newtonsoft.com/json)

> 您发现了我们项目中的bug? 对我们的项目中有不满意的地方? <br/>
> 或是您愿意加入我们，与我们一同开发？ <br/>
> 联系: Xcube Studio 工作室 QQ群:597704076 / a-275@qq.com (作者本人邮箱)

### 启动核心的简单调用

#### 初始化启动核心并启动游戏

首先添加引用

``` c#
using FluentCore.Model.Auth;
using FluentCore.Model.Launch;
using FluentCore.Service.Component.Launch;
using FluentCore.Wrapper;
```

添加以下代码到方法中

``` c#
var coreLocator = new CoreLocator(path);//初始化一个核心定位器，path:minecarft游戏目录

LaunchConfig launchConfig = new LaunchConfig//初始化一个游戏配置
{
  MoreBehindArgs = string.Empty,//可选的，游戏额外参数，如 --demo
  MoreFrontArgs = string.Empty,//可选的，JVM额外参数
  JavaPath = javaPath,//javaw.exe可执行文件路径
  MaximumMemory = 2048,//JVM最大内存
  NativesFolder = nativesFolder,//可选的，设置natives所在目录
  AuthDataModel = new AuthDataModel//验证信息
  {
    AccessToken = accessToken,//AccessToken令牌
    UserName = userName,//游戏内名称
    Uuid = guid//Guid类型，玩家uuid
  }
};

var launcher = new MinecraftLauncher(coreLocator, launchConfig);//初始化启动器
launcher.Launch(id);//启动游戏，id:要启动的游戏的id
```

>具体详细功能请转到wiki翻阅
