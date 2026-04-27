#include "StdAfx.h"
#include "Commands.h"
#include "EventTargetWindow.h"

using namespace System;
using namespace System::Collections::Generic;

void EventTargetWindow::WndProc(Message% m) {

	if (m.Msg == WM_EVENTFIRED) {
		//an event has been fired. 

		MemoryStore* store = MemoryStore::OpenStore((int)m.WParam, (int)m.LParam, true);
		if (store!= NULL) {
			List<Object^>^ params= (List<Object^>^)store->GetParameters();
			if (params != nullptr) {
				_ASSERTE(params->Count == 3);
				if (Desktop::proxyCache->ContainsKey((IntPtr)params[0])) {
					ControlProxy^ proxy = Desktop::proxyCache[(IntPtr)params[0]];
					_ASSERTE(proxy != nullptr);
					//fire the event.
					EventDescriptorCollection^ events = proxy->GetEvents();
					if (events != nullptr && (int)params[1]<= events->Count && (int)params[1]>=0) {
						proxy->RaiseEvent(gcnew ProxyEventArgs(events[(int)params[1]], (EventArgs^)params[2]));
					}
				}
			}
			store->Release();	//release, don't delete if in the same process.
		}
	}
	else if (m.Msg == WM_WINDOWDESTROYED) {
		if (Desktop::proxyCache->ContainsKey(m.WParam)) {
			Desktop::proxyCache->Remove(m.WParam);
		}
	}
	else if (m.Msg == WM_HANDLECHANGED) {
		IntPtr oldHandle = m.WParam;
		IntPtr newHandle = m.LParam;
		ControlProxy^ proxy = nullptr;
		if (Desktop::proxyCache->ContainsKey(oldHandle)) {
			proxy = Desktop::proxyCache[oldHandle];
		}

		if (proxy == nullptr) {
			for each (KeyValuePair<IntPtr, ControlProxy^> entry in Desktop::proxyCache) {
				if (entry.Value != nullptr && entry.Value->Handle == oldHandle) {
					proxy = entry.Value;
					break;
				}
			}
		}

		if (proxy != nullptr) {
			List<IntPtr>^ staleKeys = gcnew List<IntPtr>();
			for each (KeyValuePair<IntPtr, ControlProxy^> entry in Desktop::proxyCache) {
				if (Object::ReferenceEquals(entry.Value, proxy) || entry.Key == oldHandle || entry.Key == newHandle) {
					staleKeys->Add(entry.Key);
				}
			}

			for each (IntPtr key in staleKeys) {
				Desktop::proxyCache->Remove(key);
			}

			proxy->Handle = newHandle;
			if (newHandle != IntPtr::Zero && !Desktop::proxyCache->ContainsKey(newHandle)) {
				Desktop::proxyCache->Add(newHandle, proxy);
			}
		}
	}
	else {
		Control::WndProc(m);
	}
}
