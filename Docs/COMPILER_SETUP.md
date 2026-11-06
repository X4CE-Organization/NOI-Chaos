# C++14 编译器配置

《NOI 乱斗》的代码编辑器需要调用本机 C++14 编译器。项目不会把编译器打包进游戏。

## 检测规则

- macOS 优先查找 `/usr/bin/clang++` 和 Xcode Command Line Tools 中的 `clang++`。
- Windows 从系统 `PATH` 中查找 `g++.exe` 或 `clang++.exe`。
- 编译参数为 `-std=c++14 -O2 -pipe`。
- 游戏会生成一个临时 `bits/stdc++.h` 兼容头，因此万能头和普通标准头都能使用。
- 编译器会执行玩家提交的代码，只运行来源可信的代码。

找不到编译器时，编辑器仍可查看题库内的原始模板和邻座代码，但只能对未修改的原版代码使用离线兼容判定。

## macOS

### 1. 安装 Xcode Command Line Tools

打开终端执行：

```bash
xcode-select --install
```

如果已经安装，执行下面的命令应能返回 Command Line Tools 的路径：

```bash
xcode-select -p
```

典型输出：

```text
/Library/Developer/CommandLineTools
```

### 2. 验证 C++14 编译器

```bash
clang++ --version
```

再执行一次最小编译测试：

```bash
printf '#include <iostream>\nint main(){int a,b;std::cin>>a>>b;std::cout<<a+b;}\n' \
  | clang++ -std=c++14 -x c++ - -o /tmp/noi-compiler-check
/tmp/noi-compiler-check <<< "1 2"
```

正确输出应为：

```text
3
```

### 3. 启动游戏

安装完成后重新启动《NOI 乱斗》。进入做题终端，左下角应显示：

```text
编译器：已就绪
```

## Windows

推荐使用 MSYS2 的 **UCRT64 MinGW-w64 GCC**。

### 1. 安装 MSYS2

从 [msys2.org](https://www.msys2.org/) 安装 MSYS2，完成后打开 **MSYS2 UCRT64** 终端。

### 2. 安装 GCC

```bash
pacman -Syu
```

如果终端提示关闭并重新打开，按提示完成后再执行：

```bash
pacman -S --needed mingw-w64-ucrt-x86_64-gcc
```

默认安装位置通常是：

```text
C:\msys64\ucrt64\bin
```

### 3. 把 GCC 加入 PATH

按 `Win + R`，输入 `sysdm.cpl`，打开：

```text
高级 → 环境变量 → 用户变量 Path → 编辑 → 新建
```

加入：

```text
C:\msys64\ucrt64\bin
```

保存后关闭所有终端，再重新打开 PowerShell。

### 4. 验证

```powershell
where.exe g++
g++ --version
```

`where.exe g++` 应返回类似：

```text
C:\msys64\ucrt64\bin\g++.exe
```

再验证 C++14：

```powershell
"#include <iostream>`nint main(){int a,b;std::cin>>a>>b;std::cout<<a+b;}" |
  Set-Content -Encoding ascii "$env:TEMP\noi-check.cpp"

g++ -std=c++14 "$env:TEMP\noi-check.cpp" -o "$env:TEMP\noi-check.exe"
"1 2" | & "$env:TEMP\noi-check.exe"
```

正确输出为 `3`。

### 5. 启动游戏

重新启动《NOI 乱斗》。做题终端中的编译器状态应变为“已就绪”。

## 使用 LLVM Clang 的替代方案

Windows 也可以安装 LLVM Clang：

```powershell
winget install --id LLVM.LLVM -e
```

然后确认以下目录在 `PATH` 中：

```text
C:\Program Files\LLVM\bin
```

验证命令：

```powershell
where.exe clang++
clang++ --version
```

游戏支持 `clang++.exe`，不需要额外修改配置。

## 常见问题

### 游戏仍显示“编译器：未找到”

1. 在终端执行 `where.exe g++` 或 `where.exe clang++`。
2. 确认安装目录已经加入用户 `PATH`。
3. 修改环境变量后重新启动游戏，不能只关闭游戏窗口。
4. 如果刚安装 MSYS2，重新登录 Windows 后再试。

### Visual Studio 的 `cl.exe` 能用吗

当前版本不自动调用 Visual Studio 的 `cl.exe`。请使用 MinGW-w64 的 `g++.exe` 或 LLVM 的 `clang++.exe`。

### 为什么使用 `bits/stdc++.h` 也能编译

游戏会把兼容头写入本次编译使用的临时目录。MSYS2 GCC 本身也包含该头文件。

### 编译失败会显示在哪里

编译错误、运行超时和每个隐藏测试点的 `AC / WA / TLE` 会显示在代码编辑器下方的状态区。

Copyright © 2026 X4CE.
