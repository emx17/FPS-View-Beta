[Setup]
AppId={{5D0E1C9B-1234-4F2A-A123-BCD987654321}
AppName=emx17_FPSViewer
AppVersion=2.0.0
AppPublisher=emx17
AppPublisherURL=https://github.com/emx17
DefaultDirName={autopf}\emx17_FPSViewer
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=emx17_FPSViewer_Setup
OutputBaseFilename=emx17FPSViewer_SetupBeta_v2
SetupIconFile=app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\emx17_FPSViewer"; Filename: "{app}\FPSOverlay.exe"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\emx17_FPSViewer"; Filename: "{app}\FPSOverlay.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\FPSOverlay.exe"; Description: "{cm:LaunchProgram,emx17_FPSViewer}"; Flags: nowait postinstall skipifsilent shellexec

[Code]
var
  DownloadPage: TDownloadWizardPage;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  if CurPageID = wpReady then
  begin
    if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
    begin
      DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
      DownloadPage.Clear;
      
      // Net 8.0 downlaod link
      DownloadPage.Add('https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe', 'dotnet_desktop.exe', '');
      
      DownloadPage.Show;
      try
        try
          DownloadPage.Download;
          Exec(ExpandConstant('{tmp}\dotnet_desktop.exe'), '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
        except
          MsgBox('Requirements could not be downloaded (.NET 8). Please check your internet connection and firewall settings.', mbError, MB_OK);
          Result := False;
        end;
      finally
        DownloadPage.Hide;
      end;
    end;
  end;
end;
