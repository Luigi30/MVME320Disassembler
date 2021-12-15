using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVME320Disassembler.N8X305
{
    internal class AutoCommenter
    { 
        public static string HighIsAssertedOrCleared(string mnemonic)
        {
            if (mnemonic.EndsWith('n'))
            {
                return "cleared";
            }
            else
            {
                return "asserted";
            }
        }

        public static string VCRAssertedLines(byte value)
        {
            string output = " ";
            byte invertedValue = (byte)~value; // The input to VCR is inverted

            for(int i=0; i<8; i++)
            {
                string mnemonic = Mnemonics.MVME320.VCR[i];
                if (HighIsAssertedOrCleared(mnemonic) == "asserted")
                {
                    // High is asserted
                    if ((invertedValue & (1 << i)) > 0)
                    {
                        output = output + Mnemonics.MVME320.VCR[i] + " ";
                    }
                }
                else
                {
                    // Low is asserted
                    if ((invertedValue & (1 << i)) == 0)
                    {
                        output = output + Mnemonics.MVME320.VCR[i] + " ";
                    }
                }
            }

            return output;
        }

        public static string? AutoComment(Disassembler.InstructionFields cpu, Disassembler.FastIOFields io)
        {
            // Check for outputting to VCR.
            if (io.Op1Mnemonic == "VCR")
            {
                if (cpu.Mnemonic == "XMIT" && cpu.S1 == 1 && cpu.S0 == 3)
                {
                    // XMIT Imm8,IVlD
                    return $"Assert VCR: ({VCRAssertedLines(cpu.J)})";
                }
            }

            // Check for an NZT with VSR2, the CPU is checking certain bits.
            else if (io.Op2Mnemonic == "VSR2")
            {
                if (cpu.Mnemonic == "NZT" && cpu.S1 == 3)
                {
                    int bit = 7 - cpu.S0;
                    string mnemonic = Mnemonics.MVME320.VSR2[bit];

                    return $"Branch if VSR2.{mnemonic} asserted";
                }
            }

            // ADD/AND/XOR IV -> Reg
            else if (cpu.Mnemonic == "ADD" && cpu.S1 == 3 && cpu.D1 == 0)
            {
                return $"(({Mnemonics.FastIO.Op2[io.Op2]} >> {7 - cpu.S0}) + AUX) -> {Mnemonics.CPU.Registers[cpu.D]}";
            }
            else if (cpu.Mnemonic == "AND" && cpu.S1 == 3 && cpu.D1 == 0)
            {
                return $"(({Mnemonics.FastIO.Op2[io.Op2]} >> {7 - cpu.S0}) & AUX) -> {Mnemonics.CPU.Registers[cpu.D]}";
            }
            else if (cpu.Mnemonic == "XOR" && cpu.S1 == 3 && cpu.D1 == 0)
            {
                return $"(({Mnemonics.FastIO.Op2[io.Op2]} >> {7 - cpu.S0}) ^ AUX) -> {Mnemonics.CPU.Registers[cpu.D]}";
            }

            // XMIT to AUX
            else if (cpu.Mnemonic == "XMIT" && cpu.S == 0)
            {
                return $"#${cpu.J:X2} -> AUX";
            }
            
            // XMIT to DBU
            else if (cpu.Mnemonic == "XMIT" && cpu.S == 7 && io.Instruction == 0x87)
            {
                return $"#${cpu.J:X2} -> DBU address latch";
            }

            // XMIT to DBCR
            else if (cpu.Mnemonic == "XMIT" && cpu.S == 11 && io.Instruction == 0xCF)
            {
                return $"#${cpu.J:X2} -> Disk Bit Control Register";
            }
            else if (cpu.Mnemonic == "MOVE" && cpu.S1 == 0 && cpu.D1 == 3 && cpu.D0 == 7)
            {
                // MOVE Reg -> IV with a register in Op1
                return $"{Mnemonics.CPU.Registers[cpu.S]} -> {Mnemonics.FastIO.Op1[io.Op1]}";
            }

            return null;
        }
    }
}
