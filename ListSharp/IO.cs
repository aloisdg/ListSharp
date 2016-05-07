﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListSharp
{
    class IO
    {
        public static string scriptfile = "";
        public static string currentdir = "";
        public static void setScriptFile(string[] args)
        {
            foreach (string s in args)
            {
                scriptfile = s;
            }



        }

        public static void setScriptLocation()
        {
            currentdir = Path.GetDirectoryName(scriptfile);
        }

        public static string getFullCode()
        {
            return File.ReadAllText(scriptfile);
        }


    }
}