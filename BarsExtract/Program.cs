using System;
using System.IO;
using BARS;

namespace BarsExtract
{
    public class Program
    {
        private static void DumpABarFile(string filePath)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            var dir = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory($@"{dir}\{name}");
            var fileIn = BARSAudio.Read(filePath);
            string info;
            fileIn.Export(dir, true, out info);
            if (info != "")
            {
                Console.WriteLine(filePath + ": " + info);
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                var filePath = args[0];
                filePath = Path.GetFullPath(filePath);
                FileAttributes attr = File.GetAttributes(filePath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var files = Directory.GetFiles(filePath);
                    foreach (var file in files)
                    {
                        DumpABarFile(file);
                    }
                }
                else
                {
                    DumpABarFile(filePath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
