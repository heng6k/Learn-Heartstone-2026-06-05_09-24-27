#include <Windows.h>
#include <cstdint>

namespace
{
    constexpr std::uintptr_t AccessibilityDestroyFaultOffset = 0x107B81;
    PVOID g_exceptionHandler = nullptr;

    LONG CALLBACK HandleUnityShutdownException(EXCEPTION_POINTERS* exceptionPointers)
    {
        if (exceptionPointers == nullptr || exceptionPointers->ExceptionRecord == nullptr)
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }

        const EXCEPTION_RECORD* record = exceptionPointers->ExceptionRecord;
        if (record->ExceptionCode != EXCEPTION_ACCESS_VIOLATION)
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }

        const HMODULE unityPlayer = GetModuleHandleW(L"UnityPlayer.dll");
        if (unityPlayer == nullptr)
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }

        const auto moduleBase = reinterpret_cast<std::uintptr_t>(unityPlayer);
        const auto faultAddress = reinterpret_cast<std::uintptr_t>(record->ExceptionAddress);
        if (faultAddress != moduleBase + AccessibilityDestroyFaultOffset)
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }

        TerminateProcess(GetCurrentProcess(), 0);

        // TerminateProcess is asynchronous for the current process. Never return
        // to the faulting instruction while the kernel finishes terminating the
        // remaining Unity worker threads.
        for (;;)
        {
            Sleep(INFINITE);
        }
    }
}

extern "C" __declspec(dllexport) int __cdecl InstallExitGuard()
{
    if (g_exceptionHandler != nullptr)
    {
        return 1;
    }

    HMODULE pinnedModule = nullptr;
    if (!GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
            reinterpret_cast<LPCWSTR>(&g_exceptionHandler),
            &pinnedModule))
    {
        return -1;
    }

    if (GetModuleHandleW(L"UnityPlayer.dll") == nullptr)
    {
        return -2;
    }

    g_exceptionHandler = AddVectoredExceptionHandler(1, HandleUnityShutdownException);
    return g_exceptionHandler != nullptr ? 0 : -3;
}
