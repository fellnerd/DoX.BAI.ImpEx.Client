
[Setup]
;AppId={{1552F929-F460-46FE-9CAE-E4B1C2BEF349}
AppName=BAI-Client
AppVerName=BAI-Client
AppPublisher=Dorner Electronic
AppPublisherURL=http://www.dorner.at
AppSupportURL=http://www.dorner.at
AppUpdatesURL=http://www.dorner.at
DefaultDirName=C:\DornerLink\FileClient
DefaultGroupName=Dorner
DisableProgramGroupPage=yes
OutputBaseFilename=BAI-Client Setup
OutputDir=.
;SetupIconFile=DoX.BAI.ImpEx.Client.GUI\Resources\handshake.ico
Compression=lzma
SolidCompression=yes
AppCopyright=Copyright (C) 2011 Dorner Electronic
BackColor=$00FF00
BackColor2=$000000
UninstallFilesDir={app}\{code:GetClientname}\uninst
UninstallDisplayName=BAI-Client [{code:GetClientname}]
UninstallDisplayIcon={app}\{code:GetClientname}\DoX.BAI.ImpEx.Client.GUI.exe
WizardImageFile=Resources\setup_large.bmp
WizardSmallImageFile=Resources\setup_small.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
; --- Unterverzeichnis mit dem Clientnamen erzeugen
Name: {app}\{code:GetClientname}

[Files]
Source: "DoX.BAI.ImpEx.Client.ClientNotificationServer.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.BAI.ImpEx.Client.ConsoleHost.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.BAI.ImpEx.Client.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.BAI.ImpEx.Client.GUI.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.BAI.ImpEx.Client.ServiceHost.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.BAI.ImpEx.Client.ServiceHost.exe.config"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.BAI.ImpEx.Client.Updater.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.BAI.ImpEx.Shared.Client.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.FX.API.ClientServer.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.FX.API.Util.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.FX.Util.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion
Source: "DoX.FX.Contracts.Notification.Wcf.???"; DestDir: "{app}\{code:GetClientname}"; Flags: ignoreversion

[Icons]
Name: "{group}\BAI-Client [{code:GetClientname}]\Management GUI [{code:GetClientname}]"; Filename: "{app}\{code:GetClientname}\DoX.BAI.ImpEx.Client.GUI.exe"; IconFilename: "{app}\{code:GetClientname}\DoX.BAI.ImpEx.Client.GUI.exe"
Name: "{group}\BAI-Client [{code:GetClientname}]\Uninstall [{code:GetClientname}]"; Filename: "{uninstallexe}"

[Run]
Filename: "{win}\Microsoft.NET\Framework\v4.0.30319\installutil.exe"; Parameters: "/name=""{code:GetClientname}"" /TargetDir=""{app}\{code:GetClientname}"" /Account=localsystem DoX.BAI.ImpEx.Client.ServiceHost.exe"; WorkingDir: "{app}\{code:GetClientname}"

[UninstallRun]
Filename: "{win}\Microsoft.NET\Framework\v4.0.30319\installutil.exe"; Parameters: "/name=""{code:GetClientname}"" DoX.BAI.ImpEx.Client.ServiceHost.exe /u"; WorkingDir: "{app}\{code:GetClientname}"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\{code:GetClientname}"

[Code]
const
  STANDARD_RIGHTS_REQUIRED = $F0000;
  SC_MANAGER_CONNECT = $0001;
  SC_MANAGER_CREATE_SERVICE = $0002;
  SC_MANAGER_ENUMERATE_SERVICE = $0004;
  SC_MANAGER_LOCK = $0008;
  SC_MANAGER_QUERY_LOCK_STATUS = $0010;
  SC_MANAGER_MODIFY_BOOT_CONFIG = $0020;
  SC_MANAGER_ALL_ACCESS  = (STANDARD_RIGHTS_REQUIRED + SC_MANAGER_CONNECT + SC_MANAGER_CREATE_SERVICE + SC_MANAGER_ENUMERATE_SERVICE + SC_MANAGER_LOCK + SC_MANAGER_QUERY_LOCK_STATUS + SC_MANAGER_MODIFY_BOOT_CONFIG);

  SERVICE_QUERY_CONFIG = $0001;
  SERVICE_CHANGE_CONFIG = $0002;
  SERVICE_QUERY_STATUS = $0004;
  SERVICE_ENUMERATE_DEPENDENTS = $0008;
  SERVICE_START= $0010;
  SERVICE_STOP= $0020;
  SERVICE_PAUSE_CONTINUE = $0040;
  SERVICE_INTERROGATE = $0080;
  SERVICE_USER_DEFINED_CONTROL = $0100;
  SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED + SERVICE_QUERY_CONFIG + SERVICE_CHANGE_CONFIG + SERVICE_QUERY_STATUS + SERVICE_ENUMERATE_DEPENDENTS + SERVICE_START + SERVICE_STOP + SERVICE_PAUSE_CONTINUE + SERVICE_INTERROGATE + SERVICE_USER_DEFINED_CONTROL);

var
  UserPage: TInputQueryWizardPage;

function OpenSCManager(lpMachineName: string; lpDatabaseName: string; dwDesiredAccess: Longword): Longword;
external 'OpenSCManagerA@advapi32.dll stdcall';

function OpenService(hSCManager: Longword; lpServiceName: string; dwDesiredAccess: Longword): Longword;
external 'OpenServiceA@advapi32.dll stdcall';

procedure InitializeWizard;
begin
  UserPage := CreateInputQueryPage(wpWelcome, 'Clientname', 'Description of this instance', 'Enter a short description of this instance (e.g. Dispo). Attention: This is NOT the Username that is used to connect to the BAI-Server, which you can enter in a later step. This clientname is only used to distinguish multiple installations of this software on the same computer.');
  UserPage.Add('Clientname:', False);
  UserPage.Values[0] := 'Default';
end;

function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1.4322'     .NET Framework 1.1
//    'v2.0.50727'    .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
  key: string;
  install, serviceCount: cardinal;
  success: boolean;
begin
  key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;
  // .NET 3.0 uses value InstallSuccess in subkey Setup
  if Pos('v3.0', version) = 1 then
  begin
    success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
  end
  else
  begin
    success := RegQueryDWordValue(HKLM, key, 'Install', install);
  end;
  
  // .NET 4.0 uses value Servicing instead of SP
  if Pos('v4', version) = 1 then
  begin
    success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
  end 
  else
  begin
    success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
  end;
  result := success and (install = 1) and (serviceCount >= service);
end;

function InitializeSetup(): Boolean;
begin
  if not IsDotNetDetected('v4\Full', 0) then
  begin
    MsgBox('Microsoft .NET Framework 4.0 is required to run this software.'#13#13'Please use Windows Update to install this version,'#13'and re-run the setup program.', mbInformation, MB_OK);
    result := false;
  end 
  else
    result := true;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  SCMHandle: Longword;
  ServiceHandle: Longword;
  ServiceName: string;
begin
  if CurPageID = UserPage.ID then
  begin
    if UserPage.Values[0] = '' then
    begin
      MsgBox('You must enter a Clientname!', mbError, MB_OK);
      Result := False;
    end
    else
    begin
	  ServiceName := 'BAI-Client [' + UserPage.Values[0] + ']';
      SCMHandle := OpenSCManager('', '', SC_MANAGER_ALL_ACCESS);
      ServiceHandle := OpenService(SCMHandle, ServiceName, SERVICE_ALL_ACCESS);
      if ServiceHandle = 0 then
      begin
        Result := True;
      end
      else
      begin
        MsgBox('Service ' + ServiceName + ' already exists. Please change Clientname.', mbError, MB_OK);
        Result := False;
      end
    end
  end
  else
    Result := True;
end;

function GetClientname(Param: String): String;
begin
  Result := UserPage.Values[0];
end;
