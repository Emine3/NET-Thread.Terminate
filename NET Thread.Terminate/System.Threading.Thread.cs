using NativeThreadExtensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using static NativeThreadExtensions.NativeThreadExtensions;

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
            // field offset of m_ThreadhandleforClose
            // .net 5,6; 64 bit: 416, 32 bit: 268
            // .net 7,8;   64 bit: 408,   32 bit: 264
            // .net 9, 10;  64 bit: 272, 32 bit: 176
            // .net 11; 64 bit: 192, 32 bit: 132
            // CLR 4;  64 bit: 528, 32 bit: 336
            // CLR 2; 64 bit: 504, 32 bit: 328
            // if compiled on AOT, the thread handle is stored in the .net object itself: _osHandle
#if NET8_0_OR_GREATER
            if (isAOT)
            {

                return ((Microsoft.Win32.SafeHandles.SafeWaitHandle*)(*(byte**)&instance + sizeof(void*) * 8))->DangerousGetHandle();

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

#if NET5_0
          
        return      *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));
#elif NET9_0_OR_GREATER
            return *(NETInternalThreadState*)GetInternalCPPThreadObject(instance);

#elif NET6_0_OR_GREATER
     return  *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*)) ;
#endif
#if NET40_OR_GREATER

             return  *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*)) ;
#elif NET20_OR_GREATER
  return *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));
   
 
#endif



        }
        public unsafe static void SetThreadState(this Thread instance, ThreadState state)
        {

#if NET8_0_OR_GREATER
            if (isAOT)
            {
             *(ThreadState*)((byte*)*(IntPtr*)(*(byte**)&instance + 9 * sizeof(void*))) = state;
                return;
            }

#endif

#if NET5_0
          
           NETInternalThreadState NativeThreadState = *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));
#elif NET9_0_OR_GREATER
            NETInternalThreadState NativeThreadState = *(NETInternalThreadState*)GetInternalCPPThreadObject(instance);

#elif NET6_0_OR_GREATER
    NETInternalThreadState NativeThreadState = *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));
#endif
#if NET40_OR_GREATER

            NETInternalThreadState NativeThreadState = *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));
#elif NET20_OR_GREATER
    NETInternalThreadState NativeThreadState = *(NETInternalThreadState*)(GetInternalCPPThreadObject(instance) + sizeof(void*));
   
#else
            /* throw new PlatformNotSupportedException("SetThreadState() isn't supported on this platform yet ):");*/
#endif

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


        /// <summary>Terminates the thread immediately on the low level by calling the Windows API TerminateThread. Do not adapt using this method unless you have a good reason to terminate the thread by its native handle using TerminateThread; nonetheless, DotNetAbortInternal is preffered.</summary>
        public static void Terminate(this Thread instance, int ExitCode = 0)
        {
            if (!instance.IsAlive)
                return;

            TerminateThread(instance.GetNativeHandle(), ExitCode);
            instance.SetThreadState(ThreadState.Stopped);
        }

        [Obsolete("On .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.", false)]
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
            if ((instance.ThreadState & (ThreadState.SuspendRequested | ThreadState.Suspended)) >= ThreadState.SuspendRequested)
            {
                throw new ThreadStateException($"Suspension for the thread has been requested; in that case, aborting the thread is not possible until the thread is out of the suspended state (resumed) ):");
            }

            if ((instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) >= ThreadState.AbortRequested || !instance.IsAlive)
                return;

            instance.SetThreadState(ThreadState.AbortRequested);

        }
#if NET45_OR_GREATER
         [Obsolete("On .NET 5 and .NET 6, this method is experimental and might induce unexpected behavior such as throwing a random exception just before the ThreadAbortException which requires extra exception handling.", false)]
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
            if ((instance.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) >= ThreadState.AbortRequested || !instance.IsAlive)
                return;
            instance.SetThreadState(ThreadState.AbortRequested);
            for (; (instance.ThreadState & ThreadState.Aborted) != ThreadState.Aborted;) ;
        }
#endif
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
        public static void SuspendNative(this Thread instance)
        {

            if ((instance.ThreadState & (ThreadState.SuspendRequested | ThreadState.Suspended)) >= ThreadState.SuspendRequested || !instance.IsAlive)
                return;
            SuspendThread(instance.GetNativeHandle());
            instance.SetThreadState(ThreadState.Suspended);

        }
        public static void ResumeNative(this Thread instance)
        {
            if ((instance.GetNativeThreadState() & NETInternalThreadState.TS_SyncSuspended) == 0)
                return;
            instance.SetThreadState(ThreadState.Running);
            ResumeThread(instance.GetNativeHandle());

          
        }
    }
}
