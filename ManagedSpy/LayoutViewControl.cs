using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.ManagedSpy;

namespace ManagedSpy
{
    internal sealed class LayoutViewControl : Control
    {
        private static readonly Color MarginFillColor = Color.FromArgb(255, 236, 179);
        private static readonly Color MarginAccentColor = Color.FromArgb(245, 124, 0);
        private static readonly Color ElementFillColor = Color.FromArgb(227, 242, 253);
        private static readonly Color ElementAccentColor = Color.FromArgb(25, 118, 210);
        private static readonly Color PaddingFillColor = Color.FromArgb(232, 245, 233);
        private static readonly Color PaddingAccentColor = Color.FromArgb(46, 125, 50);
        private static readonly Color ContentFillColor = Color.FromArgb(243, 229, 245);
        private static readonly Color ContentAccentColor = Color.FromArgb(123, 31, 162);

        private ControlLayoutInfo layoutInfo;
        private LayoutSection hoveredSection;
        private Rectangle marginRectangle;
        private Rectangle elementRectangle;
        private Rectangle paddingRectangle;
        private Rectangle contentRectangle;

        public event EventHandler HoveredSectionChanged;

        public LayoutViewControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ControlLayoutInfo LayoutInfo
        {
            get { return layoutInfo; }
            set
            {
                layoutInfo = value;
                SetHoveredSection(LayoutSection.None);
                Invalidate();
            }
        }

        public LayoutSection HoveredSection
        {
            get { return hoveredSection; }
        }

        internal static Color GetSectionAccentColor(LayoutSection section)
        {
            switch (section)
            {
                case LayoutSection.Margin:
                    return MarginAccentColor;
                case LayoutSection.Element:
                    return ElementAccentColor;
                case LayoutSection.Padding:
                    return PaddingAccentColor;
                case LayoutSection.Content:
                    return ContentAccentColor;
                default:
                    return SystemColors.ControlDarkDark;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);

            if (layoutInfo == null)
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    "Select a managed control to inspect layout.",
                    Font,
                    ClientRectangle,
                    SystemColors.GrayText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }

            UpdateBoxRectangles();
            DrawSectionFills(e.Graphics);
            DrawSectionBorders(e.Graphics);
            DrawSectionAnnotations(e.Graphics);
            DrawHoverBorder(e.Graphics);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateBoxRectangles();
            SetHoveredSection(GetSectionAtPoint(e.Location));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            SetHoveredSection(LayoutSection.None);
        }

        private void UpdateBoxRectangles()
        {
            Rectangle bounds = ClientRectangle;
            bounds.Inflate(-12, -12);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                marginRectangle = Rectangle.Empty;
                elementRectangle = Rectangle.Empty;
                paddingRectangle = Rectangle.Empty;
                contentRectangle = Rectangle.Empty;
                return;
            }

            marginRectangle = bounds;
            int inset = Math.Max(18, Math.Min(36, Math.Min(marginRectangle.Width, marginRectangle.Height) / 8));
            elementRectangle = Inset(marginRectangle, inset);
            paddingRectangle = Inset(elementRectangle, inset);
            contentRectangle = Inset(paddingRectangle, inset);
        }

        private static Rectangle Inset(Rectangle rectangle, int inset)
        {
            Rectangle result = rectangle;
            result.Inflate(-inset, -inset);
            if (result.Width <= 0 || result.Height <= 0)
            {
                return Rectangle.Empty;
            }

            return result;
        }

        private LayoutSection GetSectionAtPoint(Point point)
        {
            if (contentRectangle.Contains(point))
            {
                return LayoutSection.Content;
            }

            if (paddingRectangle.Contains(point))
            {
                return LayoutSection.Padding;
            }

            if (elementRectangle.Contains(point))
            {
                return LayoutSection.Element;
            }

            if (marginRectangle.Contains(point))
            {
                return LayoutSection.Margin;
            }

            return LayoutSection.None;
        }

        private void SetHoveredSection(LayoutSection section)
        {
            if (hoveredSection == section)
            {
                return;
            }

            hoveredSection = section;
            Invalidate();
            HoveredSectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DrawSectionFills(Graphics graphics)
        {
            DrawSectionFill(graphics, marginRectangle, MarginFillColor);
            DrawSectionFill(graphics, elementRectangle, ElementFillColor);
            DrawSectionFill(graphics, paddingRectangle, PaddingFillColor);
            DrawSectionFill(graphics, contentRectangle, ContentFillColor);
        }

        private static void DrawSectionFill(Graphics graphics, Rectangle rectangle, Color fillColor)
        {
            if (rectangle == Rectangle.Empty)
            {
                return;
            }

            using (SolidBrush brush = new SolidBrush(fillColor))
            {
                graphics.FillRectangle(brush, rectangle);
            }
        }

        private void DrawSectionBorders(Graphics graphics)
        {
            DrawSectionBorder(graphics, marginRectangle, LayoutSection.Margin, 1);
            DrawSectionBorder(graphics, elementRectangle, LayoutSection.Element, 1);
            DrawSectionBorder(graphics, paddingRectangle, LayoutSection.Padding, 1);
            DrawSectionBorder(graphics, contentRectangle, LayoutSection.Content, 1);
        }

        private static void DrawSectionBorder(Graphics graphics, Rectangle rectangle, LayoutSection section, int width)
        {
            if (rectangle == Rectangle.Empty)
            {
                return;
            }

            using (Pen pen = new Pen(GetSectionAccentColor(section), width))
            {
                graphics.DrawRectangle(pen, rectangle);
            }
        }

        private void DrawSectionAnnotations(Graphics graphics)
        {
            DrawLayerLabel(
                graphics,
                marginRectangle,
                elementRectangle,
                LayoutSection.Margin,
                "Margin",
                String.Empty);
            DrawEdgeValues(graphics, marginRectangle, elementRectangle, layoutInfo.Margin, LayoutSection.Margin);

            DrawLayerLabel(
                graphics,
                elementRectangle,
                paddingRectangle,
                LayoutSection.Element,
                "Element",
                FormatSize(layoutInfo.ElementSize));

            DrawLayerLabel(
                graphics,
                paddingRectangle,
                contentRectangle,
                LayoutSection.Padding,
                "Padding",
                String.Empty);
            DrawEdgeValues(graphics, paddingRectangle, contentRectangle, layoutInfo.Padding, LayoutSection.Padding);

            DrawLayerLabel(
                graphics,
                contentRectangle,
                Rectangle.Empty,
                LayoutSection.Content,
                "Content",
                FormatSize(layoutInfo.ContentSize));
        }

        private void DrawLayerLabel(
            Graphics graphics,
            Rectangle outerRectangle,
            Rectangle innerRectangle,
            LayoutSection section,
            string title,
            string value)
        {
            if (outerRectangle == Rectangle.Empty)
            {
                return;
            }

            Rectangle labelRectangle = GetLayerLabelRectangle(outerRectangle, innerRectangle);
            if (labelRectangle.Width <= 10 || labelRectangle.Height <= 10)
            {
                return;
            }

            bool multiline = labelRectangle.Height >= 30;
            string text = String.IsNullOrEmpty(value) ? title :
                multiline ?
                    title + Environment.NewLine + value :
                    title + "  " + value;
            TextFormatFlags flags =
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix;
            flags |= section == LayoutSection.Content ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left;
            flags |= multiline ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine;

            TextRenderer.DrawText(
                graphics,
                text,
                Font,
                labelRectangle,
                GetSectionAccentColor(section),
                flags);
        }

        private static Rectangle GetLayerLabelRectangle(Rectangle outerRectangle, Rectangle innerRectangle)
        {
            if (innerRectangle == Rectangle.Empty)
            {
                Rectangle result = outerRectangle;
                result.Inflate(-6, -6);
                return result;
            }

            int topBandHeight = innerRectangle.Top - outerRectangle.Top;
            if (topBandHeight >= 16)
            {
                return Rectangle.FromLTRB(
                    outerRectangle.Left + 6,
                    outerRectangle.Top + 2,
                    outerRectangle.Right - 6,
                    innerRectangle.Top - 2);
            }

            int leftBandWidth = innerRectangle.Left - outerRectangle.Left;
            if (leftBandWidth >= 48)
            {
                return Rectangle.FromLTRB(
                    outerRectangle.Left + 4,
                    innerRectangle.Top + 4,
                    innerRectangle.Left - 4,
                    innerRectangle.Bottom - 4);
            }

            Rectangle fallback = outerRectangle;
            fallback.Inflate(-6, -6);
            return fallback;
        }

        private void DrawEdgeValues(Graphics graphics, Rectangle outerRectangle, Rectangle innerRectangle, LayoutThickness thickness, LayoutSection section)
        {
            if (outerRectangle == Rectangle.Empty || innerRectangle == Rectangle.Empty)
            {
                return;
            }

            Color textColor = GetSectionAccentColor(section);
            DrawEdgeLabel(graphics, thickness.Top.ToString(), Rectangle.FromLTRB(innerRectangle.Left, outerRectangle.Top, innerRectangle.Right, innerRectangle.Top), textColor);
            DrawEdgeLabel(graphics, thickness.Right.ToString(), Rectangle.FromLTRB(innerRectangle.Right, innerRectangle.Top, outerRectangle.Right, innerRectangle.Bottom), textColor);
            DrawEdgeLabel(graphics, thickness.Bottom.ToString(), Rectangle.FromLTRB(innerRectangle.Left, innerRectangle.Bottom, innerRectangle.Right, outerRectangle.Bottom), textColor);
            DrawEdgeLabel(graphics, thickness.Left.ToString(), Rectangle.FromLTRB(outerRectangle.Left, innerRectangle.Top, innerRectangle.Left, innerRectangle.Bottom), textColor);
        }

        private void DrawEdgeLabel(Graphics graphics, string text, Rectangle rectangle, Color textColor)
        {
            if (rectangle.Width < 18 || rectangle.Height < 12)
            {
                return;
            }

            TextRenderer.DrawText(
                graphics,
                text,
                Font,
                rectangle,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private void DrawHoverBorder(Graphics graphics)
        {
            Rectangle rectangle = GetViewRectangle(hoveredSection);
            if (rectangle == Rectangle.Empty)
            {
                return;
            }

            DrawSectionBorder(graphics, rectangle, hoveredSection, 3);
        }

        private Rectangle GetViewRectangle(LayoutSection section)
        {
            switch (section)
            {
                case LayoutSection.Margin:
                    return marginRectangle;
                case LayoutSection.Element:
                    return elementRectangle;
                case LayoutSection.Padding:
                    return paddingRectangle;
                case LayoutSection.Content:
                    return contentRectangle;
                default:
                    return Rectangle.Empty;
            }
        }

        private static string FormatThickness(LayoutThickness thickness)
        {
            return "T " + thickness.Top +
                " R " + thickness.Right +
                " B " + thickness.Bottom +
                " L " + thickness.Left;
        }

        private static string FormatSize(Size size)
        {
            return size.Width + " x " + size.Height;
        }
    }
}
