/* * * * *
 * A simple CSV parser which works on a given TextReader. The TextReader is not
 * "owned" by the parser and therefore not Disposed. 
 * It provides two ways to parse a CSV file. Either line by line by using the
 * NextLine() method and provide an empty list to write the values of the line
 * to, or by using the Parse method which returns a jagged array of strings 
 * string[][]. The first method is better to parse a large CSV file on the fly.
 * 
 * The default value delimiter is the comma ','. However you can pass any other
 * character as delimiter, like ';' to the constructor.
 * 
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2019 Markus Göbel (Nunny83)
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
using System.Text;

public class CSVParser
{
    private char m_Delimiter;
    private TextReader m_Reader;
    private StringBuilder m_Sb = new StringBuilder();
    private bool isQuoted = false;
    public CSVParser(TextReader aReader, char aDelimiter = ',')
    {
        m_Reader = aReader;
        m_Delimiter = aDelimiter;
    }
    public CSVParser(string aText, char aDelimiter = ',') : this(new StringReader(aText), aDelimiter) { }

    public bool NextLine(List<string> aColumns)
    {
        aColumns.Clear();
        int chn;
        m_Sb.Clear();
        while ((chn = m_Reader.Read()) != -1)
        {
            char ch = (char)chn;
            if (isQuoted)
            {
                if (ch == '"')
                {
                    if ((char)m_Reader.Peek() == '"')
                    {
                        m_Reader.Read();
                        m_Sb.Append('"');
                    }
                    else
                        isQuoted = false;
                }
                else
                    m_Sb.Append(ch);
            }
            else
            {
                if (ch == m_Delimiter)
                {
                    aColumns.Add(m_Sb.ToString());
                    m_Sb.Clear();
                }
                else if (ch == '"')
                    isQuoted = true;
                else if (ch == '\r')
                {
                    if ((char)m_Reader.Peek() == '\n')
                        m_Reader.Read();
                    aColumns.Add(m_Sb.ToString());
                    m_Sb.Clear();
                    return true;
                }
                else if (ch == '\n')
                {
                    aColumns.Add(m_Sb.ToString());
                    m_Sb.Clear();
                    return true;
                }
                else
                    m_Sb.Append(ch);
            }
        }
        aColumns.Add(m_Sb.ToString());
        m_Sb.Clear();
        if (aColumns.Count == 1 && aColumns[0].Length == 0)
            return false;
        else
            return true;
    }
    public string[][] Parse(int aLinesToSkip = 0)
    {
        List<string> line = new List<string>();
        List<string[]> file = new List<string[]>();
        while (NextLine(line))
        {
            if (aLinesToSkip <= 0)
                file.Add(line.ToArray());
            else
                --aLinesToSkip;
        }
        return file.ToArray();
    }
}
