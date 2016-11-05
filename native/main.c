#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commctrl.h>

#define IDC_FILEPATH_EDIT 1001
#define IDC_FILEPATH_LABEL 1002
#define IDC_MAIN_STATUS 1003
#define EDIT_X_START 75
#define EDIT_HEIGHT 25
#define LABEL_WIDTH EDIT_X_START
#define LABEL_HEIGHT 25

static const char* g_szClassName = "wfgloggerclass";

static const LPSTR get_edit_text(HWND hwnd)
{
	int len = GetWindowTextLength(GetDlgItem(hwnd, IDC_FILEPATH_EDIT));
	if (len > 0)
	{
		LPSTR buf;
		buf = GlobalAlloc(GPTR, len + 1);
		GetDlgItemText(hwnd, IDC_FILEPATH_EDIT, buf, len + 1);
		//MessageBox(hwnd, buf, "Text", 0);
		//GlobalFree((HANDLE)buf);
	}
}

static const LPSTR get_last_errmsg()
{
	LPSTR buf = GlobalAlloc(GPTR, 256);
	FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM, NULL, GetLastError(),
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), buf, 256, NULL);
	return buf;
}

static void create_controls(HWND hwnd)
{
	HFONT hfDefault;
	HWND hEdit;
	HWND hLabel;
	HWND hStatus;

	hStatus = CreateWindowEx(0, STATUSCLASSNAME, NULL,
		WS_CHILD | WS_VISIBLE, 0, 0, 0, 0,
		hwnd, (HMENU)IDC_MAIN_STATUS, GetModuleHandle(NULL), NULL);

	hLabel = CreateWindowEx(0, "static", "ST_U",
		WS_CHILD | WS_VISIBLE | WS_TABSTOP| SS_CENTER, 0, 2, LABEL_WIDTH, LABEL_HEIGHT,
		hwnd, (HMENU)(NULL), GetModuleHandle(NULL), NULL);
	SetWindowText(hLabel, "File Path:");
	hfDefault = GetStockObject(DEFAULT_PALETTE);
	//SendMessage(hEdit, SETBACKGROUND, (WPARAM)hfDefault, MAKELPARAM(FALSE, 0));

	hEdit = CreateWindowEx(WS_EX_CLIENTEDGE, "EDIT", "", WS_CHILD | WS_VISIBLE | ES_LEFT,
		EDIT_X_START, 0, 125, EDIT_HEIGHT, hwnd, (HMENU)IDC_FILEPATH_EDIT, GetModuleHandle(NULL), NULL);
	if (hEdit == NULL)
		MessageBox(hwnd, "Could not create edit box.", "Error", MB_OK | MB_ICONERROR);

	hfDefault = GetStockObject(DEFAULT_GUI_FONT);
	SendMessage(hEdit, WM_SETFONT, (WPARAM)hfDefault, MAKELPARAM(FALSE, 0));

	SetDlgItemText(hwnd, IDC_FILEPATH_EDIT, "This is a string");
}

static void handle_resize(HWND hwnd)
{
	HWND hControl;
	RECT rcClient;

	GetClientRect(hwnd, &rcClient);

	hControl = GetDlgItem(hwnd, IDC_FILEPATH_EDIT);
	SetWindowPos(hControl, NULL, EDIT_X_START, 0, rcClient.right, EDIT_HEIGHT, SWP_NOZORDER);

	// resize the status bar right away too. it will do itself sorta, sometimes, usually when hitting grip
	hControl = GetDlgItem(hwnd, IDC_MAIN_STATUS);
	SetWindowPos(hControl, NULL, 0, 0, rcClient.right, 0, SWP_NOZORDER);
}

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	switch (msg)
	{
	case WM_CTLCOLORSTATIC:
	{
		// STATIC labels have awful gray background by default
		HDC hdcStatic = (HDC)wParam;
		SetTextColor(hdcStatic, RGB(0, 0, 0));
		SetBkMode(hdcStatic, TRANSPARENT);
		return (LRESULT)GetStockObject(NULL_BRUSH);
	}
	case WM_CREATE:
	{
		create_controls(hwnd);
		break;
	}
	case WM_SIZE:
	{
		handle_resize(hwnd);
		break;
	}
	case WM_CLOSE:
	{
		//DWORD err = GetLastError();
		DestroyWindow(hwnd);
		break;
	}
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(hwnd, msg, wParam, lParam);
	}
	return 0;
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
	LPSTR lpCmdLine, int nCmdShow)
{
	InitCommonControls();

	WNDCLASSEX wc;
	HWND hwnd;
	MSG Msg;
	wc.cbSize = sizeof(WNDCLASSEX);
	wc.style = 0;
	wc.lpfnWndProc = WndProc;
	wc.cbClsExtra = 0;
	wc.cbWndExtra = 0;
	wc.hInstance = hInstance;
	wc.hIcon = LoadIcon(NULL, IDI_APPLICATION);
	wc.hCursor = LoadCursor(NULL, IDC_ARROW);
	wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
	wc.lpszMenuName = NULL;
	wc.lpszClassName = g_szClassName;
	wc.hIconSm = LoadIcon(NULL, IDI_APPLICATION);

	if (!RegisterClassEx(&wc))
	{
		MessageBox(NULL, "Window Registration Failed!", "Error!",
			MB_ICONEXCLAMATION | MB_OK);
		return 0;
	}
	hwnd = CreateWindowEx(
		WS_EX_CLIENTEDGE,
		g_szClassName,
		"Foreground Window Logger",
		WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, CW_USEDEFAULT, 300, 120,
		NULL, NULL, hInstance, NULL);

	if (hwnd == NULL)
	{
		MessageBox(NULL, "Window Creation Failed!", "Error!",
			MB_ICONEXCLAMATION | MB_OK);
		return 0;
	}

	ShowWindow(hwnd, nCmdShow);
	UpdateWindow(hwnd);

	// we must have a message loop to receive hook events
	while (GetMessage(&Msg, NULL, 0, 0) > 0)
	{
		TranslateMessage(&Msg);
		DispatchMessage(&Msg);
	}
	return Msg.wParam;
}
