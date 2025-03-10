
:: Convert spec into interop library.
echo off
cls

set "ODIR=%cd%"
pushd LBOT
set LUA_PATH=;;"%ODIR%\?.lua";?.lua;
lua gen_interop.lua -c "%ODIR%\interop_spec.lua" "%ODIR%"
lua gen_interop.lua -cppcli "%ODIR%\interop_spec.lua" "%ODIR%"
popd

pause
