// Code concerning modification of .NET internal implementations; do not alter any part of the file.
// This file is meant to be only read and maintained by the owners of the library or contributors.
// any alteration and copying of this source file is not permitted and should not move out of this place.
using NativeThreadExtensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using static NativeThreadExtensions.NativeThreadExtensions;

// attempting to add support for extension methods on older .NET Framework
#if  !NET40_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExtensionAttribute : Attribute
    {
    }
}
#endif
namespace System.Threading
{
    public static class NativeThreadExtensions
    {
        public unsafe static IntPtr GetNativeHandle(this Thread instance)
        {
            // internal field offsets of m_ThreadhandleforClose in the associated C++ object created on each .NET runtime versions and architecture
            // .net 5, 6, 64 bit: 416, 32 bit: 268;
            // .net 7, 8, 64 bit: 408, 32 bit: 264;
            // .net 9, 10, 64 bit: 272, 32 bit: 176;
            // .net 11, 64 bit: 192, 32 bit: 132;

            // On .NET Framework, the address to the native handle in the C++ object mentioned only differs by the CLR version: CLR 2.x and CLR 4.x.
            // CLR 4, 64 bit: 528, 32 bit: 336
            // CLR 2, 64 bit: 504, 32 bit: 328;

            // the right for these offsets to change is reserved by the .NET team.

            // things are way less complicated on NativeAOT :)
            // the native handle of the thread is essentially located at the field layout of the .NET object
            // if compiled on AOT, the thread handle is stored in the .NET object itself: _osHandle Field

#if NET8_0_OR_GREATER
            if (isAOT)
            {

                return ((Microsoft.Win32.SafeHandles.SafeWaitHandle*)(*(byte**)&instance + sizeof(void*) * 8) /* Dereferncing _osHandle Field */)->DangerousGetHandle();

            }

#endif

#if NET11_0
          
              return *(IntPtr*)(GetInternalCPPThreadObject(instance) + (SixtyFourBitPointerSize ? 192 : 132));
            
#endif
#if NET10_0 || NET9_0

            return *(IntPtr*)(GetInternalCPPThreadObject(instance) + (SixtyFourBitPointerSize ? 272 : 176));

#endif
#if NET8_0 || NET7_0
            
                    return *(IntPtr*)(GetInternalCPPThreadObject(instance) + (SixtyFourBitPointerSize ? 408 : 264));
             
#endif
#if NET6_0 || NET5_0
             
                  return *(IntPtr*)(GetInternalCPPThreadObject(instance) + (SixtyFourBitPointerSize ? 416 : 268));
               
#endif

#if NET40_OR_GREATER

            return *(IntPtr*)(GetInternalCPPThreadObject(instance) + (SixtyFourBitPointerSize ? 528 : 336));

#elif NET20_OR_GREATER
            return *(IntPtr*)(GetInternalCPPThreadObject(instance) + (SixtyFourBitPointerSize ? 504 : 328));
#else
            throw new PlatformNotSupportedException("GetNativeHandle() isn't supported on this platform yet ):");
#endif


        }
        internal unsafe static NETInternalThreadState GetNativeThreadState(this Thread instance)
        {
            // On NativeAOT, there is no native thread state field, hence as a result, we construct a "fake" value to satisfy the need of ResetAbort and ResumeNative since they rely on this method to check the thread state
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                NETInternalThreadState ConstructedNativeThreadState = NETInternalThreadState.TS_WeOwn | NETInternalThreadState.TS_FullyInitialized | NETInternalThreadState.TS_LegalToJoin | ((instance.ThreadState & ThreadState.SuspendRequested) == ThreadState.SuspendRequested ? NETInternalThreadState.TS_GCSuspendRedirected | NETInternalThreadState.TS_GCSuspendPending : (NETInternalThreadState)0);
                if ((instance.ThreadState & ThreadState.Unstarted) == ThreadState.Unstarted)
                {


                    ConstructedNativeThreadState |= NETInternalThreadState.TS_Unstarted;


                }
                if ((instance.ThreadState & ThreadState.Background) == ThreadState.Background)
                {


                    ConstructedNativeThreadState |= NETInternalThreadState.TS_Background;


                }
                if ((instance.ThreadState & ThreadState.Suspended) == ThreadState.Suspended)
                {
                    ConstructedNativeThreadState &= ~NETInternalThreadState.TS_GCSuspendPending | NETInternalThreadState.TS_DebugSuspendPending;
                    ConstructedNativeThreadState |= NETInternalThreadState.TS_SyncSuspended;

                }
                if ((instance.ThreadState & ThreadState.AbortRequested) == ThreadState.AbortRequested)
                {

                    ConstructedNativeThreadState |= NETInternalThreadState.TS_AbortRequested;

                }
                if ((instance.ThreadState & ThreadState.Aborted) == ThreadState.Aborted)
                {

                    ConstructedNativeThreadState &= ~NETInternalThreadState.TS_AbortRequested;
                    ConstructedNativeThreadState |= NETInternalThreadState.TS_AbortInitiated;


                }

                if ((instance.ThreadState & ThreadState.WaitSleepJoin) == ThreadState.WaitSleepJoin)
                {


                    ConstructedNativeThreadState |= NETInternalThreadState.TS_Interruptible;


                }
                if ((instance.ThreadState & ThreadState.Stopped) == ThreadState.Stopped)
                {
                    ConstructedNativeThreadState &= ~(NETInternalThreadState.TS_Interruptible);

                    ConstructedNativeThreadState |= NETInternalThreadState.TS_Dead | NETInternalThreadState.TS_ReportDead;


                }

                return ConstructedNativeThreadState;
            }

#endif
            // internal field offsets of m_State in the associated C++ object on each .NET runtime versions and architecture
            // On .net 5+ but before .NET 9 so particularly .NET 5, .NET 6, .NET 7, and .NET 8,, it is one field after a reference sized field so essentially, it is located at GetInternalCPPThreadObject(instance) + sizeof(IntPtr);
            // the same also holds true on .NET Framework.
            // Since .NET 9, it is the very first field of the C++ object

#if NET9_0_OR_GREATER                  
            return *(NETInternalThreadState*)GetInternalCPPThreadObject(instance);

#elif NET5_0_OR_GREATER
          
        return      *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));

#endif
            // All CLR versions and all .NET Framework versions
            return *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));





        }

        [Obsolete("This method is preserved for advanced scenarios. Do not use without intention to change or read internal material with adequate CLR internal knowledge.")]
        public unsafe static void SetThreadState(this Thread instance, ThreadState state)
        {
            // interpreting the managed ThreadState value and assigning it to the native the way it is relevant to our context.
            // NativeAOT hasn't implemented any internal thread state; there is only a ThreadState field _threadState representing the managed thread state.
#if NET8_0_OR_GREATER
            if (isAOT)
            {
             /* assigning the thread state to the address of _threadState Field */
             *(ThreadState*)((byte*)*(IntPtr*)(*(byte**)&instance + 9 * sizeof(void*))) = state;
                return;
            }

#endif



            // obtaining the current native thread state to alter
            NETInternalThreadState NativeThreadState = instance.GetNativeThreadState();

            // manipulating the native thread state and interpreting accordingly; extra explanation will provided in the main upcomming project
            if ((state & ThreadState.Running) == ThreadState.Running)
            {
                NativeThreadState &= ~(NETInternalThreadState.TS_Interrupted | NETInternalThreadState.TS_GCSuspendRedirected | NETInternalThreadState.TS_DebugSuspendPending | NETInternalThreadState.TS_GCSuspendPending | NETInternalThreadState.TS_SyncSuspended | NETInternalThreadState.TS_AbortRequested | NETInternalThreadState.TS_AbortInitiated);

            }
            if ((state & ThreadState.Suspended) == ThreadState.Suspended)
            {

                NativeThreadState |= NETInternalThreadState.TS_SyncSuspended;

            }
            if ((state & ThreadState.Stopped) == ThreadState.Stopped)
            {
                NativeThreadState &= ~NETInternalThreadState.TS_Interruptible;
                NativeThreadState |= NETInternalThreadState.TS_Dead | NETInternalThreadState.TS_ReportDead;
            }
            if ((state & ThreadState.AbortRequested) == ThreadState.AbortRequested)
                NativeThreadState |= NETInternalThreadState.TS_AbortRequested;

            // internal field offsets of m_State in the associated C++ object on each .NET runtime versions and architecture
            // On .net 5+ but before .NET 9 so particularly .NET 5, .NET 6, .NET 7, and .NET 8,, it is one field after a reference sized field so essentially, it is located at GetInternalCPPThreadObject(instance) + sizeof(IntPtr);
            // the same also holds true on .NET Framework.
            // Since .NET 9, it is the very first field of the C++ object
#if NET5_0
          
           *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*)) = NativeThreadState;
#elif NET9_0_OR_GREATER
            *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance)) = NativeThreadState;

#elif NET6_0_OR_GREATER
   *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*)) = NativeThreadState;
#endif
#if NET40_OR_GREATER

            *(NETInternalThreadState*)((byte*)GetInternalCPPThreadObject(instance) + sizeof(void*)) = NativeThreadState;
#elif NET20_OR_GREATER
            *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*)) = NativeThreadState;


#endif

        }

        [Obsolete("Calling this method might cause corruption in the runtime and lead to resource leakage, unreleased locks, critical points of the code unreached, and potentially, affecting the rest of the application's behavior. Do not use this method in production code.")]
        /// <summary>Terminates the thread immediately on the low level by calling the Windows API TerminateThread. Do not adapt using this method in an ideal production scenario; nonetheless, DotNetAbortInternal is prefferred over this method.</summary>
        public static void Terminate(this Thread instance, int ExitCode = 0)
        {
            if (!instance.IsAlive)
                return;

            TerminateThread(instance.GetNativeHandle(), ExitCode);
            instance.SetThreadState(ThreadState.Stopped);
        }

        [Obsolete("The DotNetAbort method 'may corrupt the process and should not be used in production code.\r\nOn .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.", false)]
        /// <summary>[Experimental] Aborts the thread the .NET style by throwing an exception on the thread.</summary>
        public static void DotNetAbort(this Thread instance)
        {
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                // note to warn that the gradual or "safe" dotnet style of suspenion is not supported on AOT.
                throw new PlatformNotSupportedException($"DotNetAbort isn't supported on AOT ):");

            }
#endif

#if NET7_0_OR_GREATER
          instance.DotNetAbortInternal();
          return;
#endif
            // https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.abort?view=net-10.0#remarks
            if ((instance.ThreadState & (ThreadState.SuspendRequested | ThreadState.Suspended)) >= ThreadState.SuspendRequested)
            {
                throw new ThreadStateException($"Suspension for the thread has been requested; in that case, aborting the thread is not possible until the thread is out of the suspended state (resumed) ):");
            }

            if ((instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) >= ThreadState.AbortRequested || !instance.IsAlive)
                return;
            // request the runtime to gracefully perform .NET abortion
            instance.SetThreadState(ThreadState.AbortRequested);

        }
#if NET45_OR_GREATER
         [Obsolete("The DotNetAbort method 'may corrupt the process and should not be used in production code.\r\nOn .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.\r\n This method does not fallback to DotNetAbortInternal.", false)]
        /// <summary>[Experimental] Aborts the thread the .NET style by throwing an exception on the thread. This is aysynchronycs version of DotNetAbort which basically awaits the proper internal state change that indicates that the thread is aborted.</summary>
        public static async void DotNetAbortAsync(this Thread instance)
        {
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                // note to warn that the gradual or "safe" dotnet style of suspenion is not supported on AOT.
                throw new PlatformNotSupportedException($"DotNetAbortAsync isn't supported on AOT ):");

            }
#endif
        // https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.abort?view=net-10.0#remarks
            if ((instance.ThreadState & (ThreadState.SuspendRequested | ThreadState.Suspended)) >= ThreadState.SuspendRequested)
            {
                throw new ThreadStateException($"Suspension for the thread has been requested; in that case, aborting the thread is not possible until the thread is out of the suspended state (resumed) ):");
            }

            if ((instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) >= ThreadState.AbortRequested || !instance.IsAlive)
                return;
                  // request the runtime to gracefully perform .NET abortion
            instance.SetThreadState(ThreadState.AbortRequested);
            for (; (instance.ThreadState & ThreadState.Aborted) != ThreadState.Aborted;) ;
        }
#endif
        [Obsolete("The DotNetAbortInternal method 'may corrupt the process and should not be used in production code.\r\nOn .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.", false)]
        public unsafe static void DotNetAbortInternal(this Thread instance)
        {

#if NET8_0_OR_GREATER
            if (isAOT)
            {
                // note to warn that the gradual or "safe" dotnet style of suspenion is not supported on AOT.
                throw new PlatformNotSupportedException($"DotNetAbortInternal isn't supported on AOT ):");

            }
#endif
#if NET7_0_OR_GREATER
            if ((instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) >= ThreadState.AbortRequested || !instance.IsAlive)
                return;

            if (ThreadAbortNativeFunction == null)
            {
                try
                {
                    ThreadAbortNativeFunction = Marshal.GetDelegateForFunctionPointer<InternalThreadAbort>(typeof(ControlledExecution).GetMethod("AbortThread", (BindingFlags)int.MaxValue).MethodHandle.GetFunctionPointer());
                }
                catch
                {
                    throw new PlatformNotSupportedException("DotNetAbortInternal can't be used on this platform. This method requires reflection to be enabled on the platform.");
                }
            }
            try
            {
                ThreadAbortNativeFunction(GetInternalCPPThreadObject(instance));
            }
            catch
            {
                throw new PlatformNotSupportedException("DotNetAbortInternal isn't supported on this platform yet ):");
            }
#else
            instance.DotNetAbort();
#endif
        }
        [Obsolete("The DotNetAbort and ResetAbort 'may corrupt the process and should not be used in production code.\r\nOn .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.", false)]
        public static void ResetAbort(this Thread instance)
        {
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                // note to warn that the gradual or "safe" dotnet style of suspenion is not supported on AOT.
                throw new PlatformNotSupportedException($"ResetAbort isn't supported on AOT ):");

            }
#endif
            if ((instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) < ThreadState.AbortRequested)
                return;
            for (; (instance.GetNativeThreadState() & NETInternalThreadState.TS_AbortInitiated) == 0;) ;
            instance.SetThreadState(ThreadState.Running);

        }
#if NET45_OR_GREATER
        [Obsolete("The DotNetAbort and ResetAbort 'may corrupt the process and should not be used in production code.\r\nOn .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.", false)]
        public static async void ResetAbortAsync(this Thread instance)
        {
#if NET8_0_OR_GREATER
            if (isAOT)
            {
                // note to warn that the gradual or "safe" dotnet style of suspenion is not supported on AOT.
                throw new PlatformNotSupportedException($"ResetAbortAsync isn't supported on AOT ):");

            }
#endif
            if ((instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) < ThreadState.AbortRequested)
                return;
            for (; (instance.GetNativeThreadState() & NETInternalThreadState.TS_AbortInitiated) == 0;) ;
            instance.SetThreadState(ThreadState.Running);
            for (; (instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) != 0;) ;
        }
#endif
        [Obsolete("Calling this method might cause corruption in the runtime and lead to resource leakage, unreleased locks, critical points of the code unreached, and potentially, affecting the rest of the application's behavior. Do not use this method in production code.")]
        public static void SuspendNative(this Thread instance)
        {

            if ((instance.ThreadState & (ThreadState.SuspendRequested | ThreadState.Suspended)) >= ThreadState.SuspendRequested || !instance.IsAlive)
                return;
            SuspendThread(instance.GetNativeHandle());
            instance.SetThreadState(ThreadState.Suspended);

        }
        [Obsolete("Calling this method might cause corruption in the runtime and lead to resource leakage, unreleased locks, critical points of the code unreached, and potentially, affecting the rest of the application's behavior. Do not use this method in production code.")]
        public static void ResumeNative(this Thread instance)
        {
            if ((instance.GetNativeThreadState() & NETInternalThreadState.TS_SyncSuspended) == 0)
                return;
            instance.SetThreadState(ThreadState.Running);
            ResumeThread(instance.GetNativeHandle());


        }
    }
}
