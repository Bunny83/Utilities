/* * * * *
 * Collection of several math methods.
 * 
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2017 Markus Göbel (Nunny83)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * * * * */

using System.Collections.Generic;
using UnityEngine;

namespace B83.MathHelpers
{
    public static class MathHelper
    {
        /// <summary>
        /// Calculates the signed area of the given polygon points. The points can be defined in any
        /// arbitrary plane in 3d space as long as they lie in the same plane.
        /// </summary>
        /// <param name="aPoints"></param>
        /// <returns>the signed area of the given polygon</returns>
        public static float SignedPolygonArea(IEnumerable<Vector3> aPoints)
        {
            if (aPoints == null)
                return 0f;
            IEnumerator<Vector3> enumerator = aPoints.GetEnumerator();
            if (!enumerator.MoveNext())
                return 0f;
            Vector3 offset = enumerator.Current;
            if (!enumerator.MoveNext())
                return 0f;
            Vector3 last = enumerator.Current - offset;
            if (!enumerator.MoveNext())
                return 0f;
            Vector3 cur = enumerator.Current - offset;
            Vector3 normal = Vector3.Cross(last, cur);
            float area = normal.magnitude;
            normal.Normalize();
            last = cur;
            while (enumerator.MoveNext())
            {
                cur = enumerator.Current - offset;
                area += Vector3.Dot(Vector3.Cross(last, cur), normal);
                last = cur;
            }
            return area * 0.5f;
        }

        /// <summary>
        /// This method returns the absolute value of the signed area of the given polygon.
        /// See "SignedPolygonArea" for more details
        /// </summary>
        /// <param name="aPoints"></param>
        /// <returns>a positive area value of the given polygon</returns>
        public static float PolygonArea(IEnumerable<Vector3> aPoints)
        {
            return Mathf.Abs(SignedPolygonArea(aPoints));
        }

        /// <summary>
        /// Calculates the signed area of the given polygon points. The points can be defined in any
        /// arbitrary plane in 3d space as long as they lie in the same plane.
        /// Note: This version directly uses a List. This can help to reduce garbage generation from
        ///       which "SignedPolygonArea" suffers.
        /// </summary>
        /// <param name="aPoints"></param>
        /// <returns>the signed area of the given polygon</returns>
        public static float SignedPolygonAreaList(List<Vector3> aPoints)
        {
            if (aPoints == null || aPoints.Count < 3)
                return 0f;
            Vector3 offset = aPoints[0];
            Vector3 last = aPoints[1] - offset;
            Vector3 cur = aPoints[2] - offset;
            Vector3 normal = Vector3.Cross(last, cur);
            float area = normal.magnitude;
            normal.Normalize();
            last = cur;
            for (int i = 3; i < aPoints.Count; i++)
            {
                cur = aPoints[i] - offset;
                area += Vector3.Dot(Vector3.Cross(last, cur), normal);
                last = cur;
            }
            return area * 0.5f;
        }

        /// <summary>
        /// This method returns the absolute value of the signed area of the given polygon.
        /// See "SignedPolygonAreaList" for more details
        /// </summary>
        /// <param name="aPoints"></param>
        /// <returns>a positive area value of the given polygon</returns>
        public static float PolygonAreaList(List<Vector3> aPoints)
        {
            return Mathf.Abs(SignedPolygonAreaList(aPoints));
        }

        /// <summary>
        /// Calculates the center of the circle that passes through the 3 given points
        /// </summary>
        /// <param name="aP0"></param>
        /// <param name="aP1"></param>
        /// <param name="aP2"></param>
        /// <param name="normal">returns the normal of the plane the circle lies in</param>
        /// <returns>The circle center position</returns>
        public static Vector3 CircleCenter(Vector3 aP0, Vector3 aP1, Vector3 aP2, out Vector3 normal)
        {
            // two circle chords
            var v1 = aP1 - aP0;
            var v2 = aP2 - aP0;

            normal = Vector3.Cross(v1, v2);
            if (normal.sqrMagnitude < 0.00001f)
                return Vector3.one * float.NaN;
            normal.Normalize();

            // perpendicular of both chords
            var p1 = Vector3.Cross(v1, normal).normalized;
            var p2 = Vector3.Cross(v2, normal).normalized;
            // distance between the chord midpoints
            var r = (v1 - v2) * 0.5f;
            // center angle between the two perpendiculars
            var c = Vector3.Angle(p1, p2);
            // angle between first perpendicular and chord midpoint vector
            var a = Vector3.Angle(r, p1);
            // law of sine to calculate length of p2
            var d = r.magnitude * Mathf.Sin(a * Mathf.Deg2Rad) / Mathf.Sin(c * Mathf.Deg2Rad);
            if (Vector3.Dot(v1, aP2 - aP1) > 0)
                return aP0 + v2 * 0.5f - p2 * d;
            return aP0 + v2 * 0.5f + p2 * d;
        }

    }

}
