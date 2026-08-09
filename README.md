******.NET Thread.Terminate 1.0.5****** [![NuGet](https://img.shields.io/nuget/v/NET-Thread.Terminate.svg)](https://www.nuget.org/packages/NET-Thread.Terminate/)

## Introduction
".NET Thread.Terminate" is a partial implementation of a bigger project [Untitled](https://github.com/Emine3/Untitled) under development. It offers capability to perform native operations such as termination on any managed threads on both .NET and .NET Framework. This project serves as a proof of concept demonstrating the power of the upcoming project.

## .NET threads: a managed wrapper
Obtaining the handle of the relative native thread makes this possible; however, none of .NET runtimes offers an option to expose the native handle to the created thread; instead we're left with a managed thread ID that essentially serves as a number to distinguish between our instantiated managed threads: ultimately a Thread instance in C# is not a real native thread and only relevant to the managed context, right? Not quite right! The Thread class can be concisely explained as a managed wrapper around a C++ object containing the handle of the created native thread by the runtime; that "C++ object" is preserved in a field named "DONT_USE_InternalThread" we have no business even looking at as suggested by the name 🙂 This field represents the internally instantiated C++ object.
 
The thread's handle is assigned to the field "m_ThreadHandleForClose" which preserves the handle to the created thread by CreateThread called by .NET internals, and there is this other important field "m_state" which's the internal version of ThreadState (especially useful for properties such as IsAlive). Now, were we to access the C++ object in "DONT_USE_InternalThread" and calculate the offset of these two fields for all different .NET versions, we could obtain the OS handle to that "native thread" and make calling native Windows API functions on any targeted managed threads possible; having access to these two fields also gives us so much power such as restoration of Abort and Suspend methods on .NET Framework; these offsets have been tested against each supported runtime in Section [Compatibility](#Compatibility); however, over the time, there is no guarantee, these offsets stay relevant and correct (read [Additional Information](#Additional-Information) Section).


## Installation
The best way to use .NET Thread.Terminate is by installing its NuGet package;
the easiest way to do that is using Visual Studio's package manager and search for ".NET Thread.Terminate".

You can also install the NuGet package by using the dotnet CLI:
```shell
dotnet add package NET-Thread.Terminate
```

## How to use
.NET Terminate is easy to use: it adds extension methods to the Thread class so you could call them like you would do any other methods on your Thread object. These methods are, also, defined and available in System.Threading.NativeThreadExtensions.

> [!CAUTION]
> Calling 'ThreadTerminate' on a thread might cause corruption in the runtime and lead to resource leakage, unreleased locks, critical points of the code unreached, and potentially, affecting the rest of the application's behavior. Do not use this method in production code.

### 1. Terminate
```csharp
SomeThread.Terminate(ExitCode = 0);
```
Terminates the thread immediately on the low level by calling the Windows API TerminateThread on its native handle. This method is intended to be tested and tried conceptually in advanced scenarios that concern debugging needs and should be avoided in production code as it is extremely dangerous; a thread stuck at a p/invoke call (in the preemptive mode) or troublesome threads created in a third-party library are rare phenomenons that shouldn't occur in a production environment in the first place: your code shouldn't ever reach the point that causes loss of control of your thread in an ideal production ready app; when that happens, you'll lose every safety benefit a managed language offers and should reconsider techniques or approaches you've implemented. 

![GIF 1](https://raw.githubusercontent.com/Emine3/NET-Thread.Terminate/refs/heads/main/Assets/Homelander.gif)

To end a thread in any production code, use cooperative cancellation such as [CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken).

![Reading a file using a created Thread stuck at calling a native function](https://raw.githubusercontent.com/Emine3/NET-Thread.Terminate/refs/heads/main/Assets/Thread.Terminate%20Instance.png)

> [!CAUTION]
> The DotNetAbort method 'may corrupt the process and should not be used in production code.'

> [!warning]
> This method requires special exception handling on .NET 5 and .NET 6

### 2. DotNetAbort 
```csharp
SomeThread.DotNetAbort();
```
Aborts the thread in the same style as the Abort method on .NET Framework; it interrupts the thread by throwing an exception at the running thread which gives us the opportunity to properly release the resources we used in the thread when catching the exception in the exception clause.

To reset the thread abortion, the extension method ThreadAbort should be called on the thread object.

This method works on all .NET versions starting with .NET 5. On .NET 5 and .NET 6, it works by changing the internal thread state to request the runtime to abort the thread; starting with .NET 7, it just falls back to DotNetAbortInternal because it's more stable.


There is a catch you should absolutely consider and take into account when aborting the thread on .NET 5 and .NET 6 using this method: a random exception might be thrown at the thread before the main ThreadAbortException exception; this possible case must be handled like the code below:

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

> [!CAUTION]
> The DotNetAbortInternal method 'may corrupt the process and should not be used in production code.'

### 3. DotNetAbortInternal 
```csharp
SomeThread.DotNetAbortInternal();
```
Aborts the thread in the same style as the Abort method on .NET Framework; analogously does the the same thing DotNetAbort does.

ControlledExecution was introduced on .NET 7 which would allow you to run a piece of code and abort it; to do that, it implements a p/Invoke declaration responsible for aborting the thread; that declaration is used to abort the thread. On .NET platforms older than .NET 7, it falls back to DotNetAbort.


### 4. ResetAbort 
```csharp
SomeThread.ResetAbort();
```
```csharp
Thread.CurrentThread.ResetAbort();
```
Applies the same mechanism as the documented static method Thread.ResetAbort to reset an abort request. This extension method should be called instead of the static method in the Thread class (Thread.ResetAbort) starting with .NET 5. Like on .NET Framework when calling the Abort Method, you should reset the abort request once you catch the ThreadAbortException exception; starting with .NET 7, you should do that by calling this method directly on the thread instance.

> [!CAUTION]
> Calling 'SuspendNative' on a thread might cause corruption in the runtime and lead to resource leakage, unreleased locks, critical points of the code unreached, and potentially, affecting the rest of the application's behavior. Do not use this method in production code.

### 5. SuspendNative 
```csharp
SomeThread.SuspendNative();
```
Suspends the thread on the low level by calling the Windows API SuspendThread on its native handle. Not to be confused with the Suspend method on .NET Framework. Do not adopt using this method unless you have a good reason. 



### 6. ResumeNative 
```csharp
SomeThread.ResumeNative();
```
Resumes the suspended thread by calling the Windows API ResumeThread on its native handle. Not to be confused with the Resume method on .NET Framework. Do not adopt using this method unless you have a good reason.


### 7. GetNativeHandle 
```csharp
SomeThread.GetNativeHandle();
```
Returns the handle to the native thread associated with the managed thread object.



****Utility Class****
A class containing some methods or fields that might come in handy; the class is defined under the NativeThreadExtension namespace.

### ⭐1. CheckCompatibilityatRuntime 
```csharp
Utility.CheckCompatibilityatRuntime = false;
```
Determines whether the runtime version of the imported library matches the running application's or not. If true, it will check the application's runtime version against the compiled library to ensure compatibility; if it doesn't match, an exception will be thrown as a means to let you know you've imported the reference of the library. If you encounter this error unintentionally, you should add the right reference closest to your runtime version; the best way to do that is by installing the NuGet package in your project, the closest reference will be added automatically. If you assign false to this field, the compatibility checking will not be performed.

This method is supported on all .NET and .NET Framework versions.

### 2. GetRuntimeVersion 
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



### 3. GetRuntimeVersionString
```csharp
 Console.WriteLine($"Running on {Utility.GetRuntimeVersionString(Utility.GetRuntimeVersion(), IncludeRuntimeVersion = true)}");
```
Returns a string representation of the Enum value returned by GetRuntimeVersion.


### 4. GetNativeThreadState

Returns the internal thread state. This method is preserved for advanced scenarios. Do not use without intention to change or read internal material with adequate CLR internal knowledge.

### 5. SetNativeThreadState

Sets the internal thread state. This method is preserved for advanced scenarios. Do not use without intention to change or read internal material with adequate CLR internal knowledge.



## Compatibility

| Runtime | Compatible? | Terminate | GetNativeHandle | DotNetAbort | SuspendNative | ResumeNative | ResetAbort |
|---------|:-----------:|:---------:|:---------------:|:-----------:|:-------------:|:------------:|:----------:|
| `.NET 11` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 10` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 9` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 8` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 7` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `.NET 6` | ✔️ | ✔️ | ✔️ | [✔️](#2-dotnetabort) | ✔️ | ✔️ | [✔️](#2-dotnetabort) | 
| `.NET 5` | ✔️ | ✔️ | ✔️ | [✔️](#2-dotnetabort) | ✔️ | ✔️ | [✔️](#2-dotnetabort) |
| `.NET Framework 2.x– 4.8.1` | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ | ✔️ |
| `NativeAOT` | ✔️ | ✔️ | ✔️ | ❌ | ✔️ | ✔️ | ❌ |
| `.NET Framework 1.x` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `.NET Core 1.x - 3.1` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Terminate**, **SuspendNative**, and **ResumeNative** are only supported on Windows. Support for other platforms will be added.

## Additional Information

Some important information regarding the internal thread implementation that deserves attention:
* The .NET Framework style of thread suspension is not implemented on this repository. It is preserved for the main untitled project.

* **DotNetAbort** is not supported on NativeAOT. NativeAOT does not benefit from the internal runtime implementation handling thread "gradual" abortion requests and throwing a `ThreadAbortException` exception at the GC safe points since the concept doesn't exist on the Native level.


* On .NET Framework, the address to the native handle in the C++ object mentioned only differs by the CLR version: CLR 2.x and CLR 4.x. All .NET Frameworks, hence are supported with an exception of .NET Framework versions older than 2. Notwithstanding, starting with .NET 5, that is not the case anymore since each .NET version has its separate runtime with different offsets. So if you use this project on .NET 5+, you should update the package and check if the newly released .NET is supported.
