#region License and Information
/*****
* BinarySearch.cs
* -------------
* 
* A binary search implementation for the generic List class. It expects the list
* to be SORTED already.
* 
*  - If the exact value is found it returns the index of that value.
*  - If the exact value is not found the method returns a negative value. That
*    value represents the negative index of the closest element minus 1. So a
*    value of "-1" means the closest element is "0". If it's "-2" the closest
*    element is "1"
*    
* This method can work with any ordered List, no matter if it's sorted in
* ascending or descending order.
* 
* Copyright (c) 2017 Markus Göbel (Bunny83)
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to
* deal in the Software without restriction, including without limitation the
* rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
* sell copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
* IN THE SOFTWARE.
* 
*****/
#endregion License and Information

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BinarySearchImpl
{
    /// <summary>
    /// Does a binary search on the given sorted List for the value "aValue".
    /// </summary>
    /// <param name="aList">The List that should be searched through</param>
    /// <param name="aValue">The value that is searched for</param>
    /// <returns>The index of the found element. If not found it will return a negative value.
    /// The negative value gives the index of the closest element to the searched value by doing:
    /// closest = -result -1
    /// In case of an error it returns int.MinValue (-2147483648)
    /// </returns>
    public static int BinarySearch<T>(this List<T> aList, T aValue) where T : System.IComparable<T>
    {
        if (aList == null)
            return int.MinValue;
        int min = 0;
        int max = aList.Count - 1;
        if (aList[min].CompareTo(aList[max]) > 0)
        {   // highest element comes first

            if (aValue.CompareTo(aList[min]) > 0)
                return -1;
            if (aValue.CompareTo(aList[max]) < 0)
                return -aList.Count;
            while (min < max-1)
            {
                int mid = (max + min) / 2;
                int p = aValue.CompareTo(aList[mid]);
                if (p == 0)
                    return mid;
                if (p > 0)
                    max = mid;
                else
                    min = mid;
            }
            return -min - 1;
        }
        else
        {   // smallest element comes first

            if (aValue.CompareTo(aList[min]) > 0)
                return -1;
            if (aValue.CompareTo(aList[max]) < 0)
                return -aList.Count;
            while (min < max - 1)
            {
                int mid = (max + min) / 2;
                int p = aValue.CompareTo(aList[mid]);
                if (p == 0)
                    return mid;
                if (p > 0)
                    min = mid;
                else
                    max = mid;
            }
            return -min - 1;
        }
    }
}
