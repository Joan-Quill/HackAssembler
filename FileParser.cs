using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HackAssembler
{
    internal class FileParser
    {

        private Dictionary<string, int> defaultSymbols = new() 
        {
            {"R0", 0}, {"R1", 1}, {"R2", 2}, {"R3", 3}, {"R4", 4}, {"R5", 5}, {"R6", 6}, {"R7", 7},
            {"R8", 8}, {"R9", 9}, {"R10", 10}, {"R11", 11}, {"R12", 12}, {"R13", 13}, {"R14", 14}, {"R15", 15},
            {"SCREEN", 16384},
            {"KBD", 24576},
            {"SP", 0},
            {"LCL", 1},
            {"ARG", 2},
            {"THIS", 3},
            {"THAT", 4}
        };

        private Dictionary<string, byte> CompSymbols = new()
        {
            {"0",   42},
            {"1",   63},
            {"-1",  58},
            {"D",   12},
            {"A",   48}, {"M",   48},
            {"!D",  13},
            {"!A",  49}, {"!M",  49},
            {"-D",  15},
            {"-A",  51}, {"-M",  51},
            {"D+1", 31},
            {"A+1", 55}, {"M+1", 55},
            {"D-1", 14},
            {"A-1", 50}, {"M-1", 50},
            {"D+A",  2}, {"D+M",  2},
            {"D-A", 19}, {"D-M", 19},
            {"A-D",  7}, {"M-D",  7},
            {"D&A",  0}, {"D&M",  0},
            {"D|A", 21}, {"D|M", 21}
        };

        private Dictionary<string, byte> DestJumpSymbols = new()
        {
            {"M",   1}, {"JGT", 1},
            {"D",   2}, {"JEQ", 2},
            {"MD",  3}, {"JGE", 3},
            {"A",   4}, {"JLT", 4},
            {"AM",  5}, {"JNE", 5},
            {"AD",  6}, {"JLE", 6},
            {"AMD", 7}, {"JMP", 7}
        };

        private Dictionary<string, int> labelSymbols;
        private Dictionary<string, int> varSymbols;

        private List<string> workingFile { get; set; }

        public bool OperationComplete { get; private set; }
        public int Errors { get; private set; } = 0;
        
        public void Parse(string filePath)
        {
            if (File.ReadLines(filePath) != null)
            {
                workingFile = new();
                labelSymbols = new();
                varSymbols = new();
                int totalLines = 0;
                bool isBlock = false;
                StreamReader sr = new StreamReader(filePath);
                StreamWriter wr = new StreamWriter(filePath.Replace(".asm", ".hack"));

                string? workingLine;
                while ((workingLine = sr.ReadLine()) != null)
                {
                    totalLines++;
                    workingLine = workingLine.Trim();

                    if (workingLine.Contains(@"/*") || isBlock)
                    {
                        string[] workLinesStart = workingLine.Split(@"/*");
                        if (workLinesStart.Count() > 2)
                        {
                            ConsoleWriter.ConsoleError(new string[] { $"Block comment error on line {totalLines} in {Path.GetFileName(filePath)}.",
                                                                       "Skipping file and moving to next operation." }, false);
                            Errors++;
                            OperationComplete = false;
                            return;
                        }
                        
                        isBlock = true;

                        if (workingLine.Contains(@"*/") && !workingLine.Contains(workLinesStart[0]))
                        {
                            string[] workLinesEnd = workingLine.Split(@"*/");
                            if (workLinesEnd.Count() > 2)
                            {
                                ConsoleWriter.ConsoleError(new string[] { $"Block comment error on line {totalLines} in {Path.GetFileName(filePath)}.",
                                                                           "Skipping file and moving to next operation." }, false);
                                Errors++;
                                OperationComplete = false;
                                return;
                            }
                            isBlock = false;
                        }
                        else
                        {
                            continue;
                        }

                    }
                    else if (IsWhiteSpace(workingLine))
                    {
                        continue;
                    }
                    else
                    {
                        workingFile.Add(TrimComments(workingLine));
                        OperationComplete = true;
                    }
                }

                sr.Close();

                if (workingFile.Count() == 0)
                {
                    ConsoleWriter.ConsoleError(new string[] { $"No readable lines found in {Path.GetFileName(filePath)}" }, false);
                    Errors++;
                    return;
                }
                else
                {
                    FirstPass();
                    SecondPass(wr);
                }

                wr.Close();

                //ConsoleWriter.VerboseFile(workingFile);
                //foreach (KeyValuePair<string, int> kvp in labelSymbols)
                //{
                //    Console.WriteLine(kvp.Key + ": " + kvp.Value);
                //}

            }
        }

        private void FirstPass()
        {
            for (int i = 0; i < workingFile.Count(); i++)
            {
                if (workingFile[i].StartsWith('(') && workingFile[i].EndsWith(')'))
                {
                    string trimmedLine = workingFile[i];
                    trimmedLine = trimmedLine.Replace("(", "");
                    trimmedLine = trimmedLine.Replace(")", "");
                    labelSymbols.Add(trimmedLine, i);
                    workingFile.Remove(workingFile[i]);
                    i--;
                }
            }
        }

        private void SecondPass(StreamWriter writer)
        {
            int varAddress = 0;
            for (int i = 0 ; i < workingFile.Count(); i++)
            {
                string line = workingFile[i].Replace(" ", "");
                
                if (line.StartsWith('@'))
                {
                    string aReg = line.Replace("@", "");
                    if (defaultSymbols.ContainsKey(aReg))
                    {
                        WriteA(writer, defaultSymbols[aReg]);
                    }
                    else if (labelSymbols.ContainsKey(aReg))
                    {
                        WriteA(writer, labelSymbols[aReg]);
                    }
                    else if (varSymbols.ContainsKey(aReg))
                    {
                        WriteA(writer, varSymbols[aReg]);
                    }
                    else if (!int.TryParse(aReg, out int result))
                    {
                        varSymbols.Add(aReg, varAddress + 16);
                        varAddress++;
                        WriteA(writer, varSymbols[aReg]);
                    }
                    else
                    {
                        WriteA(writer, Convert.ToInt32(aReg));
                    }
                }
                else
                {
                    string cReg = "111";
                    //string instr = workingFile[i].Replace(" ", "");
                    string? comp = null, dest = null, jump = null;

                    if (line.Contains('=') || line.Contains(';'))
                    {
                        if (line.Contains('=') && line.Contains(';'))
                        {
                            string[] instructions = line.Split(new char[] { '=', ';' });
                            dest = instructions[0];
                            comp = instructions[1];
                            jump = instructions[2];
                        }
                        else if (line.Contains('='))
                        {
                            string[] instructions = line.Split('=');
                            dest = instructions[0];
                            comp = instructions[1];
                        }
                        else if (line.Contains(';'))
                        {
                            string[] instructions = line.Split(';');
                            comp = instructions[0];
                            jump = instructions[1];
                        }
                    }

                    if (comp != null)
                    {
                        if (comp.Contains("M")) cReg = string.Concat(cReg, "1");
                        else cReg = string.Concat(cReg, "0");
                    }

                    cReg = string.Concat(cReg, Convert.ToString(CompSymbols[comp], 2).PadLeft(6, '0'));
                    
                    if (dest != null)
                    {
                        cReg = string.Concat(cReg, Convert.ToString(DestJumpSymbols[dest], 2).PadLeft(3, '0'));
                    }
                    else
                    {
                        cReg = string.Concat(cReg, "000");
                    }

                    if (jump != null)
                    {
                        cReg = string.Concat(cReg, Convert.ToString(DestJumpSymbols[jump], 2).PadLeft(3, '0'));
                    }
                    else
                    {
                        cReg = string.Concat(cReg, "000");
                    }

                    writer.WriteLine(cReg);

                }
            }
        }

        private void WriteA(StreamWriter writer, int instr)
        {
            writer.WriteLine(Convert.ToString(instr, 2).PadLeft(16, '0'));
        }

        private void WriteC(StreamWriter writer, string instr)
        {

        }

        private static bool IsWhiteSpace(string line)
        {
            string workingLine = line.Trim();

            if (string.IsNullOrWhiteSpace(workingLine)) return true;
            if (workingLine.StartsWith("//")) return true;

            return false;
        }

        private static string TrimComments(string line)
        {
            string workingLine = line.Trim();

            if (workingLine.Contains("//"))
            {
                workingLine = workingLine.Split(@"//")[0];
            }

            return workingLine.Trim();
        }
    }
}
