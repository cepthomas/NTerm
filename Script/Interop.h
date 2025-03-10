///// Warning - this file is created by gen_interop.lua - do not edit. 2025-03-10 10:06:57 /////

#pragma once
#include "InteropCore.h"

using namespace System;
using namespace System::Collections::Generic;

namespace ScriptInterop
{

//============= C => C# callback payload .h =============//

//--------------------------------------------------------//
public ref class LogArgs : public EventArgs
{
public:
    /// <summary>Is error</summary>
    property bool err;
    /// <summary>The message</summary>
    property String^ msg;
    /// <summary>Constructor.</summary>
    LogArgs(bool err, const char* msg)
    {
        this->err = err;
        this->msg = gcnew String(msg);
    }
};


//----------------------------------------------------//
public ref class Interop : InteropCore::Core
{

//============= C# => C functions .h =============//
public:

    /// <summary>Send</summary>
    /// <param name="msg">Specific message</param>
    /// <returns>Script return</returns>
    String^ Send(String^ msg);

//============= C => C# callback functions =============//
public:
    static event EventHandler<LogArgs^>^ Log;
    static void Notify(LogArgs^ args) { Log(nullptr, args); }


//============= Infrastructure .h =============//
public:
    /// <summary>Initialize and execute.</summary>
    /// <param name="scriptFn">The script to load.</param>
    /// <param name="luaPath">LUA_PATH components</param>
    void Run(String^ scriptFn, List<String^>^ luaPath);
};

}
