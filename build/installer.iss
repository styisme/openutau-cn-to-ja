; =====================================================================
; ChineseToJapanesePhonemizer - Inno Setup 安装脚本
; 版本：v2.1.7
; 用途：把 MyZHtoJAPlugin.dll 安装到 OpenUtau 的 Plugins 目录
;       安装完成后可选自动启动 OpenUtau
; =====================================================================

#define MyAppName "ChineseToJapanesePhonemizer"
#define MyAppVersion "2.1.7"
#define MyAppPublisher "Deepseek"
#define MyAppURL "https://github.com/X-starRelight/openutau-cn-to-ja"

[Setup]
AppId={{8A3F5B21-9D4E-4C7A-B6F2-1E8D3C9A5F70}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={code:GetDefaultDir}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\dist
OutputBaseFilename=MyZHtoJAPlugin-v{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayName={#MyAppName}
LicenseFile=..\LICENSE
; SetupIconFile=icon.ico

[Languages]
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; 主 DLL（编译产物）
Source: "..\src\ChineseToJapanesePhonemizer\bin\Release\net10.0\MyZHtoJAPlugin.dll"; DestDir: "{app}"; Flags: ignoreversion

; 默认 YAML 配置模板（不覆盖用户已存在的）
Source: "..\src\ChineseToJapanesePhonemizer\zh2ja.yaml"; DestDir: "{app}"; Flags: onlyifdoesntexist

; 说明文档
Source: "..\docs\使用说明.md"; DestDir: "{app}"; Flags: ignoreversion

[Run]
; 安装完成后勾选启动 OpenUtau（仅在找到 OpenUtau.exe 时显示）
Filename: "{code:GetOpenUtauPath}"; Description: "启动 OpenUtau"; Flags: postinstall nowait skipifsilent; Check: HasOpenUtau

; 安装完成后勾选查看说明文档
Filename: "{app}\使用说明.md"; Description: "查看使用说明"; Flags: shellexec postinstall skipifsilent

[Code]
var
  OpenUtauPath: String;
  OpenUtauFound: Boolean;

// ---------------------------------------------------------------
// 自动探测 OpenUtau 的 Plugins 目录
// ---------------------------------------------------------------
function GetDefaultDir(Param: String): String;
var
  DocsDir: String;
begin
  // 1. 优先用 <文档目录>\OpenUtau\Plugins\
  DocsDir := ExpandConstant('{userdocs}\OpenUtau\Plugins');
  if DirExists(DocsDir) then
  begin
    Result := DocsDir;
    exit;
  end;

  // 2. 检查 <文档目录>\OpenUtau（还没建 Plugins 的情况）
  DocsDir := ExpandConstant('{userdocs}\OpenUtau');
  if DirExists(DocsDir) then
  begin
    Result := DocsDir + '\Plugins';
    exit;
  end;

  // 3. 找不到就默认给一个
  Result := ExpandConstant('{userdocs}\OpenUtau\Plugins');
end;

// ---------------------------------------------------------------
// 探测 OpenUtau.exe 的常见位置
// ---------------------------------------------------------------
function FindOpenUtau(): String;
var
  Candidates: array[0..11] of String;
  I: Integer;
begin
  Result := '';

  Candidates[0]  := ExpandConstant('{userdocs}\OpenUtau\OpenUtau.exe');
  Candidates[1]  := ExpandConstant('{userdocs}\OpenUtau-win-x64\OpenUtau.exe');
  Candidates[2]  := ExpandConstant('{userdocs}\OpenUtau-win-x64 (1)\OpenUtau.exe');
  Candidates[3]  := ExpandConstant('{userdocs}\OpenUtau\App\OpenUtau.exe');
  Candidates[4]  := ExpandConstant('{userdesktop}\OpenUtau\OpenUtau.exe');
  Candidates[5]  := ExpandConstant('{userdesktop}\OpenUtau-win-x64\OpenUtau.exe');
  Candidates[6]  := ExpandConstant('{pf}\OpenUtau\OpenUtau.exe');
  Candidates[7]  := ExpandConstant('{pf32}\OpenUtau\OpenUtau.exe');
  Candidates[8]  := ExpandConstant('{sd}\OpenUtau\OpenUtau.exe');
  Candidates[9]  := ExpandConstant('{sd}\OpenUtau-win-x64\OpenUtau.exe');
  Candidates[10] := ExpandConstant('{userappdata}\OpenUtau\OpenUtau.exe');
  Candidates[11] := ExpandConstant('{localappdata}\OpenUtau\OpenUtau.exe');

  for I := 0 to 11 do
  begin
    if FileExists(Candidates[I]) then
    begin
      Result := Candidates[I];
      exit;
    end;
  end;
end;

// ---------------------------------------------------------------
// 安装前检查 + 探测 OpenUtau
// ---------------------------------------------------------------
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // 探测 OpenUtau 路径
  OpenUtauPath := FindOpenUtau();
  OpenUtauFound := (OpenUtauPath <> '');

  // 如果 OpenUtau 正在运行，友好提醒
  if Exec('cmd.exe', '/C tasklist /FI "IMAGENAME eq OpenUtau.exe" /NH | find /I "OpenUtau.exe"',
          '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
    begin
      if MsgBox('检测到 OpenUtau 正在运行。' + #13#10 + #13#10 +
                '为了避免 DLL 被锁定，建议先完全关闭 OpenUtau 再安装。' + #13#10 + #13#10 +
                '是否继续安装？',
                mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
        exit;
      end;
    end;
  end;
end;

// ---------------------------------------------------------------
// 供 [Run] 段调用：返回 OpenUtau.exe 完整路径
// ---------------------------------------------------------------
function GetOpenUtauPath(Param: String): String;
begin
  Result := OpenUtauPath;
end;

// ---------------------------------------------------------------
// 供 [Run] 段调用：是否找到 OpenUtau
// ---------------------------------------------------------------
function HasOpenUtau(): Boolean;
begin
  Result := OpenUtauFound;
end;

// ---------------------------------------------------------------
// 安装完成提示
// ---------------------------------------------------------------
procedure CurStepChanged(CurStep: TSetupStep);
var
  Msg: String;
begin
  if CurStep = ssPostInstall then
  begin
    Msg := '安装完成！' + #13#10 + #13#10 +
           '请启动 OpenUtau，在音轨设置里把音素器切换为 "ZH to JA"。' + #13#10 + #13#10 +
           '提示：更换 DLL 或修改 zh2ja.yaml 后，记得清空 <文档目录>\OpenUtau\Cache\ 再渲染。';

    if not OpenUtauFound then
    begin
      Msg := Msg + #13#10 + #13#10 +
             '（未自动找到 OpenUtau.exe，请手动启动 OpenUtau）';
    end;

    MsgBox(Msg, mbInformation, MB_OK);
  end;
end;