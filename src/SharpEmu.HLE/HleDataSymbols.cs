// Copyright (C) 2026 SharpEmu Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Runtime.InteropServices;
using System.Text;

namespace SharpEmu.HLE;

public static class HleDataSymbols
{
    private const string StackChkGuardNid = "f7uOxY9mM1U";
    private const string ProgNameNid = "djxxOmW6-aw";
    private const string LibcNeedFlagNid = "P330P3dFF68";
    private const string LibcInternalNeedFlagNid = "ZT4ODD2Ts9o";
    private const string StdoutNid = "2sWzhYqFH4E";
    private const string StderrNid = "H8AprKeZtNg";
    private const string SceLibcClassicLocaleNid = "Qoo175Ig+-k";
    private const string StdCoutNid = "5PfqUBaQf4g";
    private const string ClassTypeInfoVTableNid = "byV+FWlAnB4";
    private const string SiClassTypeInfoVTableNid = "pZ9WXcClPO8";
    private const string VmiClassTypeInfoVTableNid = "9ByRMdo7ywg";
    private const string StdExceptionTypeInfoNid = "n2kx+OmFUis";
    private const string StdIosBaseTypeInfoNid = "BJCgW9-OxLA";
    private const string StdRuntimeErrorVTableNid = "-L+-8F0+gBc";
    private const string StdRuntimeErrorTypeInfoNid = "bLPn1gfqSW8";
    private const string StdBadCastTypeInfoNid = "qOD-ksTkE08";
    private const string StdBadCastVTableNid = "tVHE+C8vGXk";
    private const string StdLogicErrorTypeInfoNid = "x8LHSvl5N6I";
    private const string StdLogicErrorVTableNid = "udTM6Nxx-Ng";
    private const string StdIosBaseFailureTypeInfoNid = "sBCTjFk7Gi4";
    private const string StdIosBaseFailureVTableNid = "yLE5H3058Ao";
    private const string StdSystemErrorVTableNid = "Bq8m04PN1zw";
    private const string StdCtypeIdNid = "Cv+zC4EjGMA";
    private const string StdLocaleIdCountNid = "H4fcpQOpc08";
    private const string StdCodecvtVTableNid = "aK1Ymf-NhAs";
    private const string StdCodecvtIdNid = "eVFYZnYNDo0";
    private const string StdBadOffNid = "FQ9NFbBHb5Y";
    private const string StdFpzNid = "wiR+rIcbnlc";
    private const int ProgNameMaxBytes = 511;
    // Terminator canaries reserve the low byte as NUL. Keep the process data
    // symbol and every per-thread TLS copy byte-for-byte identical.
    private const ulong StackChkGuardValue = 0xC0DEC0DECAFEBA00UL;
    private const int StdStreamSize = 272;
    private const int StdClassTypeInfoVTableSize = 88;
    private const int StdTypeInfoSize = 16;
    private const int StdTypeInfoWithBaseSize = 24;
    private const int StdVTableSize = 40;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint FakeLocaleMethod(nint thisPtr, nint arg1, nint arg2, nint arg3);

    private static readonly FakeLocaleMethod _fakeLocaleMethod10 = FakeLocaleMethod10;
    private static readonly FakeLocaleMethod _fakeLocaleMethod18 = FakeLocaleMethod18;
    private static readonly nint _fakeLocaleMethod10Ptr = Marshal.GetFunctionPointerForDelegate(_fakeLocaleMethod10);
    private static readonly nint _fakeLocaleMethod18Ptr = Marshal.GetFunctionPointerForDelegate(_fakeLocaleMethod18);
    private static nint _fakeLocaleObjectAddress;

    private static readonly FakeLocaleMethod _fakeCodecvtMethod10 = FakeCodecvtMethod10;
    private static readonly FakeLocaleMethod _fakeCodecvtMethod18 = FakeCodecvtMethod18;
    private static readonly FakeLocaleMethod _fakeCodecvtMethod20 = FakeCodecvtMethod20;
    private static readonly FakeLocaleMethod _fakeCodecvtMethod28 = FakeCodecvtMethod28;
    private static readonly nint _fakeCodecvtMethod10Ptr = Marshal.GetFunctionPointerForDelegate(_fakeCodecvtMethod10);
    private static readonly nint _fakeCodecvtMethod18Ptr = Marshal.GetFunctionPointerForDelegate(_fakeCodecvtMethod18);
    private static readonly nint _fakeCodecvtMethod20Ptr = Marshal.GetFunctionPointerForDelegate(_fakeCodecvtMethod20);
    private static readonly nint _fakeCodecvtMethod28Ptr = Marshal.GetFunctionPointerForDelegate(_fakeCodecvtMethod28);

    private static readonly object _gate = new();
    private static readonly nint _libstdcxxHandle = TryLoadLibrary("libstdc++.so.6");
    private static readonly nint _libcHandle = TryLoadLibrary(OperatingSystem.IsMacOS() ? "libSystem.dylib" : "libc.so.6");
    private static readonly nint _stackChkGuardAddress = Allocate(sizeof(ulong) * 2);
    private static readonly nint _progNameBufferAddress = Allocate(ProgNameMaxBytes + 1);
    private static readonly nint _progNamePointerAddress = Allocate(nint.Size);
    private static readonly nint _libcNeedFlagAddress = Allocate(sizeof(uint));
    private static readonly nint _libcInternalNeedFlagAddress = Allocate(sizeof(uint));
    private static readonly nint _stdoutAddress = CopyPointerSymbol(_libcHandle, "stdout", "_IO_2_1_stdout_");
    private static readonly nint _stderrAddress = CopyPointerSymbol(_libcHandle, "stderr", "_IO_2_1_stderr_");
    private static readonly nint _classicLocaleHostAddress = TryGetClassicLocaleHostAddress();
    private static readonly nint _sceLibcClassicLocaleAddress = CopyClassicLocaleProxy(_classicLocaleHostAddress);
    private static readonly nint _stdCoutAddress = CopyExportedObject(_libstdcxxHandle, "_ZSt4cout", StdStreamSize);
    private static readonly nint _stdCerrAddress = CopyExportedObject(_libstdcxxHandle, "_ZSt4cerr", StdStreamSize);
    private static readonly nint _stdClogAddress = CopyExportedObject(_libstdcxxHandle, "_ZSt4clog", StdStreamSize);
    private static readonly nint _classTypeInfoVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVN10__cxxabiv117__class_type_infoE", StdClassTypeInfoVTableSize);
    private static readonly nint _siClassTypeInfoVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVN10__cxxabiv120__si_class_type_infoE", StdClassTypeInfoVTableSize);
    private static readonly nint _vmiClassTypeInfoVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVN10__cxxabiv121__vmi_class_type_infoE", StdClassTypeInfoVTableSize);
    private static readonly nint _stdExceptionTypeInfoAddress = CopyExportedObject(_libstdcxxHandle, "_ZTISt9exception", StdTypeInfoSize);
    private static readonly nint _stdIosBaseTypeInfoAddress = CopyExportedObject(_libstdcxxHandle, "_ZTISt8ios_base", StdTypeInfoSize);
    private static readonly nint _stdRuntimeErrorVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVSt13runtime_error", StdVTableSize);
    private static readonly nint _stdRuntimeErrorTypeInfoAddress = CopyExportedObject(_libstdcxxHandle, "_ZTISt13runtime_error", StdTypeInfoWithBaseSize);
    private static readonly nint _stdBadCastVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVSt8bad_cast", StdVTableSize);
    private static readonly nint _stdBadCastTypeInfoAddress = CopyExportedObject(_libstdcxxHandle, "_ZTISt8bad_cast", StdTypeInfoWithBaseSize);
    private static readonly nint _stdLogicErrorVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVSt11logic_error", StdVTableSize);
    private static readonly nint _stdLogicErrorTypeInfoAddress = CopyExportedObject(_libstdcxxHandle, "_ZTISt11logic_error", StdTypeInfoWithBaseSize);
    private static readonly nint _stdIosBaseFailureVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVNSt8ios_base7failureE", StdVTableSize);
    private static readonly nint _stdIosBaseFailureTypeInfoAddress = CopyExportedObject(_libstdcxxHandle, "_ZTINSt8ios_base7failureE", StdTypeInfoWithBaseSize);
    private static readonly nint _stdSystemErrorVTableAddress = CopyExportedObject(_libstdcxxHandle, "_ZTVSt12system_error", StdVTableSize);
    private static readonly nint _stdCtypeIdAddress = CopyPointerSymbol(_libstdcxxHandle, "_ZNSt5ctypeIcE2idE");
    private static readonly nint _stdLocaleIdCountAddress = CreatePointerValueProxy(3);
    private static readonly nint _stdCodecvtVTableAddress = CopyCodecvtVTableProxy(_classicLocaleHostAddress);
    private static readonly nint _stdCodecvtIdAddress = CreateAssignedLocaleIdProxy();
    private static readonly nint _stdBadOffAddress = CreatePointerValueProxy(unchecked((ulong)-1));
    private static readonly nint _stdFpzAddress = CreatePointerValueProxy(0);

    static HleDataSymbols()
    {
        if (_stackChkGuardAddress != 0)
        {
            Marshal.WriteInt64(_stackChkGuardAddress, unchecked((long)StackChkGuardValue));
            Marshal.WriteInt64(
                IntPtr.Add(_stackChkGuardAddress, sizeof(ulong)),
                unchecked((long)StackChkGuardValue));
        }

        if (_libcNeedFlagAddress != 0)
        {
            Marshal.WriteInt32(_libcNeedFlagAddress, 1);
        }

        if (_libcInternalNeedFlagAddress != 0)
        {
            Marshal.WriteInt32(_libcInternalNeedFlagAddress, 1);
        }

        ConfigureProcessImageName("eboot.bin");
    }

    public static IEnumerable<string> EnumerateKnownNids()
    {
        yield return StackChkGuardNid;
        yield return ProgNameNid;
        yield return LibcNeedFlagNid;
        yield return LibcInternalNeedFlagNid;
        yield return StdoutNid;
        yield return StderrNid;
        yield return SceLibcClassicLocaleNid;
        yield return StdCoutNid;
        yield return ClassTypeInfoVTableNid;
        yield return SiClassTypeInfoVTableNid;
        yield return VmiClassTypeInfoVTableNid;
        yield return StdExceptionTypeInfoNid;
        yield return StdIosBaseTypeInfoNid;
        yield return StdRuntimeErrorVTableNid;
        yield return StdRuntimeErrorTypeInfoNid;
        yield return StdBadCastVTableNid;
        yield return StdBadCastTypeInfoNid;
        yield return StdLogicErrorVTableNid;
        yield return StdLogicErrorTypeInfoNid;
        yield return StdIosBaseFailureVTableNid;
        yield return StdIosBaseFailureTypeInfoNid;
        yield return StdSystemErrorVTableNid;
        yield return StdCtypeIdNid;
        yield return StdLocaleIdCountNid;
        yield return StdCodecvtVTableNid;
        yield return StdCodecvtIdNid;
        yield return StdBadOffNid;
        yield return StdFpzNid;
    }

    public static void ConfigureProcessImageName(string? processImageName)
    {
        var effectiveName = string.IsNullOrWhiteSpace(processImageName)
            ? "eboot.bin"
            : processImageName;
        var encodedName = Encoding.UTF8.GetBytes(effectiveName);
        var byteCount = Math.Min(encodedName.Length, ProgNameMaxBytes);

        lock (_gate)
        {
            if (_progNameBufferAddress == 0 || _progNamePointerAddress == 0)
            {
                return;
            }

            for (var i = 0; i <= ProgNameMaxBytes; i++)
            {
                Marshal.WriteByte(_progNameBufferAddress, i, 0);
            }

            Marshal.Copy(encodedName, 0, _progNameBufferAddress, byteCount);
            WritePointer(_progNamePointerAddress, _progNameBufferAddress);
        }
    }

    public static bool TryGetAddress(string nid, out ulong address)
    {
        var pointer = nid switch
        {
            StackChkGuardNid => _stackChkGuardAddress,
            ProgNameNid => _progNamePointerAddress,
            LibcNeedFlagNid => _libcNeedFlagAddress,
            LibcInternalNeedFlagNid => _libcInternalNeedFlagAddress,
            StdoutNid => _stdoutAddress,
            StderrNid => _stderrAddress,
            SceLibcClassicLocaleNid => _sceLibcClassicLocaleAddress,
            StdCoutNid => _stdCoutAddress,
            ClassTypeInfoVTableNid => _classTypeInfoVTableAddress,
            SiClassTypeInfoVTableNid => _siClassTypeInfoVTableAddress,
            VmiClassTypeInfoVTableNid => _vmiClassTypeInfoVTableAddress,
            StdExceptionTypeInfoNid => _stdExceptionTypeInfoAddress,
            StdIosBaseTypeInfoNid => _stdIosBaseTypeInfoAddress,
            StdRuntimeErrorVTableNid => _stdRuntimeErrorVTableAddress,
            StdRuntimeErrorTypeInfoNid => _stdRuntimeErrorTypeInfoAddress,
            StdBadCastVTableNid => _stdBadCastVTableAddress,
            StdBadCastTypeInfoNid => _stdBadCastTypeInfoAddress,
            StdLogicErrorVTableNid => _stdLogicErrorVTableAddress,
            StdLogicErrorTypeInfoNid => _stdLogicErrorTypeInfoAddress,
            StdIosBaseFailureVTableNid => _stdIosBaseFailureVTableAddress,
            StdIosBaseFailureTypeInfoNid => _stdIosBaseFailureTypeInfoAddress,
            StdSystemErrorVTableNid => _stdSystemErrorVTableAddress,
            StdCtypeIdNid => _stdCtypeIdAddress,
            StdLocaleIdCountNid => _stdLocaleIdCountAddress,
            StdCodecvtVTableNid => _stdCodecvtVTableAddress,
            StdCodecvtIdNid => _stdCodecvtIdAddress,
            StdBadOffNid => _stdBadOffAddress,
            StdFpzNid => _stdFpzAddress,
            _ => 0,
        };

        if (pointer == 0)
        {
            address = 0;
            return false;
        }

        address = unchecked((ulong)pointer);
        return true;
    }

    private static nint Allocate(int size)
    {
        try
        {
            var memory = Marshal.AllocHGlobal(size);
            for (var i = 0; i < size; i++)
            {
                Marshal.WriteByte(memory, i, 0);
            }

            return memory;
        }
        catch
        {
            return 0;
        }
    }

    private static nint TryLoadLibrary(string libraryName)
    {
        try
        {
            return NativeLibrary.Load(libraryName);
        }
        catch
        {
            return 0;
        }
    }

    private static nint CopyExportedObject(nint libraryHandle, string exportName, int size)
    {
        if (libraryHandle == 0 || size <= 0)
        {
            return 0;
        }

        try
        {
            var source = NativeLibrary.GetExport(libraryHandle, exportName);
            if (source == 0)
            {
                return 0;
            }

            var destination = Allocate(size);
            if (destination == 0)
            {
                return 0;
            }

            var bytes = new byte[size];
            Marshal.Copy(source, bytes, 0, size);
            Marshal.Copy(bytes, 0, destination, size);
            return destination;
        }
        catch
        {
            return 0;
        }
    }

    private static nint CopyPointerSymbol(nint libraryHandle, params string[] exportNames)
    {
        if (libraryHandle == 0 || exportNames.Length == 0)
        {
            return 0;
        }

        foreach (var exportName in exportNames)
        {
            try
            {
                var source = NativeLibrary.GetExport(libraryHandle, exportName);
                if (source == 0)
                {
                    continue;
                }

                var destination = Allocate(nint.Size);
                if (destination == 0)
                {
                    return 0;
                }

                WritePointer(destination, ReadPointer(source));
                return destination;
            }
            catch
            {
                // Try the next export name.
            }
        }

        return 0;
    }

    private static nint CopyClassicLocaleProxy(nint source)
    {
        _ = source;

        try
        {
            var facetArray = Allocate(nint.Size * 4);
            if (facetArray == 0)
            {
                return 0;
            }

            var vtable = Allocate(StdVTableSize);
            if (vtable == 0)
            {
                return 0;
            }

            WritePointer(IntPtr.Add(vtable, 0x10), _fakeLocaleMethod10Ptr);
            WritePointer(IntPtr.Add(vtable, 0x18), _fakeLocaleMethod18Ptr);

            var localeObject = Allocate(0x110);
            if (localeObject == 0)
            {
                return 0;
            }

            WritePointer(localeObject, vtable);
            WritePointer(IntPtr.Add(localeObject, 0x10), facetArray);
            WritePointer(IntPtr.Add(localeObject, 0x18), unchecked((nint)4));
            WritePointer(IntPtr.Add(localeObject, 0x68), localeObject);

            _fakeLocaleObjectAddress = localeObject;

            var destination = Allocate(nint.Size);
            if (destination == 0)
            {
                return 0;
            }

            WritePointer(destination, localeObject);
            return destination;
        }
        catch
        {
            return 0;
        }
    }

    private static nint FakeLocaleMethod10(nint thisPtr, nint arg1, nint arg2, nint arg3)
    {
        _ = thisPtr;
        _ = arg1;
        _ = arg2;
        _ = arg3;
        return _fakeLocaleObjectAddress;
    }

    private static nint FakeLocaleMethod18(nint thisPtr, nint arg1, nint arg2, nint arg3)
    {
        _ = thisPtr;
        _ = arg1;
        _ = arg2;
        _ = arg3;
        return 0;
    }

    private static nint FakeCodecvtMethod10(nint thisPtr, nint arg1, nint arg2, nint arg3)
    {
        _ = thisPtr;
        _ = arg1;
        _ = arg2;
        _ = arg3;
        return 0;
    }

    private static nint FakeCodecvtMethod18(nint thisPtr, nint arg1, nint arg2, nint arg3)
    {
        _ = thisPtr;
        _ = arg1;
        _ = arg2;
        _ = arg3;
        return 0;
    }

    private static nint FakeCodecvtMethod20(nint thisPtr, nint arg1, nint arg2, nint arg3)
    {
        _ = thisPtr;
        _ = arg1;
        _ = arg2;
        _ = arg3;
        return 1;
    }

    private static nint FakeCodecvtMethod28(nint thisPtr, nint arg1, nint arg2, nint arg3)
    {
        _ = thisPtr;
        _ = arg1;
        _ = arg2;
        _ = arg3;
        return 0;
    }

    private static nint CopyCodecvtVTableProxy(nint classicLocale)
    {
        _ = classicLocale;

        try
        {
            var destination = Allocate(0x40);
            if (destination == 0)
            {
                return 0;
            }

            WritePointer(IntPtr.Add(destination, 0x10), _fakeCodecvtMethod10Ptr);
            WritePointer(IntPtr.Add(destination, 0x18), _fakeCodecvtMethod18Ptr);
            WritePointer(IntPtr.Add(destination, 0x20), _fakeCodecvtMethod10Ptr);
            WritePointer(IntPtr.Add(destination, 0x28), _fakeCodecvtMethod18Ptr);
            WritePointer(IntPtr.Add(destination, 0x30), _fakeCodecvtMethod20Ptr);
            WritePointer(IntPtr.Add(destination, 0x38), _fakeCodecvtMethod28Ptr);
            return destination;
        }
        catch
        {
            return 0;
        }
    }

    private static nint CreatePointerValueProxy(ulong value)
    {
        var destination = Allocate(nint.Size);
        if (destination == 0)
        {
            return 0;
        }

        WritePointer(destination, unchecked((nint)value));
        return destination;
    }

    private static nint CreateAssignedLocaleIdProxy()
    {
        try
        {
            var destination = Allocate(nint.Size);
            if (destination == 0)
            {
                return 0;
            }

            _ = StdLocaleIdMId(destination);
            return destination;
        }
        catch
        {
            return 0;
        }
    }

    private static nint TryGetClassicLocaleHostAddress()
    {
        try
        {
            return StdLocaleClassic();
        }
        catch
        {
            return 0;
        }
    }

    private static nint ReadPointer(nint source)
    {
        return nint.Size == sizeof(int)
            ? new nint(Marshal.ReadInt32(source))
            : new nint(Marshal.ReadInt64(source));
    }

    private static void WritePointer(nint target, nint value)
    {
        if (nint.Size == sizeof(int))
        {
            Marshal.WriteInt32(target, value.ToInt32());
            return;
        }

        Marshal.WriteInt64(target, value.ToInt64());
    }

    [DllImport("libstdc++.so.6", EntryPoint = "_ZNKSt6locale2id5_M_idEv")]
    private static extern nuint StdLocaleIdMId(nint id);

    [DllImport("libstdc++.so.6", EntryPoint = "_ZSt9use_facetISt7codecvtIcc11__mbstate_tEERKT_RKSt6locale")]
    private static extern nint StdUseFacetCodecvt(nint locale);

    [DllImport("libstdc++.so.6", EntryPoint = "_ZNSt6locale7classicEv")]
    private static extern nint StdLocaleClassic();
}
