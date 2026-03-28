// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;
using System.Buffers.Binary;
using System.Text;

namespace SharpEmu.Libs.AppContent;

public static class AppContentExports
{
    private const ulong BootParamAttrOffset = 4;
    private const string Temp0MountPoint = "/temp0";

    [SysAbiExport(
        Nid = "R9lA82OraNs",
        ExportName = "sceAppContentInitialize",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceAppContent")]
    public static int AppContentInitialize(CpuContext ctx)
    {
        var initParamAddress = ctx[CpuRegister.Rdi];
        var bootParamAddress = ctx[CpuRegister.Rsi];
        if (initParamAddress == 0 || bootParamAddress == 0)
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_INVALID_ARGUMENT;
        }

        Span<byte> attrBytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(attrBytes, 0);
        if (!ctx.Memory.TryWrite(bootParamAddress + BootParamAttrOffset, attrBytes))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "xnd8BJzAxmk",
        ExportName = "sceAppContentGetAddcontInfoList",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceAppContent")]
    public static int AppContentGetAddcontInfoList(CpuContext ctx)
    {
        var hitCountAddress = ctx[CpuRegister.Rcx];
        if (hitCountAddress != 0)
        {
            Span<byte> hitCountBytes = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(hitCountBytes, 0);
            if (!ctx.Memory.TryWrite(hitCountAddress, hitCountBytes))
            {
                return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
            }
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(
        Nid = "buYbeLOGWmA",
        ExportName = "sceAppContentTemporaryDataMount2",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libSceAppContent")]
    public static int AppContentTemporaryDataMount2(CpuContext ctx)
    {
        var mountPointAddress = ctx[CpuRegister.Rsi];
        if (mountPointAddress == 0)
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_INVALID_ARGUMENT;
        }

        Directory.CreateDirectory(ResolveTemp0Root());
        var mountPointBytes = Encoding.ASCII.GetBytes($"{Temp0MountPoint}\0");
        if (!ctx.Memory.TryWrite(mountPointAddress, mountPointBytes))
        {
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    private static string ResolveTemp0Root()
    {
        const string temp0VariableName = "SHARPEMU_TEMP0_DIR";
        var configuredRoot = Environment.GetEnvironmentVariable(temp0VariableName);
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            return Path.GetFullPath(configuredRoot);
        }

        var app0Root = Environment.GetEnvironmentVariable("SHARPEMU_APP0_DIR");
        var appName = string.IsNullOrWhiteSpace(app0Root)
            ? "default"
            : Path.GetFileName(Path.TrimEndingDirectorySeparator(app0Root));
        if (string.IsNullOrWhiteSpace(appName))
        {
            appName = "default";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        appName = new string(appName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        var root = Path.Combine(Path.GetTempPath(), "SharpEmu", appName, "temp0");
        Environment.SetEnvironmentVariable(temp0VariableName, root);
        return root;
    }
}
