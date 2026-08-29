#ifndef MyAppVersion
  #define MyAppVersion "0.21.1"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish\installer-input"
#endif
#ifndef OutputDir
  #define OutputDir "..\publish\installer"
#endif

#define MyAppName "Splitaria"
#define MyAppPublisher "Marcos Luciano Tagliari Junior"
#define MyAppExeName "Splitaria.exe"
#define MyAppURL "https://github.com/RY0UK3N/Splitaria"

[Setup]
AppId={{A0AE565A-7FEB-4EFA-98CC-80D891748396}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=Splitaria-Setup-{#MyAppVersion}-win-x64
SetupIconFile=..\assets\brand\Splitaria.ico
UninstallDisplayIcon={app}\Splitaria-{#MyAppVersion}.ico
LicenseFile=..\LICENSE
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
MinVersion=10.0.17763
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Instalador do Splitaria
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (c) 2026 Marcos Luciano Tagliari Junior

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\assets\brand\Splitaria.ico"; DestDir: "{app}"; DestName: "Splitaria-{#MyAppVersion}.ico"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Splitaria"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Splitaria-{#MyAppVersion}.ico"
Name: "{autodesktop}\Splitaria"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\Splitaria-{#MyAppVersion}.ico"; Tasks: desktopicon

[InstallDelete]
Type: filesandordirs; Name: "{autoprograms}\Splitaria"
Type: files; Name: "{app}\Splitaria-*.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o Splitaria"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#MyAppExeName}"; Flags: nowait skipifnotsilent; Check: IsUpdateMode

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
const
  SHCNE_ASSOCCHANGED = $08000000;
  SHCNF_IDLIST = $0000;

procedure SHChangeNotify(wEventId: LongWord; uFlags: LongWord; dwItem1: Integer; dwItem2: Integer);
  external 'SHChangeNotify@shell32.dll stdcall';

function IsUpdateMode: Boolean;
var
  Index: Integer;
begin
  Result := False;
  for Index := 1 to ParamCount do
    if CompareText(ParamStr(Index), '/UPDATE') = 0 then
    begin
      Result := True;
      Exit;
    end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
end;
