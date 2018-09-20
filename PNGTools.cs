/* * * * *
 * PNGTools.cs
 * ------------------------------
 * 
 * Those PNG tools were quickly created for a UnityAnswers question:
 * https://answers.unity.com/questions/1436933/encodetopng-with-custom-ppi.html
 * 
 * It's main feature is to parse the PNG structure and break it into its chunks.
 * Additionally (the only feature required atm) you can change the PPM settings
 * of a PNG file. For this it searches for the "pHYs" chunk (if none is found it
 * will add one) and set the given PPM (or converted PPI) values.
 * 
 * 
 * Written by Bunny83 
 * 2017-11-30
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2017 Markus Göbel (Bunny83)
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
using System.IO;
using UnityEngine;

namespace B83.Image
{
    public enum EPNGChunkType : uint
    {
        IHDR = 0x49484452,
        sRBG = 0x73524742,
        gAMA = 0x67414D41,
        cHRM = 0x6348524D,
        pHYs = 0x70485973,
        IDAT = 0x49444154,
        IEND = 0x49454E44,
    }

    public class PNGFile
    {
        public static ulong m_Signature = 0x89504E470D0A1A0AU; // 
        public ulong Signature;
        public List<PNGChunk> chunks;
        public int FindChunk(EPNGChunkType aType, int aStartIndex = 0)
        {
            if (chunks == null)
                return -1;
            for(int i = aStartIndex; i < chunks.Count; i++)
            {
                if (chunks[i].type == aType)
                    return i;
            }
            return -1;
        }
    }

    public class PNGChunk
    {
        public uint length;
        public EPNGChunkType type;
        public byte[] data;
        public uint crc;
        public uint CalcCRC()
        {
            var crc = PNGTools.UpdateCRC(0xFFFFFFFF, (uint)type);
            crc = PNGTools.UpdateCRC(crc, data);
            return crc ^ 0xFFFFFFFF;
        }
    }

    public class PNGTools
    {
        static uint[] crc_table = new uint[256];
        static PNGTools()
        {
            for (int n = 0; n < 256; n++)
            {
                uint c = (uint)n;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) > 0)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c = c >> 1;
                }
                crc_table[n] = c;
            }
        }

        public static uint UpdateCRC(uint crc, byte aData)
        {
            return crc_table[(crc ^ aData) & 0xff] ^ (crc >> 8);
        }

        public static uint UpdateCRC(uint crc, uint aData)
        {
            crc = crc_table[(crc ^ ((aData >> 24) & 0xFF)) & 0xff] ^ (crc >> 8);
            crc = crc_table[(crc ^ ((aData >> 16) & 0xFF)) & 0xff] ^ (crc >> 8);
            crc = crc_table[(crc ^ ((aData >> 8) & 0xFF)) & 0xff] ^ (crc >> 8);
            crc = crc_table[(crc ^ (aData & 0xFF)) & 0xff] ^ (crc >> 8);
            return crc;
        }


        public static uint UpdateCRC(uint crc, byte[] buf)
        {
            for (int n = 0; n < buf.Length; n++)
                crc = crc_table[(crc ^ buf[n]) & 0xff] ^ (crc >> 8);
            return crc;
        }

        public static uint CalculateCRC(byte[] aBuf)
        {
            return UpdateCRC(0xffffffff, aBuf) ^ 0xffffffff;
        }
        public static List<PNGChunk> ReadChunks(BinaryReader aReader)
        {
            var res = new List<PNGChunk>();
            while (aReader.BaseStream.Position < aReader.BaseStream.Length - 4)
            {
                var chunk = new PNGChunk();
                chunk.length = aReader.ReadUInt32BE();
                if (aReader.BaseStream.Position >= aReader.BaseStream.Length - 4 - chunk.length)
                    break;
                res.Add(chunk);
                chunk.type = (EPNGChunkType)aReader.ReadUInt32BE();
                chunk.data = aReader.ReadBytes((int)chunk.length);
                chunk.crc = aReader.ReadUInt32BE();

                uint crc = chunk.CalcCRC();

                if ((chunk.crc ^ crc) != 0)
                    Debug.Log("Chunk CRC wrong. Got 0x" + chunk.crc.ToString("X8") + " expected 0x" + crc.ToString("X8"));
                if (chunk.type == EPNGChunkType.IEND)
                    break;
            }
            return res;
        }

        public static PNGFile ReadPNGFile(BinaryReader aReader)
        {
            if (aReader == null || aReader.BaseStream.Position >= aReader.BaseStream.Length - 8)
                return null;
            var file = new PNGFile();
            file.Signature = aReader.ReadUInt64BE();
            file.chunks = ReadChunks(aReader);
            return file;
        }
        public static void WritePNGFile(PNGFile aFile, BinaryWriter aWriter)
        {
            aWriter.WriteUInt64BE(PNGFile.m_Signature);
            foreach (var chunk in aFile.chunks)
            {
                aWriter.WriteUInt32BE((uint)chunk.data.Length);
                aWriter.WriteUInt32BE((uint)chunk.type);
                aWriter.Write(chunk.data);
                aWriter.WriteUInt32BE(chunk.crc);
            }
        }

        public static void SetPPM(PNGFile aFile, uint aXPPM, uint aYPPM)
        {
            int pos = aFile.FindChunk(EPNGChunkType.pHYs);
            PNGChunk chunk;
            if (pos > 0)
            {
                chunk = aFile.chunks[pos];
                if (chunk.data == null || chunk.data.Length < 9)
                    throw new System.Exception("PNG: pHYs chunk data size is too small. It should be at least 9 bytes");
            }
            else
            {
                chunk = new PNGChunk();
                chunk.type = EPNGChunkType.pHYs;
                chunk.length = 9;
                chunk.data = new byte[9];
                aFile.chunks.Insert(1, chunk); // insert right afrer IHDR
            }
            var data = chunk.data;
            data[0] = (byte)((aXPPM >> 24) & 0xFF);
            data[1] = (byte)((aXPPM >> 16) & 0xFF);
            data[2] = (byte)((aXPPM >> 8) & 0xFF);
            data[3] = (byte)((aXPPM) & 0xFF);

            data[4] = (byte)((aYPPM >> 24) & 0xFF);
            data[5] = (byte)((aYPPM >> 16) & 0xFF);
            data[6] = (byte)((aYPPM >> 8) & 0xFF);
            data[7] = (byte)((aYPPM) & 0xFF);

            data[8] = 1;
            chunk.crc = chunk.CalcCRC();
        }

        public static byte[] ChangePPM(byte[] aPNGData, uint aXPPM, uint aYPPM)
        {
            PNGFile file;
            using (var stream = new MemoryStream(aPNGData))
            using (var reader = new BinaryReader(stream))
            {
                file = ReadPNGFile(reader);
            }
            SetPPM(file, aXPPM, aYPPM);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                WritePNGFile(file, writer);
                return stream.ToArray();
            }
        }
        public static byte[] ChangePPI(byte[] aPNGData, float aXPPI, float aYPPI)
        {
            return ChangePPM(aPNGData, (uint)(aXPPI * 39.3701f), (uint)(aYPPI * 39.3701f));
        }
    }

    public static class BinaryReaderWriterExt
    {
        public static uint ReadUInt32BE(this BinaryReader aReader)
        {
            return ((uint)aReader.ReadByte() << 24) | ((uint)aReader.ReadByte() << 16)
                | ((uint)aReader.ReadByte() << 8) | ((uint)aReader.ReadByte());
        }
        public static ulong ReadUInt64BE(this BinaryReader aReader)
        {
            return (ulong)aReader.ReadUInt32BE()<<32 | aReader.ReadUInt32BE();
        }
        public static void WriteUInt32BE(this BinaryWriter aWriter, uint aValue)
        {
            aWriter.Write((byte)((aValue >> 24) & 0xFF));
            aWriter.Write((byte)((aValue >> 16) & 0xFF));
            aWriter.Write((byte)((aValue >> 8 ) & 0xFF));
            aWriter.Write((byte)((aValue      ) & 0xFF));
        }
        public static void WriteUInt64BE(this BinaryWriter aWriter, ulong aValue)
        {
            aWriter.WriteUInt32BE((uint)(aValue >> 32));
            aWriter.WriteUInt32BE((uint)(aValue));
        }
    }
}
