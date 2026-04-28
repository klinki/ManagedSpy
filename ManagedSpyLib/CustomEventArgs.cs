using System;
using System.Windows.Forms;

namespace Microsoft.ManagedSpy
{
    [Serializable]
    internal sealed class SerializableMouseEventArgs : EventArgs
    {
        internal SerializableMouseEventArgs(MouseEventArgs mouseEventArgs)
        {
            Button = mouseEventArgs.Button;
            Clicks = mouseEventArgs.Clicks;
            X = mouseEventArgs.X;
            Y = mouseEventArgs.Y;
            Delta = mouseEventArgs.Delta;
        }

        public MouseButtons Button { get; }
        public int Clicks { get; }
        public int X { get; }
        public int Y { get; }
        public int Delta { get; }

        public override string ToString()
        {
            return "{ MouseEventArgs [Button:" + Button +
                " Clicks:" + Clicks +
                " X:" + X +
                " Y:" + Y +
                " Delta:" + Delta +
                "] }";
        }
    }

    [Serializable]
    internal sealed class NonSerializableEventArgs : EventArgs
    {
        internal NonSerializableEventArgs(EventArgs eventArgs)
        {
            TypeName = eventArgs.GetType().FullName;
        }

        public string TypeName { get; }

        public override string ToString()
        {
            return "{ NonSerializable EventArgs [" + TypeName + "] }";
        }
    }
}
