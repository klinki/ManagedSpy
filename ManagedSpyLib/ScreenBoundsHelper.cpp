#include "StdAfx.h"
#include "ScreenBoundsHelper.h"

using namespace Microsoft::ManagedSpy;

namespace
{
#pragma warning(push)
#pragma warning(disable : 4642)
    bool TryGetAccessibleBounds(System::Windows::Forms::Control^ control, System::Drawing::Rectangle% bounds)
    {
        System::Windows::Forms::AccessibleObject^ accessibilityObject = control->AccessibilityObject;
        if (accessibilityObject == nullptr)
        {
            bounds = System::Drawing::Rectangle::Empty;
            return false;
        }

        bounds = accessibilityObject->Bounds;
        return bounds.Width > 0 && bounds.Height > 0;
    }
#pragma warning(pop)

    bool TryGetWindowRectangle(HWND handle, System::Drawing::Rectangle% bounds)
    {
        RECT rect = {};
        if (::GetWindowRect(handle, &rect) == 0)
        {
            bounds = System::Drawing::Rectangle::Empty;
            return false;
        }

        bounds = System::Drawing::Rectangle::FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    bool TryGetClientScreenBounds(HWND handle, System::Drawing::Rectangle% bounds)
    {
        RECT clientRect = {};
        if (::GetClientRect(handle, &clientRect) == 0)
        {
            bounds = System::Drawing::Rectangle::Empty;
            return false;
        }

        POINT corners[2] = {
            { clientRect.left, clientRect.top },
            { clientRect.right, clientRect.bottom }
        };

        if (::MapWindowPoints(handle, HWND_DESKTOP, corners, 2) == 0 && ::GetLastError() != 0)
        {
            bounds = System::Drawing::Rectangle::Empty;
            return false;
        }

        bounds = System::Drawing::Rectangle::FromLTRB(corners[0].x, corners[0].y, corners[1].x, corners[1].y);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    HWND GetManagedRootHandle(System::Windows::Forms::Control^ control)
    {
        System::Windows::Forms::Control^ current = control;
        while (current != nullptr && current->Parent != nullptr)
        {
            current = current->Parent;
        }

        return current == nullptr ? nullptr : static_cast<HWND>(current->Handle.ToPointer());
    }

    long long GetIntersectionArea(System::Drawing::Rectangle bounds, System::Drawing::Rectangle containerBounds)
    {
        System::Drawing::Rectangle intersection = System::Drawing::Rectangle::Intersect(bounds, containerBounds);
        return static_cast<long long>(intersection.Width) * static_cast<long long>(intersection.Height);
    }

    bool TryConvertLogicalToPhysical(HWND referenceHandle, System::Drawing::Rectangle logicalBounds, System::Drawing::Rectangle% physicalBounds)
    {
        if (referenceHandle == nullptr)
        {
            physicalBounds = System::Drawing::Rectangle::Empty;
            return false;
        }

        POINT points[2] = {
            { logicalBounds.Left, logicalBounds.Top },
            { logicalBounds.Right, logicalBounds.Bottom }
        };

        if (::LogicalToPhysicalPointForPerMonitorDPI(referenceHandle, &points[0]) == 0 ||
            ::LogicalToPhysicalPointForPerMonitorDPI(referenceHandle, &points[1]) == 0)
        {
            physicalBounds = System::Drawing::Rectangle::Empty;
            return false;
        }

        physicalBounds = System::Drawing::Rectangle::FromLTRB(points[0].x, points[0].y, points[1].x, points[1].y);
        return physicalBounds.Width > 0 && physicalBounds.Height > 0;
    }

    System::Drawing::Rectangle NormalizeManagedScreenBounds(System::Windows::Forms::Control^ control, System::Drawing::Rectangle candidateBounds)
    {
        HWND rootHandle = GetManagedRootHandle(control);
        System::Drawing::Rectangle rootBounds;
        if (rootHandle == nullptr || !TryGetWindowRectangle(rootHandle, rootBounds))
        {
            return candidateBounds;
        }

        System::Drawing::Rectangle physicalBounds;
        if (!TryConvertLogicalToPhysical(rootHandle, candidateBounds, physicalBounds))
        {
            return candidateBounds;
        }

        if (rootBounds.Contains(candidateBounds))
        {
            return candidateBounds;
        }

        long long candidateScore = GetIntersectionArea(candidateBounds, rootBounds);
        long long physicalScore = GetIntersectionArea(physicalBounds, rootBounds);
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
}

System::Drawing::Rectangle ScreenBoundsHelper::GetControlScreenBounds(System::Windows::Forms::Control^ control, bool preferAccessibility)
{
    if (control == nullptr)
    {
        return System::Drawing::Rectangle::Empty;
    }

    HWND handle = static_cast<HWND>(control->Handle.ToPointer());
    if (handle == nullptr)
    {
        return System::Drawing::Rectangle::Empty;
    }

    if (control->Parent == nullptr)
    {
        System::Drawing::Rectangle windowBounds;
        if (TryGetWindowRectangle(handle, windowBounds))
        {
            return windowBounds;
        }

        return control->Bounds;
    }

    System::Drawing::Rectangle clientScreenBounds;
    if ((!preferAccessibility || !TryGetAccessibleBounds(control, clientScreenBounds)) &&
        !TryGetClientScreenBounds(handle, clientScreenBounds))
    {
        clientScreenBounds = control->RectangleToScreen(control->ClientRectangle);
        if (clientScreenBounds.Width <= 0 || clientScreenBounds.Height <= 0)
        {
            System::Drawing::Point screenLocation = control->PointToScreen(System::Drawing::Point::Empty);
            clientScreenBounds = System::Drawing::Rectangle(screenLocation, control->Size);
        }
    }

    clientScreenBounds = NormalizeManagedScreenBounds(control, clientScreenBounds);

    for (System::Windows::Forms::Control^ ancestor = control->Parent; ancestor != nullptr; ancestor = ancestor->Parent)
    {
        System::Drawing::Rectangle ancestorClientBounds;
        HWND ancestorHandle = static_cast<HWND>(ancestor->Handle.ToPointer());
        if (!TryGetClientScreenBounds(ancestorHandle, ancestorClientBounds))
        {
            ancestorClientBounds = ancestor->RectangleToScreen(ancestor->ClientRectangle);
        }

        clientScreenBounds = System::Drawing::Rectangle::Intersect(clientScreenBounds, ancestorClientBounds);
        if (clientScreenBounds.Width <= 0 || clientScreenBounds.Height <= 0)
        {
            return System::Drawing::Rectangle::Empty;
        }
    }

    return clientScreenBounds;
}
