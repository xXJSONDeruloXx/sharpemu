// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using System;
using System.Buffers.Binary;
using SharpEmu.HLE;
using SharpEmu.Libs.Kernel;

namespace SharpEmu.Libs.LibcMath;

public static class LibcMathExports
{
    [SysAbiExport(
        Nid = "2WE3BTYVwKM",
        ExportName = "cos",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libc")]
    public static int Cos(CpuContext ctx)
    {
        ctx.GetXmmRegister(0, out var low, out _);
        var input = BitConverter.Int64BitsToDouble(unchecked((long)low));
        var result = Math.Cos(input);
        ctx.SetXmmRegister(0, unchecked((ulong)BitConverter.DoubleToInt64Bits(result)), 0);
        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "9LCjpWyQ5Zc",
        ExportName = "pow",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libc")]
    public static int Pow(CpuContext ctx)
    {
        ctx.GetXmmRegister(0, out var baseBits, out _);
        ctx.GetXmmRegister(1, out var exponentBits, out _);
        var @base = BitConverter.Int64BitsToDouble(unchecked((long)baseBits));
        var exponent = BitConverter.Int64BitsToDouble(unchecked((long)exponentBits));
        var result = Math.Pow(@base, exponent);
        ctx.SetXmmRegister(0, unchecked((ulong)BitConverter.DoubleToInt64Bits(result)), 0);
        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "pztV4AF18iI",
        ExportName = "sincosf",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libc")]
    public static int SinCosF(CpuContext ctx)
    {
        ctx.GetXmmRegister(0, out var inputBits, out _);
        var input = BitConverter.Int32BitsToSingle(unchecked((int)inputBits));
        var sine = MathF.Sin(input);
        var cosine = MathF.Cos(input);

        if (!TryWriteFloat(ctx, ctx[CpuRegister.Rdi], sine) ||
            !TryWriteFloat(ctx, ctx[CpuRegister.Rsi], cosine))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    private static bool TryWriteFloat(CpuContext ctx, ulong address, float value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, BitConverter.SingleToInt32Bits(value));
        return ctx.Memory.TryWrite(address, bytes) || KernelMemoryCompatExports.TryWriteHostMemory(address, bytes);
    }
}
