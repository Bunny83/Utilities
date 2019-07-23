#region License and Information
/*****
* Some simple struct List implementations. StructList4 has a fix capacity of 4,
* StructList8 a capacity of 8 and StructList16 a capacity of 16 elements. It's
* useful when you want to return a relatively small collection from a method
* without heap memory allocation. They provide the usual List operations: Add,
* Insert, Remove, RemoveAt, IndexOf, LastIndexOf, Contains, Clear. The struct
* list includes a struct Enumerator which also shouldn't allocate memory. It
* also has a ToList method which will create a normal generic List<T> out of
* the struct list. The Linq property provides an IEnumerable<T> interface if
* required. Of course ToList() and Linq WILL allocate memory.
* 
* [License]
* Copyright (c) 2018 Markus Göbel (Bunny83)
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
using System.Collections.Generic;

namespace B83.StructLists
{
    public struct StructList4<T>
    {
        private static T DEF = default(T);
        public const int CAPACITY = 4;
        public int count;
        public T e0, e1, e2, e3;

        public struct Enumerator
        {
            private StructList4<T> list;
            private int state;
            public Enumerator(StructList4<T> aList)
            {
                list = aList;
                state = -1;
            }
            public T Current
            {
                get { return list[state]; }
            }
            public bool MoveNext()
            {
                return ++state >= 0 && state < list.count;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        public IEnumerable<T> Linq { get { foreach (T item in this) yield return item; } }

        #region constructors
        public StructList4(T aE0, T aE1, T aE2, T aE3)
        {
            e0 = aE0; e1 = aE1; e2 = aE2; e3 = aE3; count = 4;
        }
        public StructList4(T aE0, T aE1, T aE2) : this(aE0, aE1, aE2, DEF) { count = 3; }
        public StructList4(T aE0, T aE1 ) : this(aE0, aE1, DEF, DEF) { count = 2; }
        public StructList4(T aE0) : this(aE0, DEF, DEF, DEF) { count = 1; }
        public StructList4(IList<T> aItems, int aOffset = 0)
        {
            int c = aItems.Count - aOffset;
            count = 0;
            e0 = (count < c) ? aItems[aOffset + count++] : DEF;
            e1 = (count < c) ? aItems[aOffset + count++] : DEF;
            e2 = (count < c) ? aItems[aOffset + count++] : DEF;
            e3 = (count < c) ? aItems[aOffset + count++] : DEF;
        }
        #endregion constructors

        public T this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= count)
                    throw new System.IndexOutOfRangeException("aIndex out of range");
                switch (aIndex)
                {
                    case 0: return e0;
                    case 1: return e1;
                    case 2: return e2;
                    case 3: return e3;
                    default: return default(T);
                }
            }
            set
            {
                if (aIndex < 0 || aIndex >= count)
                    throw new System.IndexOutOfRangeException("aIndex out of range");
                switch (aIndex)
                {
                    case 0: e0 = value; break;
                    case 1: e1 = value; break;
                    case 2: e2 = value; break;
                    case 3: e3 = value; break;
                }
            }
        }

        public bool Add(T aItem)
        {
            switch (count)
            {
                case 0: e0 = aItem; ++count; return true;
                case 1: e1 = aItem; ++count; return true;
                case 2: e2 = aItem; ++count; return true;
                case 3: e3 = aItem; ++count; return true;
                default: return false;
            }
        }

        public int IndexOf(T aItem)
        {
            var comp = EqualityComparer<T>.Default;
            if (count <= 0) return -1; if (comp.Equals(e0, aItem)) return 0;
            if (count <= 1) return -1; if (comp.Equals(e1, aItem)) return 1;
            if (count <= 2) return -1; if (comp.Equals(e2, aItem)) return 2;
            if (count <= 3) return -1; if (comp.Equals(e3, aItem)) return 3;
            return -1;
        }

        public int LastIndexOf(T aItem)
        {
            var comp = EqualityComparer<T>.Default;
            if (count > 3 && comp.Equals(e3, aItem)) return 3;
            if (count > 2 && comp.Equals(e2, aItem)) return 2;
            if (count > 1 && comp.Equals(e1, aItem)) return 1;
            if (count > 0 && comp.Equals(e0, aItem)) return 0;
            return -1;
        }

        public bool Contains(T aItem)
        {
            return IndexOf(aItem) >= 0;
        }

        public T RemoveAt(int aIndex)
        {
            if (aIndex < 0 || aIndex >= count)
                throw new System.IndexOutOfRangeException("aIndex out of range");
            T tmp = this[aIndex];
            for (int i = aIndex; i < count - 1; i++)
                this[i] = this[i + 1];
            this[count - 1] = default(T);
            --count;
            return tmp;
        }

        public bool Remove(T aItem)
        {
            int index = IndexOf(aItem);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        public bool Insert(int aIndex, T aItem)
        {
            if (aIndex < 0 || aIndex > count || count == CAPACITY)
                return false;
            ++count;
            for (int i = count - 2; i >= aIndex; i--)
                this[i + 1] = this[i];
            this[aIndex] = aItem;
            return true;
        }

        public void Swap(int aFirst, int aSecond)
        {
            if (aFirst < 0 || aSecond < 0 || aFirst >= count || aSecond >= count || aFirst == aSecond)
                return;
            T tmp = this[aFirst];
            this[aFirst] = this[aSecond];
            this[aSecond] = tmp;
        }

        public void Clear()
        {
            e0 = e1 = e2 = e3 = default(T);
            count = 0;
        }
        public List<T> ToList()
        {
            var res = new List<T>(count);
            foreach(T item in this)
                res.Add(item);
            return res;
        }
    }

    public struct StructList8<T>
    {
        private static T DEF = default(T);
        public const int CAPACITY = 8;
        public int count;
        public T e0, e1, e2, e3, e4, e5, e6, e7;

        public struct Enumerator
        {
            private StructList8<T> list;
            private int state;
            public Enumerator(StructList8<T> aList)
            {
                list = aList;
                state = -1;
            }
            public T Current
            {
                get { return list[state]; }
            }
            public bool MoveNext()
            {
                return ++state >= 0 && state < list.count;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        public IEnumerable<T> Linq { get { foreach (T item in this) yield return item; } }

        #region constructors
        public StructList8(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7)
        {
            e0 = aE0; e1 = aE1; e2 = aE2; e3 = aE3; e4 = aE4; e5 = aE5; e6 = aE6; e7 = aE7;
            count = 8;
        }
        public StructList8(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6) : this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, DEF) { count = 7; }
        public StructList8(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5) : this(aE0, aE1, aE2, aE3, aE4, aE5, DEF, DEF) { count = 6; }
        public StructList8(T aE0, T aE1, T aE2, T aE3, T aE4) : this(aE0, aE1, aE2, aE3, aE4, DEF, DEF, DEF) { count = 5; }
        public StructList8(T aE0, T aE1, T aE2, T aE3)
        {
            e0 = aE0; e1 = aE1; e2 = aE2; e3 = aE3; e4 = e5 = e6 = e7 = DEF;
            count = 4;
        }
        public StructList8(T aE0, T aE1, T aE2) : this(aE0, aE1, aE2, DEF ) { count = 3; }
        public StructList8(T aE0, T aE1) : this(aE0, aE1, DEF, DEF) { count = 2; }
        public StructList8(T aE0) : this(aE0, DEF, DEF, DEF) { count = 1; }
        public StructList8(IList<T> aItems, int aOffset = 0)
        {
            int c = aItems.Count - aOffset;
            count = 0;
            e0 = (count < c) ? aItems[aOffset + count++] : DEF;
            e1 = (count < c) ? aItems[aOffset + count++] : DEF;
            e2 = (count < c) ? aItems[aOffset + count++] : DEF;
            e3 = (count < c) ? aItems[aOffset + count++] : DEF;
            e4 = (count < c) ? aItems[aOffset + count++] : DEF;
            e5 = (count < c) ? aItems[aOffset + count++] : DEF;
            e6 = (count < c) ? aItems[aOffset + count++] : DEF;
            e7 = (count < c) ? aItems[aOffset + count++] : DEF;
        }

        #endregion constructors

        public T this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= count)
                    throw new System.IndexOutOfRangeException("aIndex out of range");
                switch (aIndex)
                {
                    case 0: return e0;
                    case 1: return e1;
                    case 2: return e2;
                    case 3: return e3;
                    case 4: return e4;
                    case 5: return e5;
                    case 6: return e6;
                    case 7: return e7;
                    default: return default(T);
                }
            }
            set
            {
                if (aIndex < 0 || aIndex >= count)
                    throw new System.IndexOutOfRangeException("aIndex out of range");
                switch (aIndex)
                {
                    case 0: e0 = value; break;
                    case 1: e1 = value; break;
                    case 2: e2 = value; break;
                    case 3: e3 = value; break;
                    case 4: e4 = value; break;
                    case 5: e5 = value; break;
                    case 6: e6 = value; break;
                    case 7: e7 = value; break;
                }
            }
        }

        public bool Add(T aItem)
        {
            switch (count)
            {
                case 0: e0 = aItem; ++count; return true;
                case 1: e1 = aItem; ++count; return true;
                case 2: e2 = aItem; ++count; return true;
                case 3: e3 = aItem; ++count; return true;
                case 4: e4 = aItem; ++count; return true;
                case 5: e5 = aItem; ++count; return true;
                case 6: e6 = aItem; ++count; return true;
                case 7: e7 = aItem; ++count; return true;
                default: return false;
            }
        }

        public int IndexOf(T aItem)
        {
            var comp = EqualityComparer<T>.Default;
            if (count <= 0) return -1; if (comp.Equals(e0, aItem)) return 0;
            if (count <= 1) return -1; if (comp.Equals(e1, aItem)) return 1;
            if (count <= 2) return -1; if (comp.Equals(e2, aItem)) return 2;
            if (count <= 3) return -1; if (comp.Equals(e3, aItem)) return 3;
            if (count <= 4) return -1; if (comp.Equals(e4, aItem)) return 4;
            if (count <= 5) return -1; if (comp.Equals(e5, aItem)) return 5;
            if (count <= 6) return -1; if (comp.Equals(e6, aItem)) return 6;
            if (count <= 7) return -1; if (comp.Equals(e7, aItem)) return 7;
            return -1;
        }

        public int LastIndexOf(T aItem)
        {
            var comp = EqualityComparer<T>.Default;
            if (count > 7 && comp.Equals(e7, aItem)) return 7;
            if (count > 6 && comp.Equals(e6, aItem)) return 6;
            if (count > 5 && comp.Equals(e5, aItem)) return 5;
            if (count > 4 && comp.Equals(e7, aItem)) return 4;
            if (count > 3 && comp.Equals(e3, aItem)) return 3;
            if (count > 2 && comp.Equals(e2, aItem)) return 2;
            if (count > 1 && comp.Equals(e1, aItem)) return 1;
            if (count > 0 && comp.Equals(e0, aItem)) return 0;
            return -1;
        }

        public bool Contains(T aItem)
        {
            return IndexOf(aItem) >= 0;
        }

        public T RemoveAt(int aIndex)
        {
            if (aIndex < 0 || aIndex >= count)
                throw new System.IndexOutOfRangeException("aIndex out of range");
            T tmp = this[aIndex];
            for (int i = aIndex; i < count - 1; i++)
                this[i] = this[i + 1];
            this[count - 1] = default(T);
            --count;
            return tmp;
        }

        public bool Remove(T aItem)
        {
            int index = IndexOf(aItem);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        public bool Insert(int aIndex, T aItem)
        {
            if (aIndex < 0 || aIndex > count || count == CAPACITY)
                return false;
            ++count;
            for (int i = count - 2; i >= aIndex; i--)
                this[i + 1] = this[i];
            this[aIndex] = aItem;
            return true;
        }

        public void Swap(int aFirst, int aSecond)
        {
            if (aFirst < 0 || aSecond < 0 || aFirst >= count || aSecond >= count || aFirst == aSecond)
                return;
            T tmp = this[aFirst];
            this[aFirst] = this[aSecond];
            this[aSecond] = tmp;
        }

        public void Clear()
        {
            e0 = e1 = e2 = e3 = e4 = e5 = e6 = e7 = default(T);
            count = 0;
        }
        public List<T> ToList()
        {
            var res = new List<T>(count);
            foreach (T item in this)
                res.Add(item);
            return res;
        }
    }

    public struct StructList16<T>
    {
        private static T DEF = default(T);
        public const int CAPACITY = 16;
        public int count;
        public T e0, e1, e2, e3, e4, e5, e6, e7;
        public T e8, e9, e10, e11, e12, e13, e14, e15;

        public struct Enumerator
        {
            private StructList16<T> list;
            private int state;
            public Enumerator(StructList16<T> aList)
            {
                list = aList;
                state = -1;
            }
            public T Current
            {
                get { return list[state]; }
            }
            public bool MoveNext()
            {
                return ++state >= 0 && state < list.count;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        public IEnumerable<T> Linq { get { foreach (T item in this) yield return item; } }

        #region constructors
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8, T aE9, T aE10, T aE11, T aE12, T aE13, T aE14, T aE15)
        {
            e0 = aE0; e1 = aE1; e2  = aE2;  e3  = aE3;  e4  = aE4;  e5  = aE5;  e6  = aE6;  e7  = aE7;
            e8 = aE8; e9 = aE9; e10 = aE10; e11 = aE11; e12 = aE12; e13 = aE13; e14 = aE14; e15 = aE15;
            count = 16;
        }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8, T aE9, T aE10, T aE11, T aE12, T aE13, T aE14) :
            this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, aE7, aE8, aE9, aE10, aE11, aE12, aE13, aE14, DEF) { count = 15; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8, T aE9, T aE10, T aE11, T aE12, T aE13) :
            this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, aE7, aE8, aE9, aE10, aE11, aE12, aE13, DEF, DEF) { count = 14; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8, T aE9, T aE10, T aE11, T aE12) :
            this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, aE7, aE8, aE9, aE10, aE11, aE12, DEF, DEF, DEF) { count = 13; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8, T aE9, T aE10, T aE11) :
            this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, aE7, aE8, aE9, aE10, aE11, DEF, DEF, DEF, DEF) { count = 12; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8, T aE9, T aE10) :
            this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, aE7, aE8, aE9, aE10, DEF, DEF, DEF, DEF, DEF) { count = 11; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8, T aE9) :
            this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, aE7, aE8, aE9, DEF, DEF, DEF, DEF, DEF, DEF) { count = 10; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7, T aE8) :
            this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, aE7, aE8, DEF, DEF, DEF, DEF, DEF, DEF, DEF) { count = 9; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6, T aE7)
        {
            e0 = aE0; e1 = aE1; e2 = aE2; e3 = aE3; e4 = aE4; e5 = aE5; e6 = aE6; e7 = aE7;
            e8 = e9 = e10 = e11 = e12 = e13 = e14 = e15 = DEF;
            count = 8;
        }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5, T aE6) : this(aE0, aE1, aE2, aE3, aE4, aE5, aE6, DEF) { count = 7; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4, T aE5) : this(aE0, aE1, aE2, aE3, aE4, aE5, DEF, DEF) { count = 6; }
        public StructList16(T aE0, T aE1, T aE2, T aE3, T aE4) : this(aE0, aE1, aE2, aE3, aE4, DEF, DEF, DEF) { count = 5; }
        public StructList16(T aE0, T aE1, T aE2, T aE3) : this(aE0, aE1, aE2, aE3, DEF, DEF, DEF, DEF) { count = 4; }
        public StructList16(T aE0, T aE1, T aE2) : this(aE0, aE1, aE2, DEF, DEF, DEF, DEF, DEF) { count = 3; }
        public StructList16(T aE0, T aE1) : this(aE0, aE1, DEF, DEF, DEF, DEF, DEF, DEF) { count = 2; }
        public StructList16(T aE0) : this(aE0, DEF, DEF, DEF, DEF, DEF, DEF, DEF) { count = 1; }
        public StructList16(IList<T> aItems, int aOffset = 0)
        {
            int c = aItems.Count - aOffset;
            count = 0;
            e0 = (count < c) ? aItems[aOffset + count++] : DEF;
            e1 = (count < c) ? aItems[aOffset + count++] : DEF;
            e2 = (count < c) ? aItems[aOffset + count++] : DEF;
            e3 = (count < c) ? aItems[aOffset + count++] : DEF;
            e4 = (count < c) ? aItems[aOffset + count++] : DEF;
            e5 = (count < c) ? aItems[aOffset + count++] : DEF;
            e6 = (count < c) ? aItems[aOffset + count++] : DEF;
            e7 = (count < c) ? aItems[aOffset + count++] : DEF;
            e8 = (count < c) ? aItems[aOffset + count++] : DEF;
            e9 = (count < c) ? aItems[aOffset + count++] : DEF;
            e10 = (count < c) ? aItems[aOffset + count++] : DEF;
            e11 = (count < c) ? aItems[aOffset + count++] : DEF;
            e12 = (count < c) ? aItems[aOffset + count++] : DEF;
            e13 = (count < c) ? aItems[aOffset + count++] : DEF;
            e14 = (count < c) ? aItems[aOffset + count++] : DEF;
            e15 = (count < c) ? aItems[aOffset + count++] : DEF;
        }
        #endregion constructors

        public T this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= count)
                    throw new System.IndexOutOfRangeException("aIndex out of range");
                switch (aIndex)
                {
                    case 0: return e0;
                    case 1: return e1;
                    case 2: return e2;
                    case 3: return e3;
                    case 4: return e4;
                    case 5: return e5;
                    case 6: return e6;
                    case 7: return e7;
                    case 8: return e8;
                    case 9: return e9;
                    case 10: return e10;
                    case 11: return e11;
                    case 12: return e12;
                    case 13: return e13;
                    case 14: return e14;
                    case 15: return e15;
                    default: return default(T);
                }
            }
            set
            {
                if (aIndex < 0 || aIndex >= count)
                    throw new System.IndexOutOfRangeException("aIndex out of range");
                switch (aIndex)
                {
                    case 0: e0 = value; break;
                    case 1: e1 = value; break;
                    case 2: e2 = value; break;
                    case 3: e3 = value; break;
                    case 4: e4 = value; break;
                    case 5: e5 = value; break;
                    case 6: e6 = value; break;
                    case 7: e7 = value; break;
                    case 8: e8 = value; break;
                    case 9: e9 = value; break;
                    case 10: e10 = value; break;
                    case 11: e11 = value; break;
                    case 12: e12 = value; break;
                    case 13: e13 = value; break;
                    case 14: e14 = value; break;
                    case 15: e15 = value; break;
                }
            }
        }

        public bool Add(T aItem)
        {
            switch(count)
            {
                case 0: e0 = aItem; ++count; return true;
                case 1: e1 = aItem; ++count; return true;
                case 2: e2 = aItem; ++count; return true;
                case 3: e3 = aItem; ++count; return true;
                case 4: e4 = aItem; ++count; return true;
                case 5: e5 = aItem; ++count; return true;
                case 6: e6 = aItem; ++count; return true;
                case 7: e7 = aItem; ++count; return true;
                case 8: e8 = aItem; ++count; return true;
                case 9: e9 = aItem; ++count; return true;
                case 10: e10 = aItem; ++count; return true;
                case 11: e11 = aItem; ++count; return true;
                case 12: e12 = aItem; ++count; return true;
                case 13: e13 = aItem; ++count; return true;
                case 14: e14 = aItem; ++count; return true;
                case 15: e15 = aItem; ++count; return true;
                default: return false;
            }
        }

        public int IndexOf(T aItem)
        {
            var comp = EqualityComparer<T>.Default;
            if (count <= 0) return -1; if (comp.Equals(e0, aItem)) return 0;
            if (count <= 1) return -1; if (comp.Equals(e1, aItem)) return 1;
            if (count <= 2) return -1; if (comp.Equals(e2, aItem)) return 2;
            if (count <= 3) return -1; if (comp.Equals(e3, aItem)) return 3;
            if (count <= 4) return -1; if (comp.Equals(e4, aItem)) return 4;
            if (count <= 5) return -1; if (comp.Equals(e5, aItem)) return 5;
            if (count <= 6) return -1; if (comp.Equals(e6, aItem)) return 6;
            if (count <= 7) return -1; if (comp.Equals(e7, aItem)) return 7;
            if (count <= 8) return -1; if (comp.Equals(e8, aItem)) return 8;
            if (count <= 9) return -1; if (comp.Equals(e9, aItem)) return 9;
            if (count <= 10) return -1; if (comp.Equals(e10, aItem)) return 10;
            if (count <= 11) return -1; if (comp.Equals(e11, aItem)) return 11;
            if (count <= 12) return -1; if (comp.Equals(e12, aItem)) return 12;
            if (count <= 13) return -1; if (comp.Equals(e13, aItem)) return 13;
            if (count <= 14) return -1; if (comp.Equals(e14, aItem)) return 14;
            if (count <= 15) return -1; if (comp.Equals(e15, aItem)) return 15;
            return -1;
        }

        public int LastIndexOf(T aItem)
        {
            var comp = EqualityComparer<T>.Default;
            if (count > 8)
            {
                if (count > 15 && comp.Equals(e15, aItem)) return 15;
                if (count > 14 && comp.Equals(e14, aItem)) return 14;
                if (count > 13 && comp.Equals(e13, aItem)) return 13;
                if (count > 12 && comp.Equals(e12, aItem)) return 12;
                if (count > 11 && comp.Equals(e11, aItem)) return 11;
                if (count > 10 && comp.Equals(e10, aItem)) return 10;
                if (count > 9 && comp.Equals(e9, aItem)) return 9;
                if (count > 8 && comp.Equals(e8, aItem)) return 8;
            }
            if (count > 7 && comp.Equals(e7, aItem)) return 7;
            if (count > 6 && comp.Equals(e6, aItem)) return 6;
            if (count > 5 && comp.Equals(e5, aItem)) return 5;
            if (count > 4 && comp.Equals(e7, aItem)) return 4;
            if (count > 3 && comp.Equals(e3, aItem)) return 3;
            if (count > 2 && comp.Equals(e2, aItem)) return 2;
            if (count > 1 && comp.Equals(e1, aItem)) return 1;
            if (count > 0 && comp.Equals(e0, aItem)) return 0;
            return -1;
        }

        public bool Contains(T aItem)
        {
            return IndexOf(aItem) >= 0;
        }

        public T RemoveAt(int aIndex)
        {
            if (aIndex < 0 || aIndex >= count)
                throw new System.IndexOutOfRangeException("aIndex out of range");
            T tmp = this[aIndex];
            for (int i = aIndex; i < count - 1; i++)
                this[i] = this[i + 1];
            this[count - 1] = default(T);
            --count;
            return tmp;
        }

        public bool Remove(T aItem)
        {
            int index = IndexOf(aItem);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        public bool Insert(int aIndex, T aItem)
        {
            if (aIndex < 0 || aIndex > count || count == CAPACITY)
                return false;
            ++count;
            for (int i = count - 2; i >= aIndex; i--)
                this[i+1] = this[i];
            this[aIndex] = aItem;
            return true;
        }

        public void Swap(int aFirst, int aSecond)
        {
            if (aFirst < 0 || aSecond < 0 || aFirst >= count || aSecond >= count || aFirst == aSecond)
                return;
            T tmp = this[aFirst];
            this[aFirst] = this[aSecond];
            this[aSecond] = tmp;
        }

        public void Clear()
        {
            e0 = e1 = e2 = e3 =
            e4 = e5 = e6 = e7 =
            e8 = e9 = e10 = e11 =
            e12 = e13 = e14 = e15 = default(T);
            count = 0;
        }

        public List<T> ToList()
        {
            var res = new List<T>(count);
            foreach (T item in this)
                res.Add(item);
            return res;
        }
    }
}
