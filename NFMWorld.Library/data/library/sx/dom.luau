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
local createRoot = Host.createRoot
local createInstance = Host.createInstance
local createTextInstance = Host.createTextInstance
local appendChild = Host.appendChild
local insertBefore = Host.insertBefore
local removeChild = Host.removeChild
local setProperty = Host.setProperty
local commitTextUpdate = Host.commitTextUpdate
local setActiveRoot = Host.setActiveRoot

local Dom = {}

local types = require('./types')

-- ------------------------------------------------------------------ devtools (component tree)
-- Optional component-tree capture for the Sx devtools. Inert (nil-checks only) unless
-- `Dom.devtools.enable()` is called. `DevStack` tracks the current mount path so each
-- component / host element / flow registers itself as a child of its parent.

local DevTools = { enabled = false, root = nil }
local DevStack = {}

-- Format "source:line" for a devtools call site. A call site captured at the top of a
-- module (e.g. a `styled(...)` definition) resolves to the module's MAIN chunk frame,
-- which has `linedefined == 0` — fall back to `currentline` (the active line) so styled
-- definitions still show their source line. `src` alone is the last resort.
local function formatCallSite(src: any, linedefined: any, currentline: any): string
	if type(src) ~= "string" or src == "" then
		return ""
	end
	if type(linedefined) == "number" and linedefined > 0 then
		return src .. ":" .. linedefined
	end
	if type(currentline) == "number" and currentline > 0 then
		return src .. ":" .. currentline
	end
	return src
end

local function devName(fn: any): string
    -- Cheap guard: when devtools is off we never use the name, so don't spend a pcall.
    if not DevTools.enabled then
        return ""
    end
    if type(fn) == "string" then
        return fn
    end

    local lineNumber = ''
    if Sx.debug.getCallSite(fn) then
        local info = Sx.debug.getCallSite(fn)
        lineNumber = formatCallSite(info.short_src, info.linedefined, info.currentline)
    else
        local ok2, info = pcall(debug.getinfo, fn)
        if ok2 and type(info) == "table" then
            lineNumber = formatCallSite(info.short_src, info.linedefined, info.currentline)
        end
    end

    -- Styled components register a readable name (they are anonymous closures, so
    -- debug.info can't name them). Looked up via a weak-keyed registry because
    -- Lua-CSharp functions can't hold properties.
    local sxName = Sx.debug.styledName(fn)
    if type(sxName) == "string" and sxName ~= "" then
        return sxName .. ' (' .. lineNumber .. ')'
    end
    -- Lua-CSharp's debug.info(fn, "n") returns the function's stored declaration name
    -- (recorded on the prototype at parse time). Fall back to the definition site
    -- (source + line) via debug.getinfo if the function is unnamed.
    local ok, name = pcall(debug.info, fn, "n")
    if ok and type(name) == "string" and name ~= "" and name ~= "?" then
        return name .. ' (' .. lineNumber .. ')'
    end
    return "anonymous"
end

local function devPush(kind: string, name: string): any
    if not DevTools.enabled then
        return nil
    end
    local parent = DevStack[#DevStack]
    local node = { name = name, kind = kind, children = {}, parent = parent }
    if parent then
        parent.children[#parent.children + 1] = node
    elseif DevTools.root == nil then
        DevTools.root = node
    end
    DevStack[#DevStack + 1] = node
    return node
end

local function devPop()
    DevStack[#DevStack] = nil
end

local function devRemove(node: any)
    if node == nil then
        return
    end
    node.children = {}
    if node.parent then
        local pc = node.parent.children
        for i = 1, #pc do
            if pc[i] == node then
                table.remove(pc, i)
                break
            end
        end
        node.parent = nil
    end
    if DevTools.root == node then
        DevTools.root = nil
    end
end

-- ------------------------------------------------------------------ markers

local FLOW = {} -- unique marker for flow descriptors
local Fragment = {} -- unique marker for fragment descriptors (mounts children inline)

-- ------------------------------------------------------------------ types

-- A mount result: the host nodes inserted for a mounted subtree plus a dispose function
-- that tears them down (including any reactive effects).
export type MountResult = {
    nodes: { any },
    dispose: () -> (),
}

-- ------------------------------------------------------------------ small helpers

local function isNode<T>(v: T): boolean
    return type(v) == "table" and v.type ~= nil
end

local function isEventKey(key: string): boolean
    return string.sub(key, 1, 2) == "on"
end

local function toAccessor<T>(v: T | (() -> T)): (() -> T)
    if type(v) == "function" then
        return v
    end
    return function()
        return v
    end
end

local function toValueOrCall<T>(v: T | (() -> T)): T
    if type(v) == "function" then
        return v()
    end
    return v
end

local function insertInto(parent: any, node: any, marker: any?): ()
    if marker ~= nil then
        insertBefore(parent, node, marker)
    else
        appendChild(parent, node)
    end
end

local function removeNode(parent: any, node: any): ()
    removeChild(parent, node)
end

local function createAnchor(parent: any): any
    local anchor = createTextInstance("")
    appendChild(parent, anchor)
    return anchor
end

-- ------------------------------------------------------------------ mount result

-- A mount result is { nodes = {hostNode,...}, dispose = fn }.

local function emptyMount(): MountResult
    return { nodes = {}, dispose = function() end }
end

-- ------------------------------------------------------------------ mountValue / mountNode (mutually recursive)

local mountNode: (descriptor: types.NodeDescriptor, parent: any, marker: any?) -> MountResult
local mountValueInto: (parent: any, value: any, marker: any?) -> MountResult?
local insertDynamic: (parent: any, accessor: (() -> any), marker: any?) -> MountResult
local mountFlow: (descriptor: types.NodeDescriptor, parent: any, marker: any?) -> MountResult

-- Mount a static value (text / descriptor / array) into `parent` before `marker`.
-- A function value is mounted reactively (dynamic accessor).
mountValueInto = function(parent: any, value: any, marker: any?): MountResult?
    if value == nil then
        return nil
    end
    local t = type(value)
    if t == "string" or t == "number" then
        local node = createTextInstance(tostring(value))
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
insertDynamic = function(parent: any, accessor: (() -> any), marker: any?): MountResult
    local anchor
    local ownsAnchor = false
    if marker ~= nil then
        anchor = marker
    else
        anchor = createTextInstance("")
        appendChild(parent, anchor)
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
                    commitTextUpdate(anchor, currentText, str)
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
                commitTextUpdate(anchor, currentText, str)
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
                commitTextUpdate(anchor, currentText, "")
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
                commitTextUpdate(anchor, currentText, "")
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
local function mountHostElement(descriptor: types.NodeDescriptor, parent: any, marker: any?): MountResult
    local devNode = devPush("host", tostring(descriptor.type))
    local staticProps: { [string]: any } = {}
    local reactiveProps: { [string]: any } = {}
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

    local el = createInstance(descriptor.type, staticProps)
    insertInto(parent, el, marker)
    if devNode ~= nil then
        -- devtools: keep a live ref to the host node so the devtools can
        -- draw a highlight border over it when hovering its tree node.
        devNode.__host = el
    end

    -- reactive non-event props -> per-prop effect -> setProperty. The effect skips the
    -- host call when the accessor returns the same reference as last time, so stable
    -- (memoized/constant) props don't re-set on every recompute.
    for key, accessor in pairs(reactiveProps) do
        local last = nil
        Sx.createEffect(function()
            local v = accessor()
            if v ~= last then
                setProperty(el, key, v)
                last = v
            end
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
        devRemove(devNode)
    end

    devPop()
    return {
        nodes = { el },
        dispose = dispose,
    }
end

-- Mount a component: run it once under its own owner (createRoot); its returned
-- descriptor is mounted; disposal tears down the owner (all its effects/cleanups).
local function mountComponent(descriptor: types.NodeDescriptor, parent: any, marker: any?): MountResult
    local fn = descriptor.type
    local compProps = {}
    for k, v in pairs(descriptor.props) do
        compProps[k] = v
    end
    compProps.children = descriptor.children

    local name = devName(fn)
    local devNode = devPush("component", name)
    if DevTools.enabled then
        -- Tag the component's root computation so signals/memos created inside it can be
        -- attributed to this component in the devtools signals tab.
        Sx.debug.setPendingRootName(name)
    end

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

    devPop()

    local dispose = function()
        if mounted then
            mounted.dispose()
            mounted = nil
        end
        if rootDispose then
            rootDispose()
            rootDispose = nil
        end
        devRemove(devNode)
    end

    return {
        nodes = mounted and mounted.nodes or {},
        dispose = dispose,
    }
end

-- Dispatch a descriptor to host element / flow / fragment / component.
mountNode = function(descriptor: types.NodeDescriptor, parent: any, marker: any?): MountResult
    local t = descriptor.type
    if type(t) == "string" then
        return mountHostElement(descriptor, parent, marker)
    elseif t == FLOW then
        return mountFlow(descriptor, parent, marker)
    elseif t == Fragment then
        -- mount each child inline into `parent` (no wrapper host node)
        local nodes = {}
        local disposes = {}
        for i = 1, #descriptor.children do
            local m = mountValueInto(parent, descriptor.children[i], marker)
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
    else
        return mountComponent(descriptor, parent, marker)
    end
end

-- ------------------------------------------------------------------ flow components

local FlowKind_Show = "show"
local FlowKind_Switch = "switch"
local FlowKind_Match = "match"
local FlowKind_For = "for"
local FlowKind_Index = "index"

local function Show(props: { when: any | (() -> any), fallback: types.NodeDescriptor?, children: {types.NodeDescriptor} }): types.NodeDescriptor
    return { type = FLOW, kind = FlowKind_Show, props = props, children = props.children }
end

local function Switch(props: { children: {types.NodeDescriptor}? }): types.NodeDescriptor
    return { type = FLOW, kind = FlowKind_Switch, props = props, children = props.children }
end

local function Match(props: { when: boolean | (() -> boolean), children: {types.NodeDescriptor} }): types.NodeDescriptor
    return { type = FLOW, kind = FlowKind_Match, props = props, children = props.children }
end

local function For<T>(props: { each: {T} | (() -> {T}), children: (item: () -> T, index: number) -> types.NodeDescriptor }): types.NodeDescriptor
    return { type = FLOW, kind = FlowKind_For, props = props, children = props.children }
end

local function Index<T>(props: { of: {T} | (() -> {T}), children: (item: () -> T, index: number) -> types.NodeDescriptor }): types.NodeDescriptor
    return { type = FLOW, kind = FlowKind_Index, props = props, children = props.children }
end

local function disposeMount(m: MountResult?): ()
    if m then
        if m.dispose then
            m.dispose()
        else
            -- unreachable; kept defensive
        end
    end
end

-- Show: mounts `children` when `when` is truthy, else `fallback`.
local function mountShow(props: any, children: { any }, parent: any, marker: any?): MountResult
    local when = toAccessor(props.when)
    local fallback = props.fallback
    local anchor = marker or createAnchor(parent)
    local current: MountResult? = nil

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
local function mountSwitch(props: any, children: { any }, parent: any, marker: any?): MountResult
    local anchor = marker or createAnchor(parent)
    local current: MountResult? = nil

    local eff = Sx.createEffect(function()
        local chosen = nil
        for i = 1, #children do
            local matchDesc = children[i]
            local isMatch = (matchDesc.type == Match) or
                (matchDesc.type == FLOW and matchDesc.kind == FlowKind_Match)
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

local function rowKey(item: any, index: number, keyed: boolean): any
    if keyed then
        return item
    end
    return index
end

-- For (keyed) / Index (positional): mounts rows from `each`; rows are rebuilt only when
-- the list's keys change, so per-row updates flow through signals read inside row content.
local function mountList(props: any, children: { any }, parent: any, marker: any?, keyed: boolean): MountResult
    local each = toAccessor(props.each)
    local fallback = props.fallback
    local rowFn = children[1]
    local anchor = marker or createAnchor(parent)

    local rows: { { key: any, dispose: () -> () } } = {}
    local currentFallback: MountResult? = nil

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

mountFlow = function(descriptor: types.NodeDescriptor, parent: any, marker: any?): MountResult
    local kind = descriptor.kind
    local devNode = devPush("flow", kind or "flow")
    local result
    if kind == FlowKind_Show or kind == FlowKind_Match then
        result = mountShow(descriptor.props, descriptor.children, parent, marker)
    elseif kind == FlowKind_Switch then
        result = mountSwitch(descriptor.props, descriptor.children, parent, marker)
    elseif kind == FlowKind_For then
        result = mountList(descriptor.props, descriptor.children, parent, marker, true)
    elseif kind == FlowKind_Index then
        result = mountList(descriptor.props, descriptor.children, parent, marker, false)
    else
        devPop()
        error("Sx: unknown flow kind " .. tostring(kind))
    end
    devPop()
    local origDispose = result.dispose
    result.dispose = function()
        origDispose()
        devRemove(devNode)
    end
    return result
end

-- ------------------------------------------------------------------ public

-- Install a descriptor tree into a fresh host root and activate it.
local function render(rootNode: types.NodeDescriptor): any
    local rootView = createRoot()
    Sx.batch(function()
        mountNode(rootNode, rootView, nil)
    end)
    Sx.flushSync()
    setActiveRoot(rootView)
    return rootView
end

-- Wire the end-of-frame scheduler: UiLib.defer (via Host.defer) coalesces all signal
-- writes in a frame into ONE effect pass, exactly like the game's per-frame batching.
-- Tests use a synchronous fake defer; a consumer can opt out with Sx.setScheduler(nil).
Sx.setScheduler(Host.defer)

Dom.render = render
Dom.Fragment = Fragment
Dom.Show = Show
Dom.Switch = Switch
Dom.Match = Match
Dom.For = For
Dom.Index = Index
Dom.mountNode = mountNode
Dom.mountValueInto = mountValueInto

Dom.devtools = {
    enable = function()
        DevTools.enabled = true
    end,
    disable = function()
        DevTools.enabled = false
    end,
    snapshot = function(): any?
        if DevTools.root == nil then
            return nil
        end
        local function walk(node: any): any
            local out: any = { name = node.name, kind = node.kind, children = {} }
            if node.__host ~= nil then
                out.host = node.__host
            end
            local kids = node.children
            for i = 1, #kids do
                out.children[i] = walk(kids[i])
            end
            return out
        end
        return walk(DevTools.root)
    end,
}

return Dom
