@echo off
setlocal

@REM To Build mgfxc:
@REM cd MonoGame\Tools\MonoGame.Effect.Compiler
@REM dotnet build .\MonoGame.Effect.Compiler.csproj -c Release

SET MGFXC="..\..\..\..\..\Artifacts\MonoGame.Effect.Compiler\Release\mgfxc.exe"

@for /f %%f IN ('dir /b *.fx') do (

  call %MGFXC% %%~nf.fx %%~nf.ogl.mgfxo

  call %MGFXC% %%~nf.fx %%~nf.dx11.mgfxo /Profile:DirectX_11

  call %MGFXC% %%~nf.fx %%~nf.metal.mgfxo /Profile:Metal
)

endlocal
pause
