-- String utilities. ...

local M = {}


---------------------------------------------------------------
-- Simple interpolated string function. Stolen/modified from http://lua-users.org/wiki/StringInterpolation.
-- ex: interp( [[Hello {name}, welcome to {company}.]], { name = name, company = get_company_name() } )
-- @param str Source string.
-- @param vars Replacement values dict.
-- @return Formatted string.
function M.interp(str, vars)
    if not vars then
        vars = str
        str = vars[1]
    end
    return (str:gsub("({([^}]+)})", function(whole, i) return vars[i] or whole end))
end

-----------------------------------------------------------------------------
-- Concat the contents of the parameter list, separated by the string delimiter.
-- Example: strjoin(", ", {"Anna", "Bob", "Charlie", "Dolores"})
-- Borrowed from http://lua-users.org/wiki/SplitJoin.
-- @param delimiter Delimiter.
-- @param list The pieces parts.
-- @return string Concatenated list.
function M.strjoin(delimiter, list)
    local len = #list
    if len == 0 then
        return ""
    end
    local string = table.concat(list, delimiter)
    -- local string = list[1]
    -- for i = 2, len do
    --     string = string .. delimiter .. list[i]
    -- end
    return string
end

-----------------------------------------------------------------------------
-- Split text into a list.
-- Consisting of the strings in text, separated by strings matching delimiter (which may be a pattern).
--   Example: strsplit(",%s*", "Anna, Bob, Charlie,Dolores")
--   Borrowed from http://lua-users.org/wiki/SplitJoin.
-- @param text The string to split.
-- @param delimiter Delimiter.
-- @param trim Remove leading and trailing whitespace, and empty entries.
-- @return list Split input.
function M.strsplit(text, delimiter, trim)
    local list = {}
    local pos = 1

    if text == nil then
        return {}
    end

    if string.find("", delimiter, 1, true) then -- this would result in endless loops
        error("Delimiter matches empty string.")
    end

    while 1 do
        local first, last = text:find(delimiter, pos, true)
        if first ~= nil then -- found?
            local s = text:sub(pos, first - 1)
            if trim then
                s = M.strtrim(s)
                if #s > 0 then
                    table.insert(list, s)
                end
            else
                table.insert(list, s)
            end
            pos = last + 1
        else -- no delim, take it all
            local s = text:sub(pos)
            if trim then
                s = M.strtrim(s)
                if #s > 0 then
                    table.insert(list, s)
                end
            else
                table.insert(list, s)
            end
            break
        end
    end
    return list
end

-----------------------------------------------------------------------------
-- Trims whitespace from both ends of a string.
-- Borrowed from http://lua-users.org/wiki/SplitJoin.
-- @param s The string to clean up.
-- @return string Cleaned up input string.
function M.strtrim(s)
    return (s:gsub("^%s*(.-)%s*$", "%1"))
end

-----------------------------------------------------------------------------
--- does s contain the phrase?
-- @string s a string
-- @param phrase a string
function M.contains(s, phrase)
    local res = s:find(phrase, 1, true)
    return res ~= nil and res >= 1
end

-----------------------------------------------------------------------------
--- does s start with prefix?
-- @string s a string
-- @param prefix a string
function M.startswith(s, prefix)
    return s:find(prefix, 1, true) == 1
end

-----------------------------------------------------------------------------
--- does s end with suffix?
-- @string s a string
-- @param suffix a string
function M.endswith(s, suffix)
    return #s >= #suffix and s:find(suffix, #s-#suffix+1, true) and true or false
end

-----------------------------------------------------------------------------
-- Return the module.
return M
