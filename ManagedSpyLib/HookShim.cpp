#include "stdafx.h"

using namespace System;
using namespace System::Reflection;

namespace
{
    delegate void HookBridgeDelegate(int nCode, IntPtr wParam, IntPtr lParam);

    ref class HookBridgeResolver abstract sealed
    {
    public:
        static HookBridgeDelegate^ Resolve()
        {
            if (s_hookBridge != nullptr)
            {
                return s_hookBridge;
            }

            Type^ bridgeType = Type::GetType("Microsoft.ManagedSpy.HookBridge, ManagedSpyLib", false);
            if (bridgeType == nullptr)
            {
                Assembly^ managedSpyAssembly = Assembly::Load("ManagedSpyLib");
                if (managedSpyAssembly == nullptr)
                {
                    return nullptr;
                }

                bridgeType = managedSpyAssembly->GetType("Microsoft.ManagedSpy.HookBridge", false);
                if (bridgeType == nullptr)
                {
                    return nullptr;
                }
            }

            MethodInfo^ bridgeMethod = bridgeType->GetMethod(
                "MessageHookProc",
                BindingFlags::Public | BindingFlags::Static);

            if (bridgeMethod == nullptr)
            {
                return nullptr;
            }

            s_hookBridge = safe_cast<HookBridgeDelegate^>(
                Delegate::CreateDelegate(HookBridgeDelegate::typeid, bridgeMethod, false));
            return s_hookBridge;
        }

    private:
        static HookBridgeDelegate^ s_hookBridge = nullptr;
    };
}

extern "C" __declspec(dllexport)
LRESULT CALLBACK MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam)
{
    try
    {
        if (nCode == HC_ACTION)
        {
            HookBridgeDelegate^ hookBridge = HookBridgeResolver::Resolve();
            if (hookBridge != nullptr)
            {
                hookBridge(nCode, IntPtr((void*)wparam), IntPtr((void*)lparam));
            }
        }
    }
    catch (...)
    {
    }

    return CallNextHookEx(NULL, nCode, wparam, lparam);
}
