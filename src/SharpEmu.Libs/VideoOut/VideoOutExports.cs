// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SharpEmu.Libs.VideoOut;

public static class VideoOutExports
{
    private const int OrbisVideoOutErrorInvalidValue = unchecked((int)0x80290001);
    private const int OrbisVideoOutErrorInvalidAddress = unchecked((int)0x80290002);
    private const int OrbisVideoOutErrorResourceBusy = unchecked((int)0x80290009);
    private const int OrbisVideoOutErrorInvalidIndex = unchecked((int)0x8029000A);
    private const int OrbisVideoOutErrorInvalidHandle = unchecked((int)0x8029000B);
    private const int OrbisVideoOutErrorInvalidOption = unchecked((int)0x8029001A);
    private const int SceVideoOutBusTypeMain = 0;
    private const int SceVideoOutBufferAttributeOptionNone = 0;
    private const int MaxOpenPorts = 4;
    private const int MaxDisplayBuffers = 16;
    private const int VideoOutBufferAttributeSize = 0x24;
    private const int VideoOutBufferAttribute2Size = 0x50;
    private const int VideoOutBuffersEntrySize = 0x20;

    private static readonly object _stateGate = new();
    private static readonly Dictionary<int, VideoOutPortState> _ports = new();
    private static int _nextHandle = 1;

    private sealed class VideoOutPortState
    {
        public required int Handle { get; init; }
        public int FlipRate { get; set; }
        public ulong VblankCount { get; set; }
        public int NextSetId { get; set; } = 1;
        public HashSet<int> RegisteredSetIds { get; } = new();
    }

    [SysAbiExport(
        Nid = "Up36PTk687E",
        ExportName = "sceVideoOutOpen",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutOpen(CpuContext ctx)
    {
        var userId = unchecked((int)ctx[CpuRegister.Rdi]);
        var busType = unchecked((int)ctx[CpuRegister.Rsi]);
        var index = unchecked((int)ctx[CpuRegister.Rdx]);
        _ = ctx[CpuRegister.Rcx];

        if (busType != SceVideoOutBusTypeMain || index != 0)
        {
            return OrbisVideoOutErrorInvalidValue;
        }

        if (userId != 0 && userId != 255)
        {
            return OrbisVideoOutErrorInvalidValue;
        }

        lock (_stateGate)
        {
            if (_ports.Count >= MaxOpenPorts)
            {
                return OrbisVideoOutErrorResourceBusy;
            }

            var handle = _nextHandle++;
            _ports[handle] = new VideoOutPortState
            {
                Handle = handle,
            };
            return handle;
        }
    }

    [SysAbiExport(
        Nid = "uquVH4-Du78",
        ExportName = "sceVideoOutClose",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutClose(CpuContext ctx)
    {
        var handle = unchecked((int)ctx[CpuRegister.Rdi]);
        lock (_stateGate)
        {
            _ports.Remove(handle);
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "CBiu4mCE1DA",
        ExportName = "sceVideoOutSetFlipRate",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutSetFlipRate(CpuContext ctx)
    {
        var handle = unchecked((int)ctx[CpuRegister.Rdi]);
        var rate = unchecked((int)ctx[CpuRegister.Rsi]);
        if (rate is < 0 or > 2)
        {
            return OrbisVideoOutErrorInvalidValue;
        }

        if (!TryGetPort(handle, out var port))
        {
            return OrbisVideoOutErrorInvalidHandle;
        }

        port.FlipRate = rate;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "j6RaAUlaLv0",
        ExportName = "sceVideoOutWaitVblank",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutWaitVblank(CpuContext ctx)
    {
        var handle = unchecked((int)ctx[CpuRegister.Rdi]);
        if (!TryGetPort(handle, out var port))
        {
            return OrbisVideoOutErrorInvalidHandle;
        }

        Thread.Sleep(1);
        lock (_stateGate)
        {
            port.VblankCount++;
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "MTxxrOCeSig",
        ExportName = "sceVideoOutSetWindowModeMargins",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutSetWindowModeMargins(CpuContext ctx)
    {
        var handle = unchecked((int)ctx[CpuRegister.Rdi]);
        _ = unchecked((int)ctx[CpuRegister.Rsi]);
        _ = unchecked((int)ctx[CpuRegister.Rdx]);

        return TryGetPort(handle, out _)
            ? (int)OrbisGen2Result.ORBIS_GEN2_OK
            : OrbisVideoOutErrorInvalidHandle;
    }

    [SysAbiExport(
        Nid = "N5KDtkIjjJ4",
        ExportName = "sceVideoOutUnregisterBuffers",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutUnregisterBuffers(CpuContext ctx)
    {
        var handle = unchecked((int)ctx[CpuRegister.Rdi]);
        var attributeIndex = unchecked((int)ctx[CpuRegister.Rsi]);
        if (!TryGetPort(handle, out var port))
        {
            return OrbisVideoOutErrorInvalidHandle;
        }

        if (attributeIndex < 0)
        {
            return OrbisVideoOutErrorInvalidValue;
        }

        lock (_stateGate)
        {
            return port.RegisteredSetIds.Remove(attributeIndex)
                ? (int)OrbisGen2Result.ORBIS_GEN2_OK
                : OrbisVideoOutErrorInvalidValue;
        }
    }

    [SysAbiExport(
        Nid = "i6-sR91Wt-4",
        ExportName = "sceVideoOutSetBufferAttribute",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutSetBufferAttribute(CpuContext ctx)
    {
        var attributeAddress = ctx[CpuRegister.Rdi];
        var pixelFormat = unchecked((uint)ctx[CpuRegister.Rsi]);
        var tilingMode = unchecked((uint)ctx[CpuRegister.Rdx]);
        var aspectRatio = unchecked((uint)ctx[CpuRegister.Rcx]);
        var width = unchecked((uint)ctx[CpuRegister.R8]);
        var height = unchecked((uint)ctx[CpuRegister.R9]);
        if (!TryReadStackUInt32(ctx, 0, out var pitchInPixel))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        if (attributeAddress == 0)
        {
            return OrbisVideoOutErrorInvalidAddress;
        }

        Span<byte> attribute = stackalloc byte[VideoOutBufferAttributeSize];
        attribute.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x00..0x04], pixelFormat);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x04..0x08], tilingMode);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x08..0x0C], aspectRatio);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x0C..0x10], width);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x10..0x14], height);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x14..0x18], pitchInPixel);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x18..0x1C], SceVideoOutBufferAttributeOptionNone);
        if (!ctx.Memory.TryWrite(attributeAddress, attribute))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "PjS5uASwcV8",
        ExportName = "sceVideoOutSetBufferAttribute2",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutSetBufferAttribute2(CpuContext ctx)
    {
        var attributeAddress = ctx[CpuRegister.Rdi];
        var pixelFormat = ctx[CpuRegister.Rsi];
        var tilingMode = unchecked((uint)ctx[CpuRegister.Rdx]);
        var width = unchecked((uint)ctx[CpuRegister.Rcx]);
        var height = unchecked((uint)ctx[CpuRegister.R8]);
        var option = ctx[CpuRegister.R9];
        if (!TryReadStackUInt32(ctx, 0, out var dccControl) ||
            !ctx.TryReadUInt64(ctx[CpuRegister.Rsp] + 0x10, out var dccClearColor))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        if (attributeAddress == 0)
        {
            return OrbisVideoOutErrorInvalidAddress;
        }

        Span<byte> attribute = stackalloc byte[VideoOutBufferAttribute2Size];
        attribute.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x04..0x08], tilingMode);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x0C..0x10], width);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x10..0x14], height);
        BinaryPrimitives.WriteUInt64LittleEndian(attribute[0x18..0x20], option);
        BinaryPrimitives.WriteUInt64LittleEndian(attribute[0x20..0x28], pixelFormat);
        BinaryPrimitives.WriteUInt64LittleEndian(attribute[0x28..0x30], dccClearColor);
        BinaryPrimitives.WriteUInt32LittleEndian(attribute[0x30..0x34], dccControl);
        if (!ctx.Memory.TryWrite(attributeAddress, attribute))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "w3BY+tAEiQY",
        ExportName = "sceVideoOutRegisterBuffers",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutRegisterBuffers(CpuContext ctx)
    {
        var handle = unchecked((int)ctx[CpuRegister.Rdi]);
        var startIndex = unchecked((int)ctx[CpuRegister.Rsi]);
        var addressesAddress = ctx[CpuRegister.Rdx];
        var bufferNum = unchecked((int)ctx[CpuRegister.Rcx]);
        var attributeAddress = ctx[CpuRegister.R8];
        if (!TryGetPort(handle, out var port))
        {
            return OrbisVideoOutErrorInvalidHandle;
        }

        if (addressesAddress == 0)
        {
            return OrbisVideoOutErrorInvalidAddress;
        }

        if (attributeAddress == 0)
        {
            return OrbisVideoOutErrorInvalidOption;
        }

        if (!IsValidBufferRange(startIndex, bufferNum))
        {
            return OrbisVideoOutErrorInvalidValue;
        }

        for (var i = 0; i < bufferNum; i++)
        {
            if (!ctx.TryReadUInt64(addressesAddress + ((ulong)i * 8), out _))
            {
                return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
            }
        }

        var setId = AllocateBufferSet(port);
        return setId;
    }

    [SysAbiExport(
        Nid = "rKBUtgRrtbk",
        ExportName = "sceVideoOutRegisterBuffers2",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceVideoOut")]
    public static int VideoOutRegisterBuffers2(CpuContext ctx)
    {
        var handle = unchecked((int)ctx[CpuRegister.Rdi]);
        var setIndex = unchecked((int)ctx[CpuRegister.Rsi]);
        var bufferIndexStart = unchecked((int)ctx[CpuRegister.Rdx]);
        var buffersAddress = ctx[CpuRegister.Rcx];
        var bufferNum = unchecked((int)ctx[CpuRegister.R8]);
        var attributeAddress = ctx[CpuRegister.R9];
        if (!ctx.TryReadUInt64(ctx[CpuRegister.Rsp] + 0x08, out var categoryRaw) ||
            !ctx.TryReadUInt64(ctx[CpuRegister.Rsp] + 0x10, out var option))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        if (!TryGetPort(handle, out var port))
        {
            return OrbisVideoOutErrorInvalidHandle;
        }

        if (buffersAddress == 0)
        {
            return OrbisVideoOutErrorInvalidAddress;
        }

        if (attributeAddress == 0)
        {
            return OrbisVideoOutErrorInvalidOption;
        }

        if (!IsValidBufferRange(bufferIndexStart, bufferNum))
        {
            return OrbisVideoOutErrorInvalidValue;
        }

        if (categoryRaw != 0 || option != 0)
        {
            return OrbisVideoOutErrorInvalidValue;
        }

        for (var i = 0; i < bufferNum; i++)
        {
            var entryAddress = buffersAddress + ((ulong)i * VideoOutBuffersEntrySize);
            if (!ctx.TryReadUInt64(entryAddress + 0x00, out _) ||
                !ctx.TryReadUInt64(entryAddress + 0x08, out _))
            {
                return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
            }
        }

        lock (_stateGate)
        {
            port.RegisteredSetIds.Add(setIndex);
        }

        return setIndex;
    }

    private static int AllocateBufferSet(VideoOutPortState port)
    {
        lock (_stateGate)
        {
            var setId = port.NextSetId++;
            port.RegisteredSetIds.Add(setId);
            return setId;
        }
    }

    private static bool IsValidBufferRange(int startIndex, int bufferNum)
    {
        return startIndex >= 0 &&
               startIndex < MaxDisplayBuffers &&
               bufferNum >= 1 &&
               bufferNum <= MaxDisplayBuffers &&
               startIndex + bufferNum <= MaxDisplayBuffers;
    }

    private static bool TryGetPort(int handle, [NotNullWhen(true)] out VideoOutPortState? port)
    {
        lock (_stateGate)
        {
            return _ports.TryGetValue(handle, out port);
        }
    }

    private static bool TryReadStackUInt32(CpuContext ctx, int stackIndex, out uint value)
    {
        var address = ctx[CpuRegister.Rsp] + 0x08 + ((ulong)stackIndex * 0x08);
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        if (!ctx.Memory.TryRead(address, buffer))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        return true;
    }
}
