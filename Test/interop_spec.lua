-- Specifies the generated interop code.

local M = {}

M.config =
{
    lua_lib_name = "luainterop",    -- for require
    class_name = "Interop",         -- host filenames
    namespace = "Script"            -- host namespace
}

------------------------ Host => Script ------------------------
M.script_funcs =
{
    -- {
    --     lua_func_name = "setup",
    --     host_func_name = "Setup",
    --     required = "true",
    --     description = "Initialize.",
    --     args =
    --     {
    --         { name = "opt", type = "I", description = "Option" },
    --     },
    --     ret = { type = "I", description = "Return integer" }
    -- },

    {
        lua_func_name = "send",
        host_func_name = "Send",
        required = "true",
        description = "Send string and return response.",
        args =
        {
            { name = "tx", type = "S", description = "String to send" },
        },
        ret = { type = "S", description = "Script rx" }
    },
}

------------------------ Script => Host ------------------------
M.host_funcs =
{
    {
        lua_func_name = "log",
        host_func_name = "Log",
        description = "Script wants to log something.",
        args =
        {
            { name = "err", type = "B", description = "Is error" },
            { name = "msg", type = "S", description = "The message" },
        },
        ret = { type = "I", description = "Unused" }
    },
}

return M
