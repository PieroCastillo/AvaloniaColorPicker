﻿/*
    AvaloniaColorPicker - A color picker for Avalonia.
    Copyright (C) 2020  Giorgio Bianchini
 
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, version 3.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using System;
using System.Threading.Tasks;

namespace AvaloniaColorPicker
{
    /// <summary>
    /// A control that can be used to select a <see cref="Avalonia.Media.Color"/>.
    /// </summary>
    public class ColorButton : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Color"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<ColorPicker, Color>(nameof(Color), Color.FromArgb(255, 0, 0, 0));

        /// <summary>
        /// The <see cref="Avalonia.Media.Color"/> that is currently selected.
        /// </summary>
        public Color Color
        {
            get { return GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        private Grid PaletteContainer { get; }

        private ColorVisualBrush ColorBrush { get; }

        private ToggleButton ContentButton { get; }

        /// <summary>
        /// Creates a new <see cref="ColorButton"/> instance.
        /// </summary>
        public ColorButton()
        {
            SetStyles();

            ContentButton = new ToggleButton();

            ContentButton.Classes.Add("ContentButton");
            ContentButton.Padding = new Thickness(4, 4, 0, 4);

            Grid mainGrid = new Grid();
            ContentButton.Content = mainGrid;

            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(16, GridUnitType.Pixel));

            Border colorBorder = new Border() { MinHeight = 16, MinWidth = 24, BorderThickness = new Avalonia.Thickness(1), BorderBrush = (IBrush)Application.Current.FindResource("ThemeForegroundBrush"), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch };

            mainGrid.Children.Add(colorBorder);

            ColorBrush = new ColorVisualBrush(Color);

            colorBorder.Background = ColorBrush;

            Canvas arrowCanvas = new Canvas() { Width = 16, Height = 16 };
            arrowCanvas.Classes.Add("ArrowCanvas");
            Grid.SetColumn(arrowCanvas, 1);

            PathGeometry arrowGeometry = new PathGeometry();
            PathFigure arrowFigure = new PathFigure() { StartPoint = new Avalonia.Point(4, 6), IsClosed = true, IsFilled = true };
            arrowFigure.Segments.Add(new LineSegment() { Point = new Avalonia.Point(8, 10) });
            arrowFigure.Segments.Add(new LineSegment() { Point = new Avalonia.Point(12, 6) });
            arrowGeometry.Figures.Add(arrowFigure);
            Path arrowPath = new Path() { Data = arrowGeometry };
            arrowCanvas.Children.Add(arrowPath);
            mainGrid.Children.Add(arrowCanvas);

            arrowCanvas.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            Popup palettePopup = new Popup
            {
                PlacementMode = PlacementMode.AnchorAndGravity,
                PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.Bottom,
                PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.Bottom,
                PlacementConstraintAdjustment = Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.FlipY,
                PlacementTarget = ContentButton
            };
            mainGrid.Children.Add(palettePopup);

            Border paletteBorder = new Border() { BorderThickness = new Thickness(1), BorderBrush = (IBrush)Application.Current.FindResource("ThemeBorderLowBrush") };
            palettePopup.Child = paletteBorder;

            Grid paletteGrid = new Grid() { Margin = new Thickness(5) };
            paletteGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));
            paletteGrid.RowDefinitions.Add(new RowDefinition(0, GridUnitType.Auto));

            Button openColorPickerButton = new Button() { Content = "More colours..." };
            Grid.SetRow(openColorPickerButton, 1);
            paletteGrid.Children.Add(openColorPickerButton);

            PaletteContainer = new Grid() { Margin = new Thickness(0, 0, 0, 5) };

            paletteGrid.Children.Add(PaletteContainer);

            openColorPickerButton.Click += async (s, e) =>
            {
                palettePopup.Close();
                ColorPickerWindow win = new ColorPickerWindow(Color);
                Color? newCol = await win.ShowDialog(this.FindLogicalAncestorOfType<Window>());
                if (newCol != null)
                {
                    this.Color = newCol.Value;
                }
                ContentButton.IsChecked = false;
            };

            paletteBorder.Child = paletteGrid;

            ContentButton.PropertyChanged += async (s, e) =>
            {
                if (e.Property == ToggleButton.IsCheckedProperty)
                {
                    if ((e.NewValue as bool?).Value == true)
                    {
                        if (Palette.CurrentPalette != null && Palette.CurrentPalette.Colors.Count > 0)
                        {
                            PaletteContainer.Children.Clear();
                            PaletteContainer.Children.Add(GetPaletteCanvas(Palette.CurrentPalette));
                            palettePopup.Open();
                        }
                        else
                        {
                            ColorPickerWindow win = new ColorPickerWindow(Color);
                            Color? newCol = await win.ShowDialog(this.FindLogicalAncestorOfType<Window>());
                            if (newCol != null)
                            {
                                this.Color = newCol.Value;
                            }
                            ContentButton.IsChecked = false;
                        }
                    }
                    else
                    {
                        palettePopup.Close();
                    }
                }
            };

            this.Content = ContentButton;
        }

        /// <inheritdoc/>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            IInputElement element = FocusManager.Instance.Current;

            if (!HasParent(element as IControl, ContentButton))
            {
                ContentButton.IsChecked = false;
            }
        }

        private static bool HasParent(IControl child, IControl parent)
        {
            while (child != null)
            {
                if (child == parent)
                {
                    return true;
                }

                child = child.Parent;
            }

            return false;
        }

        private void SetStyles()
        {
            Style arrowColor = new Style(x => x.OfType<ToggleButton>().Class("ContentButton").Child().OfType<Grid>().Child().OfType<Canvas>().Class("ArrowCanvas").Child().OfType<Path>());
            arrowColor.Setters.Add(new Setter(Path.FillProperty, Application.Current.FindResource("ThemeForegroundBrush")));
            this.Styles.Add(arrowColor);

            Style arrowColorPressed = new Style(x => x.OfType<ToggleButton>().Class("ContentButton").Class(":pressed").Child().OfType<Grid>().Child().OfType<Canvas>().Class("ArrowCanvas").Child().OfType<Path>());
            arrowColorPressed.Setters.Add(new Setter(Path.FillProperty, Application.Current.FindResource("ThemeControlHighlightLowBrush")));
            this.Styles.Add(arrowColorPressed);

            Style arrowColorActive = new Style(x => x.OfType<ToggleButton>().Class("ContentButton").Class(":checked").Child().OfType<Grid>().Child().OfType<Canvas>().Class("ArrowCanvas").Child().OfType<Path>());
            arrowColorActive.Setters.Add(new Setter(Path.FillProperty, Application.Current.FindResource("ThemeControlHighlightLowBrush")));
            this.Styles.Add(arrowColorActive);

            Style canvasRotationActive = new Style(x => x.OfType<ToggleButton>().Class("ContentButton").Class(":checked").Child().OfType<Grid>().Child().OfType<Canvas>().Class("ArrowCanvas"));
            canvasRotationActive.Setters.Add(new Setter(Canvas.RenderTransformProperty, new RotateTransform(180)));
            this.Styles.Add(canvasRotationActive);

            if (!ColorPicker.TransitionsDisabled)
            {
                Transitions transformTransitions = new Transitions
                {
                    new TransformOperationsTransition() { Property = Path.RenderTransformProperty, Duration = new TimeSpan(0, 0, 0, 0, 100) }
                };

                Transitions strokeTransitions = new Transitions
                {
                    new DoubleTransition() { Property = Path.StrokeThicknessProperty, Duration = new TimeSpan(0, 0, 0, 0, 100) }
                };

                Style HexagonLeft = new Style(x => x.OfType<Path>().Class("HexagonLeftButton"));
                HexagonLeft.Setters.Add(new Setter(Path.TransitionsProperty, transformTransitions));
                this.Styles.Add(HexagonLeft);

                Style HexagonRight = new Style(x => x.OfType<Path>().Class("HexagonRightButton"));
                HexagonRight.Setters.Add(new Setter(Path.TransitionsProperty, transformTransitions));
                this.Styles.Add(HexagonRight);

                Style HexagonCenter = new Style(x => x.OfType<Path>().Class("HexagonCenterButton"));
                HexagonCenter.Setters.Add(new Setter(Path.StrokeProperty, Application.Current.FindResource("ThemeBackgroundBrush")));
                HexagonCenter.Setters.Add(new Setter(Path.TransitionsProperty, strokeTransitions));
                this.Styles.Add(HexagonCenter);
            }

            Style HexagonLeftOver = new Style(x => x.OfType<Path>().Class("HexagonLeftButton").Class(":pointerover"));
            HexagonLeftOver.Setters.Add(new Setter(Path.RenderTransformProperty, TransformOperations.Parse("translate(-4.33px, 0)")));
            this.Styles.Add(HexagonLeftOver);

            Style HexagonRightOver = new Style(x => x.OfType<Path>().Class("HexagonRightButton").Class(":pointerover"));
            HexagonRightOver.Setters.Add(new Setter(Path.RenderTransformProperty, TransformOperations.Parse("translate(4.33px, 0)")));
            this.Styles.Add(HexagonRightOver);

            Style HexagonCenterOver = new Style(x => x.OfType<Path>().Class("HexagonCenterButton").Class(":pointerover"));
            HexagonCenterOver.Setters.Add(new Setter(Path.StrokeThicknessProperty, 3.0));
            this.Styles.Add(HexagonCenterOver);

            Style rightOverBlurring = new Style(x => x.OfType<Path>().Class("rightOverBlurring"));
            rightOverBlurring.Setters.Add(new Setter(Path.ZIndexProperty, 9));
            this.Styles.Add(rightOverBlurring);

            Style leftOverBlurring = new Style(x => x.OfType<Path>().Class("leftOverBlurring"));
            leftOverBlurring.Setters.Add(new Setter(Path.ZIndexProperty, 9));
            this.Styles.Add(leftOverBlurring);

            Style centerOverBlurring = new Style(x => x.OfType<Path>().Class("centerOverBlurring"));
            centerOverBlurring.Setters.Add(new Setter(Path.ZIndexProperty, 9));
            this.Styles.Add(centerOverBlurring);

            Style rightOver = new Style(x => x.OfType<Path>().Class("rightOver"));
            rightOver.Setters.Add(new Setter(Path.ZIndexProperty, 10));
            this.Styles.Add(rightOver);

            Style leftOver = new Style(x => x.OfType<Path>().Class("leftOver"));
            leftOver.Setters.Add(new Setter(Path.ZIndexProperty, 10));
            this.Styles.Add(leftOver);

            Style centerOver = new Style(x => x.OfType<Path>().Class("centerOver"));
            centerOver.Setters.Add(new Setter(Path.ZIndexProperty, 10));
            this.Styles.Add(centerOver);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ColorProperty)
            {
                Color col = (change.NewValue.Value as Color?).Value;
                ColorBrush.Color = col;
            }
        }

        private const double HexagonRadius = 20;
        private const int MaxColumns = 3;

        private Canvas GetPaletteCanvas(Palette palette)
        {
            Canvas can = new Canvas
            {
                Width = ((Math.Min(MaxColumns, palette.Colors.Count) - 1) * 2.5 + 3) * HexagonRadius + 8.66,
                Height = ((Math.Ceiling((double)palette.Colors.Count / MaxColumns) - 1) * 2 + 3) * HexagonRadius * Math.Sin(Math.PI / 3)
            };

            if (palette.Colors.Count % MaxColumns == 1)
            {
                can.Height -= HexagonRadius * Math.Sin(Math.PI / 3);
            }

            for (int i = 0; i < palette.Colors.Count; i++)
            {
                AddColorHexagon(i, palette, can);
            }

            return can;
        }


        private void AddColorHexagon(int i, Palette palette, Canvas container)
        {
            int rowInd = i / MaxColumns;
            int colInd = i % MaxColumns;

            double centerX;
            double centerY;

            if (i == 0 && palette.Colors.Count == 1)
            {
                centerX = HexagonRadius * (1.5 + colInd * 2.5);
                centerY = HexagonRadius * Math.Sin(Math.PI / 3) * (2 + 2 * rowInd - 1);
            }
            else if (colInd == 0)
            {
                centerX = HexagonRadius * (1.5 + 2.5);
                centerY = HexagonRadius * Math.Sin(Math.PI / 3) * (2 + 2 * rowInd - 1);
            }
            else if (colInd == 1)
            {
                centerX = HexagonRadius * 1.5;
                centerY = HexagonRadius * Math.Sin(Math.PI / 3) * (2 + 2 * rowInd);
            }
            else
            {
                centerX = HexagonRadius * (1.5 + colInd * 2.5);
                centerY = HexagonRadius * Math.Sin(Math.PI / 3) * (2 + 2 * rowInd - colInd % 2);
            }

            centerX += 4.33;

            PathGeometry leftHexagon = GetHexagonPath(new Point(centerX - 0.5 * HexagonRadius, centerY), HexagonRadius);
            PathGeometry centerHexagon = GetHexagonPath(new Point(centerX, centerY), HexagonRadius);
            PathGeometry rightHexagon = GetHexagonPath(new Point(centerX + 0.5 * HexagonRadius, centerY), HexagonRadius);


            Color color = palette.Colors[i % palette.Colors.Count];
            Color lighterColor = ColorPicker.GetLighterColor(color);
            Color darkerColor = ColorPicker.GetDarkerColor(color);


            Avalonia.Controls.Shapes.Path leftPath = new Avalonia.Controls.Shapes.Path() { Data = leftHexagon, Fill = new ColorVisualBrush(lighterColor), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            Avalonia.Controls.Shapes.Path rightPath = new Avalonia.Controls.Shapes.Path() { Data = rightHexagon, Fill = new ColorVisualBrush(darkerColor), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            Avalonia.Controls.Shapes.Path centerPath = new Avalonia.Controls.Shapes.Path() { Data = centerHexagon, Fill = new ColorVisualBrush(color), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };

            leftPath.Classes.Add("HexagonLeftButton");
            rightPath.Classes.Add("HexagonRightButton");
            centerPath.Classes.Add("HexagonCenterButton");

            rightPath.PointerEnter += (s, e) =>
            {
                rightPath.Classes.Add("rightOver");
                centerPath.Classes.Add("rightOver");
                leftPath.Classes.Add("rightOver");
            };

            rightPath.PointerLeave += async (s, e) =>
            {
                rightPath.Classes.Add("rightOverBlurring");
                centerPath.Classes.Add("rightOverBlurring");
                leftPath.Classes.Add("rightOverBlurring");
                rightPath.Classes.Remove("rightOver");
                centerPath.Classes.Remove("rightOver");
                leftPath.Classes.Remove("rightOver");

                await Task.Delay(100);
                rightPath.Classes.Remove("rightOverBlurring");
                centerPath.Classes.Remove("rightOverBlurring");
                leftPath.Classes.Remove("rightOverBlurring");
            };

            leftPath.PointerEnter += (s, e) =>
            {
                rightPath.Classes.Add("leftOver");
                centerPath.Classes.Add("leftOver");
                leftPath.Classes.Add("leftOver");
            };

            leftPath.PointerLeave += async (s, e) =>
            {
                rightPath.Classes.Add("leftOverBlurring");
                centerPath.Classes.Add("leftOverBlurring");
                leftPath.Classes.Add("leftOverBlurring");
                rightPath.Classes.Remove("leftOver");
                centerPath.Classes.Remove("leftOver");
                leftPath.Classes.Remove("leftOver");

                await Task.Delay(100);
                rightPath.Classes.Remove("leftOverBlurring");
                centerPath.Classes.Remove("leftOverBlurring");
                leftPath.Classes.Remove("leftOverBlurring");
            };

            centerPath.PointerEnter += (s, e) =>
            {
                rightPath.Classes.Add("centerOver");
                centerPath.Classes.Add("centerOver");
                leftPath.Classes.Add("centerOver");
            };

            centerPath.PointerLeave += async (s, e) =>
            {
                rightPath.Classes.Add("centerOverBlurring");
                centerPath.Classes.Add("centerOverBlurring");
                leftPath.Classes.Add("centerOverBlurring");
                rightPath.Classes.Remove("centerOver");
                centerPath.Classes.Remove("centerOver");
                leftPath.Classes.Remove("centerOver");

                await Task.Delay(100);
                rightPath.Classes.Remove("centerOverBlurring");
                centerPath.Classes.Remove("centerOverBlurring");
                leftPath.Classes.Remove("centerOverBlurring");
            };

            leftPath.PointerPressed += (s, e) =>
            {
                this.Color = lighterColor;
            };

            rightPath.PointerPressed += (s, e) =>
            {
                this.Color = darkerColor;
            };

            centerPath.PointerPressed += (s, e) =>
            {
                this.Color = color;
            };

            RelativePoint renderTransformOrigin = new RelativePoint(centerX, centerY, RelativeUnit.Absolute);

            leftPath.RenderTransformOrigin = renderTransformOrigin;
            rightPath.RenderTransformOrigin = renderTransformOrigin;
            centerPath.RenderTransformOrigin = renderTransformOrigin;

            container.Children.Add(leftPath);
            container.Children.Add(rightPath);
            container.Children.Add(centerPath);
        }

        private static PathGeometry GetHexagonPath(Point center, double radius)
        {
            PathFigure hexagonFigure = new PathFigure() { StartPoint = center + new Point(radius, 0), IsClosed = true, IsFilled = true };

            for (int i = 1; i < 6; i++)
            {
                hexagonFigure.Segments.Add(new LineSegment() { Point = center + new Point(radius * Math.Cos(Math.PI * i / 3.0), radius * Math.Sin(Math.PI * i / 3.0)) });
            }

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(hexagonFigure);
            return geometry;
        }
    }
}
