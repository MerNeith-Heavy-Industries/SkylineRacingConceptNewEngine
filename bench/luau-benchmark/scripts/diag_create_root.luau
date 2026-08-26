-- Diagnostic: create many View userdatas at TOP LEVEL (no preact, no metatable dispatch).
-- If this crashes at ~6 refs, the bug is registry growth in lua_ref generally, not reentrancy.
-- If it succeeds for large n, the crash is specific to the reentrant metatable-dispatch context.
local function run(n)
    n = n or 1000
    local t0 = os.clock()
    for i = 1, n do
        local r = UiLib.createRoot()
    end
    local t1 = os.clock()
    return t1 - t0
end
return run
