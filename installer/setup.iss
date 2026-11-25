; DermaCare Clinic Management System - Inno Setup Script
[Setup]
AppName=DermaCare Clinic Management
AppVersion=1.0.0
AppPublisher=DermaCare
DefaultDirName={autopf}\DermaCare
DefaultGroupName=DermaCare Clinic
OutputDir=output
OutputBaseFilename=DermaCareSetup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\src\DermaCare.UI\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DermaCare Clinic"; Filename: "{app}\DermaCare.UI.exe"
Name: "{autodesktop}\DermaCare Clinic"; Filename: "{app}\DermaCare.UI.exe"

[Run]
Filename: "{app}\DermaCare.UI.exe"; Description: "Launch DermaCare Clinic"; Flags: nowait postinstall skipifsilent
