﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ListSharp
{
    public static class codeParsing
    {


        public static bool startsWithSymbol(this string line)
        {
            return !Char.IsLetter(line[0]);
        }
        #region process different commands

        public static string processOperators(string line, int line_num)
        {

            if (line.StartsWith("{"))
            {
                return "{";
            }

            if (line.StartsWith("}"))
            {
                return "}";
            }

            if (line.StartsWith("//")) //to see if the code is commented out so it does net get into the final code (replaced with //skipped for debugging porpuses
            {
                return "//skipped";
            }

            if (line.StartsWith("#")) //to see if the code is commented out so it does net get into the final code (replaced with //skipped for debugging porpuses
            {
                return "//command executed: " + line;
            }

            if (line.StartsWith("<") && line.EndsWith(">")) //c# code
                return line;

            /* shouldnt be able to happen anymore */
            if (line.StartsWith("/*") || line.StartsWith("*/")) //to see if the code is commented out so it does net get into the final code
                return line;
            
            if (line.StartsWith("[") && line.EndsWith("]"))
                return processExpression(line, line_num);
            

            debug.throwException("Line: " + line_num + " invalid Operator", debug.importance.Fatal);

            return "";
        }

        public static string processExpression(string line, int line_num)
        {
            line = new Regex(@"\[(.*)\]").Match(line).Groups[1].Value; //everything between the square brackets "[]"

            if (line.StartsWith("FOREACH"))
            {
                if (line.StartsWith("FOREACH NUMB"))
                {
                    GroupCollection gc = new Regex(@"FOREACH NUMB IN (.*) AS (.*)").Match(line).Groups;
                    return "foreach (int " + gc[2].Value + " in " + serializeNumericRange(gc[1].Value) + ")";
                }
                if (line.StartsWith("FOREACH STRG"))
                {
                    GroupCollection gc = new Regex(@"FOREACH STRG IN (.*) AS (.*)").Match(line).Groups;
                    return "foreach (string " + gc[2].Value + " in " + gc[1].Value + ")";
                }

            }

            if (line.StartsWith("IF"))
            {
                return "if (" + ifBuilder(line) + ")";
            }

            debug.throwException("Line: " + line_num + " invalid Expression", debug.importance.Fatal);

            return "";
        }


        public static string processRows1(string line, int line_num, ROWS rowsVar)
        {


            if (line.StartsWith("ROWSPLIT")) //rowsplit command
            {
                GroupCollection gc = new Regex(@"ROWSPLIT ([^>]*) BY \[(.*)\]").Match(line).Groups;
                return " ROWSPLIT_F(" + gc[1].Value + "," + gc[2].Value + ");"; //interperted code
            }
            /*
            if (line.StartsWith("SELECT")) //rowsplit command
            {
                return " " + selectBuilder(line); //interperted code
            }

            */




            return processVariableIndependant(line, line_num, rowsVar);
        }

        public static string processNumb1(string line, int line_num, NUMB numbVar)
        {
            if (line.StartsWith("INPUT"))
            {
                GroupCollection gc = new Regex(@"\[(.*)\]").Match(line).Groups;
                return " (int)(long)INPT_F(" + gc[1].Value + ",typeof(int));";
            }

            return processVariableIndependant(line, line_num, numbVar);
        }

        public static string processVariableIndependant(string line, int line_num, Variable inpVar)
        {

            string cast = inpVar is STRG ? "string" : inpVar is ROWS ? "string[]" : "int";




            debug.throwException("Line: " + line_num + " invalid command", debug.importance.Fatal);

            return ""; //this never happens
        }
        #endregion

        #region SHOW command
        public static string processShow(string line, int line_num)
        {
            switch (line)
            {
                case "STRG":
                case "ROWS":
                case "ALL":
                    {
                        return (genericShow(line));
                    }
                default:
                    {
                        return "output += SHOW_F(" + line + ") + System.Environment.NewLine;";
                    }
            }
        }

        public static string genericShow(string type)
        {
            string returnedCode = "";
            returnedCode += "output += System.Environment.NewLine + \"Listing " + type + " variables\" + System.Environment.NewLine;";
            switch (type)
            {
                case "ALL":
                    {
                        returnedCode += typeShow("STRG") + "\n" + typeShow("ROWS");
                        break;
                    }
                default:
                    {
                        returnedCode += typeShow(type);
                        break;
                    }
            }
            return returnedCode;
        }

        public static string typeShow(string type)
        {
            string toReturn = "";
            foreach (Variable ver in memory.variables[type])
            {
                toReturn += "output += \"Listing " + ver.name + ":\" ;";
                toReturn += "output += SHOW_F(" + ver.name + ") + System.Environment.NewLine;"; //call SHOW_F() on any type of variable the users wants to display
            }
            return toReturn;
        }
        #endregion

        #region NOTF command
        public static string processNotification(string line, int line_num)
        {
            return "NOTIFY_F(" + line + ");";
        }
        #endregion

        #region DEBG command
        public static string processDebug(string line, int line_num)
        {
            return "DEBG_F(" + line + "," + line_num + ");";
        }
        #endregion

        #region OUTP command
        public static string processOutput(string line, int line_num)
        {
            GroupCollection gc = new Regex(@"(.*?) HERE\[(.*?)\]").Match(line).Groups;
            return "OUTP_F(" + codeParsing.processStrg(gc[2].Value) + ", " + gc[1].Value + ");"; //outputs to file
        }
        #endregion

        #region OUTP command
        public static string processOpen(string line, int line_num)
        {
            GroupCollection gc = new Regex(@"HERE\[(.*)\]").Match(line).Groups;
            return "OPEN_F(" + gc[1].Value + ");"; //outputs to file
        }
        #endregion

        public static string processStrg(string line) => "(string)ADD_F(typeof(string)," + line + ")";
        public static string processRows(string line) => "(stringarr)ADD_F(typeof(stringarr)," + line + ")";
        public static string processNumb(string line) => serializeNumericRange(line);

        public static string processCommand(string line, int line_num)
        {
            string processedLine = patternMatching.evaluateAllMatches(line);
            return processedLine == line ? line : processCommand(processedLine, line_num);
        }

        public static string processLine(string line, int line_num)
        {
            if (line.startsWithSymbol())
                return processOperators(line, line_num);


            string[] splitline = line.Split(new char[] { '=' }, 2); //splitting the line of "variable = evaluated string later to be parsed
            string varname = splitline[0].Substring(4).Trim(); //the first 4 characters will always be the variable type ex: strg,rows
            string start_argument = splitline[0].Substring(0, 4);
            splitline[1] = splitline[1].Substring(1);


            if (start_argument == "STRG")
                return varname + " = " + processStrg(processCommand(splitline[1], line_num)) + ";";


            if (start_argument == "ROWS")
                return varname + " = " + processRows(processCommand(splitline[1], line_num)) + ";";


            if (start_argument == "NUMB")
                return varname + " = " + processNumb(processCommand(splitline[1], line_num)) + ";";


            if (start_argument == "SHOW")
                return processShow(splitline[1], line_num);


            if (start_argument == "OUTP")
                return processOutput(splitline[1], line_num);


            if (start_argument == "DEBG")
                return processDebug(splitline[1], line_num);


            if (start_argument == "NOTF")
                return processNotification(splitline[1], line_num);


            if (start_argument == "OPEN")
                return processOpen(splitline[1], line_num);

            debug.throwException("Line: " + line + " could not be interpeted", debug.importance.Fatal);
            return "";

        }

        
        #region queryFunctions
        #region if
        public static string ifBuilder(string line)
        {
            line = new Regex(@"IF (.*)").Match(line).Groups[1].Value;
            GroupCollection gc = new Regex(@"(.*)(ISOVER|ISUNDER|ISEQUALOVER|ISEQUALUNDER|ISEQUAL|ISNOT|IS|CONTAINSNOT|CONTAINS)(.*)").Match(line).Groups;
            Tuple<string, string> variables = getVarnames2(line);
            Tuple<string, string> sides = new Tuple<string, string>(gc[1].Value, gc[3].Value);
            switch (gc[2].Value)
            {
                case "ISOVER":
                case "ISUNDER":
                case "ISEQUAL":
                case "ISEQUALOVER":
                case "ISEQUALUNDER":
                    return numericIf(variables, sides, baseDefinitions.operatorConversion[gc[2].Value]);

                case "CONTAINSNOT":
                case "CONTAINS":
                    return containIf(variables, sides, baseDefinitions.operatorConversion[gc[2].Value]);

                case "IS":
                case "ISNOT":
                    return equallityIf(variables, sides, baseDefinitions.operatorConversion[gc[2].Value]);
            }
            debug.throwException("if mode does not exist", debug.importance.Fatal);
            return "";

        }

        public static string numericIf(Tuple<string, string> variables, Tuple<string, string> line, string operation)
        {
            return serializeNumericString(line.Item1) + operation + serializeNumericString(line.Item2);
        }

        public static string containIf(Tuple<string, string> variables, Tuple<string, string> line, string operation)
        {
            return operation + variables.Item1 + ".Contains(" + variables.Item2 + ")";
        }

        public static string equallityIf(Tuple<string, string> variables, Tuple<string, string> line, string operation)
        {
            if (variables.Item1.ofVarType("ROWS") && variables.Item2.ofVarType("ROWS"))
                return operation == "==" ? variables.Item1 + ".SequenceEqual(" + variables.Item2 + ")" : "!" + variables.Item1 + ".SequenceEqual(" + variables.Item2 + ")";

            return variables.Item1 + operation + variables.Item2;
        }
        #endregion

        #region select
        public static string selectBuilder(string variableName,string query)
        {
            GroupCollection gc = new Regex(@"(.*)(ISOVER|ISUNDER|ISEQUALOVER|ISEQUALUNDER|ISEQUAL|ISNOT|IS|CONTAINSNOT|CONTAINS)(.*)").Match(query).Groups;
            Tuple<string, string> variables = getVarnames(query, variableName);
            Tuple<string, string> sides = new Tuple<string, string>(gc[1].Value, gc[3].Value);
            switch (gc[2].Value)
            {
                case "ISOVER":
                case "ISUNDER":
                case "ISEQUAL":
                case "ISEQUALOVER":
                case "ISEQUALUNDER":
                    return numericSelect(variables, sides, baseDefinitions.operatorConversion[gc[2].Value]);

                case "CONTAINSNOT":
                case "CONTAINS":
                    return containSelect(variables, sides, baseDefinitions.operatorConversion[gc[2].Value]);

                case "IS":
                case "ISNOT":
                    return equallitySelect(variables, sides, baseDefinitions.operatorConversion[gc[2].Value]);
            }
            debug.throwException("select mode does not exist", debug.importance.Fatal);
            return "";


        }

        public static string numericSelect(Tuple<string, string> variables, Tuple<string, string> line, string operation)
        {
            if (line.Item2.Contains("EVERY"))
                return variables.Item1 + ".Where(temp => returnLength(temp) " + operation + " " + variables.Item2 + ".Max(temp_2 => temp_2.Length)).ToArray()";

            if (line.Item2.Contains("ANY"))
                return variables.Item1 + ".Where(temp => returnLength(temp) " + operation + " " + variables.Item2 + ".Min(temp_2 => temp_2.Length)).ToArray()";

            return variables.Item1 + ".Where(temp => returnLength(temp) " + operation + " " + serializeNumericString(line.Item2) + ").ToArray()";
        }

        public static string containSelect(Tuple<string, string> variables, Tuple<string, string> line, string operation)
        {
            if (line.Item2.Contains("EVERY"))
                return variables.Item1 + ".Where(temp => " + variables.Item2 + ".Where(temp_2 => " + operation + "temp_2.Contains(temp)).ToArray().Length == " + variables.Item2 + ".Length).ToArray()";

            if (line.Item2.Contains("ANY"))
                return variables.Item1 + ".Where(temp => " + variables.Item2 + ".Where(temp_2 => " + operation + "temp_2.Contains(temp)).ToArray().Length > 0).ToArray()";

            return variables.Item1 + ".Where(temp => " + operation + "temp.Contains(" + variables.Item2 + ")).ToArray()";
        }

        public static string equallitySelect(Tuple<string, string> variables, Tuple<string, string> line, string operation)
        {
            bool positive = (operation == "==");
            bool enclusive = line.Item2.StartsWith(" EVERY");

            if (enclusive || line.Item2.StartsWith(" ANY"))
            {

                if (positive == enclusive)
                    return variables.Item1 + ".Where(temp => " + variables.Item2 + ".Where(temp_2 => temp_2 " + operation + " temp).ToArray().Length == " + variables.Item2 + ".Length).ToArray()";

                if (!enclusive)
                    return variables.Item1 + ".Where(temp => " + variables.Item2 + ".Where(temp_2 => temp_2 " + operation + " temp).ToArray().Length > 0).ToArray()";
            }

            return variables.Item1 + ".Where(temp => temp " + operation + " " + variables.Item2 + ").ToArray()";
        }

        public static Tuple<string, string> getVarnames(string inp, string var1)
        {
            string literal = "";
            if (inp.Contains("\""))
            {
                literal = new Regex("\"(.*)\"").Match(inp).Groups[0].Value;
                inp = inp.Replace(" " + literal, "");
            }

            string[] t = inp.Split(' ').Where(temp => !new string[] { "ANY", "EVERY", "LENGTH", "IN", "STRG", "IS", "ISNOT", "ISUNDER", "ISOVER", "ISEQUAL", "ISDIFF", "CONTAINS", "CONTAINSNOT" }.Contains(temp)).ToArray();
            if (t.Length == 0)
                return new Tuple<string, string>(var1, literal);
            return new Tuple<string, string>(var1, t[0]);
        }

        public static Tuple<string, string> getVarnames2(string inp)
        {
            string literal = "";
            if (inp.Contains("\""))
            {
                literal = new Regex("\"(.*)\"").Match(inp).Groups[0].Value;
                inp = inp.Replace(" " + literal, "");
            }

            string[] t = inp.Split(' ').Where(temp => !new string[] { "ANY", "EVERY", "LENGTH", "IN", "STRG", "IS", "ISNOT", "ISUNDER", "ISOVER", "ISEQUAL", "ISDIFF", "CONTAINS", "CONTAINSNOT" }.Contains(temp)).ToArray();
            if (t.Length == 1)
                return new Tuple<string, string>(t[0], literal);
            return new Tuple<string, string>(t[0], t[1]);
        }

        public static string serializeNumericString(string input)
        {
            foreach (Match m in Regex.Matches(input, @"(\w+) LENGTH"))
                input = input.Replace(m.Groups[0].Value, "returnLength(" + m.Groups[1].Value + ")");

            return input;
        }

        public static string serializeNumericRange(string input)
        {
            string[] rangeElements = input.Split(',');
            string query = "new List<IEnumerable<int>>() {";

            for (int i = 0; i < rangeElements.Length; i++)
            {
                string element = rangeElements[i];
                string[] splitElement = Regex.Split(element, " TO ");
                splitElement = splitElement.Select(n => serializeNumericString(n)).ToArray();

                query += element.Contains(" TO ") ? "EdgeRange(" + splitElement[0] + "," + splitElement[1] + ")" : "EdgeRange(" + splitElement[0] + "," + splitElement[0] + ")";
                if (i != rangeElements.Length - 1)
                    query += ",";
            }


            return query + "}.SelectMany(n => n).ToList()";
        }
        #endregion
        #endregion

    }
}
