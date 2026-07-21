// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Text;
using SharpEmu.HLE;
using SharpEmu.Libs.Kernel;
using Xunit;

namespace SharpEmu.Libs.Tests.Kernel;

public sealed class KernelSemaphoreSemanticsTests
{
    private const ulong MemoryBase = 0x1_0000_0000;
    private const ulong HandleAddress = MemoryBase + 0x100;
    private const ulong NameAddress = MemoryBase + 0x200;
    private const ulong TimeoutAddress = MemoryBase + 0x300;

    [Fact]
    public void WaitWithZeroTimeoutDoesNotParkWhenNoTokenIsAvailable()
    {
        var context = CreateContext();
        var handle = CreateSemaphore(context, initialCount: 0, maxCount: 1);
        WriteUInt32(context, TimeoutAddress, 0);

        context[CpuRegister.Rdi] = handle;
        context[CpuRegister.Rsi] = 1;
        context[CpuRegister.Rdx] = TimeoutAddress;

        Assert.Equal((int)OrbisGen2Result.ORBIS_GEN2_ERROR_TIMED_OUT,
            KernelSemaphoreCompatExports.KernelWaitSema(context));
        Assert.Equal(0U, ReadUInt32(context, TimeoutAddress));

        DeleteSemaphore(context, handle);
    }

    [Fact]
    public async Task CancelWakesBlockedWaiterWithCanceledResult()
    {
        var context = CreateContext();
        var handle = CreateSemaphore(context, initialCount: 0, maxCount: 1);
        var waiterContext = CreateContext();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitTask = Task.Run(() =>
        {
            started.SetResult(true);
            return WaitSemaphore(waiterContext, handle);
        });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await Task.Delay(25);
        CancelSemaphore(context, handle);

        var result = await waitTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal((int)OrbisGen2Result.ORBIS_GEN2_ERROR_CANCELED, result);
    }

    [Fact]
    public async Task DeleteWakesBlockedWaiterWithDeletedResult()
    {
        var context = CreateContext();
        var handle = CreateSemaphore(context, initialCount: 0, maxCount: 1);
        var waiterContext = CreateContext();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitTask = Task.Run(() =>
        {
            started.SetResult(true);
            return WaitSemaphore(waiterContext, handle);
        });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await Task.Delay(25);
        DeleteSemaphore(context, handle);

        var result = await waitTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal((int)OrbisGen2Result.ORBIS_GEN2_ERROR_DELETED, result);
    }

    private static CpuContext CreateContext() =>
        new(new FakeCpuMemory(MemoryBase, 0x1000), Generation.Gen5);

    private static uint CreateSemaphore(CpuContext context, int initialCount, int maxCount)
    {
        var name = Encoding.UTF8.GetBytes("test-sema\0");
        Assert.True(context.Memory.TryWrite(NameAddress, name));

        context[CpuRegister.Rdi] = HandleAddress;
        context[CpuRegister.Rsi] = NameAddress;
        context[CpuRegister.Rdx] = 1;
        context[CpuRegister.Rcx] = unchecked((ulong)initialCount);
        context[CpuRegister.R8] = unchecked((ulong)maxCount);
        context[CpuRegister.R9] = 0;

        Assert.Equal(0, KernelSemaphoreCompatExports.KernelCreateSema(context));
        return ReadUInt32(context, HandleAddress);
    }

    private static int WaitSemaphore(CpuContext context, uint handle)
    {
        context[CpuRegister.Rdi] = handle;
        context[CpuRegister.Rsi] = 1;
        context[CpuRegister.Rdx] = 0;
        return KernelSemaphoreCompatExports.KernelWaitSema(context);
    }

    private static void CancelSemaphore(CpuContext context, uint handle)
    {
        context[CpuRegister.Rdi] = handle;
        context[CpuRegister.Rsi] = unchecked((ulong)(-1));
        context[CpuRegister.Rdx] = 0;
        context[CpuRegister.Rcx] = 0;
        Assert.Equal(0, KernelSemaphoreCompatExports.KernelCancelSema(
            context,
            handle,
            -1,
            0));
    }

    private static void DeleteSemaphore(CpuContext context, uint handle)
    {
        context[CpuRegister.Rdi] = handle;
        Assert.Equal(0, KernelSemaphoreCompatExports.KernelDeleteSema(context));
    }

    private static uint ReadUInt32(CpuContext context, ulong address)
    {
        Assert.True(context.TryReadUInt32(address, out var value));
        return value;
    }

    private static void WriteUInt32(CpuContext context, ulong address, uint value) =>
        Assert.True(context.TryWriteUInt32(address, value));
}
