# N8X305/MVME320 Disassembler

This is a disassembler for the Signetics N8X305 microcontroller as used in the Motorola MVME320 disk controller.
It supports both the N8X305 microcontroller and the Fast I/O Extended Microcode that is used in the MVME320.

## Usage
The disassembler looks for three files, the same ROM set as MAME. U1/U9 are the even/odd PROM and U3 is the
Fast I/O PROM. It will produce a disassembly file in the same folder.

I'm pretty sure nobody else is ever going to use this so it just looks in `C:\MAME\ROMS\MVME320`.

## Syntax
The instruction mnemonics themselves are consistent with the N8X305 manual but I've made a few modifications to
operands for clarity. There's no documentation for Motorola's Fast I/O so I had to come up with that
from scratch.

Addresses are prefixed with `$`.

Literals are prefixed with `#`.

Registers are prefixed with `R`.

Some instructions take special destination operands that correspond to different ways to use the IV peripheral bus.
- `IVlD`/`IVrD`: The source operand is placed on the IV bus with the WC data strobe asserted.
- `IVlA`/`IVrA`: The source operand is placed on the IV bus with the SC address strobe asserted.
- `IVl#`/`IVr#`: The source operand is latched, shifted left by # bits, ORed with the L field, and placed on the IV bus with the data strobe asserted.

The columns are as follows:

```
0001: XMIT #$20, ,IVr3     [$DB20] | [$49] FAST_IO -B- WDC2n,RDBCn

  pc  inst src  L dest       data      io  fast_io wbs  op1  op2
```

The left half is for the N8X305.
- `PC`:		Program Counter
- `INST`:	Instruction
- `SRC`:	Source
- `DEST`:	Destination. See note above regarding IV bus destinations.
- `DATA`:	Instruction data. All N8X305 instructions are two bytes long.

The right half is for Fast I/O.
- `IO`:		Fast I/O instruction data. All Fast I/O instructions are one byte long.
- `FAST_IO`:The Fast I/O mnemonic for disambiguation.
- `WBS`:	Three bit flags that control discrete board signals:
 * `W`:		The state of the WATCHDOG signal.
 * `B`:		Selects which group of OP1 functions is active.
 * `S`:		The state of the S0 signal.
- `OP1`:	Selects one of 16 different write functions.
- `OP2`:	Selects one of 8 different read functions.

### Fast I/O
The Fast I/O microcode byte is broken down into four sections.
```
- bit 7: Asserts WATCHDOG
- bit 6: Selects either OP1 functions $0-$7 (cleared) or $8-$E (asserted)
- bits 5-3: Select OP1 function
- bits 2-0: Select OP2 function
```

OP1 function table (where B selects [0-7] or [8-F]):
```
- 0:	NOP
- 1:	WUASn	(Write Upper Address Strobe)
- 2:	WUDSn	(Write Upper Data Strobe)
- 3:	WRDn	(Write Disk Status Register)
- 4:	WLDSn	(Write Lower Data Strobe)
- 5:	VCR		(not labeled on schematic - writes to VME Control Register)
- 6:	WMASn	(Write Middle Address Strobe)
- 7:	WLASn	(Write Lower Address Strobe)
- 8:	WDC1n	(Write Disk Control Register 1)
- 9:	WDC2n	(Write Disk Control Register 2)
- A:	WBUn	(Write Disk Buffer)
- B:	WDBCn	(Write Disk Bit Control Register)
- C:	NOP
- D:	NOP
- E:	NOP
```

OP2 function table:
```
- 0:	NOP
- 1:	RDBCn	(Read Disk Bit Control Register)
- 2:	RDSn	(Read Disk Status Register)
- 3:	RBUn	(Read Disk Buffer)
- 4:	VRDUn	(VME Read Upper, triggers D15-D8 read at Address Strobe)
- 5:	VRDLn	(VME Read Lower, triggers D7-D0 read at Address Strobe)
- 6:	VSR2	(not labeled on schematic - reads VME Status Register 2)	
- 7:	VSR1	(not labeled on schematic - reads VME Status Register 1)
```