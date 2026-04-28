#pragma once

namespace Microsoft {
namespace ManagedSpy {

	public ref class ScreenBoundsHelper abstract sealed
	{
	public:
		static System::Drawing::Rectangle GetControlScreenBounds(System::Windows::Forms::Control^ control, bool preferAccessibility);
		static bool ShouldUseRawWindowDpiFallback(
			System::Drawing::Rectangle candidateRectangle,
			System::Drawing::Rectangle rawWindowRectangle,
			System::Drawing::Rectangle rootWindowRectangle);
	};
}
}
