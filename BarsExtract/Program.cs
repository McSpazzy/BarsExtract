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
            var name = Path.GetFileNameWithoutExtension(filename);
            var dir = Path.GetDirectoryName(filename);
            Directory.CreateDirectory($@"{dir}\{name}");

            var fileIn = BARSAudio.Read(filename);
            foreach (var amta in fileIn.BWAV)
            {
                File.WriteAllBytes($@"{dir}\{name}\{amta.Name}.bwav", amta.Data);
                Console.WriteLine($@"{name}\{amta.Name}.bwav | {amta.Data.Length}");
            }
        }
    }
}
