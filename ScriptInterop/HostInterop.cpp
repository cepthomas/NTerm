///// Warning - this file is created by gen_interop.lua - do not edit. /////

#include <windows.h>
#include "luainterop.h"
#include "HostInterop.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace Interop;

//============= C# => C functions .cpp =============//

//--------------------------------------------------------//
String^ HostInterop::Send(String^ msg)
{
    LOCK();
    String^ ret = ToManagedString(luainterop_Send(_l, ToCString(msg)));
    _EvalLuaInteropStatus("Send()");
    return ret;
}


//============= C => C# callback functions .cpp =============//


//============= Infrastructure .cpp =============//

//--------------------------------------------------------//
void HostInterop::Run(String^ scriptFn, List<String^>^ luaPath)
{
    InitLua(luaPath);
    // Load C host funcs into lua space.
    luainterop_Load(_l);
    // Clean up stack.
    lua_pop(_l, 1);
    OpenScript(scriptFn);
}
