// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using SharpEmu.Libs.Kernel;
using System.Buffers.Binary;

namespace SharpEmu.Libs.Libc;

public static class LibcLocaleExports
{
    [SysAbiExport(
        Nid = "hqi8yMOCmG0",
        ExportName = "_ZNSt8_LocinfoC1EPKc",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int LocinfoCtor(CpuContext ctx)
    {
        return ctx.SetReturn((int)OrbisGen2Result.ORBIS_GEN2_ERROR_NOT_IMPLEMENTED);
    }

    [SysAbiExport(
        Nid = "p6LrHjIQMdk",
        ExportName = "_ZNSt8_LocinfoD1Ev",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int LocinfoDtor(CpuContext ctx)
    {
        return ctx.SetReturn((int)OrbisGen2Result.ORBIS_GEN2_ERROR_NOT_IMPLEMENTED);
    }

    [SysAbiExport(
        Nid = "QW2jL1J5rwY",
        ExportName = "_ZNSt6locale5facet9_RegisterEv",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int FacetRegister(CpuContext ctx)
    {
        ctx[CpuRegister.Rax] = 0;
        return 0;
    }

    [SysAbiExport(
        Nid = "iPBqs+YUUFw",
        ExportName = "_Atomic_fetch_add_4",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int AtomicFetchAdd4(CpuContext ctx)
    {
        var address = ctx[CpuRegister.Rdi];
        var addend = unchecked((int)ctx[CpuRegister.Rsi]);
        if (!TryReadInt32Compat(ctx, address, out var original))
        {
            ctx[CpuRegister.Rax] = 0;
            return 0;
        }

        _ = TryWriteInt32Compat(ctx, address, unchecked(original + addend));
        ctx[CpuRegister.Rax] = unchecked((ulong)(uint)original);
        return 0;
    }

    [SysAbiExport(
        Nid = "2HnmKiLmV6s",
        ExportName = "_Atomic_fetch_sub_4",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int AtomicFetchSub4(CpuContext ctx)
    {
        var address = ctx[CpuRegister.Rdi];
        var subtrahend = unchecked((int)ctx[CpuRegister.Rsi]);
        if (!TryReadInt32Compat(ctx, address, out var original))
        {
            ctx[CpuRegister.Rax] = 0;
            return 0;
        }

        _ = TryWriteInt32Compat(ctx, address, unchecked(original - subtrahend));
        ctx[CpuRegister.Rax] = unchecked((ulong)(uint)original);
        return 0;
    }

    private static void ZeroMaybe(CpuContext ctx, ulong address, int length)
    {
        if (address == 0 || length <= 0)
        {
            return;
        }

        Span<byte> zeros = stackalloc byte[length];
        zeros.Clear();
        _ = ctx.Memory.TryWrite(address, zeros) || KernelMemoryCompatExports.TryWriteHostMemory(address, zeros);
    }

    private static bool TryReadInt32Compat(CpuContext ctx, ulong address, out int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        if (!ctx.Memory.TryRead(address, bytes) && !KernelMemoryCompatExports.TryReadHostMemory(address, bytes))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        return true;
    }

    private static bool TryWriteInt32Compat(CpuContext ctx, ulong address, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return ctx.Memory.TryWrite(address, bytes) || KernelMemoryCompatExports.TryWriteHostMemory(address, bytes);
    }
}
