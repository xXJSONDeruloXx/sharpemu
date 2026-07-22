// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;

namespace SharpEmu.Libs.Libc;

public static class LibcLockExports
{
    [SysAbiExport(
        Nid = "kALvdgEv5ME",
        ExportName = "_Locksyslock",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int Locksyslock(CpuContext ctx)
    {
        ctx[CpuRegister.Rax] = 0;
        return 0;
    }

    [SysAbiExport(
        Nid = "9nf8joUTSaQ",
        ExportName = "_Unlocksyslock",
        Target = Generation.Gen5,
        LibraryName = "libc")]
    public static int Unlocksyslock(CpuContext ctx)
    {
        ctx[CpuRegister.Rax] = 0;
        return 0;
    }
}
