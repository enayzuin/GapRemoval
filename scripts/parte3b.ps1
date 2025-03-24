# Parte 3B do setup: Instalação de dependências no projeto único GapRemovalApp
Write-Host '`n📦 Instalando pacotes NuGet no projeto GapRemovalApp...'

# Pacotes para FFmpeg e manipulação de áudio
dotnet add ./GapRemovalApp/GapRemovalApp.csproj package Xabe.FFmpeg
dotnet add ./GapRemovalApp/GapRemovalApp.csproj package NAudio

# MVVM Toolkit e player de vídeo
dotnet add ./GapRemovalApp/GapRemovalApp.csproj package CommunityToolkit.Mvvm
dotnet add ./GapRemovalApp/GapRemovalApp.csproj package LibVLCSharp.WPF

# Logging (opcional)
dotnet add ./GapRemovalApp/GapRemovalApp.csproj package Microsoft.Extensions.Logging

Write-Host '`n🔄 Restaurando e compilando a solução...'
dotnet restore
dotnet build

Write-Host '`n✅ Parte 3B concluída com sucesso!'