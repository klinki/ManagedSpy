#include "stdafx.h"

using namespace System;
using namespace System::IO;
using namespace System::Reflection;

extern "C" __declspec(dllexport)
LRESULT CALLBACK MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam);

namespace
{
    delegate void HookBridgeDelegate(int nCode, IntPtr wParam, IntPtr lParam);

    bool TryGetManagedSpyAssemblyPath(String^% managedSpyAssemblyPath)
    {
        managedSpyAssemblyPath = nullptr;

        HMODULE moduleHandle = NULL;
        if (!GetModuleHandleEx(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            reinterpret_cast<LPCTSTR>(&MessageHookProc),
            &moduleHandle))
        {
            return false;
        }

        TCHAR modulePathBuffer[MAX_PATH] = {};
        DWORD pathLength = GetModuleFileName(moduleHandle, modulePathBuffer, _countof(modulePathBuffer));
        if (pathLength == 0 || pathLength >= _countof(modulePathBuffer))
        {
            return false;
        }

        String^ hookPath = gcnew String(modulePathBuffer);
        String^ hookDirectory = Path::GetDirectoryName(hookPath);
        if (String::IsNullOrWhiteSpace(hookDirectory))
        {
            return false;
        }

        managedSpyAssemblyPath = Path::Combine(hookDirectory, "ManagedSpyLib.dll");
        return File::Exists(managedSpyAssemblyPath);
    }

    Assembly^ ResolveManagedSpyAssembly(String^ managedSpyAssemblyPath)
    {
        for each (Assembly^ loadedAssembly in AppDomain::CurrentDomain->GetAssemblies())
        {
            if (loadedAssembly == nullptr)
            {
                continue;
            }

            try
            {
                if (String::Equals(
                    loadedAssembly->GetName()->Name,
                    "ManagedSpyLib",
                    StringComparison::OrdinalIgnoreCase))
                {
                    return loadedAssembly;
                }
            }
            catch (Exception^)
            {
            }
        }

        return Assembly::LoadFrom(managedSpyAssemblyPath);
    }

    ref class HookBridgeResolver abstract sealed
    {
    public:
        static HookBridgeDelegate^ Resolve()
        {
            if (s_hookBridge != nullptr)
            {
                return s_hookBridge;
            }

            String^ managedSpyAssemblyPath = nullptr;
            if (!TryGetManagedSpyAssemblyPath(managedSpyAssemblyPath))
            {
                return nullptr;
            }
            RegisterDependencyResolver(Path::GetDirectoryName(managedSpyAssemblyPath));

            Assembly^ managedSpyAssembly = nullptr;
            try
            {
                managedSpyAssembly = ResolveManagedSpyAssembly(managedSpyAssemblyPath);
            }
            catch (Exception^)
            {
                return nullptr;
            }

            if (managedSpyAssembly == nullptr)
            {
                return nullptr;
            }

            Type^ bridgeType = managedSpyAssembly->GetType("Microsoft.ManagedSpy.HookBridge", false);
            if (bridgeType == nullptr)
            {
                return nullptr;
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
        static bool s_dependencyResolverRegistered = false;
        static String^ s_hookDirectory = nullptr;

        static void RegisterDependencyResolver(String^ hookDirectory)
        {
            if (String::IsNullOrWhiteSpace(hookDirectory))
            {
                return;
            }

            s_hookDirectory = hookDirectory;
            if (s_dependencyResolverRegistered)
            {
                return;
            }

            AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler(&HookBridgeResolver::ResolveDependency);
            s_dependencyResolverRegistered = true;
        }

        static Assembly^ ResolveDependency(Object^ sender, ResolveEventArgs^ args)
        {
            if (String::IsNullOrWhiteSpace(s_hookDirectory) || args == nullptr || String::IsNullOrWhiteSpace(args->Name))
            {
                return nullptr;
            }

            AssemblyName^ assemblyName = gcnew AssemblyName(args->Name);
            String^ assemblyPath = Path::Combine(s_hookDirectory, assemblyName->Name + ".dll");
            if (!File::Exists(assemblyPath))
            {
                return nullptr;
            }

            return Assembly::LoadFrom(assemblyPath);
        }
    };
}

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
