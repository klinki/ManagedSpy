#include "StdAfx.h"
#include "ScreenBoundsHelper.h"

using namespace Microsoft::ManagedSpy;

System::Drawing::Rectangle ScreenBoundsHelper::GetControlScreenBounds(System::Windows::Forms::Control^ control)
{
    if (control == nullptr)
    {
        return System::Drawing::Rectangle::Empty;
    }

    if (control->Parent == nullptr)
    {
        return control->Bounds;
    }

    System::Drawing::Rectangle clientScreenBounds = control->RectangleToScreen(control->ClientRectangle);
    if (clientScreenBounds.Width > 0 && clientScreenBounds.Height > 0)
    {
        return clientScreenBounds;
    }

    System::Drawing::Point screenLocation = control->PointToScreen(System::Drawing::Point::Empty);
    return System::Drawing::Rectangle(screenLocation, control->Size);
}
