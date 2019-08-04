using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ItemLib
{
    public static class CilExtensions
    {
        public static int DecodeLdcI4Arg(this Instruction x)
        {
            if (x.OpCode == OpCodes.Ldc_I4_0)
                return 0;
            if (x.OpCode == OpCodes.Ldc_I4_1)
                return 1;
            if (x.OpCode == OpCodes.Ldc_I4_2)
                return 2;
            if (x.OpCode == OpCodes.Ldc_I4_3)
                return 3;
            if (x.OpCode == OpCodes.Ldc_I4_4)
                return 4;
            if (x.OpCode == OpCodes.Ldc_I4_5)
                return 5;
            if (x.OpCode == OpCodes.Ldc_I4_6)
                return 6;
            if (x.OpCode == OpCodes.Ldc_I4_7)
                return 7;
            if (x.OpCode == OpCodes.Ldc_I4_8)
                return 8;
            if (x.OpCode == OpCodes.Ldc_I4_M1)
                return -1;
            if (x.OpCode == OpCodes.Ldc_I4_S)
                return (byte) x.Operand;
            if (x.OpCode == OpCodes.Ldc_I4)
                return (int) x.Operand;

            throw new ArgumentException("Instruction is not an Ldc_I4 variant");
        }
    }
}
