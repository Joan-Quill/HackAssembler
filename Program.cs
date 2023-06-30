using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HackAssembler;

internal class Program
{

    private static int operations = 0;
    private static FileParser? parser;

    private static void Main(string[] args)
    {
        Console.WriteLine("This program will convert any ASM files into binary HACK files in the current directory.");
        Console.Write("Press any key to continue...");
        if (null != Console.ReadLine())
        {
            //Load all necessary files
            ConsoleWriter.ConsoleBar();
            List<string> fileNames = new();

            foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.asm", SearchOption.TopDirectoryOnly))
            {
                fileNames.Add(file);
                Console.WriteLine($"File captured at: {file}");
            }


            //Parse through each file independently
            parser = new FileParser();
            foreach (string file in fileNames)
            {
                ConsoleWriter.ConsoleBar();
                Console.WriteLine($"Working on {Path.GetFileName(file)}");
                parser.Parse(file);
                if(parser.OperationComplete) operations++;
            }

            ConsoleWriter.ConsoleFinish(new string[] { $"Completed {operations} operations with {parser.Errors} errors.",
                                                        "Press any key to close the application..."}, true);
        }
    }

}