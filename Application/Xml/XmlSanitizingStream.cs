﻿using System;
using System.IO;
using System.Text;

namespace FeedCenter.Xml;

/// <summary>
/// A StreamReader that excludes XML-illegal characters while reading.
/// </summary>
public class XmlSanitizingStream : StreamReader
{
    /// <summary>
    /// The character that denotes the end of a file has been reached.
    /// </summary>
    private const int Eof = -1;

    /// <summary>Create an instance of XmlSanitizingStream.</summary>
    /// <param name="streamToSanitize">
    /// The stream to sanitize of illegal XML characters.
    /// </param>
    public XmlSanitizingStream(Stream streamToSanitize)
        : base(streamToSanitize, true)
    {
    }

    public XmlSanitizingStream(Stream streamToSanitize, Encoding encoding)
        : base(streamToSanitize, encoding)
    {
    }

    /// <summary>
    /// Get whether an integer represents a legal XML 1.0 or 1.1 character. See
    /// the specification at w3.org for these characters.
    /// </summary>
    /// <param name="xmlVersion">
    /// The version number as a string. Use "1.0" for XML 1.0 character
    /// validation, and use "1.1" for XML 1.1 character validation.
    /// </param>
    /// <param name="character"> </param>
    public static bool IsLegalXmlChar(string xmlVersion, int character)
    {
        switch (xmlVersion)
        {
            case "1.1": // http://www.w3.org/TR/xml11/#charsets
                {
                    return
                        !(
                            character <= 0x8 ||
                            character == 0xB ||
                            character == 0xC ||
                            character is >= 0xE and <= 0x1F ||
                            character is >= 0x7F and <= 0x84 ||
                            character is >= 0x86 and <= 0x9F ||
                            character > 0x10FFFF
                        );
                }
            case "1.0": // http://www.w3.org/TR/REC-xml/#charsets
                {
                    return
                    character == 0x9 /* == '\t' == 9   */ ||
                    character == 0xA /* == '\n' == 10  */ ||
                    character == 0xD /* == '\r' == 13  */ ||
                    character is >= 0x20 and <= 0xD7FF ||
                    character is >= 0xE000 and <= 0xFFFD ||
                    character is >= 0x10000 and <= 0x10FFFF;
                }
            default:
                {
                    throw new ArgumentOutOfRangeException
                        (nameof(xmlVersion), @$"'{xmlVersion}' is not a valid XML version.");
                }
        }
    }

    /// <summary>
    /// Get whether an integer represents a legal XML 1.0 character. See the  
    /// specification at w3.org for these characters.
    /// </summary>
    public static bool IsLegalXmlChar(int character)
    {
        return IsLegalXmlChar("1.0", character);
    }

    public override int Read()
    {
        // Read each character, skipping over characters that XML has prohibited

        int nextCharacter;

        do
        {
            // Read a character

            if ((nextCharacter = base.Read()) == Eof)
            {
                // If the character denotes the end of the file, stop reading

                break;
            }
        }

        // Skip the character if it's prohibited, and try the next

        while (!IsLegalXmlChar(nextCharacter));

        return nextCharacter;
    }

    public override int Peek()
    {
        // Return the next legal XML character without reading it 

        int nextCharacter;

        do
        {
            // See what the next character is 

            nextCharacter = base.Peek();
        } while
        (
            // If it's prohibited XML, skip over the character in the stream
            // and try the next.
            !IsLegalXmlChar(nextCharacter) &&
            (nextCharacter = base.Read()) != Eof
        );

        return nextCharacter;
    } // method

    // The following methods are exact copies of the methods in TextReader, 
    // extracting by disassembling it in Reflector

    public override int Read(char[] buffer, int index, int count)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (buffer.Length - index < count)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }

        var num = 0;
        do
        {
            var num2 = Read();
            if (num2 == -1)
            {
                return num;
            }

            buffer[index + num++] = (char) num2;
        } while (num < count);

        return num;
    }

    public override int ReadBlock(char[] buffer, int index, int count)
    {
        int num;
        var num2 = 0;
        do
        {
            num2 += num = Read(buffer, index + num2, count - num2);
        } while (num > 0 && num2 < count);

        return num2;
    }

    public override string ReadLine()
    {
        var builder = new StringBuilder();
        while (true)
        {
            var num = Read();
            switch (num)
            {
                case -1:
                    return builder.Length > 0 ? builder.ToString() : null;

                case 13:
                case 10:
                    if (num == 13 && Peek() == 10)
                    {
                        Read();
                    }

                    return builder.ToString();
            }

            builder.Append((char) num);
        }
    }

    public override string ReadToEnd()
    {
        int num;
        var buffer = new char[0x1000];
        var builder = new StringBuilder(0x1000);
        while ((num = Read(buffer, 0, buffer.Length)) != 0)
        {
            builder.Append(buffer, 0, num);
        }

        return builder.ToString();
    }
} // class