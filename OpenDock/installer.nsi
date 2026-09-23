; ==============================================================================
; OpenDock 6.0.0 - Nullsoft Scriptable Install System (NSIS) Script
; ==============================================================================

!define PRODUCT_NAME "OpenDock"
!define PRODUCT_VERSION "6.0.0"
!define PRODUCT_PUBLISHER "omerdev"
!define PRODUCT_WEB_SITE "https://github.com/bruhgit/OpenDock"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\OpenDock.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"

SetCompressor /SOLID lzma
RequestExecutionLevel user

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "bin\Release\OpenDock-Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\OpenDock"
InstallDirRegKey HKCU "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

Section "MainSection" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  File /r "bin\Release\net9.0-windows10.0.17763.0\*.*"

  CreateDirectory "$SMPROGRAMS\OpenDock"
  CreateShortcut "$SMPROGRAMS\OpenDock\OpenDock.lnk" "$INSTDIR\OpenDock.exe"
  CreateShortcut "$DESKTOP\OpenDock.lnk" "$INSTDIR\OpenDock.exe"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKCU "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\OpenDock.exe"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\OpenDock.exe"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd

Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "$(^Name) successfully removed from your computer."
FunctionEnd

Section Uninstall
  Delete "$SMPROGRAMS\OpenDock\OpenDock.lnk"
  RMDir "$SMPROGRAMS\OpenDock"
  Delete "$DESKTOP\OpenDock.lnk"

  RMDir /r "$INSTDIR"

  DeleteRegKey HKCU "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKCU "${PRODUCT_DIR_REGKEY}"
  SetAutoClose true
SectionEnd
