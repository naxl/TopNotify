#include "Teams.h"
#include <easyhook.h>
#include <mmsystem.h>
#include "Settings.h"

void Teams::SetupHooks() {
	HOOK_TRACE_INFO posHook = { NULL };

	NTSTATUS result = LhInstallHook(GetProcAddress(GetModuleHandle(TEXT("user32")), "SetWindowPos"), SetWindowPosHook, NULL, &posHook);

	if (!NT_SUCCESS(result)) {
		//Display Error Message
		MessageBoxW(NULL, L"Failed To Setup Hooks For Microsoft Teams, TopNotify Won't Intercept Teams Notifications", L"TopNotify", 0);
		return;
	}

	ULONG ACLEntries[1] = { 0 };
	LhSetExclusiveACL(ACLEntries, 1, &posHook);
}

BOOL Teams::SetWindowPosHook(HWND hWnd, HWND hWndInsertAfter, int x, int y, int cx, int cy, UINT uFlags) {

	//372PX Seems To Be The Standard Width Of Teams Notifications
	//So Check If It's 372PX To Determine Whether This Window Is A Notification Window
	if (cx == 372) {
		int overrideX = x;
		int overrideY = y;

		if (GlobalSettings::LoadedSettingsFile != nullptr) {
			if (GlobalSettings::LoadedSettingsFile->Location == NotifyLocation::TopLeft) {
				overrideX = 15;
				overrideY = 15;
			}
			else if (GlobalSettings::LoadedSettingsFile->Location == NotifyLocation::TopRight) {
				overrideY = 15;
			}
			else if (GlobalSettings::LoadedSettingsFile->Location == NotifyLocation::BottomLeft) {
				overrideX = 15;
			}
			else if (GlobalSettings::LoadedSettingsFile->Location == NotifyLocation::BottomRight) {
				//Do Nothing
			}
			else if (GlobalSettings::LoadedSettingsFile->Location == NotifyLocation::Custom) {
				overrideX = GlobalSettings::LoadedSettingsFile->CustomPositionX + 15; // Alignment Offset Of 15
				overrideY = GlobalSettings::LoadedSettingsFile->CustomPositionY;
			}
		}

		return SetWindowPos(hWnd, hWndInsertAfter, overrideX, overrideY, cx, cy, uFlags);
	}
	else {
		return SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);
	}
}