using System;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.ManagedSpy
{
    public enum LayoutSection
    {
        None,
        Margin,
        Element,
        Padding,
        Content
    }

    [Serializable]
    public struct LayoutThickness
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public LayoutThickness(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Horizontal
        {
            get { return Left + Right; }
        }

        public int Vertical
        {
            get { return Top + Bottom; }
        }

        public static LayoutThickness FromPadding(Padding padding)
        {
            return new LayoutThickness(padding.Left, padding.Top, padding.Right, padding.Bottom);
        }
    }

    [Serializable]
    public sealed class ControlLayoutInfo
    {
        public LayoutThickness Margin { get; set; }

        public LayoutThickness Padding { get; set; }

        public Rectangle MarginBoundsScreen { get; set; }

        public Rectangle BoundsScreen { get; set; }

        public Rectangle ClientBoundsScreen { get; set; }

        public Rectangle ContentBoundsScreen { get; set; }

        public Size ElementSize { get; set; }

        public Size ContentSize { get; set; }

        public static ControlLayoutInfo FromControl(Control control)
        {
            if (control == null)
            {
                return null;
            }

            LayoutThickness margin = LayoutThickness.FromPadding(control.Margin);
            LayoutThickness padding = LayoutThickness.FromPadding(control.Padding);
            Rectangle boundsScreen = GetBoundsScreen(control);
            Rectangle clientBoundsScreen = ScreenBoundsHelper.GetRelativeScreenBounds(control, control.ClientRectangle);
            Rectangle contentBoundsScreen = ScreenBoundsHelper.GetRelativeScreenBounds(control, control.DisplayRectangle);

            return new ControlLayoutInfo
            {
                Margin = margin,
                Padding = padding,
                BoundsScreen = boundsScreen,
                MarginBoundsScreen = GetMarginBoundsScreen(control, boundsScreen, margin),
                ClientBoundsScreen = clientBoundsScreen,
                ContentBoundsScreen = contentBoundsScreen,
                ElementSize = control.Size,
                ContentSize = control.DisplayRectangle.Size
            };
        }

        public Rectangle GetSectionBounds(LayoutSection section)
        {
            switch (section)
            {
                case LayoutSection.Margin:
                    return MarginBoundsScreen;
                case LayoutSection.Element:
                    return BoundsScreen;
                case LayoutSection.Padding:
                    return ClientBoundsScreen;
                case LayoutSection.Content:
                    return ContentBoundsScreen;
                default:
                    return Rectangle.Empty;
            }
        }

        private static Rectangle GetBoundsScreen(Control control)
        {
            if (control.Parent != null)
            {
                Rectangle screenBounds = ScreenBoundsHelper.GetRelativeScreenBounds(control.Parent, control.Bounds);
                if (screenBounds != Rectangle.Empty)
                {
                    return screenBounds;
                }
            }

            if (control.Handle != IntPtr.Zero &&
                NativeMethods.GetWindowRect(control.Handle, out NativeMethods.RectNative windowRectangle))
            {
                Rectangle rectangle = windowRectangle.ToRectangle();
                if (rectangle.Width > 0 && rectangle.Height > 0)
                {
                    return rectangle;
                }
            }

            return control.Bounds;
        }

        private static Rectangle GetMarginBoundsScreen(Control control, Rectangle boundsScreen, LayoutThickness thickness)
        {
            if (control.Parent == null)
            {
                return Inflate(boundsScreen, thickness);
            }

            Rectangle marginBounds = Rectangle.FromLTRB(
                control.Bounds.Left - thickness.Left,
                control.Bounds.Top - thickness.Top,
                control.Bounds.Right + thickness.Right,
                control.Bounds.Bottom + thickness.Bottom);
            Rectangle marginBoundsScreen = ScreenBoundsHelper.GetRelativeScreenBounds(control.Parent, marginBounds);
            return marginBoundsScreen == Rectangle.Empty
                ? Inflate(boundsScreen, thickness)
                : marginBoundsScreen;
        }

        private static Rectangle Inflate(Rectangle rectangle, LayoutThickness thickness)
        {
            if (rectangle == Rectangle.Empty)
            {
                return Rectangle.Empty;
            }

            return Rectangle.FromLTRB(
                rectangle.Left - thickness.Left,
                rectangle.Top - thickness.Top,
                rectangle.Right + thickness.Right,
                rectangle.Bottom + thickness.Bottom);
        }
    }
}
