using System;
using System.IO;
using System.Linq;

using MMX4TextTool.IO;

namespace MMX4TextTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("MMX4TextTool by gdkchan");
            Console.WriteLine("Version 0.1.0");
            Console.ResetColor();

            Console.Write(Environment.NewLine);

            if (args.Length != 3)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Usage:");
                Console.ResetColor();

                Console.Write(Environment.NewLine);

                Console.WriteLine("tool -xarc file.arc out_folder    Extracts all files on ARC to a folder");
                Console.WriteLine("tool -xtext file.arc output.txt   Dumps texts from a ARC with texts");
                Console.WriteLine("tool -itext file.arc input.txt    Inserts texts back into the ARC");
            }
            else
            {
                Archive ARC = Archive.FromFile(args[1]);

                switch (args[0])
                {
                    case "-xarc":
                        Directory.CreateDirectory(args[2]);
                        for (int i = 0; i < ARC.Files.Length; i++)
                        {
                            string FileName = Path.Combine(args[2], string.Format("File_{0:D5}.bin", i));
                            File.WriteAllBytes(FileName, ARC.Files[i].Data);
                        }
                        break;
                    case "-xtext": File.WriteAllText(args[2], Text.FromBytes(ARC.Files.Last().Data)); break;
                    case "-itext":
                        string Txt = File.ReadAllText(args[2]);
                        ARC.Files[ARC.Files.Length - 1].Data = Text.ToBytes(Txt);
                        Archive.ToFile(args[1], ARC);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("The option you specified is not valid!");
                        Console.ResetColor();
                        break;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Finished!");
                Console.ResetColor();
            }
        }
    }
}
