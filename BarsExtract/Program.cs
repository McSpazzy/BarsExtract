using System;
using System.IO;
using BARS;

namespace BarsExtract
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var filename = args[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine("File does not exist.");
                return;
            }
            filename = Path.GetFullPath(filename);

            var name = Path.GetFileNameWithoutExtension(filename);
            var dir = Path.GetDirectoryName(filename);

            Directory.CreateDirectory($@"{dir}\{name}");

            var fileIn = BARSAudio.Read(filename);

            foreach (var itemName in fileIn.List)
            {
                Console.WriteLine($"{itemName}");
            }

            fileIn.Export(dir, true);
        }
    }
}
