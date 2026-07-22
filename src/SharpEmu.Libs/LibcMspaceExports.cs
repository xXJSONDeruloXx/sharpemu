// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using SharpEmu.Libs.Kernel;

namespace SharpEmu.Libs.Libc;

public static unsafe class LibcMspaceExports
{
    [SysAbiExport(
        Nid = "-hn1tcVHq5Q",
        ExportName = "sceLibcMspaceCreate",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int MspaceCreate(CpuContext ctx)
    {
        var handle = HostMemory.Alloc(
            null,
            (nuint)0x1000,
            HostMemory.MEM_RESERVE | HostMemory.MEM_COMMIT,
            HostMemory.PAGE_READWRITE);
        if (handle is null)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        ctx[CpuRegister.Rax] = (ulong)(nint)handle;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "OJjm-QOIHlI",
        ExportName = "sceLibcMspaceMalloc",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int MspaceMalloc(CpuContext ctx)
    {
        _ = ctx[CpuRegister.Rdi];
        var size = ctx[CpuRegister.Rsi];
        if (size == 0)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_OK;
        }

        var allocation = HostMemory.Alloc(
            null,
            (nuint)size,
            HostMemory.MEM_RESERVE | HostMemory.MEM_COMMIT,
            HostMemory.PAGE_READWRITE);
        if (allocation is null)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        ctx[CpuRegister.Rax] = (ulong)(nint)allocation;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "iF1iQHzxBJU",
        ExportName = "sceLibcMspaceMemalign",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int MspaceMemalign(CpuContext ctx)
    {
        _ = ctx[CpuRegister.Rdi];
        var size = ctx[CpuRegister.Rdx];
        if (size == 0)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_OK;
        }

        var allocation = HostMemory.Alloc(
            null,
            (nuint)size,
            HostMemory.MEM_RESERVE | HostMemory.MEM_COMMIT,
            HostMemory.PAGE_READWRITE);
        if (allocation is null)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        ctx[CpuRegister.Rax] = (ulong)(nint)allocation;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "Vla-Z+eXlxo",
        ExportName = "sceLibcMspaceFree",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int MspaceFree(CpuContext ctx)
    {
        var allocation = ctx[CpuRegister.Rsi];
        if (allocation != 0)
        {
            _ = HostMemory.Free((void*)allocation, 0, HostMemory.MEM_RELEASE);
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "W6SiVSiCDtI",
        ExportName = "sceLibcMspaceDestroy",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int MspaceDestroy(CpuContext ctx)
    {
        var handle = ctx[CpuRegister.Rdi];
        if (handle != 0)
        {
            _ = HostMemory.Free((void*)handle, 0, HostMemory.MEM_RELEASE);
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "mfHdJTIvhuo",
        ExportName = "sceLibcMspaceMallocStats",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int MspaceMallocStats(CpuContext ctx)
    {
        var outAddress = ctx[CpuRegister.Rsi];
        var length = ctx[CpuRegister.Rcx];
        if (outAddress != 0)
        {
            var zeroLen = (int)Math.Min(length is 0 or > 0x400 ? 0x40u : length, 0x400);
            if (zeroLen > 0)
            {
                Span<byte> zeros = stackalloc byte[zeroLen];
                zeros.Clear();
                _ = ctx.Memory.TryWrite(outAddress, zeros) || KernelMemoryCompatExports.TryWriteHostMemory(outAddress, zeros);
            }
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }
}
