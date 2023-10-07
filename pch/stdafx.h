// stdafx.h : include file for standard system include files,
//  or project specific include files that are used frequently, but
//      are changed infrequently
//

#pragma once

//----------------------------------------------------------------------------- Warnings

#pragma warning(disable: 4503)  // "identifier": decorated name length exceeded, name was truncated
#pragma warning(disable: 4786)  // identifier was truncated in the debug information

// disable deprecation warnings
#define _SCL_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_NONSTDC_NO_WARNINGS

#define NO_WARN_MBCS_MFC_DEPRECATION
#define _AFX_ALL_WARNINGS

// Error with C++ 17 Standard
#define _SILENCE_CXX17_ITERATOR_BASE_CLASS_DEPRECATION_WARNING

//----------------------------------------------------------------------------- Memory Leak Detection

// Visual Leak Detector
#ifdef _VLD
#include <vld.h>
#endif

// CRT
#ifdef _DEBUG
#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>
#endif

//----------------------------------------------------------------------------- AFX

// Exclude rarely-used stuff from Windows headers
#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN
#endif
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include "targetver.h"

// Disable infamous 'min' and 'max' macros
// WARNING:
// - 'afxcontrolbars.h' use them -> include header before disabling them
// - opt-in by defining '_AFX_CONTROL_BARS' before including 'zou/pch/stdafx'
// Example:
//   #pragma once
//   #define _AFX_CONTROL_BARS
//   #include "zou/pch/stdafx.h"
#ifdef _AFX_CONTROL_BARS
#include <afxcontrolbars.h> // prise en charge des MFC pour les rubans et les barres de contrôles
#endif
#define NOMINMAX

#include <afxwin.h>  // MFC core and standard components
#include <afxext.h>  // MFC extensions
#include <afxdisp.h> // MFC Automation
#include <afxmt.h>   // MFC Multithreaded Extensions

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxdtctl.h> // Prise en charge MFC pour les contrôles communs Internet Explorer 4
#endif
#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h> // Prise en charge des MFC pour les contrôles communs Windows
#endif // _AFX_NO_AFXCMN_SUPPORT

#include <afxpriv.h> // MFC support for Windows 95 Common Controls

#ifdef _UNICODE
#if defined _M_IX86
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif
#endif

//----------------------------------------------------------------------------- STL

#define _SCL_SECURE_NO_WARNINGS

#include <string>

#include <array>
#include <list>
#include <map>
#include <queue>
#include <set>
#include <vector>
#include <unordered_map>
#include <unordered_set>
