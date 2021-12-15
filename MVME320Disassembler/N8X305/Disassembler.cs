using CsvHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVME320Disassembler
{
    namespace N8X305
    {
        enum InstructionClass
        {
            Move,
            Add,
            And,
            Xor,
            Xec,
            Nzt,
            Xmit,
            Jmp
        }

        public class Mnemonics
        {
            public struct MVME320
            {
                public static List<string> VCR = new List<string>
                {
                    "CLED1n",
                    "CDS0",
                    "CDS1",
                    "CWRT",
                    "nc",
                    "CBERR",
                    "CBR",
                    "STARTn"
                };

                public static List<string> VSR2 = new List<string>
                {
                    "n/c",
                    "n/c",
                    "n/c",
                    "n/c",
                    "ACFAIL",
                    "BCLR",
                    "LBERRn",
                    "CYACTIV"
                };
            }

            public struct CPU
            {
                public static List<string> Instructions = new List<string>
                {
                    "MOVE",
                    "ADD",
                    "AND",
                    "XOR",
                    "XEC",
                    "NZT",
                    "XMIT",
                    "JMP"
                };

                public static List<string> Registers = new List<string>
                {
                    "AUX",
                    "R1",
                    "R2",
                    "R3",
                    "R4",
                    "R5",
                    "R6",
                    "IVL",
                    "R10",
                    "R11",
                    "R12",
                    "R13",
                    "R14",
                    "R15",
                    "R16",
                    "IVR"
                };
            }
            public struct FastIO
            {
                public static List<string> Op1 = new List<string>
                {
                    "NOP0",     // B == 0
                    "WUASn",
                    "WUDSn",
                    "WRDn",
                    "WLDSn",
                    "VCR",
                    "WMASn",
                    "WLASn",
                    "WDC1n",    // B == 1
                    "WDBCn",
                    "WDC3n",
                    "NOP3",
                    "WDC2n",
                    "NOP5",
                    "WBUn",
                    "NOP7"
                };

                public static List<string> Op2 = new List<string>()
                {
                    "VSR1",
                    "RDBCn",
                    "VRDLn",
                    "RBUn",
                    "VSR2",
                    "RDSn",
                    "VRDUn",
                    "NOP7"
                };
            }
        }

        internal class Disassembler
        {
            UInt16 PC;
            UInt16 IR;
            byte FAST_IO;
            List<CodeLabel> Labels;

            public Disassembler(string commentFile, string labelFile)
            {
                PC = 0;
                IR = 0;
                FAST_IO = 0;

                using (var commentReader = new StreamReader(commentFile))
                {
                    using (var csv = new CsvReader(commentReader, CultureInfo.InvariantCulture))
                    {
                        Labels = csv.GetRecords<CodeLabel>().ToList();
                    }
                }
            }

            public string Octal(byte val)
            {
                return Convert.ToString(val, 8);
            }

            public string Octal(UInt16 val)
            {
                return Convert.ToString(val, 8);
            }

            public struct InstructionFields
            {
                public UInt16 Instruction;
                public UInt16 ProgramCounter;

                public string Mnemonic => Mnemonics.CPU.Instructions[Opcode];

                public byte Opcode => (byte)(Instruction >> 13);
                public byte S => (byte)((Instruction >> 8) & 0x1F);
                public byte S0 => (byte)((Instruction >> 8) & 0x7);
                public byte S1 => (byte)((Instruction >> 11) & 0x3);

                public byte L => (byte)((Instruction >> 5) & 0x7);
                public byte D => (byte)(Instruction & 0x1F);
                public byte D0 => (byte)(Instruction & 0x7);
                public byte D1 => (byte)((Instruction >> 3) & 0x3);
                public byte J => (byte)(Instruction & 0xFF);
                public UInt16 A => (UInt16)(Instruction & 0x1FFF);

                public InstructionFields(UInt16 PC, UInt16 IR)
                {
                    ProgramCounter = PC;
                    Instruction = IR;
                }

                public N8X305.Registers.CPUBank GetSBank()
                {
                    int bank = S >> 3;

                    switch (bank)
                    {
                        case 0:
                        case 1:
                            return N8X305.Registers.CPUBank.Reg;
                        case 2:
                            return N8X305.Registers.CPUBank.LB;
                        case 3:
                            return N8X305.Registers.CPUBank.RB;
                        default:
                            throw new ArgumentException();
                    }
                }

                public N8X305.Registers.CPUBank GetDBank()
                {
                    int bank = D >> 3;

                    switch (bank)
                    {
                        case 0:
                        case 1:
                            return N8X305.Registers.CPUBank.Reg;
                        case 2:
                            return N8X305.Registers.CPUBank.LB;
                        case 3:
                            return N8X305.Registers.CPUBank.RB;
                        default:
                            throw new ArgumentException();
                    }
                }

                public string GetSMnemonic()
                {
                    switch(GetSBank())
                    {
                        case N8X305.Registers.CPUBank.Reg:
                            return Mnemonics.CPU.Registers[S];
                        case N8X305.Registers.CPUBank.LB:
                            return $"IVl{S0}";
                        case N8X305.Registers.CPUBank.RB:
                            return $"IVr{S0}";
                        default:
                            throw new ArgumentException();
                    }
                }

                public string GetDMnemonic()
                {
                    switch (GetDBank())
                    {
                        case N8X305.Registers.CPUBank.Reg:
                            return Mnemonics.CPU.Registers[D];
                        case N8X305.Registers.CPUBank.LB:
                            return $"IVl{D0}";
                        case N8X305.Registers.CPUBank.RB:
                            return $"IVr{D0}";
                        default:
                            throw new ArgumentException();
                    }
                }
            }

            public struct FastIOFields
            {
                public byte Instruction;

                public FastIOFields(byte val)
                {
                    Instruction = val;
                }

                public bool W => (Convert.ToBoolean(Instruction >> 7));
                public bool B => (Convert.ToBoolean(Instruction >> 6));
                public bool S => (Convert.ToBoolean(Instruction >> 5));

                public byte Op1 => (byte)((Instruction >> 3) & 7 + (8 * Convert.ToByte(B)));
                public byte Op2 => (byte)(Instruction & 7);

                public string Op1Mnemonic => Mnemonics.FastIO.Op1[Op1];
                public string Op2Mnemonic => Mnemonics.FastIO.Op2[Op2];
            }

            (string, string) DasmMove(InstructionFields fields)
            {
                // MOVE, ADD, AND, XOR
                string output;
                string S_Mnemonic, D_Mnemonic;

                // Quick check for illegal opcode.
                if(fields.D == 8)
                {
                    throw new ArgumentOutOfRangeException("D == 8 is illegal in MOVE/ADD/AND/XOR.");
                }

                S_Mnemonic = fields.GetSMnemonic();
                D_Mnemonic = fields.GetDMnemonic();
                 
                output = $"{fields.Mnemonic, 4} {S_Mnemonic,4},{fields.L},{D_Mnemonic}";


                string fieldString = $"[{fields.Opcode}|S{Octal(fields.S),2:D2}|L{Octal(fields.L)}|D{Octal(fields.D),2:D2}]";
                return (output, fieldString);
            }

            (string, string) DasmXec(InstructionFields fields)
            {
                string output = "";
                string fieldString = "";
                string S_Mnemonic;

                S_Mnemonic = fields.GetSMnemonic();

                if(fields.GetSBank() != N8X305.Registers.CPUBank.Reg)
                {
                    // IV Imm
                    output = $"{fields.Mnemonic, 4} {S_Mnemonic,4},{fields.L},${fields.D:X2}";
                    fieldString = $"[{fields.Opcode}|S{Octal(fields.S),2:D2}|L{Octal(fields.L)}|J{Octal(fields.J),3:D2}]";

                }
                else
                {
                    // Reg Imm
                    output = $"{fields.Mnemonic, 4} {S_Mnemonic,4}, ,${fields.J:X2}";
                    fieldString = $"[{fields.Opcode}|S{Octal(fields.S),2:D2}|J{Octal(fields.J),3:D3}]";
                }

                return (output, fieldString);
            }

            (string, string) DasmNzt(InstructionFields fields)
            {
                string output = "";
                string fieldString;

                string S_Mnemonic;

                S_Mnemonic = fields.GetSMnemonic();

                if (fields.GetSBank() != N8X305.Registers.CPUBank.Reg)
                {
                    // IV Imm
                    string new_pc = $"{(PC & 0x1FE0) | fields.D:X4}";
                    output = $"{fields.Mnemonic, 4} {S_Mnemonic,4},{fields.L},${new_pc}";
                    fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|L{Octal(fields.L)}|J{Octal(fields.D),2:D2}]";

                }
                else
                {
                    // Reg Imm
                    string new_pc = $"{(PC & 0x1F00) | fields.J:X4}";
                    output = $"{fields.Mnemonic, 4} {S_Mnemonic,4}, ,${new_pc}";
                    fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|J{Octal(fields.J),3:D3}]";
                }
                return (output, fieldString);
            }

            (string, string) DasmXmit(InstructionFields fields)
            {
                string output, fieldString;

                string S_Mnemonic = fields.GetSMnemonic();

                if (fields.S >= 16)
                {
                    // XMIT Variable Bit Field Immediate, IV Bus
                    output = $"{fields.Mnemonic, 4} {S_Mnemonic,4},{fields.L},${fields.D:X2}";
                    fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|L{Octal(fields.L)}|J{Octal(fields.D),2:D2}]";
                }
                else if(fields.S == 10 || fields.S == 11)
                {
                    // XMIT 8 Bits Immediate, IV Bus
                    if(fields.S == 10)
                    {
                        output = $"{fields.Mnemonic,4} #${fields.J:X2}, ,{"IVrD"}"; // output as IV LB data
                        fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|J{Octal(fields.J),3:D3}]";
                    }
                    else // fields.S == 11
                    {
                        output = $"{fields.Mnemonic,4} #${fields.J:X2}, ,{"IVlD"}"; // output as IV RB data
                        fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|J{Octal(fields.J),3:D3}]";
                    }

                }
                else if(fields.S == 7 || fields.S == 15)
                {
                    // XMIT, IV Bus Address
                    if (fields.S == 7)
                    {
                        output = $"{fields.Mnemonic,4} #${fields.J:X2}, ,{"R07lA"}"; // R07 + output as IV LB address
                        fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|J{Octal(fields.J),3:D3}]";
                    }
                    else // fields.S == 15
                    {
                        output = $"{fields.Mnemonic,4} #${fields.J:X2}, ,{"R17rA"}"; // R17 + output as IV RB address
                        fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|J{Octal(fields.J),3:D3}]";
                    }
                }
                else
                {
                    // XMIT, Register
                    output = $"{fields.Mnemonic, 4} #${fields.J:X2}, ,{S_Mnemonic}";
                    fieldString = $"[{fields.Opcode}|D{Octal(fields.S),2:D2}|J{Octal(fields.J),3:D3}]";
                }

                return (output, fieldString);
            }

            (string, string) DasmJmp(InstructionFields fields)
            {
                string output = "";

                UInt16 absolute = (UInt16)(fields.A & 0x1FFF);

                // Is this address in the labels list?
                var codelabel = Labels.Where(t => Convert.ToUInt16(t.pc, 16).Equals(absolute)).ToList().FirstOrDefault();
                if(codelabel != null)
                {
                    output = $"{fields.Mnemonic,4} {codelabel.label}";
                }
                else
                {
                    output = $"{fields.Mnemonic,4} ${absolute:X4}";
                }

                string fieldString = $"";
                return (output, fieldString);
            }

            (string, string) DasmFastIO(FastIOFields ioFields)
            {
                string watchdog, buffer_flag, s0;
                if (ioFields.W) watchdog = "W";     else watchdog = "-";
                if (ioFields.B) buffer_flag = "B";  else buffer_flag = "-";
                if (ioFields.S) s0 = "S";           else s0 = "-";

                string output = $"FAST_IO {watchdog}{buffer_flag}{s0} {ioFields.Op1Mnemonic, 5},{ioFields.Op2Mnemonic, 5}";
                return (output, "[]");
            }



            public string Next(N8X305.Roms program)
            {
                IR = program.Program[PC];
                FAST_IO = program.FastIO[PC];

                InstructionClass instructionClass = (InstructionClass)(IR >> 13);

                string dasmed;
                string dasmFields;
                string io_dasmed;
                string io_dasmFields;

                InstructionFields instructionFields = new InstructionFields(PC, IR);
                FastIOFields ioFields = new FastIOFields(FAST_IO);

                switch (instructionClass)
                {
                    case InstructionClass.Move:
                        (dasmed, dasmFields) = DasmMove(instructionFields);
                        break;
                    case InstructionClass.Add:
                        (dasmed, dasmFields) = DasmMove(instructionFields);
                        break;
                    case InstructionClass.And:
                        (dasmed, dasmFields) = DasmMove(instructionFields);
                        break;
                    case InstructionClass.Xor:
                        (dasmed, dasmFields) = DasmMove(instructionFields);
                        break;
                    case InstructionClass.Xec:
                        (dasmed, dasmFields) = DasmXec(instructionFields);
                        break;
                    case InstructionClass.Nzt:
                        (dasmed, dasmFields) = DasmNzt(instructionFields);
                        break;
                    case InstructionClass.Xmit:
                        (dasmed, dasmFields) = DasmXmit(instructionFields);
                        break;
                    case InstructionClass.Jmp:
                        (dasmed, dasmFields) = DasmJmp(instructionFields);
                        break;
                    default:
                        (dasmed, dasmFields) = ("", "");
                        break;
                }

                (io_dasmed, io_dasmFields) = DasmFastIO(ioFields);

                string? autoComment = AutoCommenter.AutoComment(instructionFields, ioFields);
                var codelabel = Labels.Where(t => Convert.ToUInt16(t.pc, 16).Equals(PC)).ToList().FirstOrDefault();
                string comment;

                // prefer the comment from the codelabel
                if(codelabel is not null && codelabel.comment is not null && codelabel.comment != "")
                {
                    comment = codelabel.comment;
                }
                else if(autoComment is not null)
                {
                    comment = autoComment;
                }
                else
                {
                    comment = "";
                }

                string output = $"{PC, 4:X4}: {codelabel?.label, 10}: {dasmed, -20} {dasmFields, -20} [${IR, 4:X4}] | [${FAST_IO,2:X2}] {io_dasmed} | // {comment}";

                PC++;

                return output;
            }
        }
    }
}
