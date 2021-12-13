using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

        internal class Disassembler
        {
            enum RegisterBank {
                Reg,
                LB,
                RB
            };

            UInt16 PC;
            UInt16 IR;
            byte FAST_IO;

            public Disassembler()
            {
                PC = 0;
                IR = 0;
                FAST_IO = 0;
            }

            struct InstructionFields
            {
                public UInt16 Instruction;
                public UInt16 ProgramCounter;

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

                public RegisterBank GetSBank()
                {
                    int bank = S >> 3;

                    switch (bank)
                    {
                        case 0:
                        case 1:
                            return RegisterBank.Reg;
                        case 2:
                            return RegisterBank.LB;
                        case 3:
                            return RegisterBank.RB;
                        default:
                            throw new ArgumentException();
                    }
                }

                public RegisterBank GetDBank()
                {
                    int bank = D >> 3;

                    switch (bank)
                    {
                        case 0:
                        case 1:
                            return RegisterBank.Reg;
                        case 2:
                            return RegisterBank.LB;
                        case 3:
                            return RegisterBank.RB;
                        default:
                            throw new ArgumentException();
                    }
                }

                public string GetSMnemonic()
                {
                    switch(GetSBank())
                    {
                        case RegisterBank.Reg:
                            return $"R{S0}";
                        case RegisterBank.LB:
                            return $"IVl{S0}";
                        case RegisterBank.RB:
                            return $"IVr{S0}";
                        default:
                            throw new ArgumentException();
                    }
                }

                public string GetDMnemonic()
                {
                    switch (GetDBank())
                    {
                        case RegisterBank.Reg:
                            return $"R{D0}";
                        case RegisterBank.LB:
                            return $"IVl{D0}";
                        case RegisterBank.RB:
                            return $"IVr{D0}";
                        default:
                            throw new ArgumentException();
                    }
                }
            }

            string DasmMove(InstructionFields fields)
            {
                // MOVE, ADD, AND, XOR
                string mnemonic;
              
                switch(fields.Opcode)
                {
                    case 0:
                        mnemonic = "MOVE";
                        break;
                    case 1:
                        mnemonic = "ADD";
                        break;
                    case 2:
                        mnemonic = "AND";
                        break;
                    case 3:
                        mnemonic = "XOR";
                        break;
                    default:
                        mnemonic = "error";
                        break;
                }

                string output;
                string S_Mnemonic, D_Mnemonic;

                // Quick check for illegal opcode.
                if(fields.D == 8)
                {
                    throw new ArgumentOutOfRangeException("D == 8 is illegal in MOVE/ADD/AND/XOR.");
                }

                S_Mnemonic = fields.GetSMnemonic();
                D_Mnemonic = fields.GetDMnemonic();
                 
                output = $"{mnemonic, 4} {S_Mnemonic,4},{fields.L},{D_Mnemonic}";

                return output;
            }

            string DasmXec(InstructionFields fields)
            {
                string mnemonic = "XEC";
                string output = "";
                string S_Mnemonic;

                S_Mnemonic = fields.GetSMnemonic();

                if(fields.GetSBank() != RegisterBank.Reg)
                {
                    // IV Imm
                    output = $"{mnemonic, 4} {S_Mnemonic,4},{fields.L},${fields.D:X2}";

                }
                else
                {
                    // Reg Imm
                    output = $"{mnemonic, 4} {S_Mnemonic,4}, ,${fields.J:X2}";
                }

                return output;
            }

            string DasmNzt(InstructionFields fields)
            {
                string mnemonic = "NZT";
                string output = "";

                string S_Mnemonic;

                S_Mnemonic = fields.GetSMnemonic();

                if (fields.GetSBank() != RegisterBank.Reg)
                {
                    // IV Imm
                    UInt16 newPC = Convert.ToUInt16((fields.ProgramCounter & 0xFFE0) | fields.D); // double-check this
                    output = $"{mnemonic, 4} {S_Mnemonic,4},{fields.L},${newPC:X4}";

                }
                else
                {
                    // Reg Imm
                    UInt16 newPC = Convert.ToUInt16((fields.ProgramCounter & 0xFF00) | fields.J);
                    output = $"{mnemonic, 4} {S_Mnemonic,4}, ,${newPC:X4}";
                }

                return output;
            }

            string DasmXmit(InstructionFields fields)
            {
                string mnemonic = "XMIT";
                string output = "";

                string S_Mnemonic = fields.GetSMnemonic();
                string D_Mnemonic = fields.GetDMnemonic();

                if (fields.D >= 16)
                {
                    // XMIT Variable Bit Field Immediate, IV Bus
                    output = $"{mnemonic, 4} {S_Mnemonic,4},{fields.L},${fields.D:X2}";
                }
                else if(fields.D == 10 || fields.D == 11)
                {
                    // XMIT 8 Bits Immediate, IV Bus
                    if(fields.D == 10)
                    {
                        output = $"{mnemonic,4} #${fields.J:X2}, ,{"IVrD"}"; // output as IV LB data
                    }
                    if (fields.D == 11)
                    {
                        output = $"{mnemonic,4} #${fields.J:X2}, ,{"IVlD"}"; // output as IV RB data
                    }

                }
                else if(fields.D == 7 || fields.D == 15)
                {
                    // XMIT, IV Bus Address
                    if (fields.D == 7)
                    {
                        output = $"{mnemonic,4} #${fields.J:X2}, ,{"R07lA"}"; // R07 + output as IV LB address
                    }
                    else if (fields.D == 15)
                    {
                        output = $"{mnemonic,4} #${fields.J:X2}, ,{"R17rA"}"; // R17 + output as IV RB address
                    }
                }
                else
                {
                    // XMIT, Register
                    output = $"{mnemonic, 4} #${fields.J:X2}, ,{S_Mnemonic}";
                }

                return output;
            }

            string DasmJmp(InstructionFields fields)
            {
                string mnemonic = "JMP";
                string output = "";

                UInt16 absolute = (UInt16)(fields.A & 0x1FFF);

                output = $"{mnemonic, 4} ${absolute:X4}";
                return output;
            }

            public string DasmFastIO(byte instruction)
            {
                string mnemonic = "FAST_IO";
                string i53, i20;
                string output = "";

                bool w = Convert.ToBoolean(FAST_IO & 0x80);
                bool b = Convert.ToBoolean(FAST_IO & 0x40);
                bool s = Convert.ToBoolean(FAST_IO & 0x20);
                int state1 = (FAST_IO >> 3) & 7;
                int state2 = FAST_IO & 7;

                if (b)
                {
                    switch (state1)
                    {
                        case 0:
                            i53 = "WDC1n";
                            break;
                        case 1:
                            i53 = "WDC2n";
                            break;
                        case 2:
                            i53 = "WDC3n";
                            break;
                        case 3:
                            i53 = "WBUn";
                            break;
                        case 4:
                            i53 = "WDBCn";
                            break;
                        case 5:
                            i53 = "NOP";
                            break;
                        case 6:
                            i53 = "NOP";
                            break;
                        case 7:
                            i53 = "NOP";
                            break;
                        default:
                            throw new ArgumentException();
                    }
                }
                else
                {
                    switch (state1)
                    {
                        case 0:
                            i53 = "NOP";
                            break;
                        case 1:
                            i53 = "WUASn";
                            break;
                        case 2:
                            i53 = "WUDSn";
                            break;
                        case 3:
                            i53 = "WRDn";
                            break;
                        case 4:
                            i53 = "WLDSn";
                            break;
                        case 5:
                            i53 = "VCR";
                            break;
                        case 6:
                            i53 = "WMASn";
                            break;
                        case 7:
                            i53 = "WLASn";
                            break;
                        default:
                            throw new ArgumentException();
                    }
                }

                switch (state2)
                {
                    case 0:
                        i20 = "VSR1";
                        break;
                    case 1:
                        i20 = "RDBCn";
                        break;
                    case 2:
                        i20 = "VRDLn";
                        break;
                    case 3:
                        i20 = "RBUn";
                        break;
                    case 4:
                        i20 = "U21D";
                        break;
                    case 5:
                        i20 = "RDSn";
                        break;
                    case 6:
                        i20 = "VRDUn";
                        break;
                    case 7:
                        i20 = "NOP";
                        break;
                    default:
                        throw new ArgumentException();
                }

                string watchdog, buffer_flag, s0;
                if (w) watchdog = "W"; else watchdog = "-";
                if (b) buffer_flag = "B"; else buffer_flag = "-";
                if (s) s0 = "S"; else s0 = "-";

                output = $"{mnemonic} {watchdog}{buffer_flag}{s0} {i53, 5},{i20, 5}";
                return output;
            }

            public string Next(N8X305.Roms program)
            {
                IR = program.Program[PC];
                FAST_IO = program.FastIO[PC];

                InstructionClass instructionClass = (InstructionClass)(IR >> 13);

                string dasmed;
                string io_dasmed;

                InstructionFields inst = new InstructionFields(PC, IR);

                switch (instructionClass)
                {
                    case InstructionClass.Move:
                        dasmed = DasmMove(inst);
                        break;
                    case InstructionClass.Add:
                        dasmed = DasmMove(inst);
                        break;
                    case InstructionClass.And:
                        dasmed = DasmMove(inst);
                        break;
                    case InstructionClass.Xor:
                        dasmed = DasmMove(inst);
                        break;
                    case InstructionClass.Xec:
                        dasmed = DasmXec(inst);
                        break;
                    case InstructionClass.Nzt:
                        dasmed = DasmNzt(inst);
                        break;
                    case InstructionClass.Xmit:
                        dasmed = DasmXmit(inst);
                        break;
                    case InstructionClass.Jmp:
                        dasmed = DasmJmp(inst);
                        break;
                    default:
                        dasmed = "";
                        break;
                }

                io_dasmed = DasmFastIO(FAST_IO);

                string output = $"{PC, 4:X4}: {dasmed, -20} [${IR, 4:X4}] | [${FAST_IO,2:X2}] {io_dasmed}";
                PC++;

                return output;
            }
        }
    }
}
