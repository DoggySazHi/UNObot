using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UNObot.ServerQuery.Queries;

public static class Utilities
{
    public static string ReadNullTerminatedString(ref BinaryReader input)
    {
        var sb = new StringBuilder();
        var read = input.ReadChar();
        while (read != '\x00')
        {
            sb.Append(read);
            read = input.ReadChar();
        }

        return sb.ToString();
    }
        
    public static byte[] LittleEndianConverter(int data)
    {
        var output = BitConverter.GetBytes(data);
            
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        return output;
    }
        
    public static byte[] LittleEndianConverter(long data)
    {
        var output = BitConverter.GetBytes(data);
            
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        return output;
    }

    public static int LittleEndianReader(ref byte[] data, int startIndex)
    {
        // We duplicate the array to not modify the original set.
        return LittleEndianReader(Array.AsReadOnly(data), startIndex);
    }

    public static int LittleEndianReader(IReadOnlyList<byte> data, int startIndex)
    {
        var temp = new [] { data[startIndex], data[startIndex + 1], data[startIndex + 2], data[startIndex + 3] };
            
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(temp);
        }

        return BitConverter.ToInt32(temp);
    }
        
    public static long LittleEndianReaderLong(ref byte[] data, int startIndex)
    {
        // We duplicate the array to not modify the original set.
        return LittleEndianReader(Array.AsReadOnly(data), startIndex);
    }

    public static long LittleEndianReaderLong(IReadOnlyList<byte> data, int startIndex)
    {
        var temp = new [] { data[startIndex], data[startIndex + 1], data[startIndex + 2], data[startIndex + 3], data[startIndex + 4], data[startIndex + 5], data[startIndex + 6], data[startIndex + 7] };
            
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(temp);
        }

        return BitConverter.ToInt64(temp);
    }
}