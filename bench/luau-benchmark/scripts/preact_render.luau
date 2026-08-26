-- Preact render benchmark — real preact-luau reconciler + the LuaUiLibrary C# host.
--
-- Globals injected by the host: `React` (the react.luau module) and `UiLib` (the host).
--
-- Two regimes, chosen by `freshProps`:
--   * freshProps = true  -> every style table and text is freshly allocated each render, so
--                           preact's diffProps fires a host `setProperty`/`commitTextUpdate`
--                           for every node: the loop is C#-interop bound (use a SMALL tree).
--   * freshProps = false -> style/text references are module constants, so diffProps finds
--                           equal references and makes ~zero host calls: the loop is bound by
--                           the pure-Lua reconciler walk (use a LARGE tree).
--
-- Contract: this chunk returns a single `run(iterations, size, freshProps)` function that
-- mounts the tree once, warms up, then re-renders `iterations` times on the same root
-- (preact updates in place). Returns the os.clock() delta (CPU seconds).

local ROW_STYLE = { height = 16 }
local TEXT_STYLE = { fontSize = 12 }
local COLUMN_STYLE = { flexDirection = 'column' }

local function makeApp(size, seed, fresh)
    local rows = {}
    for i = 1, size do
        local text = fresh and (tostring(seed) .. ':' .. tostring(i)) or tostring(seed)
        rows[i] = React.h(
            'view',
            { key = 'row' .. i, style = fresh and { height = 16 } or ROW_STYLE },
            React.h(
                'text',
                { style = fresh and { fontSize = 12 } or TEXT_STYLE },
                text
            )
        )
    end
    return React.h('view', { style = fresh and { flexDirection = 'column' } or COLUMN_STYLE }, rows)
end

local function run(iterations, size, fresh)
    size = size or 16
    fresh = fresh and true or false
    iterations = iterations or 1000

    local root = UiLib.createRoot()
    React.render(makeApp(size, 0, fresh), root)

    -- Warm-up pass.
    React.render(makeApp(size, -1, fresh), root)

    local t0 = os.clock()
    for i = 1, iterations do
        React.render(makeApp(size, i, fresh), root)
    end
    local t1 = os.clock()

    return t1 - t0
end

return run
