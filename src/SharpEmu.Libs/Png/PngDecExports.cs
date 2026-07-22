// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using SharpEmu.HLE;

namespace SharpEmu.Libs.Png;

public static class PngDecExports
{
    [SysAbiExport(
        Nid = "U6h4e5JRPaQ",
        ExportName = "scePngDecParseHeader",
        Target = Generation.Gen5,
        LibraryName = "libScePngDec")]
    public static int ParseHeader(CpuContext ctx)
    {
        // Header parsing is currently performed by the title's bundled image
        // path. Keep the system import successful until the full decoder API
        // is implemented; callers use the return value as the initialization
        // gate and assert on the unresolved-import error.
        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }
}
