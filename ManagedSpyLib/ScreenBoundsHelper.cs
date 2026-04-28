using System;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.ManagedSpy
{
    public static class ScreenBoundsHelper
    {
        public static Rectangle GetControlScreenBounds(Control control, bool preferAccessibility)
        {
            if (control == null)
            {
                return Rectangle.Empty;
            }

            IntPtr handle = control.Handle;
            if (handle == IntPtr.Zero)
            {
                return Rectangle.Empty;
            }

            if (control.Parent == null)
            {
                if (TryGetWindowRectangle(handle, out Rectangle windowBounds))
                {
                    return windowBounds;
                }

                return control.Bounds;
            }

            Rectangle accessibleScreenBounds = Rectangle.Empty;
            bool hasAccessibleScreenBounds = false;
            if (preferAccessibility && TryGetAccessibleBounds(control, out accessibleScreenBounds))
            {
                accessibleScreenBounds = NormalizeManagedScreenBounds(control, accessibleScreenBounds);
                hasAccessibleScreenBounds = accessibleScreenBounds.Width > 0 && accessibleScreenBounds.Height > 0;
            }

            bool hasClientScreenBounds = TryGetNativeClientScreenBounds(control, out Rectangle clientScreenBounds);
            if (hasClientScreenBounds)
            {
                hasClientScreenBounds = clientScreenBounds.Width > 0 && clientScreenBounds.Height > 0;
            }

            Rectangle resolvedScreenBounds;
            if (hasAccessibleScreenBounds && ShouldUseAccessibleBounds(accessibleScreenBounds, clientScreenBounds))
            {
                resolvedScreenBounds = accessibleScreenBounds;
            }
            else if (hasClientScreenBounds)
            {
                resolvedScreenBounds = clientScreenBounds;
            }
            else if (hasAccessibleScreenBounds)
            {
                resolvedScreenBounds = accessibleScreenBounds;
            }
            else
            {
                return Rectangle.Empty;
            }

            for (Control ancestor = control.Parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (!TryGetClientScreenBounds(ancestor.Handle, out Rectangle ancestorClientBounds))
                {
                    ancestorClientBounds = ancestor.RectangleToScreen(ancestor.ClientRectangle);
                }

                resolvedScreenBounds = Rectangle.Intersect(resolvedScreenBounds, ancestorClientBounds);
                if (resolvedScreenBounds.Width <= 0 || resolvedScreenBounds.Height <= 0)
                {
                    return Rectangle.Empty;
                }
            }

            return resolvedScreenBounds;
        }

        public static bool ShouldUseRawWindowDpiFallback(
            Rectangle candidateRectangle,
            Rectangle rawWindowRectangle,
            Rectangle rootWindowRectangle)
        {
            if (candidateRectangle.Width <= 0
                || candidateRectangle.Height <= 0
                || rawWindowRectangle.Width <= 0
                || rawWindowRectangle.Height <= 0)
            {
                return false;
            }

            double scaleX = (double)candidateRectangle.Width / rawWindowRectangle.Width;
            double scaleY = (double)candidateRectangle.Height / rawWindowRectangle.Height;
            if (scaleX < 1.1 || Math.Abs(scaleX - scaleY) > 0.15)
            {
                return false;
            }

            if (!IsNearScaledValue(candidateRectangle.Left, rawWindowRectangle.Left, scaleX)
                || !IsNearScaledValue(candidateRectangle.Top, rawWindowRectangle.Top, scaleY))
            {
                return false;
            }

            if (rootWindowRectangle.Width <= 0 || rootWindowRectangle.Height <= 0)
            {
                return true;
            }

            long candidateScore = GetIntersectionArea(candidateRectangle, rootWindowRectangle);
            long rawScore = GetIntersectionArea(rawWindowRectangle, rootWindowRectangle);
            return rawScore > candidateScore;
        }

        private static bool TryGetAccessibleBounds(Control control, out Rectangle bounds)
        {
            AccessibleObject accessibilityObject = control.AccessibilityObject;
            if (accessibilityObject == null)
            {
                bounds = Rectangle.Empty;
                return false;
            }

            bounds = accessibilityObject.Bounds;
            return bounds.Width > 0 && bounds.Height > 0;
        }

        private static bool TryGetWindowRectangle(IntPtr handle, out Rectangle bounds)
        {
            if (!NativeMethods.GetWindowRect(handle, out NativeMethods.RectNative rectangle))
            {
                bounds = Rectangle.Empty;
                return false;
            }

            bounds = rectangle.ToRectangle();
            return bounds.Width > 0 && bounds.Height > 0;
        }

        private static bool TryGetClientScreenBounds(IntPtr handle, out Rectangle bounds)
        {
            if (!NativeMethods.GetClientRect(handle, out NativeMethods.RectNative clientRect))
            {
                bounds = Rectangle.Empty;
                return false;
            }

            NativeMethods.PointNative[] corners =
            {
                new NativeMethods.PointNative(clientRect.Left, clientRect.Top),
                new NativeMethods.PointNative(clientRect.Right, clientRect.Bottom)
            };

            if (NativeMethods.MapWindowPoints(handle, IntPtr.Zero, corners, 2) == 0
                && System.Runtime.InteropServices.Marshal.GetLastWin32Error() != 0)
            {
                bounds = Rectangle.Empty;
                return false;
            }

            bounds = Rectangle.FromLTRB(corners[0].X, corners[0].Y, corners[1].X, corners[1].Y);
            return bounds.Width > 0 && bounds.Height > 0;
        }

        private static IntPtr GetManagedRootHandle(Control control)
        {
            Control current = control;
            while (current != null && current.Parent != null)
            {
                current = current.Parent;
            }

            return current == null ? IntPtr.Zero : current.Handle;
        }

        private static long GetIntersectionArea(Rectangle bounds, Rectangle containerBounds)
        {
            Rectangle intersection = Rectangle.Intersect(bounds, containerBounds);
            return (long)intersection.Width * intersection.Height;
        }

        private static long GetRectangleArea(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return 0;
            }

            return (long)bounds.Width * bounds.Height;
        }

        private static bool IsNearScaledValue(int candidateValue, int rawValue, double scale)
        {
            if (rawValue == 0)
            {
                return true;
            }

            return Math.Abs(candidateValue - (rawValue * scale)) <= 16.0;
        }

        private static bool TryConvertLogicalToPhysical(IntPtr referenceHandle, Rectangle logicalBounds, out Rectangle physicalBounds)
        {
            if (referenceHandle == IntPtr.Zero)
            {
                physicalBounds = Rectangle.Empty;
                return false;
            }

            NativeMethods.PointNative topLeft = new NativeMethods.PointNative(logicalBounds.Left, logicalBounds.Top);
            NativeMethods.PointNative bottomRight = new NativeMethods.PointNative(logicalBounds.Right, logicalBounds.Bottom);

            if (!NativeMethods.LogicalToPhysicalPointForPerMonitorDPI(referenceHandle, ref topLeft)
                || !NativeMethods.LogicalToPhysicalPointForPerMonitorDPI(referenceHandle, ref bottomRight))
            {
                physicalBounds = Rectangle.Empty;
                return false;
            }

            physicalBounds = Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
            return physicalBounds.Width > 0 && physicalBounds.Height > 0;
        }

        private static Rectangle NormalizeManagedScreenBounds(Control control, Rectangle candidateBounds)
        {
            IntPtr rootHandle = GetManagedRootHandle(control);
            if (rootHandle == IntPtr.Zero || !TryGetWindowRectangle(rootHandle, out Rectangle rootBounds))
            {
                return candidateBounds;
            }

            if (!TryConvertLogicalToPhysical(rootHandle, candidateBounds, out Rectangle physicalBounds))
            {
                return candidateBounds;
            }

            if (rootBounds.Contains(candidateBounds))
            {
                return candidateBounds;
            }

            long candidateScore = GetIntersectionArea(candidateBounds, rootBounds);
            long physicalScore = GetIntersectionArea(physicalBounds, rootBounds);
            if (rootBounds.Contains(physicalBounds) && !rootBounds.Contains(candidateBounds))
            {
                return physicalBounds;
            }

            if (candidateScore == 0 && physicalScore > 0)
            {
                return physicalBounds;
            }

            if (physicalScore > candidateScore)
            {
                return physicalBounds;
            }

            return candidateBounds;
        }

        private static bool TryGetNativeClientScreenBounds(Control control, out Rectangle bounds)
        {
            IntPtr handle = control.Handle;
            if (handle != IntPtr.Zero && TryGetClientScreenBounds(handle, out bounds))
            {
                return true;
            }

            bounds = control.RectangleToScreen(control.ClientRectangle);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                Point screenLocation = control.PointToScreen(Point.Empty);
                bounds = new Rectangle(screenLocation, control.Size);
            }

            return bounds.Width > 0 && bounds.Height > 0;
        }

        private static bool ShouldUseAccessibleBounds(Rectangle accessibleBounds, Rectangle nativeBounds)
        {
            long accessibleArea = GetRectangleArea(accessibleBounds);
            if (accessibleArea == 0)
            {
                return false;
            }

            if (GetRectangleArea(nativeBounds) == 0)
            {
                return true;
            }

            Rectangle expandedNativeBounds = nativeBounds;
            expandedNativeBounds.Inflate(2, 2);
            long overlapArea = GetIntersectionArea(accessibleBounds, expandedNativeBounds);
            return overlapArea * 2 >= accessibleArea;
        }
    }
}
