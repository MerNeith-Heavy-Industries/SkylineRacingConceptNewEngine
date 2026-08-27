-- Timetrial HUD repro — mirrors NFMWorld.Library/data/uis/routes/racehud.luau.
-- A small preact-luau tree (17 host nodes) where Speed / PowerDamageBars / Splits
-- subscribe to "race:hudState" and setState every frame (timetrial telemetry), driven
-- through the REAL deferred flush: UiLib.defer -> GameThreadContext.Post ->
-- ExecutePendingTasks (via __bench_flush).
--
-- Globals injected by the host: React, UiLib, __bench_push(evt, payload),
-- __bench_flush(), __bench_reset_stats().
--
-- Two regimes, chosen by `freshProps`:
--   freshProps = true  -> every style table + changing text is freshly allocated each
--                         render (current racehud.luau behaviour): diffProps fires a
--                         host setProperty per node and commitTextUpdate for text.
--   freshProps = false -> style tables are module constants (the "useMemo the styles"
--                         fix): diffProps finds equal references and skips setProperty.
--                         Text that changes still commits (unavoidable).
--
-- Contract: chunk returns run(frames, freshProps) -> os.clock() CPU delta (seconds).

local React = React
local x = React.x
local useState = React.useState
local useEffect = React.useEffect

-- module-constant styles (stable-props mode)
local STYLE_ROOT   = { width = "100%", height = "100%", position = "relative" }
local STYLE_PDB    = { position = "absolute", top = 16, right = 16, width = 200, display = "flex", flexDirection = "column", gap = 8 }
local STYLE_CARD   = { display = "flex", flexDirection = "column", gap = 4 }
local STYLE_LABEL  = { fontSize = 11, color = "rgba(255,255,255,0.5)", marginBottom = 4 }
local STYLE_BAR    = { height = 10 }
local STYLE_BOTTOM = { position = "absolute", bottom = 24, left = 0, right = 0, display = "flex", flexDirection = "column", alignItems = "center" }
local STYLE_BIG    = { fontSize = 56, fontStyle = "bold" }
local STYLE_SMALL  = { fontSize = 14, color = "rgba(255,255,255,0.5)" }

-- fresh-props mode: allocate a shallow copy of a module constant (mimics the inline
-- style-table literals in racehud.luau, which produce a new table every render).
local function fresh(copyOf)
    local t = {}
    for k, v in pairs(copyOf) do
        t[k] = v
    end
    return t
end

local function PowerDamageBars(freshProps)
    local power, setPower = useState(1)
    local damage, setDamage = useState(0)
    useEffect(function()
        return UiLib.onEvent("race:hudState", function(data)
            if data ~= nil then
                setPower(data.power)
                setDamage(data.damage)
            end
        end)
    end, {})
    return x("view") { style = freshProps and fresh(STYLE_PDB) or STYLE_PDB,
        x("view") { style = freshProps and fresh(STYLE_CARD) or STYLE_CARD,
            x("text") { style = freshProps and fresh(STYLE_LABEL) or STYLE_LABEL, "Power" },
            x("view") { style = freshProps and fresh(STYLE_BAR) or STYLE_BAR },
        },
        x("view") { style = freshProps and fresh(STYLE_CARD) or STYLE_CARD,
            x("text") { style = freshProps and fresh(STYLE_LABEL) or STYLE_LABEL, "Damage" },
            x("view") { style = freshProps and fresh(STYLE_BAR) or STYLE_BAR },
        },
    }
end

local function Splits(freshProps)
    local lapDiff, setLapDiff = useState(0)
    local lastLap, setLastLap = useState(0)
    useEffect(function()
        return UiLib.onEvent("race:hudState", function(data)
            if data ~= nil then
                setLapDiff(data.lapDiffMs or 0)
                setLastLap(data.lastLapDiffMs or 0)
            end
        end)
    end, {})
    return x("view") { style = freshProps and fresh(STYLE_BOTTOM) or STYLE_BOTTOM,
        x("text") { style = freshProps and fresh(STYLE_BIG) or STYLE_BIG, ("%d"):format(lapDiff) },
        x("text") { style = freshProps and fresh(STYLE_SMALL) or STYLE_SMALL, ("%d"):format(lastLap) },
    }
end

local function Speed(freshProps)
    local speed, setSpeed = useState(0)
    useEffect(function()
        return UiLib.onEvent("race:hudState", function(data)
            if data ~= nil then
                setSpeed(data.speed)
            end
        end)
    end, {})
    local speedKmh = speed * 1.4 * 21.0 * 60.0 * 60.0 / 100000.0
    return x("view") { style = freshProps and fresh(STYLE_BOTTOM) or STYLE_BOTTOM,
        x("text") { style = freshProps and fresh(STYLE_BIG) or STYLE_BIG, ("%d"):format(math.floor(speedKmh + 0.5)) },
        x("text") { style = freshProps and fresh(STYLE_SMALL) or STYLE_SMALL, "KM/H" },
    }
end

local function RaceHud(freshProps)
    local position, setPosition = useState(1)
    local total, setTotal = useState(1)
    local countdown, setCountdown = useState(0)
    local stateText, setStateText = useState("")
    useEffect(function()
        return UiLib.onEvent("race:hudState", function(data)
            if data ~= nil then
                setPosition(data.position)
                setTotal(data.totalRacers)
                setCountdown(data.countdownTimer)
                setStateText(data.stateText)
            end
        end)
    end, {})
    return x("view") { style = freshProps and fresh(STYLE_ROOT) or STYLE_ROOT,
        x(PowerDamageBars) { fresh = freshProps },
        x(Splits) { fresh = freshProps },
        x(Speed) { fresh = freshProps },
        x("view") { style = freshProps and fresh(STYLE_BOTTOM) or STYLE_BOTTOM,
            x("text") { style = freshProps and fresh(STYLE_BIG) or STYLE_BIG, ("%d/%d"):format(position, total) },
            x("text") { style = freshProps and fresh(STYLE_SMALL) or STYLE_SMALL, "POSITION" },
        },
    }
end

local function run(frames, freshProps)
    freshProps = freshProps and true or false
    frames = frames or 100

    local root = UiLib.createRoot()
    React.render(x(RaceHud) { fresh = freshProps }, root)
    UiLib.setActiveRoot(root)

    -- Warm-up: a few frames so the mount effects (UiLib.onEvent subscriptions) are
    -- established (they run deferred, on the first flush).
    for i = 1, 3 do
        __bench_push("race:hudState", { speed = 50, power = 0.8, damage = 0.1, lapDiffMs = 100, lastLapDiffMs = 50, position = 1, totalRacers = 1, countdownTimer = 0, stateText = "" })
        __bench_flush()
    end

    -- Count how much of the tree the reconciler actually walks per frame, without
    -- touching vendored preact-luau: wrap the options._diff / options._render hooks
    -- (the hooks addon's wrappers are already in place and are chained through).
    local opt = React.options
    local prevDiff = opt._diff
    local prevRender = opt._render
    local diffCount = 0
    local renderCount = 0
    opt._diff = function(vnode)
        diffCount = diffCount + 1
        if prevDiff then prevDiff(vnode) end
    end
    opt._render = function(vnode)
        renderCount = renderCount + 1
        if prevRender then prevRender(vnode) end
    end

    -- Per-component renderComponent / process() counters (guarded by _G.__bench_rr).
    local rr = {
        count = 0,
        processCount = 0,
        names = {},
        detail = {},
        typeNames = {
            [PowerDamageBars] = "PowerDamageBars",
            [Splits] = "Splits",
            [Speed] = "Speed",
            [RaceHud] = "RaceHud",
            [React.Fragment] = "Fragment",
        },
    }
    _G.__bench_rr = rr

    __bench_reset_stats()
    local t0 = os.clock()
    for i = 1, frames do
        -- Fresh payload every frame, like a freshly-built HudStateData each frame.
        -- speed/power/damage/lapDiff all move in timetrial, so all three subscribed
        -- components stay dirty every frame (the measured timetrial case).
        __bench_push("race:hudState", {
            speed = i,
            power = 0.8 + (i % 5) * 0.01,
            damage = 0.1 + (i % 7) * 0.005,
            lapDiffMs = i,
            lastLapDiffMs = 50,
            position = 1,
            totalRacers = 1,
            countdownTimer = 0,
            stateText = "",
        })
        __bench_flush()
    end
    local t1 = os.clock()

    _G.__bench_rr = nil
    return t1 - t0, diffCount, renderCount, rr.count, rr.processCount, rr.names, rr.detail
end

return run
