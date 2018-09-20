#region License and Information
/*****
* This is a simple straight forward implementation of a Huffman encoding which
* allows you to compress data due to entropy analysis. It generates a Huffman
* tree which is first serialized into the output stream. The format is very
* compact and only has one bit overhead per tree node. The overall structure
* looks like this:
* 
*   - serialized Huffman tree (1 bit per node + 8 bit per leave-node)
*   - length of the original data (stored in a dynamical format, See "WriteDynamicLength" below)
*   - huffman encoded data
* 
* Copyright (c) 2015 Bunny83
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
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace B83.Compression
{
    public class Huffman
    {
        #region Node classes
        public interface INode : System.IComparable<INode>
        {
            int Weight { get; }
            INode Parent { get; set; }
            void Serialize(BitStreamWriter aWriter);
            LeafNode FindLeaf(BitStreamReader aReader);
        }
        public class LeafNode : INode
        {
            public int count;
            public byte data;
            public INode parentNode = null;
            public int bitCount;
            public uint bitPattern;
            public int Weight
            {
                get { return count; }
            }
            public INode Parent
            {
                get { return parentNode; }
                set { parentNode = value; }
            }
            public LeafNode(byte aData, int aCount)
            {
                data = aData;
                count = aCount;
            }
            public LeafNode(byte aData) : this(aData, 0) { }

            public void Serialize(BitStreamWriter aWriter)
            {
                aWriter.WriteBit(1);
                aWriter.WriteBits(data, 8);
            }

            public int CompareTo(INode other)
            {
                return other.Weight.CompareTo(Weight);
            }

            public LeafNode FindLeaf(BitStreamReader aReader)
            {
                return this;
            }
        }
        public class TreeNode : INode
        {
            public int weight;
            public INode left;
            public INode right;
            public INode parentNode = null;
            public int Weight
            {
                get { return weight; }
            }
            public INode Parent
            {
                get { return parentNode; }
                set { parentNode = value; }
            }
            public void RecalculateWeight()
            {
                weight = 0;
                if (left != null)
                    weight += left.Weight;
                if (right != null)
                    weight += right.Weight;
            }
            public void SetLeft(INode aLeft)
            {
                left = aLeft;
                if (left != null)
                    left.Parent = this;
            }
            public void SetRight(INode aRight)
            {
                right = aRight;
                if (right != null)
                    right.Parent = this;
            }
            public TreeNode() { }
            public TreeNode(INode aLeft, INode aRight)
            {
                SetLeft(aLeft);
                SetRight(aRight);
                RecalculateWeight();
            }
            public void Serialize(BitStreamWriter aWriter)
            {
                aWriter.WriteBit(0);
                left.Serialize(aWriter);
                right.Serialize(aWriter);
            }
            public int CompareTo(INode other)
            {
                return other.Weight.CompareTo(Weight);
            }

            public LeafNode FindLeaf(BitStreamReader aReader)
            {
                var b = aReader.ReadBit();
                if (b == 0)
                    return left.FindLeaf(aReader);
                return right.FindLeaf(aReader);
            }
        }
        #endregion Node classes

        #region BitStream reader / writer

        public class BitStreamWriter
        {
            Stream m_Stream;
            BinaryWriter m_Writer;
            public byte m_Data = 0;
            int m_Bits = 0;
            public long BitCount
            {
                get { return m_Stream.Length * 8 + m_Bits; }
            }
            public BitStreamWriter(Stream aStream)
            {
                m_Stream = aStream;
                m_Writer = new BinaryWriter(m_Stream);
            }
            public void WriteBit(byte aBit)
            {
                m_Data |= (byte)((aBit & 0x1) << (7 - m_Bits));
                if (++m_Bits >= 8)
                {
                    m_Writer.Write(m_Data);
                    m_Data = 0;
                    m_Bits = 0;
                }
            }

            public void WriteBits(ulong aVal, int aCount)
            {
                if (aCount < 0 || aCount > 32)
                    throw new System.ArgumentOutOfRangeException("aCount", "aCount must be between 0 and 32 inclusive");
                for (int i = 0; i < aCount; i++)
                    WriteBit((byte)((aVal >> (i)) & 0x1));
            }
            public void Flush()
            {
                if (m_Bits > 0)
                {
                    m_Writer.Write(m_Data);
                    m_Data = 0;
                    m_Bits = 0;
                }
            }
        }
        public class BitStreamReader
        {
            BinaryReader m_Reader;
            byte m_Data = 0;
            int m_Bits = 0;
            public BitStreamReader(Stream aStream)
            {
                m_Reader = new BinaryReader(aStream);
            }
            public byte ReadBit()
            {
                if (m_Bits <= 0)
                {
                    m_Data = m_Reader.ReadByte();
                    m_Bits = 8;
                }
                return (byte)((m_Data >> --m_Bits) & 1);
            }

            public ulong ReadBits(int aCount)
            {
                ulong val = 0UL;
                if (aCount < 0 || aCount > 32)
                    throw new System.ArgumentOutOfRangeException("aCount", "aCount must be between 0 and 32 inclusive");
                for (int i = 0; i < aCount; i++)
                    val |= ((ulong)ReadBit() << i);
                return val;
            }
        }

        #endregion BitStream reader / writer

        private static List<INode> Analyze(byte[] aData)
        {
            var res = new List<INode>();
            var nodes = new Dictionary<byte, LeafNode>();
            for (int i = 0; i < aData.Length; i++)
            {
                LeafNode node;
                if (!nodes.TryGetValue(aData[i], out node))
                {
                    node = new LeafNode(aData[i], 0);
                    nodes.Add(aData[i], node);
                    res.Add(node);
                }
                node.count++;
            }
            if (res.Count == 0) // ensure at least one node
                res.Add(new LeafNode(0, 0));
            res.Sort();
            return res;
        }
        private static INode GenerateTree(List<INode> aNodes)
        {
            if (aNodes.Count == 0)
                return null;
            else if (aNodes.Count == 1)
                return aNodes[0];
            else if (aNodes.Count == 2)
                return new TreeNode(aNodes[0], aNodes[1]);
            var l1 = new List<INode>(aNodes);
            var l2 = new List<INode>();
            System.Func<INode> GetNext = () =>
            {
                INode n = null;
                if (l1.Count > 0)
                    n = l1[l1.Count - 1];
                if (l2.Count > 0 && (n == null || l2[l2.Count - 1].Weight < n.Weight))
                {
                    n = l2[l2.Count - 1];
                    l2.RemoveAt(l2.Count - 1);
                }
                else if (l1.Count > 0)
                    l1.RemoveAt(l1.Count - 1);
                return n;
            };
            while (l1.Count > 0 || l2.Count > 1)
            {
                INode n1 = GetNext();
                INode n2 = GetNext();
                INode n = new TreeNode(n1, n2);
                l2.Add(n);
                l2.Sort();
            }
            return l2[0];
        }
        private static INode GenerateTree(BitStreamReader aReader)
        {
            var bit = aReader.ReadBit();
            if (bit != 0)
                return new LeafNode((byte)aReader.ReadBits(8));
            var n1 = GenerateTree(aReader);
            var n2 = GenerateTree(aReader);
            return new TreeNode(n1, n2);
        }
        private static Dictionary<byte, LeafNode> GenerateLookup(List<INode> aNodes)
        {
            var res = new Dictionary<byte, LeafNode>();
            for (int i = 0; i < aNodes.Count; i++)
            {
                var n = aNodes[i] as LeafNode;
                if (n == null)
                    return null;
                res.Add(n.data, n);
                int count = 0;
                uint pattern = 0;
                INode c = n;
                while (c.Parent != null)
                {
                    var p = (TreeNode)c.Parent;
                    if (p.right == c)
                        pattern |= (1u);
                    pattern <<= 1;
                    count++;
                    c = p;
                }
                n.bitPattern = pattern >> 1;
                n.bitCount = count;
            }
            return res;
        }

        //0 xxxxxxx   // val 0 - 127                 //  8 bits
        //10 xxxxxxxx // val 128 - 383               // 10 bits
        //11 xxxxxxxx0 // val 384 - 639              // 11 bits
        //11 xxxxxxxx1 xxxx0 // val 384 - 4479       // 16 bits
        //11 xxxxxxxx1 xxxx1 xxxx0 // 384 - 65919    // 21 bits
        //11 xxxxxxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx1 xxxx0  // 81 bits worst case
        public static void WriteDynamicLength(BitStreamWriter aWriter, ulong aValue)
        {
            if (aValue < 128)
            {
                aWriter.WriteBit(0);
                aWriter.WriteBits(aValue, 7);
            }
            else if (aValue < 384)
            {
                aWriter.WriteBit(1);
                aWriter.WriteBit(0);
                aWriter.WriteBits(aValue - 128UL, 8);
            }
            else
            {
                aValue -= 384UL;
                aWriter.WriteBit(1);
                aWriter.WriteBit(1);
                aWriter.WriteBits(aValue, 8);
                aValue >>= 8;
                while (aValue > 0)
                {
                    aWriter.WriteBit(1);
                    aWriter.WriteBits(aValue, 4);
                    aValue >>= 4;
                }
                aWriter.WriteBit(0);
            }
        }
        public static ulong ReadDynamicLength(BitStreamReader aReader)
        {
            if (aReader.ReadBit() == 0)
                return aReader.ReadBits(7);
            if (aReader.ReadBit() == 0)
                return aReader.ReadBits(8) + 128UL;

            ulong val = aReader.ReadBits(8);
            int shift = 8;
            while (aReader.ReadBit() == 1)
            {
                val |= aReader.ReadBits(4) << shift;
                shift += 4;
            }
            return val + 384UL;
        }

        public static byte[] Encode(byte[] aData)
        {
            using (MemoryStream data = new MemoryStream())
            {
                BitStreamWriter writer = new BitStreamWriter(data);
                var leaves = Analyze(aData);
                var tree = GenerateTree(leaves);
                tree.Serialize(writer);
                WriteDynamicLength(writer, (ulong)aData.LongLength);
                var lookup = GenerateLookup(leaves);

                for (int i = 0; i < aData.Length; i++)
                {
                    var n = lookup[aData[i]];
                    writer.WriteBits(n.bitPattern, n.bitCount);
                }
                writer.Flush();
                return data.ToArray();
            }
        }

        public static byte[] Decode(byte[] aData)
        {
            using (MemoryStream inStream = new MemoryStream(aData))
            using (MemoryStream data = new MemoryStream())
            {
                BitStreamReader reader = new BitStreamReader(inStream);
                BinaryWriter writer = new BinaryWriter(data);
                var tree = GenerateTree(reader);
                ulong count = ReadDynamicLength(reader);
                for (ulong i = 0; i < count; i++)
                {
                    var n = tree.FindLeaf(reader);
                    writer.Write(n.data);
                }
                writer.Flush();
                return data.ToArray();
            }
        }

        // EncodeStringToBase64 tries to huffman encode the input text and convert it
        // to base64. If the new length is smaller than the original text, it marks it
        // as "compressed" with a leading "$" otherwise it returns the unchanged text.
        // If the original text starts with "$" or "§" and is not compressed, it prefixes
        // the text with "§" to mark it as "uncomressed".
        public static string EncodeStringToBase64(string aText)
        {
            var data = Encoding.UTF8.GetBytes(aText);
            var compressed = Encode(data);
            var res = System.Convert.ToBase64String(compressed);
            if (res.Length < data.Length)
                return "$" + res;
            else if (aText[0] == '$' || aText[0] == '§')
                return "§" + aText;
            else
                return aText;
        }

        public static string DecodeStringFromBase64(string aText)
        {
            if (string.IsNullOrEmpty(aText))
                return "";
            if (aText[0] == '$')
            {
                var compressed = System.Convert.FromBase64String(aText.Substring(1));
                var data = Decode(compressed);
                return Encoding.UTF8.GetString(data);
            }
            else if (aText[0] == '§')
                return aText.Substring(1);
            else
                return aText;
        }
    }
}
