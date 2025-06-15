@echo off
echo 正在发布 Mzl.Video.Process 应用程序...

:: 清理之前的发布
if exist "publish" rmdir /s /q "publish"

:: 发布应用程序
dotnet publish -c Release -r win-x64 --self-contained false -o "publish"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo 发布成功！
    echo 输出目录: %CD%\publish
    echo.
    echo 注意事项：
    echo 1. 请确保目标机器安装了 .NET 8.0 Runtime
    echo 2. 请将 ffmpeg.exe, ffplay.exe, ffprobe.exe 复制到应用程序目录
    echo 3. 或者修改代码中的 FFmpeg 路径配置
    echo.
    pause
) else (
    echo 发布失败！
    pause
)
