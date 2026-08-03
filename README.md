[![NuGet](https://img.shields.io/nuget/v/NET.Thread.Terminate.svg)](https://www.nuget.org/packages/NET-Thread.Terminate/)
## Introduction
.NET Thread.Terminate lets you terminate any managed threads on both .NET and .NET Framework on an OS level (TerminateThread) by adding an extension method Thread.Terminate to the BCL class Thread; it also restores the .NET Framework style of thread abortion (Thread.Abort) on modern .NET versions. ".NET Thread.Terminate" is a partial implementation of a bigger project [Untitled](https://github.com/Emine3/Untitled) under development.

![Thread.Terminate Homelander](https://github.com/Emine3/NET-Thread.Terminate/blob/main/Assets/Homelander.gif)

A little bit of research on the internet will probably lead to answers suggesting using canonical approaches such as using CancellationToken or constantly checking a flag to decide whether to continue or return in the thread.

In most contexts, you should take these safe approaches especially in a production environment; however, there come times when we need to end a thread we didn't create in the first place, for example, threads that were created and started in a third party library; threads hung on a native call, and then, these commonly safe methods don't exactly meet the needs of our situation; that leaves us with occasions on which we need to terminate a thread immediately the low level way as the last dangerous course of action (in a similar manner to terminating processes [TerminateProcess]).

## .NET threads: a managed wrapper
Obtaining the handle of the relative native thread makes this possible; however, none of .NET runtimes offers an option to expose the native handle to the created thread; instead we're left with a managed thread ID that essentially serves as a number to distinguish between our instantiated managed threads: ultimately a Thread instance in C# is not a real native thread and only relevant to the managed context, right? Not quite right! The Thread class can be concisely explained as a managed wrapper around a C++ object containing the handle of the created native thread by the runtime; that "C++ object" is preserved in a field named "DONT_USE_InternalThread" we have no business even looking at as suggested by the name: This field represents the internally instantiated C++ object.

Utilizing this object, this project allows native operations such as termination, suspension, or resumption, and restores or simulates the threading functionality we saw on .NET Framework.

The thread's handle is assigned to the field "m_ThreadHandleForClose" which preserves the handle to the created thread by CreateThread called by .NET internals, and there is this other important field "m_state" which's the internal version of ThreadState (especially useful for properties such as IsAlive). Now, were we to access the C++ object in "DONT_USE_InternalThread" and calculate the offset of these two fields for all different .NET versions, we could obtain the OS handle to that "native thread" and terminate it on the OS level the way we do in low level languages like C++ (that comes with its own dangers); having access to these two fields also gives us so much power such as restoration of Abort and Suspend methods on .NET Framework! That's exactly what I've done in the project

> [!warning]
> This project is only intended to be used in advanced scenarios such as debugging cases; do not adopt using this method when ending threads. Use safer approaches like [CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken).


## Installation
The best way to use .NET Thread.Terminate is by installing its [NuGet package](https://www.nuget.org/packages/NET-Thread.Terminate/);
the easiest way to do that is using Visual Studio's package manager and search for ".NET Thread.Terminate".

You can also install the NuGet package by using the dotnet CLI:
```shell
dotnet add package NET-Thread.Terminate
```

## <center>How to use</center>
.NET Terminate is easy to use: it adds extension methods to the Thread class so you could call them like you would do any other methods on your Thread object. These methods are, also, defined and available in System.Threading.NativeThreadExtensions.


### <center>⭐1. Terminate</center>
```csharp
SomeThread.Terminate(ExitCode = 0);
```
Terminates the thread immediately on the low level by calling the Windows API TerminateThread on its native handle. Do not adopt using this method unless you have a good reason to terminate the thread immediately; nonetheless, DotNetAbort is preferred over this method.

![Reading a file using a created Thread stuck at calling a native function](https://github.com/Emine3/NET-Thread.Terminate/blob/main/Assets/Thread.Terminate%20Instance.png)


> [!warning]
> This method requires special exception handling on .NET 5 and .NET 6

### <center>2. DotNetAbort <center>
```csharp
SomeThread.DotNetAbort();
```
Aborts the thread in the same style as the Abort method on .NET Framework; it interrupts the thread by throwing an exception at the running thread which gives us the opportunity to properly release the resources we used in the thread when catching the exception in the exception clause.

To reset the thread abortion, the extension method ThreadAbort should be called on the thread object.

This method works on all .NET versions starting with .NET 5. On .NET 5 and .NET 6, it works by changing the internal thread state to request the runtime to abort the thread; starting with .NET 7, it just falls back to DotNetAbortInternal because it's more stable.


[^1]: There is a catch you should absolutely consider and take into account when aborting the thread on .NET 5 and .NET 6 using this method: a random exception might be thrown at the thread before the main ThreadAbortException exception; this possible case must be handled like the code below:

```csharp
  Thread SomeThread = new Thread(() =>
  {
      Exception exception = null;
      try
      {
          try
          {
              for (; ; Thread.Sleep(150))
              {
                  Console.WriteLine("Stop me!");
              }
          }
          catch (Exception ex)
          {
              exception = ex;
          }
      }
      catch (ThreadAbortException ex)
      {
          exception = ex;
          // Use ResetAbort() to reset the abortion when using DotNetAbort methods; not Thread.ResetAbort()! 
          Thread.CurrentThread.ResetAbort();
      }
      if (exception != null)
      {    
          // doing finalizing stuff

          if (exception.GetType() == typeof(ThreadAbortException))
          {
             
              return;
          }
          Console.WriteLine($"An error occurred. {exception.Message}");
      }
  })
  { Name = ".NET Thread", IsBackground = true };

  SomeThread.Start();
  SomeThread.DotNetAbort();
```
However, this is not necessary and relevant on .NET 7+ since it just falls back to DotNetAbortInternal.


### <center>3. DotNetAbortInternal <center>
```csharp
SomeThread.DotNetAbortInternal();
```
Aborts the thread in the same style as the Abort method on .NET Framework; analogously does the the same thing DotNetAbort does.

ControlledExecution was introduced on .NET 7 which would allow you to run a piece of code and abort it; to do that, it implements a p/Invoke declaration responsible for aborting the thread; that declaration is used to abort the thread. On .NET platforms older than .NET 7, it falls back to DotNetAbort. This method is preferred over Terminate whenever it's possible.


### <center>4. ResetAbort <center>
```csharp
SomeThread.ResetAbort();
```
```csharp
Thread.CurrentThread.ResetAbort();
```
Applies the same mechanism as the documented static method Thread.ResetAbort to reset an abort request. This extension method should be called instead of the static method in the Thread class (Thread.ResetAbort) starting with .NET 5. Like on .NET Framework when calling the Abort Method, you should reset the abort request once you catch the ThreadAbortException exception; starting with .NET 7, you should do that by calling this method directly on the thread instance.

 


### <center>5. SuspendNative <center>
```csharp
SomeThread.SuspendNative();
```
Suspends the thread on the low level by calling the Windows API SuspendThread on its native handle. Not to be confused with the Suspend method on .NET Framework. Do not adopt using this method unless you have a good reason. 



### <center>6. ResumeNative <center>
```csharp
SomeThread.ResumeNative();
```
Resumes the suspended thread by calling the Windows API ResumeThread on its native handle. Not to be confused with the Resume method on .NET Framework. Do not adopt using this method unless you have a good reason.


### <center>7. GetNativeHandle <center>
```csharp
SomeThread.GetNativeHandle();
```
Returns the handle to the native thread associated with the managed thread object.



****<center>Utility Class</center>****
A class containing some methods or fields that might come in handy; the class is defined under the NativeThreadExtension namespace.

### <center>⭐1. CheckCompatibilityatRuntime <center>
```csharp
Utility.CheckCompatibilityatRuntime = false;
```
Determines whether the runtime version of the imported library matches the running application's or not. If true, it will check the application's runtime version against the compiled library to ensure compatibility; if it doesn't match, an exception will be thrown as a means to let you know you've imported the reference of the library. If you encounter this error unintentionally, you should add the right reference closest to your runtime version; the best way to do that is by installing the NuGet package in your project, the closest reference will be added automatically. If you assign false to this field, the compatibility checking will not be performed.

This method is supported on all .NET and .NET Framework versions.

### <center>2. GetRuntimeVersion <center>
```csharp
      DotNetPlatform Platform = Utility.GetRuntimeVersion();
      if((Platform & DotNetPlatform.CLR) != 0)
      {
          DotNetPlatform DotNetFrameworkVersion = Platform & ~DotNetPlatform.CLR;
          if(DotNetFrameworkVersion == DotNetPlatform.NET_Framework_2_X)
          { // do stuff
          }
          if (DotNetFrameworkVersion == DotNetPlatform.NET_Framework_4_0)
          { // do stuff
          }
          if (DotNetFrameworkVersion == DotNetPlatform.NET_Framework_4_5)
          { // do stuff
          }
          if (DotNetFrameworkVersion == DotNetPlatform.NET_Framework_4_5_1)
          { // do stuff
          }
          if (DotNetFrameworkVersion == DotNetPlatform.NET_Framework_4_8_1
              )
          { // do stuff
          }
           ...
      }
      else
      {
          if (Platform !=  DotNetPlatform.NET_11)
          { // do stuff
          }
          if (Platform != Utility.DotNetPlatform.NET_10)
          { // do stuff
          }
          if (Platform != Utility.DotNetPlatform.NET_8)
          { // do stuff
          }
          if (Platform != Utility.DotNetPlatform.NET_5)
          { // do stuff
          }
          ...
      }
```
Returns an Enum with the type of Utility.DotNetPlatform representing the .NET runtime the application is running on. On .NET 5+, the returned value will be one of the Utility.DotNetPlatform.NET_X Enum members with no extra flags set; on .NET Framework, the value is one of NET_Framework_X to indicate the .NET Framework version in conjunction with bits set to include the CLR variant. If you intend only to obtain the .NET Framework version, you should remove the extra bits set: you can do that by removing Flag Utility.DotNetPlatform.CLR "DotNetPlatform DotNetFrameworkVersion = Platform & ~DotNetPlatform.CLR;".



### <center>3. GetRuntimeVersionString<center>
```csharp
 Console.WriteLine($"Running on {Utility.GetRuntimeVersionString(Utility.GetRuntimeVersion(), IncludeRuntimeVersion = true)}");
```
Returns a string representation of the Enum value returned by GetRuntimeVersion.


### <center>4. GetNativeThreadState<center>

Returns the internal thread state. This method should not be used unless it's for an advanced case.

### <center>5. SetNativeThreadState<center>

Sets the internal thread state. This method should not be used unless it's for an advanced case.



## Compatibility

| Runtime | Compatible? | Terminate | GetNativeHandle | DotNetAbort | SuspendNative | ResumeNative | ResetAbort |
|---------|:-----------:|:---------:|:---------------:|:-----------:|:-------------:|:------------:|:----------:|
| `.NET 11` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 10` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 9` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 8` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 7` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 6` | ✔️ | ✔️ | ✔️ | ✔️[^1] | ✔️ | ✔️ | ✔️[^1] | 
| `.NET 5` | ✔️ | ✔️ | ✔️ | ✔️[^1] | ✔️ | ✔️ | ✔️[^1] |
| `.NET Framework 2.x– 4.8.1` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `NativeAOT` | ✔️ | ✔️ | ✔️ | ❌ | ✔️ | ✔️ | ❌ |
| `.NET Framework 1.x` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `.NET Core 1.x - 3.1` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Terminate**, **SuspendNative**, and **ResumeNative** are only supported on Windows. Support for other platforms will be added.

## Additional Information

Some important information regarding the internal thread implementation that deserves attention:
* The .NET Framework style of thread suspension is not implemented on this repository. It is preserved for the main untitled project.

* **DotNetAbort** is not supported on NativeAOT. NativeAOT does not benefit from the internal runtime implementation handling thread "gradual" abortion requests and throwing a `ThreadAbortException` exception at the GC safe points since the concept doesn't exist on the Native level.


* On .NET Framework, the address to the native handle in the C++ object mentioned only differs by the CLR version: CLR 2.x and CLR 4.x. All .NET Frameworks, hence are supported with an exception of .NET Framework versions older than 2. Notwithstanding, starting with .NET 5, that is not the case anymore since each .NET version has its separate runtime with different offsets. So if you use modern .NET, you should update the package and check if the newly released .NET is supported.  

## Conclusion
I've spared you the redundant disclaimers only to save them for this section: one important thing to consider is in the managed context, you should not use this approach to end a thread; instead you should take safer approaches like CancellationToken or perhaps, consider using tasks as an alternative if you can see it's appropriate so; after all, there is a reason why you were not given the liberty of accessing the thread's native handle or a way to terminate it natively; conversely, as I've indicated, there are some situations that might create contradictory needs: you should be able to adapt to different situations that need different kind of solution even if that means not taking the usual approaches we commonly practice.

Terminating a thread like that also can be a double edged sword, you as a developer, decide if it benefits you or it doesn't; think of it that way: when you drive a car, sometimes, you can anticipate an accident bound to happen, heading toward a cliff with your brakes having failed in our case so you don't have many reliable options. 

you have little time to do something about it; there are some courses of action to take to reduce the excessively high speed like steering back and forth, or using the emergency brake; depending on the situation, you might find jumping off the car the only realistic option left; the smart thing here is to have gathered your important, valuable belongings within easy reach before having started the car, if you have to jump from the moving car, at least your valuable belongings won't be lost with it! 

The same holds true in our situation, it's the best if you get to start the thread, store important resources such as references to unmanaged memory or disposable objects somewhere you can, later on, access so when you have to do the vicious termination, at least, you get to release the resources you have control over properly (the figure 2 is a nice example of that). 

To get a better perception, ask yourself these questions when deciding to terminate a .NET thread and then act accordingly: 

* Do I own the targeted thread? 
using tasks, cooperative cancellation such as using CancellationToken, and finally Thread.Interrupt() (which is still available by default) if the thread is sitting in one of (or in the WaitSleepJoin state);

* does the thread that I started contain a native call that might be blocking it?

If the answer is no, Thread.DotNetAbort() is the better fit here; 

* do I not own the thread or does the call contain a native or p/invoke call that might be blocking the thread?

If the answer to any of these questions is yes, then Thread.Terminate.
