using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using static NativeThreadExtensions.NativeThreadExtensions;
using static NativeThreadExtensions.Utility;

namespace NativeThreadExtensions
{

    public static class Utility
    {

        public static bool CheckCompatibilityatRuntime = true;
        public enum DotNetPlatform : byte
        {

            NET_Framework_2_X = 1,
            NET_Framework_4_0,
            NET_Framework_4_5,
            NET_Framework_4_5_1,
            NET_Framework_4_5_2,
            NET_Framework_4_6,
            NET_Framework_4_6_1,
            NET_Framework_4_6_2,
            NET_Framework_4_7,
            NET_Framework_4_7_1,
            NET_Framework_4_7_2,
            NET_Framework_4_8,
            NET_Framework_4_8_1,
            NET_5,
            NET_6,
            NET_7,
            NET_8,
            NET_9,
            NET_10,
            NET_11,
            CLR_2_X = 32,
            CLR_4_X = 64,
            CLR = CLR_2_X | CLR_4_X
        }
        public static DotNetPlatform GetRuntimeVersion()
        {
            // a method attempting to determine the version of the runtime running in the executing assembly
            // relevant to the neat little runtime safeguard we implemented in the class's constructor
            Assembly CurrentAssem = Assembly.GetEntryAssembly();
            if (CurrentAssem == null)
                return 0;

            CustomAttributeData attribute = null;
            string attributevalue;
            Version Ver1;

            var array = CurrentAssem.GetCustomAttributes(false);
            for (int I = array.Length; I-- != 0;)
            {
                object Attribute = array[I];

                Type AttributeType;
                if ((AttributeType = Attribute.GetType()).FullName != "System.Runtime.Versioning.TargetFrameworkAttribute")
                    continue;
                // the first field of the TargetFrameworkAttribute class
                attributevalue = (string)AttributeType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)[0].GetValue(Attribute);
#if NET40_OR_GREATER
Ver1 = Version.Parse(attributevalue.Substring(attributevalue.IndexOf('=', attributevalue.IndexOf(",") + 1) + 1).Replace("v", ""));
       
#else

                string[] VersionString = attributevalue.Substring(attributevalue.IndexOf('=', attributevalue.IndexOf(",") + 1) + 1).Replace("v", "").Split('.');
                if (VersionString.Length > 2)
                {
                    Ver1 = new Version(int.Parse(VersionString[0]), int.Parse(VersionString[1]), int.Parse(VersionString[2]));
                }
                else if (VersionString.Length == 2)
                {
                    Ver1 = new Version(int.Parse(VersionString[0]), int.Parse(VersionString[1]));
                }
                else
                {
                    Ver1 = new Version(int.Parse(VersionString[0]), 0);
                }

#endif
                if (Ver1.Major > 4)
                {
                    // .NET
                    return (DotNetPlatform)(Math.Min(((byte)(DotNetPlatform.NET_5 - 5) + (Ver1.Major)), (byte)DotNetPlatform.NET_11));
                }
                else
                {

                    // .NET Framework
                    return Ver1.Minor == 0 ? DotNetPlatform.NET_Framework_4_0 | DotNetPlatform.CLR_4_X : (DotNetPlatform)((int)DotNetPlatform.NET_Framework_4_5 + ((Ver1.Minor - 5) * 3) + Ver1.Build) | DotNetPlatform.CLR_4_X;
                }
            }
            if (string.IsNullOrEmpty(CurrentAssem.ImageRuntimeVersion))
                return 0;
#if NET40_OR_GREATER
 Version Ver = Version.Parse(CurrentAssem.ImageRuntimeVersion.TrimStart('v'));
       
#else
            string[] ImageRuntimeVersionString = CurrentAssem.ImageRuntimeVersion.TrimStart('v').Split('.');
            Version Ver;
            if (ImageRuntimeVersionString.Length > 2)
            {
                Ver = new Version(int.Parse(ImageRuntimeVersionString[0]), int.Parse(ImageRuntimeVersionString[1]), int.Parse(ImageRuntimeVersionString[2]));
            }
            else if (ImageRuntimeVersionString.Length == 2)
            {
                Ver = new Version(int.Parse(ImageRuntimeVersionString[0]), int.Parse(ImageRuntimeVersionString[1]));
            }
            else
            {
                Ver = new Version(int.Parse(ImageRuntimeVersionString[0]), 0);
            }
#endif
            if (Ver.Major != 4)
                return DotNetPlatform.NET_Framework_2_X | DotNetPlatform.CLR_2_X;

            return DotNetPlatform.CLR_4_X;

        }
        public static string GetRuntimeVersionString(DotNetPlatform dotNetPlatform, bool IncludeRuntimeVersion = false)
        {
            // constructing a string representation of the returned DotNetPlatform value.

            // removing the extra DotNetPlatform.CLR flags by shifting the enum.
            DotNetPlatform ExtraFlagsRemoved = (DotNetPlatform)((uint)dotNetPlatform << 27 >> 27);


            string Str = ExtraFlagsRemoved.ToString();
            bool OlderthanNet5 = ExtraFlagsRemoved < DotNetPlatform.NET_5;
            IncludeRuntimeVersion &= OlderthanNet5;
            Str = (OlderthanNet5 ? ".NET Framework v" + Str.Substring(Str.IndexOf('_', 4)).Replace("_", ".") : "." + Str.Replace("_", " "));
            if (IncludeRuntimeVersion)
            {
                Str += ", " + ((dotNetPlatform & DotNetPlatform.CLR_4_X) != 0 ? "CLR 4.x" : "CLR 2.x");
            }
            return Str;
        }
        [Obsolete("This method is preserved for advanced scenarios. Do not use without intention to change or read internal material with adequate CLR internal knowledge.")]
        public static unsafe void SetNativeThreadState(Thread instance, NETInternalThreadState InternalThreadState)
        {
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                throw new PlatformNotSupportedException("SetNativeThreadState() isn't supported on this platform yet ):");
            }

#endif

#if NET5_0
          
         *(NETInternalThreadState*)(((byte*)*(IntPtr*)(*(byte**)&instance + 6 * sizeof(void*))) + sizeof(void*)) =InternalThreadState ;
#elif NET9_0_OR_GREATER
            *(NETInternalThreadState*)(((byte*)*(IntPtr*)(*(byte**)&instance + 5 * sizeof(void*)))) = InternalThreadState;

#elif NET6_0_OR_GREATER
   *(NETInternalThreadState*)(((byte*)*(IntPtr*)(*(byte**)&instance + 5 * sizeof(void*))) + sizeof(void*)) = InternalThreadState;
#endif
#if NET40_OR_GREATER

            *(NETInternalThreadState*)(((byte*)*(IntPtr*)(*(byte**)&instance + 8 * sizeof(void*))) + sizeof(void*)) = InternalThreadState;
#elif NET20_OR_GREATER
            *(NETInternalThreadState*)(((byte*)*(IntPtr*)(*(byte**)&instance + 10 * sizeof(void*))) + sizeof(void*)) = InternalThreadState;


#endif


        }
        [Obsolete("This method is preserved for advanced scenarios. Do not use without intention to change or read internal material with adequate CLR internal knowledge.")]
        public static NETInternalThreadState GetNativeThreadState(Thread thread)
        {
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                throw new PlatformNotSupportedException("GetNativeThreadState() isn't supported on this platform yet ):");
            }

#endif
            return thread.GetNativeThreadState();
        }
    }
    public static partial class NativeThreadExtensions
    {
        static NativeThreadExtensions()
        {

            // a little neat optional safeguard to check Compatibility

            if (!Utility.CheckCompatibilityatRuntime)
                return;
            // On NativeAOT, we do not need to check runtime compatibility since the .NET runtime version is irrelevant to the internal code regarding NativeAOT
            // we can just skip past that
#if NET8_0_OR_GREATER

            if (isAOT)
                return;


#endif

            Utility.DotNetPlatform dotNetPlatform = Utility.GetRuntimeVersion();

            if (dotNetPlatform == 0)
                return;

#if NET40_OR_GREATER

          if((dotNetPlatform &  Utility.DotNetPlatform.CLR_4_X) != Utility.DotNetPlatform.CLR_4_X)
            {
            throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#elif NET20_OR_GREATER
            if ((dotNetPlatform & Utility.DotNetPlatform.CLR_2_X) != Utility.DotNetPlatform.CLR_2_X)
            {
                throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#endif

#if NET11_0
          
         if(dotNetPlatform != Utility.DotNetPlatform.NET_11)
            {
            throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }
#endif
#if NET10_0

            if (dotNetPlatform != Utility.DotNetPlatform.NET_10)
            {
                throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#endif
#if NET9_0
          
          if (dotNetPlatform != Utility.DotNetPlatform.NET_9)
            {
                throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#endif
#if NET8_0
          
          if (dotNetPlatform != Utility.DotNetPlatform.NET_8)
            {
                throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#endif
#if NET7_0
          
          if (dotNetPlatform != Utility.DotNetPlatform.NET_7)
            {
                throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#endif
#if NET6_0
          
          if (dotNetPlatform != Utility.DotNetPlatform.NET_6)
            {
                throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#endif
#if NET5_0
          
          if (dotNetPlatform != Utility.DotNetPlatform.NET_5)
            {
                throw new PlatformNotSupportedException($"This DLL was not compiled to be used on this platform {Utility.GetRuntimeVersionString(dotNetPlatform)}.\r\nIf you happen to have imported the library manually, you probably accidentally added the wrong reference for your runtime version; however, don't panic!\r\nDownload the right version suitable for your exact platform on NuGet (https://www.nuget.org/packages/NET-Thread.Terminate/) or Github (https://github.com/Emine3/NET-Thread.Terminate/).");
            }

#endif

        }
        internal unsafe delegate void InternalThreadAbort(void* Thread);

        public enum NETInternalThreadState : uint
        {


            TS_AbortRequested = 0x00000001,    // Abort the thread

            TS_GCSuspendPending = 0x00000002,    // ThreadSuspend::SuspendRuntime watches this thread to leave coop mode.
            TS_GCSuspendRedirected = 0x00000004,    // ThreadSuspend::SuspendRuntime has redirected the thread to suspention routine.
            TS_GCSuspendFlags = TS_GCSuspendPending | TS_GCSuspendRedirected, // used to track suspension progress. Only SuspendRuntime writes/resets these.

            TS_DebugSuspendPending = 0x00000008,    // Is the debugger suspending threads?
            TS_GCOnTransitions = 0x00000010,    // Force a GC on stub transitions (GCStress only)

            TS_LegalToJoin = 0x00000020,    // Is it now legal to attempt a Join()

            TS_ExecutingOnAltStack = 0x00000040,    // Runtime is executing on an alternate stack located anywhere in the memory


            // unused                 = 0x00000100,
            TS_Background = 0x00000200,    // Thread is a background thread
            TS_Unstarted = 0x00000400,    // Thread has never been started
            TS_Dead = 0x00000800,    // Thread is dead

            TS_WeOwn = 0x00001000,    // Exposed object initiated this thread

            // Some bits that only have meaning for reporting the state to clients.
            TS_ReportDead = 0x00010000,    // in WaitForOtherThreads()
            TS_FullyInitialized = 0x00020000,    // Thread is fully initialized and we are ready to broadcast its existence to external clients

            TS_TaskReset = 0x00040000,    // The task is reset

            TS_SyncSuspended = 0x00080000,    // Suspended via WaitSuspendEvent
            TS_DebugWillSync = 0x00100000,    // Debugger will wait for this thread to sync

            TS_StackCrawlNeeded = 0x00200000,    // A stackcrawl is needed on this thread, such as for thread abort
                                                 // See comment for s_pWaitForStackCrawlEvent for reason.


            TS_TPWorkerThread = 0x01000000,    // is this a threadpool worker thread?

            TS_Interruptible = 0x02000000,    // sitting in a Sleep(), Wait(), Join()
            TS_Interrupted = 0x04000000,    // was awakened by an interrupt APC. !!! This can be moved to TSNC

            TS_CompletionPortThread = 0x08000000,    // Completion port thread

            TS_AbortInitiated = 0x10000000,    // set when abort is begun

            TS_Finalized = 0x20000000,    // The associated managed Thread object has been finalized.
                                          // We can clean up the unmanaged part now.

            TS_FailStarted = 0x40000000,    // The thread fails during startup.
            TS_Detached = 0x80000000,    // Thread was detached by DllMain

            // <TODO> @TODO: We need to reclaim the bits that have no concurrency issues (i.e. they are only
            //         manipulated by the owning thread) and move them off to a different DWORD.  Note if this
            //         enum is changed, we also need to update SOS to reflect this.</TODO>

            // We require (and assert) that the following bits are less than 0x100.
            TS_CatchAtSafePoint = (TS_AbortRequested | TS_GCSuspendPending |
                                   TS_DebugSuspendPending | TS_GCOnTransitions),
        };
        // NativeAOT is offered starting with .NET 8; here checking System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled
        // to determine whether the application is compiled on AOT or not by checking dynamic method features available
#if NET8_0_OR_GREATER
        public static readonly bool isAOT = !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#endif

        public static readonly bool SixtyFourBitPointerSize = IntPtr.Size == 8;
        internal static InternalThreadAbort ThreadAbortNativeFunction;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static unsafe extern bool TerminateThread(
IntPtr hThread,
int dwExitCode
);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static unsafe extern bool SuspendThread(
IntPtr hThread
);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static unsafe extern bool ResumeThread(
IntPtr hThread
);
        // Code concerning .NET internal implementation; should not be tampered with.
        internal unsafe static byte* GetInternalCPPThreadObject(Thread instance)
        {
            // internal field offsets of "DONT_USE_InternalThread" in the .NET object containing the C++ object on each .NET runtime versions and architecture
            // these offsets are calculated and relative to the complex way .NET handles the field layout of an object; should not be practiced
            // .net 5; C++ instance located at *the address of the instance + the object header + 5 of pointer or reference sized units (5 * sizeof(IntPtr) + sizeof(IntPtr)); no padding is applied
            // .net 6+; starting with .NET 6, C++ instance located at *the address of the instance + the object header + 4 of pointer or reference sized units (4 * sizeof(IntPtr) + sizeof(IntPtr)); no padding is applied
            // On .NET framework, that fallows accordingly:
            // CLR 4; C++ instance located at *the address of the instance + the object header + 7 of pointer or reference sized units (7 * sizeof(IntPtr) + sizeof(IntPtr)); no padding is applied
            // CLR 2; C++ instance located at *the address of the instance + the object header + 9 of pointer or reference sized units (9 * sizeof(IntPtr) + sizeof(IntPtr)); no padding is applied
#if NET5_0
          
         return *(byte**)(*(byte**)&instance + 6 * sizeof(void*));
#elif NET6_0_OR_GREATER
     return   *(byte**)(*(byte**)&instance + 5 * sizeof(void*));
#endif
#if NET40_OR_GREATER

            return *(byte**)(*(byte**)&instance + 8 * sizeof(void*));
#elif NET20_OR_GREATER
            return *(byte**)(*(byte**)&instance + 10 * sizeof(void*));

#endif

        }
        [Obsolete("The DotNetAbort and ResetAbort 'may corrupt the process and should not be used in production code.\r\nOn .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.", false)]

        public static void ResetAbort(Thread instance = null)
        {
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                // note to warn that the gradual or "safe" dotnet style of suspenion is not supported on AOT.
                throw new PlatformNotSupportedException($"ResetAbort isn't supported on AOT ):");

            }
#endif
            if (instance == null)
                instance = Thread.CurrentThread;
            instance.ResetAbort();

        }
    }
}
