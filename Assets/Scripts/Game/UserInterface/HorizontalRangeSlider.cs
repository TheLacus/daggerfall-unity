// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: TheLacus
// Contributors:    
// 
// Notes:
//

// #define SLIDER_RANGE_DEBUG

using DaggerfallWorkshop.Utility;
using System;
using UnityEngine;

namespace DaggerfallWorkshop.Game.UserInterface
{
    /// <summary>
    /// A slider for a min-max range clamped inside lower an upper limits.
    /// Gets two values where one is lower or equal to the other.
    /// </summary>
    public class HorizontalRangeSlider : Panel
    {
        #region Fields

        static readonly Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        static readonly Color tintColor = new Color(153, 153, 0);

        int totalUnits;
        int displayUnits;
        Rect thumbRectMin;
        Rect thumbRectMax;

        bool draggingThumbMin = false;
        bool draggingThumbMax = false;
        Vector2 dragStartPositionMin;
        Vector2 dragStartPositionMax;
        int dragStartScrollIndexMin;
        int dragStartScrollIndexMax;

        TextLabel indicator;
        readonly Func<int, string> formatValueCallback;

        #endregion

        #region Properties

        /// <summary>
        /// The value on this slider.
        /// </summary>
        public Range<int> Range { get; set; }

        /// <summary>
        /// The clamp range of this slider.
        /// </summary>
        public Range<int> Limits { get; set; }

        /// <summary>
        /// Tint color of the slider.
        /// </summary>
        public Color? TintColor { get; set; }

        /// <summary>
        /// Indicator for this slider.
        /// </summary>
        public TextLabel Indicator
        {
            get { return GetIndicator(); }
        }

        /// <summary>
        /// Distance from slider. Negative is on the left.
        /// </summary>
        public int IndicatorOffset
        {
            set { Indicator.Position = new Vector2(value >= 0 ? Size.x + value : value, 0); }
        }

        #endregion

        #region Constructors

        private HorizontalRangeSlider(Range<int> limits, Range<int> range, bool initIndicator)
            : base()
        {
            this.Limits = limits;
            this.Range = range ?? limits;
            this.BackgroundColor = backgroundColor;
            this.TintColor = tintColor;
            if (initIndicator)
            {
                this.Indicator.TextColor = Color.white;
                this.Indicator.ShadowColor = Color.clear;
            }
        }

        /// <summary>
        /// Make a range slider with int min max values.
        /// </summary>
        /// <param name="limits">Lower and upper values on the slider.</param>
        /// <param name="range">Initial min and max values on the slider.</param>
        /// <param name="digits">Number of digits shown on UI.</param>
        /// <param name="initIndicator">Initialize value indicator.</param>
        public HorizontalRangeSlider(Range<int> limits, Range<int> range = null, int digits = 2, bool initIndicator = false)
            : this(limits, range, initIndicator)
        {
            string format = string.Format("d{0}", digits);
            formatValueCallback = value => value.ToString(format);
        }

        /// <summary>
        /// Make a range slider with float minmax values.
        /// </summary>
        /// <param name="limits">Lower and upper values on the slider.</param>
        /// <param name="range">Initial min and max values on the slider.</param>
        /// <param name="decimals">Number of decimals shown on UI.</param>
        /// <param name="initIndicator">Initialize value indicator.</param>
        public HorizontalRangeSlider(Range<float> limits, Range<float> range = null, int decimals = 1, bool initIndicator = false)
            : this(FloatToIntRange(limits, decimals), FloatToIntRange(range, decimals), initIndicator)
        {
            float divider = Mathf.Pow(10, decimals);
            string format = string.Format("n{0}", decimals);
            formatValueCallback = value => (value / divider).ToString(format);
        }

        #endregion

        #region Overrides

        public override void Update()
        {
            base.Update();

            if (!Limits.Contains(Range))
                throw new Exception("Range is outside limits.");

            displayUnits = Range.Max - Range.Min;
            totalUnits = Limits.Max - Limits.Min;

            if (Input.GetMouseButton(0))
            {
                Vector2 mousePosition = ScreenToLocal(MousePosition);
                if (!draggingThumbMin && thumbRectMin.Contains(mousePosition))
                {
                    draggingThumbMin = true;
                    dragStartPositionMin = mousePosition;
                    dragStartScrollIndexMin = Range.Min;
                }

                if (draggingThumbMin)
                {
                    Vector2 dragDistance = mousePosition - dragStartPositionMin;
                    float scale = Size.x / (float)totalUnits;
                    float unitsMoved = dragDistance.x / scale;
                    SetScrollIndexMin(dragStartScrollIndexMin + (int)unitsMoved);
                }

                if (!draggingThumbMax && thumbRectMax.Contains(mousePosition))
                {
                    draggingThumbMax = true;
                    dragStartPositionMax = mousePosition;
                    dragStartScrollIndexMax = Range.Max;
                }

                if (draggingThumbMax)
                {
                    Vector2 dragDistance = mousePosition - dragStartPositionMax;
                    float scale = Size.x / (float)totalUnits;
                    float unitsMoved = dragDistance.x / scale;
                    SetScrollIndexMax(dragStartScrollIndexMax + (int)unitsMoved);
                }
            }
            else
            {
                if (draggingThumbMin)
                    draggingThumbMin = false;
                if (draggingThumbMax)
                    draggingThumbMax = false;
            }
        }

        public override void Draw()
        {
            base.Draw();

            DrawSlider();

            if (indicator != null)
                indicator.Draw();
        }

        protected override void MouseScrollUp()
        {
            base.MouseScrollUp();

            if (Range.Min > Limits.Min)
            {
                Range.Min--;
                Range.Max--;
                UpdateIndicator();
            }
        }

        protected override void MouseScrollDown()
        {
            base.MouseScrollDown();

            if (Range.Max < Limits.Max)
            {
                Range.Min++;
                Range.Max++;
                UpdateIndicator();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get slider values as a range of floats.
        /// </summary>
        /// <param name="decimals">Number of decimal digits.</param>
        public Range<float> GetRangeAsFloat(int decimals)
        {
            float divider = Mathf.Pow(10, decimals);

            return new Range<float>(
                Range.Min / divider,
                Range.Max / divider);
        }

        #endregion

        #region Private Methods

        private void SetScrollIndexMin(int value)
        {
            Range.Min = value;

            if (Range.Min < Limits.Min)
                Range.Min = Limits.Min;
            if (Range.Min > Range.Max)
                Range.Min = Range.Max;

            UpdateIndicator();
            RaiseOnScrollEvent();
        }

        private void SetScrollIndexMax(int value)
        {
            Range.Max = value;
            if (Range.Max < Range.Min)
                Range.Max = Range.Min;
            if (Range.Max > Limits.Max)
                Range.Max = Limits.Max;

            UpdateIndicator();
            RaiseOnScrollEvent();
        }

        private TextLabel GetIndicator()
        {
            if (indicator == null)
            {
                indicator = new TextLabel();
                indicator.Parent = this;
                indicator.Text = GetIndicatorText();
                IndicatorOffset = 15;
            }

            return indicator;
        }

        private void UpdateIndicator()
        {
            if (indicator != null)
                indicator.Text = GetIndicatorText();
        }

        private string GetIndicatorText()
        {
            return string.Format("{0}-{1}", formatValueCallback(Range.Min), formatValueCallback(Range.Max));
        }

        private void DrawSlider()
        {

#if SLIDER_RANGE_DEBUG
            Debug.LogFormat("range: {0}, displayunits: {1}, totalUnits: {2}", Range, displayUnits, totalUnits);
#endif

            int scrollIndexLeft = Range.Min - Limits.Min;
            int scrollIndexRight = totalUnits - (Limits.Max - Range.Max);

            // Update current thumb rect in local space
            Rect totalRect = Rectangle;
            float thumbWidth = totalRect.width * ((float)displayUnits / (float)totalUnits);
            if (thumbWidth < 10) thumbWidth = 10;
            float thumbWidthSide = thumbWidth * 0.1f;
            float thumbXMin = totalUnits == displayUnits ? 0 : scrollIndexLeft * (totalRect.width - thumbWidth) / (totalUnits - displayUnits);
            float thumbXMax = totalUnits == displayUnits ? totalRect.width : scrollIndexRight * (totalRect.width - thumbWidth) / (totalUnits - displayUnits);
            thumbRectMin = ScreenToLocal(new Rect(totalRect.x + thumbXMin, totalRect.y, thumbWidthSide, totalRect.height));
            thumbRectMax = ScreenToLocal(new Rect(totalRect.x + thumbXMax - thumbWidthSide, totalRect.y, thumbWidthSide, totalRect.height));

            // Get rects
            float leftTextureWidth = 1 * LocalScale.x;
            float rightTextureWidth = 1 * LocalScale.x;
            Rect leftRect = new Rect(totalRect.x + thumbXMin, totalRect.y, leftTextureWidth, totalRect.height);
            Rect rightRect = new Rect(totalRect.x + thumbXMax - rightTextureWidth, totalRect.y, rightTextureWidth, totalRect.height);
            Rect bodyRect = new Rect(leftRect.xMax, totalRect.y, rightRect.xMax - leftRect.xMax, totalRect.height);

            // Draw thumb texture slices in screen space
            Color color = GUI.color;
            if (TintColor.HasValue)
                GUI.color = TintColor.Value;
            GUI.DrawTexture(leftRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(bodyRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(rightRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = color;
        }

        private static Range<int> FloatToIntRange(Range<float> range, int decimals)
        {
            float multiplier = Mathf.Pow(10, decimals);

            return new Range<int>(
                (int)(range.Min * multiplier),
                (int)(range.Max * multiplier));
        }

        #endregion

        #region EventHandlers

        public delegate void OnScrollHandler();
        public event OnScrollHandler OnScroll;
        void RaiseOnScrollEvent()
        {
            if (OnScroll != null)
                OnScroll();
        }

        #endregion
    }
}
