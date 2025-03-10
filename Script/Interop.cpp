///// Warning - this file is created by gen_interop.lua - do not edit. /////

#include <windows.h>
#include "luainterop.h"
#include "Interop.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace InteropCore;
using namespace ScriptInterop;


//============= C# => C functions .cpp =============//

//--------------------------------------------------------//
String^ Interop::Send(String^ msg)
{
    LOCK();
    String^ ret = gcnew String(luainterop_Send(_l, ToCString(msg)));
    _EvalLuaInteropStatus(luainterop_Error(), "Send()");
    return ret;
}


//============= C => C# callback functions .cpp =============//


//--------------------------------------------------------//

int luainteropcb_Log(lua_State* l, bool err, const char* msg)
{
    LOCK();
    LogArgs^ args = gcnew LogArgs(err, msg);
    Interop::Notify(args);
    return 0;
}


//============= Infrastructure .cpp =============//

//--------------------------------------------------------//
void Interop::Run(String^ scriptFn, List<String^>^ luaPath)
{
    InitLua(luaPath);
    // Load C host funcs into lua space.
    luainterop_Load(_l);
    // Clean up stack.
    lua_pop(_l, 1);
    OpenScript(scriptFn);
}
