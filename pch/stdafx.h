// stdafx.h : include file for standard system include files,
//  or project specific include files that are used frequently, but
//      are changed infrequently
//

#pragma once
#pragma warning(disable: 4503 4786)

// Exclude rarely-used stuff from Windows headers
#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN            
#endif
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN            
#endif

#include "targetver.h"

#define _CRT_SECURE_NO_WARNINGS
// désactive le masquage MFC de certains messages d'avertissement courants et par ailleurs souvent ignorés
#define _AFX_ALL_WARNINGS

#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions
#include <afxdisp.h>        // MFC Automation

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxdtctl.h>           // Prise en charge MFC pour les contrôles communs Internet Explorer 4
#endif
#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h>             // Prise en charge des MFC pour les contrôles communs Windows
#endif // _AFX_NO_AFXCMN_SUPPORT

#include <afxpriv.h>		// MFC support for Windows 95 Common Controls
//#include <afxcontrolbars.h>     // prise en charge des MFC pour les rubans et les barres de contrôles


#ifdef _UNICODE
#if defined _M_IX86
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif
#endif
