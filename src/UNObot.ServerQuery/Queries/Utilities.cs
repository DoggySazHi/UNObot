using System.IO;
using System.Text;

namespace UNObot.ServerQuery.Queries
{
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
    }
}