-- Sx renderer — fine-grained rendering onto the UiLib/Yoga host.
--
-- Mirrors dom-expressions: `render` installs a tree once; dynamic text/children are
-- wired through effects (per-slot), so a single signal write results in ONE host
-- commitTextUpdate/setProperty instead of a full-tree re-render.
--
-- Components run ONCE under their own owner (createRoot). They return a descriptor
-- (built by h.x) which is mounted. Reactivity comes from signals/memos read inside the
-- tree, and from function-valued children/props which the renderer installs reactively.
--
-- Flow components (Show/Switch/Match/For/Index) live here to avoid a require cycle.

local Sx = require('./signals')
local Host = require('./host')

local Dom = {}

-- ------------------------------------------------------------------ markers

local FLOW = {} -- unique marker for flow descriptors

-- ------------------------------------------------------------------ small helpers

local function isNode(v)
    return type(v) == "table" and v.type ~= nil
end

local function isEventKey(key)
    return string.sub(key, 1, 2) == "on"
end

local function toAccessor(v)
    if type(v) == "function" then
        return v
    end
    return function()
        return v
    end
end

local function toValueOrCall(v)
    if type(v) == "function" then
        return v()
    end
    return v
end

local function insertInto(parent, node, marker)
    if marker ~= nil then
        Host.insertBefore(parent, node, marker)
    else
        Host.appendChild(parent, node)
    end
end

local function removeNode(parent, node)
    Host.removeChild(parent, node)
end

local function createAnchor(parent)
    local anchor = Host.createTextInstance("")
    Host.appendChild(parent, anchor)
    return anchor
end

-- ------------------------------------------------------------------ mount result

-- A mount result is { nodes = {hostNode,...}, dispose = fn }.

local function emptyMount()
    return { nodes = {}, dispose = function() end }
end

-- ------------------------------------------------------------------ mountValue / mountNode (mutually recursive)

local mountNode
local mountValueInto
local insertDynamic
local mountFlow

-- Mount a static value (text / descriptor / array) into `parent` before `marker`.
-- A function value is mounted reactively (dynamic accessor).
mountValueInto = function(parent, value, marker)
    if value == nil then
        return nil
    end
    local t = type(value)
    if t == "string" or t == "number" then
        local node = Host.createTextInstance(tostring(value))
        insertInto(parent, node, marker)
        return {
            nodes = { node },
            dispose = function()
                removeNode(parent, node)
            end,
        }
    elseif t == "function" then
        return insertDynamic(parent, value, marker)
    elseif t == "table" then
        if isNode(value) then
            return mountNode(value, parent, marker)
        end
        -- array of children
        local nodes = {}
        local disposes = {}
        for i = 1, #value do
            local m = mountValueInto(parent, value[i], marker)
            if m then
                for _, n in ipairs(m.nodes) do
                    nodes[#nodes + 1] = n
                end
                disposes[#disposes + 1] = m.dispose
            end
        end
        return {
            nodes = nodes,
            dispose = function()
                for i = #disposes, 1, -1 do
                    disposes[i]()
                end
            end,
        }
    end
    return nil
end

-- Dynamic child slot: an accessor is installed inside an effect. Fast path keeps a
-- single text node (the anchor) updated via commitTextUpdate; structural values are
-- mounted before the anchor and swapped atomically.
insertDynamic = function(parent, accessor, marker)
    local anchor
    local ownsAnchor = false
    if marker ~= nil then
        anchor = marker
    else
        anchor = Host.createTextInstance("")
        Host.appendChild(parent, anchor)
        ownsAnchor = true
    end
    local currentText = ""
    local currentNodes = nil
    local currentDispose = nil

    local eff = Sx.createEffect(function()
        local value = accessor()
        local t = type(value)
        if t == "string" or t == "number" then
            local str = tostring(value)
            if currentNodes == nil then
                if str ~= currentText then
                    Host.commitTextUpdate(anchor, currentText, str)
                    currentText = str
                end
            else
                if currentDispose then
                    currentDispose()
                else
                    for _, n in ipairs(currentNodes) do
                        removeNode(parent, n)
                    end
                end
                currentNodes = nil
                currentDispose = nil
                Host.commitTextUpdate(anchor, currentText, str)
                currentText = str
            end
        elseif value ~= nil and t == "table" then
            local m = mountValueInto(parent, value, anchor)
            if currentNodes ~= nil then
                if currentDispose then
                    currentDispose()
                else
                    for _, n in ipairs(currentNodes) do
                        removeNode(parent, n)
                    end
                end
            end
            if currentText ~= "" then
                Host.commitTextUpdate(anchor, currentText, "")
                currentText = ""
            end
            currentNodes = m and m.nodes or nil
            currentDispose = m and m.dispose or nil
        else
            -- nil
            if currentNodes ~= nil then
                if currentDispose then
                    currentDispose()
                else
                    for _, n in ipairs(currentNodes) do
                        removeNode(parent, n)
                    end
                end
                currentNodes = nil
                currentDispose = nil
            end
            if currentText ~= "" then
                Host.commitTextUpdate(anchor, currentText, "")
                currentText = ""
            end
        end
    end)

    local dispose = function()
        eff()
        if currentNodes ~= nil then
            if currentDispose then
                currentDispose()
            else
                for _, n in ipairs(currentNodes) do
                    removeNode(parent, n)
                end
            end
            currentNodes = nil
        end
        if ownsAnchor then
            removeNode(parent, anchor)
        end
    end

    return {
        nodes = { anchor },
        dispose = dispose,
    }
end

-- Mount a host element: createInstance with static/event props, wire reactive
-- (function-valued, non-`on`) props through effects, mount children.
local function mountHostElement(descriptor, parent, marker)
    local staticProps = {}
    local reactiveProps = {}
    for key, val in pairs(descriptor.props) do
        if type(val) == "function" then
            if isEventKey(key) then
                -- `on*` props are event handlers: wired ONCE at createInstance (the host
                -- replaces, never +=). Not reactive accessors.
                staticProps[key] = val
            else
                reactiveProps[key] = val
            end
        else
            staticProps[key] = val
        end
    end

    local el = Host.createInstance(descriptor.type, staticProps)
    insertInto(parent, el, marker)

    -- reactive non-event props -> per-prop effect -> setProperty
    for key, accessor in pairs(reactiveProps) do
        Sx.createEffect(function()
            Host.setProperty(el, key, accessor())
        end)
    end

    -- children
    local childDisposes = {}
    for i = 1, #descriptor.children do
        local child = descriptor.children[i]
        local m
        if type(child) == "function" then
            m = insertDynamic(el, child, nil)
        else
            m = mountValueInto(el, child, nil)
        end
        if m then
            childDisposes[#childDisposes + 1] = m.dispose
        end
    end

    local dispose = function()
        for i = #childDisposes, 1, -1 do
            childDisposes[i]()
        end
        removeNode(parent, el)
    end

    return {
        nodes = { el },
        dispose = dispose,
    }
end

-- Mount a component: run it once under its own owner (createRoot); its returned
-- descriptor is mounted; disposal tears down the owner (all its effects/cleanups).
local function mountComponent(descriptor, parent, marker)
    local fn = descriptor.type
    local compProps = {}
    for k, v in pairs(descriptor.props) do
        compProps[k] = v
    end
    compProps.children = descriptor.children

    local rootDispose = nil
    local mounted = nil
    Sx.createRoot(function(dispose)
        rootDispose = dispose
        local node = fn(compProps)
        if node ~= nil then
            mounted = mountNode(node, parent, marker)
        end
        return node
    end)

    local dispose = function()
        if mounted then
            mounted.dispose()
            mounted = nil
        end
        if rootDispose then
            rootDispose()
            rootDispose = nil
        end
    end

    return {
        nodes = mounted and mounted.nodes or {},
        dispose = dispose,
    }
end

-- Dispatch a descriptor to host element / flow / component.
mountNode = function(descriptor, parent, marker)
    local t = descriptor.type
    if type(t) == "string" then
        return mountHostElement(descriptor, parent, marker)
    elseif t == FLOW then
        return mountFlow(descriptor, parent, marker)
    else
        return mountComponent(descriptor, parent, marker)
    end
end

-- ------------------------------------------------------------------ flow components

local FlowKind = {
    Show = "show",
    Switch = "switch",
    Match = "match",
    For = "for",
    Index = "index",
}

local function Show(props)
    return { type = FLOW, kind = FlowKind.Show, props = props, children = props.children }
end

local function Switch(props)
    return { type = FLOW, kind = FlowKind.Switch, props = props, children = props.children }
end

local function Match(props)
    return { type = FLOW, kind = FlowKind.Match, props = props, children = props.children }
end

local function For(props)
    return { type = FLOW, kind = FlowKind.For, props = props, children = props.children }
end

local function Index(props)
    return { type = FLOW, kind = FlowKind.Index, props = props, children = props.children }
end

local function disposeMount(m)
    if m then
        if m.dispose then
            m.dispose()
        else
            -- unreachable; kept defensive
        end
    end
end

-- Show: mounts `children` when `when` is truthy, else `fallback`.
local function mountShow(props, children, parent, marker)
    local when = toAccessor(props.when)
    local fallback = props.fallback
    local anchor = marker or createAnchor(parent)
    local current = nil

    local eff = Sx.createEffect(function()
        local w = when()
        local newMount
        if w then
            newMount = mountValueInto(parent, children, anchor)
        elseif fallback ~= nil then
            newMount = mountValueInto(parent, toValueOrCall(fallback), anchor)
        end
        disposeMount(current)
        current = newMount
    end)

    local dispose = function()
        eff()
        disposeMount(current)
        if marker == nil then
            removeNode(parent, anchor)
        end
    end

    return { nodes = { anchor }, dispose = dispose }
end

-- Switch: mounts the children of the FIRST Match whose `when` is truthy.
local function mountSwitch(props, children, parent, marker)
    local anchor = marker or createAnchor(parent)
    local current = nil

    local eff = Sx.createEffect(function()
        local chosen = nil
        for i = 1, #children do
            local matchDesc = children[i]
            local isMatch = (matchDesc.type == Match) or
                (matchDesc.type == FLOW and matchDesc.kind == FlowKind.Match)
            if isMatch then
                local w = matchDesc.props.when
                local wv
                if type(w) == "function" then
                    wv = w()
                else
                    wv = w
                end
                if wv then
                    chosen = matchDesc
                    break
                end
            end
        end
        local newMount
        if chosen then
            newMount = mountValueInto(parent, chosen.children, anchor)
        end
        disposeMount(current)
        current = newMount
    end)

    local dispose = function()
        eff()
        disposeMount(current)
        if marker == nil then
            removeNode(parent, anchor)
        end
    end

    return { nodes = { anchor }, dispose = dispose }
end

local function rowKey(item, index, keyed)
    if keyed then
        return item
    end
    return index
end

-- For (keyed) / Index (positional): mounts rows from `each`; rows are rebuilt only when
-- the list's keys change, so per-row updates flow through signals read inside row content.
local function mountList(props, children, parent, marker, keyed)
    local each = toAccessor(props.each)
    local fallback = props.fallback
    local rowFn = children[1]
    local anchor = marker or createAnchor(parent)

    local rows = {}
    local currentFallback = nil

    local eff = Sx.createEffect(function()
        local list = each() or {}
        local n = #list

        if n == 0 then
            if #rows > 0 or currentFallback ~= nil then
                for _, row in ipairs(rows) do
                    row.dispose()
                end
                rows = {}
                disposeMount(currentFallback)
                currentFallback = nil
                if fallback ~= nil then
                    currentFallback = mountValueInto(parent, toValueOrCall(fallback), anchor)
                end
            end
            return
        end

        -- Fast path: same keys in the same order -> nothing structural changes.
        if #rows == n then
            local same = true
            for i = 1, n do
                if rows[i].key ~= rowKey(list[i], i, keyed) then
                    same = false
                    break
                end
            end
            if same then
                return
            end
        end

        -- Rebuild rows (reuses nothing when keys move; correct, and only on list change).
        disposeMount(currentFallback)
        currentFallback = nil
        for _, row in ipairs(rows) do
            row.dispose()
        end
        rows = {}

        for i = 1, n do
            local item = list[i]
            local itemAccessor = Sx.createMemo(function()
                return item
            end)
            local descriptor
            if rowFn ~= nil then
                descriptor = rowFn(itemAccessor, i - 1)
            else
                descriptor = children[i]
            end
            local m = mountValueInto(parent, descriptor, anchor)
            rows[i] = {
                key = rowKey(item, i, keyed),
                dispose = m and m.dispose or function() end,
            }
        end
    end)

    local dispose = function()
        eff()
        for _, row in ipairs(rows) do
            row.dispose()
        end
        rows = {}
        disposeMount(currentFallback)
        if marker == nil then
            removeNode(parent, anchor)
        end
    end

    return { nodes = { anchor }, dispose = dispose }
end

mountFlow = function(descriptor, parent, marker)
    local kind = descriptor.kind
    if kind == FlowKind.Show or kind == FlowKind.Match then
        return mountShow(descriptor.props, descriptor.children, parent, marker)
    elseif kind == FlowKind.Switch then
        return mountSwitch(descriptor.props, descriptor.children, parent, marker)
    elseif kind == FlowKind.For then
        return mountList(descriptor.props, descriptor.children, parent, marker, true)
    elseif kind == FlowKind.Index then
        return mountList(descriptor.props, descriptor.children, parent, marker, false)
    end
    error("Sx: unknown flow kind " .. tostring(kind))
end

-- ------------------------------------------------------------------ public

-- Install a descriptor tree into a fresh host root and activate it.
local function render(rootNode)
    local rootView = Host.createRoot()
    Sx.batch(function()
        mountNode(rootNode, rootView, nil)
    end)
    Sx.flushSync()
    Host.setActiveRoot(rootView)
    return rootView
end

-- Wire the end-of-frame scheduler: UiLib.defer (via Host.defer) coalesces all signal
-- writes in a frame into ONE effect pass, exactly like the game's per-frame batching.
-- Tests use a synchronous fake defer; a consumer can opt out with Sx.setScheduler(nil).
Sx.setScheduler(Host.defer)

Dom.render = render
Dom.Show = Show
Dom.Switch = Switch
Dom.Match = Match
Dom.For = For
Dom.Index = Index
Dom.mountNode = mountNode
Dom.mountValueInto = mountValueInto

return Dom
