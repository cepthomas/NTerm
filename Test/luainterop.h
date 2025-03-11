#pragma once
///// Warning - this file is created by gen_interop.lua - do not edit. 2025-03-11 08:29:01 /////

#include <stdbool.h>

#ifdef __cplusplus
#include "lua.hpp"
extern "C" {
#include "luaex.h"
};
#else
#include "lua.h"
#include "luaex.h"
#endif

//============= C => Lua functions .h =============//

// Send string and return response.
// @param[in] l Internal lua state.
// @param[in] tx String to send
// @return const char* Script rx
const char* luainterop_Send(lua_State* l, const char* tx);


//============= Lua => C callback functions .h =============//

// Script wants to log something.
// @param[in] l Internal lua state.
// @param[in] err Is error
// @param[in] msg The message
// @return Unused
int luainteropcb_Log(lua_State* l, bool err, const char* msg);

//============= Infrastructure .h =============//

/// Load Lua C lib.
void luainterop_Load(lua_State* l);

/// Return operation error or NULL if ok.
const char* luainterop_Error();
