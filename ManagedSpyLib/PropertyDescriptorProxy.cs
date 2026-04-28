using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.ManagedSpy
{
    internal sealed class PropertyDescriptorProxy : PropertyDescriptor
    {
        private readonly PropertyDescriptor originalProperty;

        internal PropertyDescriptorProxy(PropertyDescriptor original)
            : base(original)
        {
            originalProperty = original;
        }

        public override Type ComponentType => originalProperty.ComponentType;

        public override TypeConverter Converter => originalProperty.Converter;

        public override bool IsLocalizable => originalProperty.IsLocalizable;

        public override bool IsReadOnly => originalProperty.IsReadOnly;

        public override Type PropertyType => originalProperty.PropertyType;

        public override void AddValueChanged(object component, EventHandler handler)
        {
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override PropertyDescriptorCollection GetChildProperties(object instance, Attribute[] filter)
        {
            object value = GetValue(instance);
            if (value == null)
            {
                return PropertyDescriptorCollection.Empty;
            }

            return filter == null
                ? TypeDescriptor.GetProperties(value)
                : TypeDescriptor.GetProperties(value, filter);
        }

        public override object GetEditor(Type editorBaseType)
        {
            return originalProperty.GetEditor(editorBaseType);
        }

        public override object GetValue(object component)
        {
            ControlProxy proxy = component as ControlProxy;
            if (proxy == null)
            {
                return null;
            }

            return Desktop.SendMarshaledMessage(proxy.Handle, ManagedSpyMessages.GetManagedProperty, Name);
        }

        public override void ResetValue(object component)
        {
            ControlProxy proxy = component as ControlProxy;
            if (proxy != null)
            {
                Desktop.SendMarshaledMessage(proxy.Handle, ManagedSpyMessages.ResetManagedProperty, Name);
            }
        }

        public override void SetValue(object component, object value)
        {
            ControlProxy proxy = component as ControlProxy;
            if (proxy == null)
            {
                return;
            }

            List<object> parameters = new List<object>();
            object valueToSend = value;
            bool useInvariantString = false;

            if (value != null && !value.GetType().IsSerializable)
            {
                TypeConverter converter = originalProperty.Converter;
                if (converter == null
                    || !converter.CanConvertTo(typeof(string))
                    || !converter.CanConvertFrom(typeof(string)))
                {
                    throw new InvalidOperationException(
                        "Property '" + Name + "' value type '" + value.GetType().FullName +
                        "' is not serializable and has no invariant string converter.");
                }

                valueToSend = converter.ConvertToInvariantString(value);
                if (valueToSend == null)
                {
                    throw new InvalidOperationException(
                        "Property '" + Name + "' converter returned null for invariant string conversion.");
                }

                useInvariantString = true;
            }

            parameters.Add(Name);
            parameters.Add(valueToSend);
            if (useInvariantString)
            {
                parameters.Add(true);
            }

            Desktop.SendMarshaledMessage(proxy.Handle, ManagedSpyMessages.SetManagedProperty, parameters);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }
}
