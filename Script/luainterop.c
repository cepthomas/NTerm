///// Warning - this file is created by gen_interop.lua - do not edit. /////

#include "luainterop.h"

#if defined(_MSC_VER)
// Ignore some generated code warnings
#pragma warning( disable : 6001 4244 4703 4090 )
#endif

static const char* _error;

//============= C => Lua functions .c =============//

//--------------------------------------------------------//
const char* luainterop_Send(lua_State* l, const char* msg)
{
    _error = NULL;
    int stat = LUA_OK;
    int num_args = 0;
    int num_ret = 1;
    const char* ret = 0;

    // Get function.
    int ltype = lua_getglobal(l, "send");
    if (ltype != LUA_TFUNCTION)
    {
        if (true) { _error = "Bad function name: send()"; }
        return ret;
    }

    // Push arguments. No error checking required.
    lua_pushstring(l, msg);
    num_args++;

    // Do the protected call.
    stat = luaex_docall(l, num_args, num_ret);
    if (stat == LUA_OK)
    {
        // Get the results from the stack.
        if (lua_isstring(l, -1)) { ret = lua_tostring(l, -1); }
        else { _error = "Bad return type for send(): should be string"; }
    }
    else { _error = lua_tostring(l, -1); }
    lua_pop(l, num_ret); // Clean up results.
    return ret;
}


//============= Lua => C callback functions .c =============//

//--------------------------------------------------------//
// Script wants to log something.
// @param[in] l Internal lua state.
// @return Number of lua return values.
// Lua arg: err Is error
// Lua arg: msg The message
// Lua return: int Unused
static int luainterop_Log(lua_State* l)
{
    // Get arguments
    bool err;
    if (lua_isboolean(l, 1)) { err = lua_toboolean(l, 1); }
    else { luaL_error(l, "Bad arg type for: err"); }
    const char* msg;
    if (lua_isstring(l, 2)) { msg = lua_tostring(l, 2); }
    else { luaL_error(l, "Bad arg type for: msg"); }

    // Do the work. One result.
    int ret = luainteropcb_Log(l, err, msg);
    lua_pushinteger(l, ret);
    return 1;
}


//============= Infrastructure .c =============//

static const luaL_Reg function_map[] =
{
    { "log", luainterop_Log },
    { NULL, NULL }
};

static int luainterop_Open(lua_State* l)
{
    luaL_newlib(l, function_map);
    return 1;
}

void luainterop_Load(lua_State* l)
{
    luaL_requiref(l, "luainterop", luainterop_Open, true);
}

const char* luainterop_Error()
{
    return _error;
}
