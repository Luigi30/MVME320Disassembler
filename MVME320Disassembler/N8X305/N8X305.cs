using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVME320Disassembler
{
    namespace N8X305
    {
        internal class N8X305
        {
            Disassembler dasm;
            private Roms roms;

            public struct Registers
            {
                public enum CPUBank
                {
                    Reg,
                    LB,
                    RB
                };

                public enum CPU
                {
                    AUX,
                    R1,
                    R2,
                    R3,
                    R4,
                    R5,
                    R6,
                    IVL,
                    R10,
                    R11,
                    R12,
                    R13,
                    R14,
                    R15,
                    R16,
                    IVR
                }

                public struct FastIO
                {
                    public enum Op1
                    {
                        NOP0,     // B == 0
                        WUASn,
                        WUDSn,
                        WRDn,
                        WLDSn,
                        VCR,
                        WMASn,
                        WLASn,
                        WDC1n,    // B == 1
                        WDBCn,
                        WDC3n,
                        NOP3,
                        WDC2n,
                        NOP5,
                        WBUn,
                        NOP7
                    }

                    public enum Op2
                    {
                        VSR1,
                        RDBCn,
                        VRDLn,
                        RBUn,
                        VSR2,
                        RDSn,
                        VRDUn,
                        NOP7
                    }
                }
            }

            public struct PartNumbers
            {
                public const string EvenPart = "u1";
                public const string OddPart = "u9";
                public const string FastIoPart = "u3";
            }

            public struct Roms
            {
                public UInt16[] Program;
                public byte[] FastIO;

                public Roms(FileStream even, FileStream odd, FileStream io)
                {
                    Program = new UInt16[4096];
                    FastIO = new byte[4096];

                    for (int i = 0; i < 4096; i++)
                    {
                        UInt16 evenByte = Convert.ToUInt16(even.ReadByte());
                        UInt16 oddByte = Convert.ToUInt16(odd.ReadByte());
                        byte ioByte = Convert.ToByte(io.ReadByte());

                        byte evenReversed = 0;
                        byte oddReversed = 0;
                        byte ioReversed = 0;

                        // Swap bit order
                        BitArray bEven = new BitArray(new int[] { evenByte });
                        bool[] bitsEven = new bool[bEven.Count];
                        bEven.CopyTo(bitsEven, 0);
                        byte[] bitEvenValues = bitsEven.Select(bit => (byte)(bit ? 1 : 0)).ToArray();

                        BitArray bOdd = new BitArray(new int[] { oddByte });
                        bool[] bitsOdd = new bool[bOdd.Count];
                        bOdd.CopyTo(bitsOdd, 0);
                        byte[] bitOddValues = bitsOdd.Select(bit => (byte)(bit ? 1 : 0)).ToArray();

                        BitArray bFastIO = new BitArray(new int[] { ioByte });
                        bool[] bitsFastIO = new bool[bFastIO.Count];
                        bFastIO.CopyTo(bitsFastIO, 0);
                        byte[] bitFastIOValues = bitsFastIO.Select(bit => (byte)(bit ? 1 : 0)).ToArray();

                        for (int j=0; j<8; j++)
                        {
                            evenReversed = Convert.ToByte((bitEvenValues[0] << 7) |
                                                          (bitEvenValues[1] << 6) |
                                                          (bitEvenValues[2] << 5) |
                                                          (bitEvenValues[3] << 4) |
                                                          (bitEvenValues[4] << 3) |
                                                          (bitEvenValues[5] << 2) |
                                                          (bitEvenValues[6] << 1) |
                                                          (bitEvenValues[7] << 0));

                            oddReversed = Convert.ToByte((bitOddValues[0] << 7) |
                                                         (bitOddValues[1] << 6) |
                                                         (bitOddValues[2] << 5) |
                                                         (bitOddValues[3] << 4) |
                                                         (bitOddValues[4] << 3) |
                                                         (bitOddValues[5] << 2) |
                                                         (bitOddValues[6] << 1) |
                                                         (bitOddValues[7] << 0));

                            ioReversed = Convert.ToByte((bitFastIOValues[0] << 7) |
                                                        (bitFastIOValues[1] << 6) |
                                                        (bitFastIOValues[2] << 5) |
                                                        (bitFastIOValues[3] << 4) |
                                                        (bitFastIOValues[4] << 3) |
                                                        (bitFastIOValues[5] << 2) |
                                                        (bitFastIOValues[6] << 1) |
                                                        (bitFastIOValues[7] << 0));
                        }

                        UInt16 programWord = Convert.ToUInt16((evenReversed << 8) | oddReversed);
                        Program[i] = programWord;
                        FastIO[i] = ioReversed;
                    }
                }
            }

            public N8X305()
            {
                roms = new Roms(File.Open("c:/mame/roms/mvme320/3.0-" + N8X305.PartNumbers.EvenPart + ".bin", FileMode.Open),
                                File.Open("c:/mame/roms/mvme320/3.0-" + N8X305.PartNumbers.OddPart + ".bin", FileMode.Open),
                                File.Open("c:/mame/roms/mvme320/3.0-" + N8X305.PartNumbers.FastIoPart + ".bin", FileMode.Open));
                dasm = new Disassembler("c:/mame/roms/mvme320/labels.csv", "c:/mame/roms/mvme320/comments.csv");
            }

            public void Disassemble()
            {
                List<string> disassembly = new List<string>();
                for (int i = 0; i < 4096; i++)
                {
                    disassembly.Add(dasm.Next(roms));
                }

                File.WriteAllLines("C:/mame/mvme320.dasm", disassembly);
            }
        }
    }
    
}
