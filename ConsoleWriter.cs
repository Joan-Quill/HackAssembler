using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackAssembler
{
    internal class ConsoleWriter
    {
        public static void VerboseFile(List<string> list)
        {
            foreach (string line in list) { Console.WriteLine(line); }
        }

        public static void ConsoleBar()
        {
            Console.WriteLine(new String('─', Console.BufferWidth - 1));
        }

        public static void ConsoleError(string[] error, bool wait)
        {
            ConsoleBar();
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR] ");
            Console.ForegroundColor = defaultColor;

            for (int i = 0; i < error.Length; i++)
            {
                if (i < error.Length - 1 || !wait)
                {
                    if (i == 0) Console.WriteLine(error[i]);
                    else Console.WriteLine("        " + error[i]);
                }
                else
                {
                    Console.Write("        " + error[i]);
                    Console.ReadKey();
                }
            }

        }

        public static void ConsoleFinish(string[] finish, bool wait)
        {
            ConsoleBar();
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[FINISH] ");
            Console.ForegroundColor = defaultColor;
            for (int i = 0; i < finish.Length; i++)
            {
                if (i < finish.Length - 1 || !wait)
                {
                    if (i == 0) Console.WriteLine(finish[i]);
                    else Console.WriteLine("          " + finish[i]);
                }
                else
                {
                    Console.Write("         " + finish[i]);
                    Console.ReadKey();
                }
            }
        }

    }
}
