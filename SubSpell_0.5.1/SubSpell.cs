using System;
using System.Collections.Generic;
using System.Text;

namespace SubSpell
{
    class SubSpell
    {
        public static void Main(string[] args)
		{
			Checker check = new Checker();

			if (args.Length == 0 || args[0].CompareTo(@"/?") == 0)
			{
				printUsage();
			}
			else
			{
				check.loadFile(args[0]);
				Console.ReadLine();
			}

			
		}

		private static void printUsage()
		{
			Console.WriteLine("\n SubSpell.exe usage:");
			Console.WriteLine("-------------------------------------");
			Console.WriteLine(" Subspell.exe file_to_check");
			Console.WriteLine("\n \\\\ accepts word as written");
			Console.WriteLine(" pressing enter without a word will");
			Console.WriteLine(" delete the word in question");
		}

    }
}
