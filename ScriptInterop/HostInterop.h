///// Warning - this file is created by gen_interop.lua - do not edit. 2025-03-07 16:46:06 /////

#pragma once
#include "Core.h"

using namespace System;
using namespace System::Collections::Generic;

namespace Interop
{

//============= C => C# callback payload .h =============//


//----------------------------------------------------//
public ref class HostInterop : Core
{

//============= C# => C functions .h =============//
public:

    /// <summary>Send</summary>
    /// <param name="msg">Specific message</param>
    /// <returns>Script return</returns>
    String^ Send(String^ msg);

//============= C => C# callback functions =============//
public:

//============= Infrastructure .h =============//
public:
    /// <summary>Initialize and execute.</summary>
    /// <param name="scriptFn">The script to load.</param>
    /// <param name="luaPath">LUA_PATH components</param>
    void Run(String^ scriptFn, List<String^>^ luaPath);
};

}
