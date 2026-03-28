// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using System.Buffers.Binary;

namespace SharpEmu.Libs.PlayGo;

public static class PlayGoExports
{
    private const int OrbisPlayGoErrorInvalidArgument = unchecked((int)0x80B20004);
    private const int OrbisPlayGoErrorNotInitialized = unchecked((int)0x80B20005);
    private const int OrbisPlayGoErrorAlreadyInitialized = unchecked((int)0x80B20006);
    private const int OrbisPlayGoErrorBadHandle = unchecked((int)0x80B20009);
    private const int OrbisPlayGoErrorBadPointer = unchecked((int)0x80B2000A);
    private const int OrbisPlayGoErrorBadSize = unchecked((int)0x80B2000B);
    private const int OrbisPlayGoErrorNotSupportPlayGo = unchecked((int)0x80B2000E);
    private const ulong PlayGoInitBufAddrOffset = 0;
    private const ulong PlayGoInitBufSizeOffset = 8;
    private const uint PlayGoMinimumInitBufferSize = 0x200000;
    private const uint PlayGoHandle = 1;

    private static readonly object _stateGate = new();
    private static bool _initialized;
    private static bool _hasPlayGoData;

    [SysAbiExport(
        Nid = "ts6GlZOKRrE",
        ExportName = "scePlayGoInitialize",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libScePlayGo")]
    public static int PlayGoInitialize(CpuContext ctx)
    {
        var initParamsAddress = ctx[CpuRegister.Rdi];
        if (initParamsAddress == 0)
        {
            return OrbisPlayGoErrorBadPointer;
        }

        if (!ctx.TryReadUInt64(initParamsAddress + PlayGoInitBufAddrOffset, out var bufferAddress))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        Span<byte> bufferSizeBytes = stackalloc byte[sizeof(uint)];
        if (!ctx.Memory.TryRead(initParamsAddress + PlayGoInitBufSizeOffset, bufferSizeBytes))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        var bufferSize = BinaryPrimitives.ReadUInt32LittleEndian(bufferSizeBytes);

        if (bufferAddress == 0)
        {
            return OrbisPlayGoErrorBadPointer;
        }

        if (bufferSize < PlayGoMinimumInitBufferSize)
        {
            return OrbisPlayGoErrorBadSize;
        }

        lock (_stateGate)
        {
            if (_initialized)
            {
                return OrbisPlayGoErrorAlreadyInitialized;
            }

            _hasPlayGoData = HasPlayGoChunkData();
            _initialized = true;
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "M1Gma1ocrGE",
        ExportName = "scePlayGoOpen",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libScePlayGo")]
    public static int PlayGoOpen(CpuContext ctx)
    {
        var outHandleAddress = ctx[CpuRegister.Rdi];
        var paramAddress = ctx[CpuRegister.Rsi];
        if (outHandleAddress == 0)
        {
            return OrbisPlayGoErrorBadPointer;
        }

        if (paramAddress != 0)
        {
            return OrbisPlayGoErrorInvalidArgument;
        }

        lock (_stateGate)
        {
            if (!_initialized)
            {
                return OrbisPlayGoErrorNotInitialized;
            }

            if (!_hasPlayGoData)
            {
                return OrbisPlayGoErrorNotSupportPlayGo;
            }
        }

        Span<byte> handleBytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(handleBytes, PlayGoHandle);
        if (!ctx.Memory.TryWrite(outHandleAddress, handleBytes))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "MPe0EeBGM-E",
        ExportName = "scePlayGoTerminate",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libScePlayGo")]
    public static int PlayGoTerminate(CpuContext ctx)
    {
        _ = ctx;
        lock (_stateGate)
        {
            if (!_initialized)
            {
                return OrbisPlayGoErrorNotInitialized;
            }

            _initialized = false;
            _hasPlayGoData = false;
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "Uco1I0dlDi8",
        ExportName = "scePlayGoClose",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libScePlayGo")]
    public static int PlayGoClose(CpuContext ctx)
    {
        var handle = unchecked((uint)ctx[CpuRegister.Rdi]);
        lock (_stateGate)
        {
            if (!_initialized)
            {
                return OrbisPlayGoErrorNotInitialized;
            }

            if (handle != PlayGoHandle)
            {
                return OrbisPlayGoErrorBadHandle;
            }
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    private static bool HasPlayGoChunkData()
    {
        var app0Root = Environment.GetEnvironmentVariable("SHARPEMU_APP0_DIR");
        if (string.IsNullOrWhiteSpace(app0Root))
        {
            return false;
        }

        var hostPath = Path.Combine(app0Root, "sce_sys", "playgo-chunk.dat");
        return File.Exists(hostPath);
    }
}
