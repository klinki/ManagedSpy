#include "StdAfx.h"
#include "Commands.h"
#include "ControlProxy.h"

using namespace Microsoft::ManagedSpy;

Object^ PropertyDescriptorProxy::GetValue(Object^ component) {
	ControlProxy^ proxy = (ControlProxy^)component;
	Object^ value = nullptr;
	if (proxy != nullptr) {
		value = Desktop::SendMarshaledMessage(proxy->Handle, WM_GETMGDPROPERTY, this->Name);
	}
	return value;
}

void PropertyDescriptorProxy::ResetValue(Object^ component) {
	ControlProxy^ proxy = (ControlProxy^)component;
	Object^ value = nullptr;
	if (proxy != nullptr) {
		value = Desktop::SendMarshaledMessage(proxy->Handle, WM_RESETMGDPROPERTY, this->Name);
	}
}

void PropertyDescriptorProxy::SetValue(Object^ component, Object^ value) {
	ControlProxy^ proxy = (ControlProxy^)component;
	if (proxy != nullptr) {
		List<Object^>^ params = gcnew List<Object^>();
		Object^ valueToSend = value;
		bool useInvariantString = false;

		if (value != nullptr && !value->GetType()->IsSerializable) {
			TypeConverter^ converter = originalProperty->Converter;
			if (converter == nullptr || !converter->CanConvertTo(String::typeid) || !converter->CanConvertFrom(String::typeid)) {
				throw gcnew InvalidOperationException(String::Format(
					"Property '{0}' value type '{1}' is not serializable and has no invariant string converter.",
					this->Name,
					value->GetType()->FullName));
			}

			valueToSend = converter->ConvertToInvariantString(value);
			if (valueToSend == nullptr) {
				throw gcnew InvalidOperationException(String::Format(
					"Property '{0}' converter returned null for invariant string conversion.",
					this->Name));
			}

			useInvariantString = true;
		}

		params->Add(this->Name);
		params->Add(valueToSend);
		if (useInvariantString) {
			params->Add(true);
		}
		Desktop::SendMarshaledMessage(proxy->Handle, WM_SETMGDPROPERTY, 
			params );
	}
}
