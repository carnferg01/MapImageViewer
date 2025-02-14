﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MapImageViewer
{
    class CustomHighlighterPen : InkToolbarCustomPen
    {
        public CustomHighlighterPen()
        {
        
        }
        protected override InkDrawingAttributes CreateInkDrawingAttributesCore(Brush brush, double strokeWidth)
        {
            InkDrawingAttributes inkDrawingAttributes = new InkDrawingAttributes();
            inkDrawingAttributes.PenTip = PenTipShape.Circle;
            inkDrawingAttributes.Size = new Windows.Foundation.Size(strokeWidth, strokeWidth);
            inkDrawingAttributes.DrawAsHighlighter = true;
            SolidColorBrush solidColorBrush = brush as SolidColorBrush;
            if (solidColorBrush != null)
            {
                inkDrawingAttributes.Color = solidColorBrush.Color;
            }
            else
            {
                inkDrawingAttributes.Color = Colors.Black;
            }


            return inkDrawingAttributes;
        }
    }
}
