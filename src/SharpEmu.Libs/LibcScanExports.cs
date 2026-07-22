// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using SharpEmu.HLE;
using SharpEmu.Libs.Kernel;

namespace SharpEmu.Libs.LibcStdio;

public static class LibcScanExports
{
    private const int ScanBufferLimit = 1_048_576;

    [ThreadStatic]
    private static int _currentSscanfInputLength;

    private enum IntegerScanMode
    {
        SignedDecimal,
        Auto,
        UnsignedDecimal,
        Octal,
        Hexadecimal,
        Pointer,
    }

    private enum ScanfLength
    {
        None,
        Char,
        Short,
        Long,
        LongDouble,
    }

    [SysAbiExport(
        Nid = "1Pk0qZQGeWo",
        ExportName = "sscanf",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libc")]
    public static int Sscanf(CpuContext ctx)
    {
        var sourceAddress = ctx[CpuRegister.Rdi];
        var formatAddress = ctx[CpuRegister.Rsi];

        if (sourceAddress == 0 || formatAddress == 0 ||
            !KernelMemoryCompatExports.TryReadCString(ctx, sourceAddress, ScanBufferLimit, out var sourceBytes) ||
            !KernelMemoryCompatExports.TryReadCString(ctx, formatAddress, ScanBufferLimit, out var formatBytes))
        {
            return MemoryFault(ctx);
        }

        var input = sourceBytes.AsSpan();
        var format = formatBytes.AsSpan();
        _currentSscanfInputLength = input.Length;
        var arguments = new RegisterScanfArgumentSource(ctx, firstGpIndex: 2);
        var inputIndex = 0;
        var formatIndex = 0;
        var assignedCount = 0;

        while (formatIndex < format.Length)
        {
            if (IsAsciiWhitespace(format[formatIndex]))
            {
                SkipAsciiWhitespace(input, ref inputIndex);
                formatIndex++;
                continue;
            }

            if (format[formatIndex] != (byte)'%')
            {
                if (inputIndex >= input.Length || input[inputIndex] != format[formatIndex])
                {
                    return ReturnScanfResult(ctx, assignedCount, inputIndex);
                }

                inputIndex++;
                formatIndex++;
                continue;
            }

            formatIndex++;
            if (formatIndex >= format.Length)
            {
                break;
            }

            if (format[formatIndex] == (byte)'%')
            {
                if (inputIndex >= input.Length || input[inputIndex] != (byte)'%')
                {
                    return ReturnScanfResult(ctx, assignedCount, inputIndex);
                }

                inputIndex++;
                formatIndex++;
                continue;
            }

            var suppress = false;
            if (format[formatIndex] == (byte)'*')
            {
                suppress = true;
                formatIndex++;
            }

            var width = -1;
            while (formatIndex < format.Length && IsAsciiDigit(format[formatIndex]))
            {
                width = width < 0 ? 0 : width;
                var digit = format[formatIndex] - (byte)'0';
                if (width > (int.MaxValue - digit) / 10)
                {
                    width = int.MaxValue;
                }
                else
                {
                    width = width * 10 + digit;
                }

                formatIndex++;
            }

            var length = ScanfLength.None;
            if (formatIndex < format.Length)
            {
                if (formatIndex + 1 < format.Length && format[formatIndex] == (byte)'h' && format[formatIndex + 1] == (byte)'h')
                {
                    length = ScanfLength.Char;
                    formatIndex += 2;
                }
                else if (formatIndex + 1 < format.Length && format[formatIndex] == (byte)'l' && format[formatIndex + 1] == (byte)'l')
                {
                    length = ScanfLength.Long;
                    formatIndex += 2;
                }
                else if (format[formatIndex] == (byte)'h')
                {
                    length = ScanfLength.Short;
                    formatIndex++;
                }
                else if (format[formatIndex] == (byte)'l' || format[formatIndex] == (byte)'j' || format[formatIndex] == (byte)'z' || format[formatIndex] == (byte)'t')
                {
                    length = ScanfLength.Long;
                    formatIndex++;
                }
                else if (format[formatIndex] == (byte)'L')
                {
                    length = ScanfLength.LongDouble;
                    formatIndex++;
                }
            }

            if (formatIndex >= format.Length)
            {
                break;
            }

            var specifier = (char)format[formatIndex++];
            switch (specifier)
            {
                case 'd':
                case 'i':
                case 'u':
                case 'o':
                case 'x':
                case 'X':
                case 'p':
                    {
                        SkipAsciiWhitespace(input, ref inputIndex);
                        if (!TryScanIntegerToken(input, inputIndex, width, GetIntegerScanMode(specifier), out var consumed, out var negative, out var rawValue))
                        {
                            return ReturnScanfResult(ctx, assignedCount, inputIndex);
                        }

                        if (!suppress)
                        {
                            var destination = arguments.NextGpArg();
                            if (destination == 0 ||
                                !TryWriteScanfInteger(ctx, destination, specifier, length, negative, rawValue))
                            {
                                return MemoryFault(ctx);
                            }

                            assignedCount++;
                        }

                        inputIndex += consumed;
                    }
                    break;

                case 'f':
                case 'F':
                case 'e':
                case 'E':
                case 'g':
                case 'G':
                case 'a':
                case 'A':
                    {
                        SkipAsciiWhitespace(input, ref inputIndex);
                        if (!TryScanFloatToken(input, inputIndex, width, specifier is 'a' or 'A', out var consumed, out var value))
                        {
                            return ReturnScanfResult(ctx, assignedCount, inputIndex);
                        }

                        if (!suppress)
                        {
                            var destination = arguments.NextGpArg();
                            if (destination == 0 || !TryWriteScanfFloat(ctx, destination, length, value))
                            {
                                return MemoryFault(ctx);
                            }

                            assignedCount++;
                        }

                        inputIndex += consumed;
                    }
                    break;

                case 's':
                    {
                        SkipAsciiWhitespace(input, ref inputIndex);
                        if (!TryScanStringToken(input, inputIndex, width, out var consumed))
                        {
                            return ReturnScanfResult(ctx, assignedCount, inputIndex);
                        }

                        if (!suppress)
                        {
                            var destination = arguments.NextGpArg();
                            if (destination == 0 || !TryWriteStringWithTerminator(ctx, destination, input.Slice(inputIndex, consumed)))
                            {
                                return MemoryFault(ctx);
                            }

                            assignedCount++;
                        }

                        inputIndex += consumed;
                    }
                    break;

                case 'c':
                    {
                        var charCount = width > 0 ? width : 1;
                        if (!TryScanFixedBytes(input, inputIndex, charCount, out var consumed))
                        {
                            return ReturnScanfResult(ctx, assignedCount, inputIndex);
                        }

                        if (!suppress)
                        {
                            var destination = arguments.NextGpArg();
                            if (destination == 0 || !TryWriteBytes(ctx, destination, input.Slice(inputIndex, consumed)))
                            {
                                return MemoryFault(ctx);
                            }

                            assignedCount++;
                        }

                        inputIndex += consumed;
                    }
                    break;

                case '[':
                    {
                        if (!TryParseScanset(format, ref formatIndex, out var allowed, out var negated))
                        {
                            return ReturnScanfResult(ctx, assignedCount, inputIndex);
                        }

                        if (!TryScanScansetToken(input, inputIndex, width, allowed, negated, out var consumed))
                        {
                            return ReturnScanfResult(ctx, assignedCount, inputIndex);
                        }

                        if (!suppress)
                        {
                            var destination = arguments.NextGpArg();
                            if (destination == 0 || !TryWriteStringWithTerminator(ctx, destination, input.Slice(inputIndex, consumed)))
                            {
                                return MemoryFault(ctx);
                            }

                            assignedCount++;
                        }

                        inputIndex += consumed;
                    }
                    break;

                case 'n':
                    {
                        if (!suppress)
                        {
                            var destination = arguments.NextGpArg();
                            if (destination == 0 || !TryWriteScanfCount(ctx, destination, length, inputIndex))
                            {
                                return MemoryFault(ctx);
                            }
                        }
                    }
                    break;

                case '%':
                    if (inputIndex >= input.Length || input[inputIndex] != (byte)'%')
                    {
                        return ReturnScanfResult(ctx, assignedCount, inputIndex);
                    }

                    inputIndex++;
                    break;

                default:
                    return ReturnScanfResult(ctx, assignedCount, inputIndex);
            }
        }

        return ReturnScanfResult(ctx, assignedCount, inputIndex);
    }

    [SysAbiExport(
        Nid = "5TjaJwkLWxE",
        ExportName = "bcmp",
        Target = Generation.Gen4 | Generation.Gen5,
        LibraryName = "libc")]
    public static int Bcmp(CpuContext ctx)
    {
        return KernelMemoryCompatExports.Memcmp(ctx);
    }

    private static int ReturnScanfResult(CpuContext ctx, int assignedCount, int inputIndex)
    {
        ctx[CpuRegister.Rax] = unchecked((ulong)(assignedCount == 0 && inputIndex >= _currentSscanfInputLength ? -1 : assignedCount));
        return (int)OrbisGen2Result.ORBIS_GEN2_OK;
    }

    private static int MemoryFault(CpuContext ctx)
    {
        ctx[CpuRegister.Rax] = 0;
        return (int)OrbisGen2Result.ORBIS_GEN2_ERROR_MEMORY_FAULT;
    }

    private static IntegerScanMode GetIntegerScanMode(char specifier)
    {
        return specifier switch
        {
            'd' => IntegerScanMode.SignedDecimal,
            'i' => IntegerScanMode.Auto,
            'u' => IntegerScanMode.UnsignedDecimal,
            'o' => IntegerScanMode.Octal,
            'x' or 'X' => IntegerScanMode.Hexadecimal,
            'p' => IntegerScanMode.Pointer,
            _ => IntegerScanMode.SignedDecimal,
        };
    }

    private static bool TryScanIntegerToken(
        ReadOnlySpan<byte> input,
        int start,
        int width,
        IntegerScanMode mode,
        out int consumed,
        out bool negative,
        out ulong value)
    {
        consumed = 0;
        negative = false;
        value = 0;

        if (start >= input.Length || width == 0)
        {
            return false;
        }

        var limit = width < 0 ? input.Length : Math.Min(input.Length, start + width);
        var index = start;

        if (index < limit && (input[index] == (byte)'+' || input[index] == (byte)'-'))
        {
            negative = input[index] == (byte)'-';
            index++;
            if (index >= limit)
            {
                return false;
            }
        }

        var baseValue = mode switch
        {
            IntegerScanMode.SignedDecimal => 10,
            IntegerScanMode.UnsignedDecimal => 10,
            IntegerScanMode.Octal => 8,
            IntegerScanMode.Hexadecimal => 16,
            IntegerScanMode.Pointer => 16,
            _ => 0,
        };

        if (mode == IntegerScanMode.Auto)
        {
            if (index < limit && input[index] == (byte)'0')
            {
                if (index + 1 < limit && (input[index + 1] == (byte)'x' || input[index + 1] == (byte)'X'))
                {
                    baseValue = 16;
                    index += 2;
                }
                else
                {
                    baseValue = 8;
                }
            }
            else
            {
                baseValue = 10;
            }
        }
        else if (mode is IntegerScanMode.Hexadecimal or IntegerScanMode.Pointer &&
                 index + 1 < limit &&
                 input[index] == (byte)'0' &&
                 (input[index + 1] == (byte)'x' || input[index + 1] == (byte)'X'))
        {
            index += 2;
        }

        var anyDigits = false;
        var accum = 0UL;
        while (index < limit)
        {
            var digit = GetDigitValue(input[index]);
            if (digit < 0 || digit >= baseValue)
            {
                break;
            }

            anyDigits = true;
            accum = accum * (ulong)baseValue + (ulong)digit;
            index++;
        }

        if (!anyDigits)
        {
            return false;
        }

        consumed = index - start;
        value = accum;
        return true;
    }

    private static bool TryScanFloatToken(
        ReadOnlySpan<byte> input,
        int start,
        int width,
        bool hexMode,
        out int consumed,
        out double value)
    {
        consumed = 0;
        value = 0;

        if (start >= input.Length || width == 0)
        {
            return false;
        }

        var limit = width < 0 ? input.Length : Math.Min(input.Length, start + width);
        var index = start;
        var negative = false;

        if (index < limit && (input[index] == (byte)'+' || input[index] == (byte)'-'))
        {
            negative = input[index] == (byte)'-';
            index++;
            if (index >= limit)
            {
                return false;
            }
        }

        if (MatchesAsciiIgnoreCase(input, index, "nan", limit, out var nanLength))
        {
            index += nanLength;
            if (index < limit && input[index] == (byte)'(')
            {
                index++;
                while (index < limit && input[index] != (byte)')')
                {
                    index++;
                }

                if (index < limit && input[index] == (byte)')')
                {
                    index++;
                }
            }

            consumed = index - start;
            value = double.NaN;
            return true;
        }

        if (MatchesAsciiIgnoreCase(input, index, "inf", limit, out var infLength))
        {
            index += infLength;
            if (MatchesAsciiIgnoreCase(input, index, "inity", limit, out var infinityLength))
            {
                index += infinityLength;
            }

            consumed = index - start;
            value = negative ? double.NegativeInfinity : double.PositiveInfinity;
            return true;
        }

        if (hexMode)
        {
            if (index + 1 >= limit || input[index] != (byte)'0' || (input[index + 1] != (byte)'x' && input[index + 1] != (byte)'X'))
            {
                return false;
            }

            index += 2;
            var anyDigits = false;
            var mantissa = 0.0;
            var fracFactor = 1.0;
            var seenDot = false;

            while (index < limit)
            {
                var digit = GetHexDigitValue(input[index]);
                if (digit >= 0)
                {
                    anyDigits = true;
                    if (!seenDot)
                    {
                        mantissa = mantissa * 16.0 + digit;
                    }
                    else
                    {
                        fracFactor *= 16.0;
                        mantissa += digit / fracFactor;
                    }

                    index++;
                    continue;
                }

                if (input[index] == (byte)'.' && !seenDot)
                {
                    seenDot = true;
                    index++;
                    continue;
                }

                break;
            }

            if (!anyDigits)
            {
                return false;
            }

            var exponent = 0;
            if (index < limit && (input[index] == (byte)'p' || input[index] == (byte)'P'))
            {
                index++;
                var exponentNegative = false;
                if (index < limit && (input[index] == (byte)'+' || input[index] == (byte)'-'))
                {
                    exponentNegative = input[index] == (byte)'-';
                    index++;
                }

                if (index >= limit || !IsAsciiDigit(input[index]))
                {
                    return false;
                }

                var exponentValue = 0;
                while (index < limit && IsAsciiDigit(input[index]))
                {
                    var digit = input[index] - (byte)'0';
                    if (exponentValue > (int.MaxValue - digit) / 10)
                    {
                        exponentValue = int.MaxValue;
                    }
                    else
                    {
                        exponentValue = exponentValue * 10 + digit;
                    }

                    index++;
                }

                exponent = exponentNegative ? -exponentValue : exponentValue;
            }

            value = mantissa * Math.Pow(2.0, exponent);
            if (negative)
            {
                value = -value;
            }

            consumed = index - start;
            return true;
        }

        var tokenEnd = index;
        var seenDigit = false;
        var decimalSeenDot = false;
        var seenExponent = false;
        while (tokenEnd < limit)
        {
            var c = input[tokenEnd];
            if (IsAsciiDigit(c))
            {
                seenDigit = true;
                tokenEnd++;
                continue;
            }

            if (c == (byte)'.' && !decimalSeenDot && !seenExponent)
            {
                decimalSeenDot = true;
                tokenEnd++;
                continue;
            }

            if ((c == (byte)'e' || c == (byte)'E') && !seenExponent && seenDigit)
            {
                var exponentIndex = tokenEnd + 1;
                if (exponentIndex < limit && (input[exponentIndex] == (byte)'+' || input[exponentIndex] == (byte)'-'))
                {
                    exponentIndex++;
                }

                if (exponentIndex >= limit || !IsAsciiDigit(input[exponentIndex]))
                {
                    return false;
                }

                seenExponent = true;
                tokenEnd = exponentIndex;
                while (tokenEnd < limit && IsAsciiDigit(input[tokenEnd]))
                {
                    tokenEnd++;
                }

                seenDigit = true;
                continue;
            }

            break;
        }

        if (!seenDigit)
        {
            return false;
        }

        var token = Encoding.ASCII.GetString(input.Slice(start, tokenEnd - start));
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        consumed = tokenEnd - start;
        return true;
    }

    private static bool TryScanStringToken(ReadOnlySpan<byte> input, int start, int width, out int consumed)
    {
        consumed = 0;
        if (start >= input.Length || width == 0)
        {
            return false;
        }

        var limit = width < 0 ? input.Length : Math.Min(input.Length, start + width);
        var index = start;
        while (index < limit && !IsAsciiWhitespace(input[index]))
        {
            index++;
        }

        if (index == start)
        {
            return false;
        }

        consumed = index - start;
        return true;
    }

    private static bool TryScanFixedBytes(ReadOnlySpan<byte> input, int start, int count, out int consumed)
    {
        consumed = 0;
        if (count <= 0 || start < 0 || count > input.Length - start)
        {
            return false;
        }

        consumed = count;
        return true;
    }

    private static bool TryScanScansetToken(
        ReadOnlySpan<byte> input,
        int start,
        int width,
        bool[] allowed,
        bool negated,
        out int consumed)
    {
        consumed = 0;
        if (start >= input.Length || width == 0)
        {
            return false;
        }

        var limit = width < 0 ? input.Length : Math.Min(input.Length, start + width);
        var index = start;
        while (index < limit && IsScansetMatch(input[index], allowed, negated))
        {
            index++;
        }

        if (index == start)
        {
            return false;
        }

        consumed = index - start;
        return true;
    }

    private static bool TryParseScanset(ReadOnlySpan<byte> format, ref int formatIndex, out bool[] allowed, out bool negated)
    {
        allowed = new bool[256];
        negated = false;

        if (formatIndex >= format.Length)
        {
            return false;
        }

        if (format[formatIndex] == (byte)'^')
        {
            negated = true;
            formatIndex++;
            if (formatIndex >= format.Length)
            {
                return false;
            }
        }

        if (format[formatIndex] == (byte)']')
        {
            allowed[']'] = true;
            formatIndex++;
        }

        var pending = -1;
        var hasPending = false;
        while (formatIndex < format.Length)
        {
            var c = format[formatIndex];
            if (c == (byte)']')
            {
                if (hasPending)
                {
                    allowed[pending] = true;
                    hasPending = false;
                }

                formatIndex++;
                return true;
            }

            if (c == (byte)'-' && hasPending && formatIndex + 1 < format.Length && format[formatIndex + 1] != (byte)']')
            {
                AddScansetRange(allowed, (byte)pending, format[formatIndex + 1]);
                hasPending = false;
                formatIndex += 2;
                continue;
            }

            if (hasPending)
            {
                allowed[pending] = true;
            }

            pending = c;
            hasPending = true;
            formatIndex++;
        }

        return false;
    }

    private static void AddScansetRange(bool[] allowed, byte start, byte end)
    {
        if (start <= end)
        {
            for (var i = start; i <= end; i++)
            {
                allowed[i] = true;
            }
        }
        else
        {
            for (var i = end; i <= start; i++)
            {
                allowed[i] = true;
            }
        }
    }

    private static bool TryWriteStringWithTerminator(CpuContext ctx, ulong address, ReadOnlySpan<byte> bytes)
    {
        var payload = new byte[bytes.Length + 1];
        bytes.CopyTo(payload);
        return TryWriteBytes(ctx, address, payload);
    }

    private static bool TryWriteScanfCount(CpuContext ctx, ulong address, ScanfLength length, int value)
    {
        return TryWriteSignedValue(ctx, address, length, value);
    }

    private static bool TryWriteScanfInteger(
        CpuContext ctx,
        ulong address,
        char specifier,
        ScanfLength length,
        bool negative,
        ulong rawValue)
    {
        if (specifier == 'p')
        {
            var pointerValue = negative ? unchecked(0UL - rawValue) : rawValue;
            return TryWriteUnsignedValue(ctx, address, ScanfLength.Long, pointerValue);
        }

        if (specifier is 'd' or 'i')
        {
            var signedValue = negative ? unchecked((long)(0UL - rawValue)) : unchecked((long)rawValue);
            return TryWriteSignedValue(ctx, address, length, signedValue);
        }

        var unsignedValue = negative ? unchecked(0UL - rawValue) : rawValue;
        return TryWriteUnsignedValue(ctx, address, length, unsignedValue);
    }

    private static bool TryWriteScanfFloat(CpuContext ctx, ulong address, ScanfLength length, double value)
    {
        Span<byte> buffer = stackalloc byte[16];
        switch (length)
        {
            case ScanfLength.LongDouble:
                BinaryPrimitives.WriteInt64LittleEndian(buffer, unchecked((long)BitConverter.DoubleToInt64Bits(value)));
                buffer.Slice(8, 8).Clear();
                return TryWriteBytes(ctx, address, buffer[..16]);

            case ScanfLength.Long:
                BinaryPrimitives.WriteInt64LittleEndian(buffer, unchecked((long)BitConverter.DoubleToInt64Bits(value)));
                return TryWriteBytes(ctx, address, buffer[..8]);

            default:
                BinaryPrimitives.WriteInt32LittleEndian(buffer, BitConverter.SingleToInt32Bits((float)value));
                return TryWriteBytes(ctx, address, buffer[..4]);
        }
    }

    private static bool TryWriteSignedValue(CpuContext ctx, ulong address, ScanfLength length, long value)
    {
        Span<byte> buffer = stackalloc byte[16];
        return length switch
        {
            ScanfLength.Char => TryWriteBytes(ctx, address, WriteSingleByte(buffer, unchecked((byte)value))),
            ScanfLength.Short => TryWriteBytes(ctx, address, WriteInt16(buffer, unchecked((short)value))),
            ScanfLength.Long or ScanfLength.LongDouble => TryWriteBytes(ctx, address, WriteInt64(buffer, value)),
            _ => TryWriteBytes(ctx, address, WriteInt32(buffer, unchecked((int)value))),
        };
    }

    private static bool TryWriteUnsignedValue(CpuContext ctx, ulong address, ScanfLength length, ulong value)
    {
        Span<byte> buffer = stackalloc byte[16];
        return length switch
        {
            ScanfLength.Char => TryWriteBytes(ctx, address, WriteSingleByte(buffer, unchecked((byte)value))),
            ScanfLength.Short => TryWriteBytes(ctx, address, WriteUInt16(buffer, unchecked((ushort)value))),
            ScanfLength.Long or ScanfLength.LongDouble => TryWriteBytes(ctx, address, WriteUInt64(buffer, value)),
            _ => TryWriteBytes(ctx, address, WriteUInt32(buffer, unchecked((uint)value))),
        };
    }

    private static ReadOnlySpan<byte> WriteSingleByte(Span<byte> buffer, byte value)
    {
        buffer[0] = value;
        return buffer[..1];
    }

    private static ReadOnlySpan<byte> WriteInt16(Span<byte> buffer, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        return buffer[..2];
    }

    private static ReadOnlySpan<byte> WriteUInt16(Span<byte> buffer, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        return buffer[..2];
    }

    private static ReadOnlySpan<byte> WriteInt32(Span<byte> buffer, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        return buffer[..4];
    }

    private static ReadOnlySpan<byte> WriteUInt32(Span<byte> buffer, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        return buffer[..4];
    }

    private static ReadOnlySpan<byte> WriteInt64(Span<byte> buffer, long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        return buffer[..8];
    }

    private static ReadOnlySpan<byte> WriteUInt64(Span<byte> buffer, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        return buffer[..8];
    }

    private static bool TryWriteBytes(CpuContext ctx, ulong address, ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return true;
        }

        return ctx.Memory.TryWrite(address, bytes) || KernelMemoryCompatExports.TryWriteHostMemory(address, bytes);
    }

    private static void SkipAsciiWhitespace(ReadOnlySpan<byte> input, ref int index)
    {
        while (index < input.Length && IsAsciiWhitespace(input[index]))
        {
            index++;
        }
    }

    private static bool IsScansetMatch(byte value, bool[] allowed, bool negated)
    {
        return negated ? !allowed[value] : allowed[value];
    }

    private static bool IsAsciiWhitespace(byte value)
    {
        return value is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' or (byte)'\v' or (byte)'\f';
    }

    private static bool IsAsciiDigit(byte value)
    {
        return value is >= (byte)'0' and <= (byte)'9';
    }

    private static int GetDigitValue(byte value)
    {
        return value switch
        {
            >= (byte)'0' and <= (byte)'9' => value - (byte)'0',
            >= (byte)'a' and <= (byte)'f' => 10 + (value - (byte)'a'),
            >= (byte)'A' and <= (byte)'F' => 10 + (value - (byte)'A'),
            _ => -1,
        };
    }

    private static int GetHexDigitValue(byte value)
    {
        return GetDigitValue(value);
    }

    private static bool MatchesAsciiIgnoreCase(ReadOnlySpan<byte> input, int start, string text, int limit, out int matchedLength)
    {
        matchedLength = 0;
        if (start < 0 || start >= limit || limit - start < text.Length)
        {
            return false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var actual = ToLowerAscii(input[start + i]);
            var expected = ToLowerAscii((byte)text[i]);
            if (actual != expected)
            {
                return false;
            }
        }

        matchedLength = text.Length;
        return true;
    }

    private static byte ToLowerAscii(byte value)
    {
        return value is >= (byte)'A' and <= (byte)'Z' ? (byte)(value + 0x20) : value;
    }

    private static ulong ReadStackArg(CpuContext ctx, ulong offset)
    {
        var rsp = ctx[CpuRegister.Rsp];
        var address = rsp + offset + 8;
        if (ctx.TryReadUInt64(address, out var value))
        {
            return value;
        }

        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        return KernelMemoryCompatExports.TryReadHostMemory(address, buffer)
            ? BinaryPrimitives.ReadUInt64LittleEndian(buffer)
            : 0;
    }

    private struct RegisterScanfArgumentSource
    {
        private readonly CpuContext _ctx;
        private int _gpIndex;
        private int _stackIndex;

        public RegisterScanfArgumentSource(CpuContext ctx, int firstGpIndex)
        {
            _ctx = ctx;
            _gpIndex = firstGpIndex;
            _stackIndex = 0;
        }

        public ulong NextGpArg()
        {
            var index = _gpIndex++;
            return index switch
            {
                0 => _ctx[CpuRegister.Rdi],
                1 => _ctx[CpuRegister.Rsi],
                2 => _ctx[CpuRegister.Rdx],
                3 => _ctx[CpuRegister.Rcx],
                4 => _ctx[CpuRegister.R8],
                5 => _ctx[CpuRegister.R9],
                _ => ReadStackArg(_ctx, (ulong)(_stackIndex++) * 8),
            };
        }
    }
}
