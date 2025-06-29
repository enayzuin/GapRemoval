@echo off
echo ==========================================
echo   Gap Removal - Build Profissional
echo ==========================================
echo.

REM Verificar se .NET está instalado
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERRO: .NET 8.0 SDK nao encontrado!
    echo Instale de: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Verificar se Inno Setup está instalado
if not exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    echo ERRO: Inno Setup 6 nao encontrado!
    echo Instale de: https://jrsoftware.org/isdl.php
    pause
    exit /b 1
)

echo Configuracao: Release
echo Runtime: win-x64
echo.

echo ==========================================
echo 1. Limpando build anterior...
echo ==========================================
dotnet clean GapRemovalApp --configuration Release
if exist "GapRemovalApp\Output" rmdir /s /q "GapRemovalApp\Output"
if exist "GapRemovalApp\publish" rmdir /s /q "GapRemovalApp\publish"

echo.
echo ==========================================
echo 2. Executando build completo...
echo ==========================================
dotnet build GapRemovalApp --configuration Release --runtime win-x64 --self-contained

if errorlevel 1 (
    echo.
    echo ERRO: Falha no build!
    pause
    exit /b 1
)

echo.
echo ==========================================
echo  BUILD CONCLUIDO COM SUCESSO!
echo ==========================================
echo Instalador: GapRemovalApp\Output\GapRemovalInstaller.exe
echo.
pause 