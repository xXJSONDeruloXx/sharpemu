// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Collections.Concurrent;
using System.Threading;
using SharpEmu.HLE;
using SharpEmu.Libs.Kernel;

namespace SharpEmu.Libs.CxxAbi;

public static class CxxMtxExports
{
    private sealed class MtxState
    {
        public object SyncRoot { get; } = new();
        public ulong OwnerThreadId { get; set; }
        public int RecursionCount { get; set; }
        public int Type { get; set; }
    }

    private static readonly ConcurrentDictionary<ulong, MtxState> _states = new();

    [SysAbiExport(Nid = "YaHc3GS7y7g", ExportName = "_Mtx_init", Target = Generation.Gen4 | Generation.Gen5, LibraryName = "libc")]
    public static int MtxInit(CpuContext ctx)
    {
        var mutexAddress = ctx[CpuRegister.Rdi];
        var type = unchecked((int)ctx[CpuRegister.Rsi]);
        if (mutexAddress == 0)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_INVALID_ARGUMENT;
        }

        var state = _states.GetOrAdd(mutexAddress, _ => new MtxState());
        lock (state.SyncRoot)
        {
            if (state.OwnerThreadId != 0)
            {
                ctx[CpuRegister.Rax] = 0;
                return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_BUSY;
            }

            state.Type = type;
            state.RecursionCount = 0;
            state.OwnerThreadId = 0;
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(Nid = "5Lf51jvohTQ", ExportName = "_Mtx_destroy", Target = Generation.Gen4 | Generation.Gen5, LibraryName = "libc")]
    public static int MtxDestroy(CpuContext ctx)
    {
        var mutexAddress = ctx[CpuRegister.Rdi];
        if (mutexAddress == 0)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_INVALID_ARGUMENT;
        }

        if (_states.TryGetValue(mutexAddress, out var state))
        {
            lock (state.SyncRoot)
            {
                if (state.OwnerThreadId != 0 || state.RecursionCount != 0)
                {
                    ctx[CpuRegister.Rax] = 0;
                    return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_BUSY;
                }
            }

            _states.TryRemove(mutexAddress, out _);
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(Nid = "iS4aWbUonl0", ExportName = "_Mtx_lock", Target = Generation.Gen4 | Generation.Gen5, LibraryName = "libc")]
    public static int MtxLock(CpuContext ctx)
    {
        var mutexAddress = ctx[CpuRegister.Rdi];
        if (mutexAddress == 0)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_INVALID_ARGUMENT;
        }

        var state = _states.GetOrAdd(mutexAddress, _ => new MtxState());
        var currentThreadId = KernelPthreadState.GetCurrentThreadHandle();

        lock (state.SyncRoot)
        {
            while (state.OwnerThreadId != 0 && state.OwnerThreadId != currentThreadId)
            {
                Monitor.Wait(state.SyncRoot);
            }

            if (state.OwnerThreadId == currentThreadId)
            {
                state.RecursionCount++;
            }
            else
            {
                state.OwnerThreadId = currentThreadId;
                state.RecursionCount = 1;
            }
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(Nid = "k6pGNMwJB08", ExportName = "_Mtx_trylock", Target = Generation.Gen4 | Generation.Gen5, LibraryName = "libc")]
    public static int MtxTryLock(CpuContext ctx)
    {
        var mutexAddress = ctx[CpuRegister.Rdi];
        if (mutexAddress == 0)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_INVALID_ARGUMENT;
        }

        var state = _states.GetOrAdd(mutexAddress, _ => new MtxState());
        var currentThreadId = KernelPthreadState.GetCurrentThreadHandle();
        var acquired = false;

        lock (state.SyncRoot)
        {
            if (state.OwnerThreadId == 0 || state.OwnerThreadId == currentThreadId)
            {
                state.OwnerThreadId = currentThreadId;
                state.RecursionCount++;
                acquired = true;
            }
        }

        ctx[CpuRegister.Rax] = acquired ? 0 : unchecked((ulong)(int)OrbisGen2Result.ORBIS_GEN2_ERROR_BUSY);
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(Nid = "gTuXQwP9rrs", ExportName = "_Mtx_unlock", Target = Generation.Gen4 | Generation.Gen5, LibraryName = "libc")]
    public static int MtxUnlock(CpuContext ctx)
    {
        var mutexAddress = ctx[CpuRegister.Rdi];
        if (mutexAddress == 0)
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_INVALID_ARGUMENT;
        }

        if (!_states.TryGetValue(mutexAddress, out var state))
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_NOT_FOUND;
        }

        var currentThreadId = KernelPthreadState.GetCurrentThreadHandle();
        lock (state.SyncRoot)
        {
            if (state.OwnerThreadId != currentThreadId || state.RecursionCount == 0)
            {
                ctx[CpuRegister.Rax] = 0;
                return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_PERMISSION_DENIED;
            }

            if (--state.RecursionCount == 0)
            {
                state.OwnerThreadId = 0;
                Monitor.PulseAll(state.SyncRoot);
            }
        }

        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(Nid = "VYQwFs4CC4Y", ExportName = "_Mtx_current_owns", Target = Generation.Gen4 | Generation.Gen5, LibraryName = "libc")]
    public static int MtxCurrentOwns(CpuContext ctx)
    {
        var mutexAddress = ctx[CpuRegister.Rdi];
        if (mutexAddress == 0 || !_states.TryGetValue(mutexAddress, out var state))
        {
            ctx[CpuRegister.Rax] = 0;
            return (int)OrbisGen2Result.ORBIS_GEN2_OK;
        }

        var currentThreadId = KernelPthreadState.GetCurrentThreadHandle();
        lock (state.SyncRoot)
        {
            ctx[CpuRegister.Rax] = state.OwnerThreadId == currentThreadId ? 1UL : 0UL;
        }

        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    [SysAbiExport(Nid = "bRujIheWlB0", ExportName = "_ZSt14_Throw_C_errori", Target = Generation.Gen4 | Generation.Gen5, LibraryName = "libc")]
    public static int ThrowCError(CpuContext ctx)
    {
        ctx[CpuRegister.Rax] = ctx[CpuRegister.Rdi];
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }
}
