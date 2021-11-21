using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SadPencil.IniToCsf
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = "input.csf.ini";
            if (args.Length >= 1)
            {
                filename = args[0];
            }

            string outFileName = "output.csf";
            if (args.Length >= 2)
            {
                outFileName = args[1];
            }

            try
            {

                Ra2CsfFile.CsfFile csf;
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    csf = Ra2CsfFile.CsfFile.LoadFromIniFile(stream);
                }
                using (var stream = new FileStream(outFileName, FileMode.Create))
                {
                    csf.WriteCsfFile(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR!!");
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
