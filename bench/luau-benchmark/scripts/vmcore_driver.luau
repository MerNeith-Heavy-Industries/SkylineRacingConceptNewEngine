-- Driver for running scripts/vmcore.luau under the standalone luau.exe:
--   luau.exe vmcore_driver.luau  (run from the scripts/ dir)
-- Prints the same CPU metric the `vmcore` scenario reports for Lua-CSharp.

local run = require("./vmcore")

local cpu, acc = run(2000, 8, true)
print(string.format("vmcore CPU %.3f ms acc %d", cpu * 1000, acc))
