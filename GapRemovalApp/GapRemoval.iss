[Setup]
AppName=Gap Removal
AppVersion=1.0
DefaultDirName={pf}\GapRemoval
DefaultGroupName=Gap Removal
OutputBaseFilename=GapRemovalInstaller
OutputDir=Output
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=gap_removal_icon.ico
DisableProgramGroupPage=yes
PrivilegesRequired=admin

[Files]
; Inclui todos os arquivos da pasta publish
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
; Inclui explicitamente o ícone para os atalhos
Source: "gap_removal_icon.ico"; DestDir: "{app}"; Flags: ignoreversion
; Inclui binários FFmpeg
Source: "ffmpeg\ffmpeg.exe"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion
Source: "ffmpeg\ffplay.exe"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion
Source: "ffmpeg\ffprobe.exe"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion
; Inclui binários VLC (apenas os essenciais)
Source: "vlc\libvlc.dll"; DestDir: "{app}\vlc"; Flags: ignoreversion
Source: "vlc\libvlccore.dll"; DestDir: "{app}\vlc"; Flags: ignoreversion
Source: "vlc\plugins\*"; DestDir: "{app}\vlc\plugins"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
; Menu iniciar
Name: "{group}\Gap Removal"; Filename: "{app}\GapRemovalApp.exe"; IconFilename: "{app}\gap_removal_icon.ico"
; Atalho na área de trabalho
Name: "{commondesktop}\Gap Removal"; Filename: "{app}\GapRemovalApp.exe"; IconFilename: "{app}\gap_removal_icon.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Opções adicionais:"

[Run]
Filename: "{app}\GapRemovalApp.exe"; Description: "Executar Gap Removal agora"; Flags: nowait postinstall skipifsilent