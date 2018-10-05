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

using System;

namespace DaggerfallWorkshop.Utility
{
    /// <summary>
    /// A range of values defined by a Min and a Max.
    /// </summary>
    public class Range<T> where T : IComparable
    {
        /// <summary>
        /// The minimum value of this range.
        /// </summary>
        public T Min;

        /// <summary>
        /// The maximum value of this range.
        /// </summary>
        public T Max;

        /// <summary>
        /// Min and Max define a valid range.
        /// </summary>
        public bool IsValid
        {
            get { return Min.CompareTo(Max) <= 0; }
        }

        /// <summary>
        /// Makes an empty range.
        /// </summary>
        public Range()
        {
        }

        /// <summary>
        /// Makes a new range from the given values.
        /// </summary>
        /// <param name="min">The minimum value of this range.</param>
        /// <param name="max">The maximum value of this range.</param>
        public Range(T min, T max)
        {
            this.Min = min;
            this.Max = max;
        }

        public override string ToString()
        {
            return string.Format("Min: {0}, Max: {1}", Min, Max);
        }

        /// <summary>
        /// Checks if the given value is inside this range.
        /// </summary>
        public bool Contains(T value)
        {
            return Min.CompareTo(value) <= 0 && Max.CompareTo(value) >= 0;
        }

        /// <summary>
        /// Checks if the given range is inside or equal to this instance.
        /// </summary>
        public bool Contains(Range<T> range)
        {
            return Min.CompareTo(range.Min) <= 0 && Max.CompareTo(range.Max) >= 0;
        }

        /// <summary>
        /// Returns the intersection of this instance with the given range, or null.
        /// </summary>
        public Range<T> Intersection(Range<T> range)
        {
            T min = this.Min.CompareTo(range.Min) >= 0 ? this.Min : range.Min;
            T max = this.Max.CompareTo(range.Max) <= 0 ? this.Max : range.Max;

            if (min.CompareTo(max) <= 0)
                return new Range<T>(min, max);

            return null;
        }
    }
}
