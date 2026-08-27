-- Reconciler-core microbenchmark: replicates the hot operations of preact-luau's
-- diffElementNodes / diffProps / diffChildren WITHOUT any C# host, so the same script
-- runs under both luau.exe and the Lua-CSharp VM (via the `vmcore` scenario) and the
-- VM gap on reconciler-style code is measurable apples-to-apples.
--
-- Operations modelled per iteration:
--   * build a `depth`-deep fresh VNode subtree (table construction, like a component
--     re-render producing new VNodes),
--   * diffProps each level (pairs() over props tables + reference-equality compares),
--   * walk the old/new chains (array indexing, string keys, `.type` dispatches).
--
-- Contract: returns run(iterations, depth) -> os.clock() CPU delta.

local STYLE_ROOT = { width = "100%", height = "100%", position = "relative" }

local function fresh(copyOf)
    local t = {}
    for k, v in pairs(copyOf) do
        t[k] = v
    end
    return t
end

local function diffProps(oldProps, newProps)
    local setProp = 0
    -- remove old props no longer present (diffElementNodes first loop)
    for i, oldValue in pairs(oldProps) do
        if i ~= "children" and newProps[i] == nil then
            setProp = setProp + 1
        end
    end
    -- set new props where the value reference changed (second loop)
    for i, newValue in pairs(newProps) do
        if i == "children" then
            -- skip
        elseif oldProps[i] ~= newValue then
            setProp = setProp + 1
        end
    end
    return setProp
end

local function buildVNode(type, props)
    return {
        type = type,
        props = props,
        _children = nil,
        _parent = nil,
        _depth = 0,
        _dom = nil,
        _component = nil,
        _original = 0,
        _index = 0,
        _flags = 0,
    }
end

local function build(depth, freshProps)
    if depth == 0 then
        -- text child: fresh string like ("%d"):format(...)
        return buildVNode(nil, tostring(depth))
    end
    return buildVNode("view", {
        style = freshProps and fresh(STYLE_ROOT) or STYLE_ROOT,
        children = build(depth - 1, freshProps),
    })
end

local function renderAndDiff(depth, freshProps)
    local old = build(depth, freshProps)
    local setProp = 0
    for _ = 1, depth do
        local nRoot = build(depth, freshProps)
        local o = old
        local n = nRoot
        while n ~= nil and n.type ~= nil do
            setProp = setProp + diffProps(o.props, n.props)
            o = o.props.children
            n = n.props.children
        end
        old = nRoot
    end
    return setProp
end

local function run(iterations, depth, freshProps)
    iterations = iterations or 1000
    depth = depth or 8
    freshProps = freshProps ~= nil and freshProps or true

    -- warmup
    renderAndDiff(depth, freshProps)

    local t0 = os.clock()
    local acc = 0
    for i = 1, iterations do
        acc = acc + renderAndDiff(depth, freshProps)
    end
    local t1 = os.clock()

    return t1 - t0, acc
end

return run
