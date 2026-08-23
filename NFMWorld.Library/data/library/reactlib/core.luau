--- The hooks engine + fiber reconciler. All of the actual React-clone logic
--- lives here.
---
--- How it works
--- ------------
--- This is a small, synchronous, double-buffered fiber reconciler:
---
---   * The element tree is described with VNodes (see `react/vdom.lua`).
---   * The committed work lives in a tree of `Fiber`s rooted at a host root
---     fiber whose `stateNode` is the container.
---   * An update builds a fresh work-in-progress tree. Every new fiber points
---     at its previous committed counterpart via `alternate` (which is where
---     hook state is read from).
---   * `workLoop` walks the new tree: function components are invoked
---     (running hooks), host elements diff props, and children are
---     reconciled by key / index.
---   * A single `commit` pass applies deletions, insertions and passive
---     effects to the host tree.
---   * State updates flush synchronously; there is no scheduler. A per-root
---     pass counter detects infinite render loops and aborts gracefully.

local R = require('./types')
local vdom = require("./vdom")

local core = {}

--------------------------------------------------------------------------
-- Host config
--------------------------------------------------------------------------

local host: R.HostConfig

function core.setHostConfig(config: R.HostConfig)
  local required = {
    "createInstance", "createTextInstance", "appendChild",
    "insertBefore", "removeChild", "setProperty", "commitTextUpdate",
  }
  for _, name in ipairs(required) do
    if type(config[name]) ~= "function" then
      error("React.setHostConfig: missing required host function '" .. name .. "'", 2)
    end
  end
  host = config
  return config
end

--------------------------------------------------------------------------
-- Render context (rendering is synchronous, so module locals are safe)
--------------------------------------------------------------------------

local roots: { [any]: R.Root } = {}

local wipFiber: R.Fiber? = nil

local currentFiber: R.Fiber? = nil -- fiber whose hooks are currently running
local currentHookIndex = 0

local contextStack: Array<{ctx: R.Context<any>, value: any}> = {}

-- Forward declarations: hooks capture these before their definitions below.
local scheduleUpdate: ((fiber: R.Fiber) -> nil)

---@type fun(root: Root)
local flushRoot

---@param msg string
local function warn(msg)
  io.stderr:write("[React] " .. msg .. "\n")
end

--- Maximum render passes per synchronous flush before we assume an
--- infinite render loop and abort.
local MAX_RENDERS = 50

--------------------------------------------------------------------------
-- Hooks
--------------------------------------------------------------------------

local function getHook(): R.Hook
  local fiber = assert(currentFiber, "Hooks can only be called while a component is rendering")
  currentHookIndex = currentHookIndex + 1
  local hooks = fiber.hooks or {}
  local hook = hooks[currentHookIndex]
  if hook == nil then
    local oldHooks = fiber.alternate and fiber.alternate.hooks
    hook = (oldHooks and oldHooks[currentHookIndex]) or {}
    hooks[currentHookIndex] = hook
    fiber.hooks = hooks
  end
  return hook
end

--- Shallow value comparison used for hook dependency arrays.
local function shallowEqual<T>(a: T, b: T): boolean
  if a == b then return true end
  if type(a) ~= "table" or type(b) ~= "table" then return false end
  for k, v in pairs(a) do
    if b[k] ~= v then return false end
  end
  for k in pairs(b) do
    if a[k] == nil then return false end
  end
  return true
end

--- useState(initial) -> state, setState
---
--- `initial` may be a lazy initializer function. `setState` accepts either a
--- value or a function `(prevState) -> nextState`. Unlike React, updates are
--- flushed synchronously (no batching outside of renders).
---@generic T
---@param initial T
---@return T state
---@return fun(action?: T|fun(prev: T): T) setState
function core.useState<T>(initial: T): (T, ((T|((T) -> T))?) -> ())
  local hook = getHook()
  if not hook.isStateHook then
    hook.isStateHook = true
    hook.state = (type(initial) == "function") and initial() or initial
  elseif hook.queue ~= nil then
    local state = hook.state
    local queue = assert(hook.queue, "no hook.queue")
    hook.queue = nil
    for i = 1, #queue do
      local action = queue[i]
      if type(action) == "function" then
        state = action(state)
      else
        state = action
      end
    end
    hook.state = state
  end
  local fiber = assert(currentFiber, "no fiber in useState")
  return hook.state, function(action)
    if action ~= nil then
      hook.queue = hook.queue or {}
      hook.queue[#hook.queue + 1] = action
    end
    scheduleUpdate(fiber)
  end
end

--- useEffect(fn, deps?)
---
--- `fn` runs after every commit for which `deps` changed (or every commit if
--- `deps` is nil). `fn` may return a cleanup function; the previous cleanup
--- runs immediately before the next run of the same effect.
function core.useEffect(fn: () -> () | () -> (() -> ()) | nil, deps: Array<any>?)
  local hook = getHook()
  local changed = deps == nil or hook.effectFn == nil or not shallowEqual(hook.deps, deps)
  if changed then
    hook.deps = deps
    hook.effectFn = fn
    hook.effectDirty = true
  else
    hook.effectDirty = false
  end
end

--- useMemo(fn, deps?) -> memoized value
function core.useMemo<T>(fn: (() -> T), deps: Array<any>?): T
  local hook = getHook()
  if deps == nil or not shallowEqual(hook.deps, deps) then
    hook.deps = deps
    hook.state = fn()
  end
  return hook.state
end

--- useCallback(fn, deps?) -> memoized function
function core.useCallback<T>(fn: T, deps: Array<any>?): T
  local hook = getHook()
  if deps == nil or not shallowEqual(hook.deps, deps) then
    hook.deps = deps
    hook.state = fn
  end
  return hook.state
end

--- useRef(initial) -> `{ current = initial }`
---
--- The returned table keeps its identity across renders.
function core.useRef<T>(initial: T): { current: T }
  local hook = getHook()
  if hook.ref == nil then
    hook.ref = { current = initial }
  end
  return hook.ref
end

--- useSyncExternalStore(subscribe, getSnapshot) -> current snapshot
---
--- `subscribe(listener)` is called once on mount and may return an
--- unsubscribe function (invoked on unmount). The snapshot is re-read on
--- every render and compared with `==`.
function core.useSyncExternalStore<T>(subscribe: (listener: () -> nil) -> (() -> nil)?, getSnapshot: () -> T): T
  local hook = getHook()
  if hook.subscribe == nil then
    hook.subscribe = subscribe
    hook.getSnapshot = getSnapshot
    hook.state = getSnapshot()
    local fiber = assert(currentFiber, "no fiber in useSyncExternalStore")
    local unsubscribe = subscribe(function()
      scheduleUpdate(fiber)
    end)
    if type(unsubscribe) == "function" then
      hook.unsubscribe = unsubscribe
    end
  end
  local snapshot = getSnapshot()
  if snapshot ~= hook.state then
    hook.state = snapshot
  end
  return hook.state
end

--- useContext(ctx) -> current context value
---
--- Reads the innermost enclosing Provider's value, or the default value.
function core.useContext<T>(context: R.Context<T>): T
  for i = #contextStack, 1, -1 do
    local entry = contextStack[i]
    if entry.ctx == context then
      return entry.value
    end
  end
  return context.defaultValue
end

function core.createContext<T>(defaultValue: T): R.Context<T>
  local ctx = { defaultValue = defaultValue }
  ctx.Provider = { __reactProvider = ctx }
  ctx.Consumer = { __reactConsumer = ctx }
  return ctx
end

--- Render a function component, running its hooks against `fiber`.
local function renderWithHooks(component: R.Component, fiber: R.Fiber, props: R.VNodeProps): any
  local prevFiber, prevIndex = currentFiber, currentHookIndex
  currentFiber = fiber
  currentHookIndex = 0
  fiber.hooks = {}
  local result = component(props)
  currentFiber, currentHookIndex = prevFiber, prevIndex
  return result
end

--------------------------------------------------------------------------
-- Fibers
--------------------------------------------------------------------------

local function tagOf(vtype: any): string
  if vtype == vdom.TEXT then return "text" end
  if vtype == vdom.FRAGMENT then return "fragment" end
  if vtype == vdom.FUNCTION then
    error("React: function children are only valid inside Context.Consumer", 2)
  end
  local t = type(vtype)
  if t == "function" then return "function" end
  if t == "string" then return "host" end
  if t == "table" then
    if vtype.__reactProvider then return "provider" end
    if vtype.__reactConsumer then return "consumer" end
    return "host"
  end
  error("React: invalid element type '" .. t .. "'", 2)
end

local function createHostRootFiber(container: any): R.Fiber
  return {
    type = nil, tag = "root", props = {}, key = nil,
    stateNode = container, parent = nil, child = nil, sibling = nil,
    alternate = nil, placement = false, update = false, deletions = nil,
    hooks = nil, index = 0, context = nil
  }
end

local function createFiber(vnode: R.VNode, parent: R.Fiber?, old: R.Fiber?): R.Fiber
  local fiber: R.Fiber = {
    type = vnode.type,
    tag = tagOf(vnode.type),
    props = vnode.props,
    key = vnode.key,
    stateNode = nil,
    parent = parent,
    child = nil,
    sibling = nil,
    alternate = old or nil,
    placement = old == nil,
    update = false,
    deletions = nil,
    hooks = nil,
    index = 0,
    context = nil,
  }
  if old ~= nil then
    fiber.stateNode = old.stateNode
    old.alternate = fiber
  end
  if fiber.tag == "provider" or fiber.tag == "consumer" then
    fiber.context = (vnode.type :: any).__reactProvider or (vnode.type :: any).__reactConsumer
  end
  return fiber
end

local function markDeletion(fiber: R.Fiber, deleted: R.Fiber)
  local deletions = fiber.deletions or {}
  fiber.deletions = deletions
  deletions[#deletions + 1] = deleted
end

--- Reconcile `children` against the fiber's previous child list, producing
--- the new child fiber chain. Keyed moves are detected via lastPlacedIndex.
local function reconcileChildren(fiber: R.Fiber, children: Array<R.VNode>)
  local alternate = fiber.alternate

  -- index old children by key (unkeyed children fall back to their position)
  local existing = {} ---@type table<any, Fiber>
  local oldChild = alternate and alternate.child or nil
  local pos = 0
  while oldChild ~= nil do
    pos = pos + 1
    local k = oldChild.key
    if k == nil then k = pos end
    existing[k] = oldChild
    oldChild = oldChild.sibling
  end

  local lastPlacedIndex = 0
  local firstChild, prevChild = nil, nil

  for i, vnode in ipairs(children) do
    local k = vnode.key
    if k == nil then k = i end
    local old = existing[k]
    existing[k] = nil

    local newFiber
    if old ~= nil and old.type == vnode.type then
      newFiber = createFiber(vnode, fiber, old)
      if old.index < lastPlacedIndex then
        newFiber.placement = true -- moved; needs reinsertion
      end
      if old.index > lastPlacedIndex then
        lastPlacedIndex = old.index
      end
    else
      if old ~= nil then
        markDeletion(fiber, old)
      end
      newFiber = createFiber(vnode, fiber, nil) -- placement = true
    end

    newFiber.index = i
    if prevChild == nil then firstChild = newFiber else prevChild.sibling = newFiber end
    prevChild = newFiber
  end

  -- anything left in `existing` no longer exists in the new element tree
  for _, old in pairs(existing) do
    markDeletion(fiber, old)
  end

  fiber.child = firstChild
end

local function propsDiffer(oldProps: { [string]: any }, newProps: { [string]: any }): boolean
  for k, v in pairs(oldProps) do
    if k ~= "children" and k ~= "key" and k ~= "ref" then
      if newProps[k] ~= v then return true end
    end
  end
  for k in pairs(newProps) do
    if k ~= "children" and k ~= "key" and k ~= "ref" then
      if oldProps[k] == nil then return true end
    end
  end
  return false
end

local function diffProps(oldProps: { [string]: any }, newProps: { [string]: any }, instance: any)
  for k, v in pairs(newProps) do
    if k ~= "children" and k ~= "key" and k ~= "ref" then
      if oldProps[k] ~= v then
        host.setProperty(instance, k, v)
      end
    end
  end
  for k in pairs(oldProps) do
    if k ~= "children" and k ~= "key" and k ~= "ref" then
      if newProps[k] == nil then
        host.setProperty(instance, k, nil) -- remove
      end
    end
  end
end

local function attachRef(fiber: R.Fiber)
  local ref = fiber.props and fiber.props.ref
  if ref == nil then return end
  if type(ref) == "function" then
    ref(fiber.stateNode)
  elseif type(ref) == "table" then
    ref.current = fiber.stateNode
  end
end

local function detachRef(fiber: R.Fiber)
  local ref = fiber.props and fiber.props.ref
  if ref == nil then return end
  if type(ref) == "function" then
    ref(nil)
  elseif type(ref) == "table" then
    ref.current = nil
  end
end

--------------------------------------------------------------------------
-- Render phase: begin / complete work
--------------------------------------------------------------------------

local function beginWork(fiber: R.Fiber)
  local tag = fiber.tag
  if tag == "root" then
    local element = fiber.pendingElement
    reconcileChildren(fiber, element and { element } or {})
  elseif tag == "function" then
    local result = renderWithHooks(fiber.type :: R.Component, fiber, fiber.props)
    reconcileChildren(fiber, vdom.normalize(result))
  elseif tag == "provider" then
    contextStack[#contextStack + 1] = { ctx = fiber.context, value = fiber.props.value }
    reconcileChildren(fiber, fiber.props.children or {})
  elseif tag == "consumer" then
    local children = fiber.props.children or {}
    local first = children[1]
    local renderFn = first and first.props.fn
    if type(renderFn) ~= "function" then
      error("React: Context.Consumer expects a function as its child", 2)
    end
    local value = core.useContext(assert(fiber.context, "no fiber context in consumer"))
    reconcileChildren(fiber, vdom.normalize(renderFn(value)))
  elseif tag == "fragment" then
    reconcileChildren(fiber, fiber.props.children or {})
  elseif tag == "host" then
    local alternate = fiber.alternate
    if alternate ~= nil and propsDiffer(alternate.props, fiber.props) then
      fiber.update = true
    end
    reconcileChildren(fiber, fiber.props.children or {})
  elseif tag == "text" then
    local alternate = fiber.alternate
    if alternate ~= nil and alternate.props.text ~= fiber.props.text then
      fiber.update = true
    end
  end
end

local function completeWork(fiber: R.Fiber)
  local tag = fiber.tag
  if tag == "provider" then
    table.remove(contextStack)
  elseif tag == "host" then
    -- Note: a moved fiber keeps its existing stateNode; only truly new
    -- fibers create a host instance.
    if fiber.stateNode == nil then
      fiber.stateNode = host.createInstance(fiber.type :: string, fiber.props)
    else
      local alternate = fiber.alternate
      if fiber.update and alternate ~= nil then
        diffProps(alternate.props, fiber.props, fiber.stateNode)
      end
      if alternate ~= nil and alternate.props.ref ~= fiber.props.ref then
        detachRef(alternate)
        attachRef(fiber)
      end
    end
  elseif tag == "text" then
    if fiber.stateNode == nil then
      fiber.stateNode = host.createTextInstance(fiber.props.text)
    elseif fiber.update then
      local alternate = fiber.alternate
      if alternate ~= nil then
        host.commitTextUpdate(fiber.stateNode, alternate.props.text, fiber.props.text)
      end
    end
  end
end

local function performUnitOfWork(fiber: R.Fiber): R.Fiber?
  beginWork(fiber)
  if fiber.child ~= nil then
    return fiber.child
  end
  local nextFiber = fiber
  while nextFiber ~= nil do
    completeWork(nextFiber)
    if nextFiber.sibling ~= nil then
      return nextFiber.sibling
    end
    nextFiber = nextFiber.parent
  end
  return nil
end

local function workLoop()
  while wipFiber ~= nil do
    wipFiber = performUnitOfWork(wipFiber)
  end
end

--------------------------------------------------------------------------
-- Commit phase
--------------------------------------------------------------------------

--- Nearest host ancestor's stateNode (container for host roots).
local function nearestHostAncestor(fiber: R.Fiber): any?
  local p = fiber.parent
  while p ~= nil do
    if p.tag == "host" or p.tag == "root" then
      return p.stateNode
    end
    p = p.parent
  end
  return nil
end

--- Find the committed host node this placement should be inserted before,
--- or nil to append at the end.
local function getHostSibling(fiber: R.Fiber): any?
  local node = fiber
  while true do
    -- climb until we find a sibling, or hit a host parent boundary
    while node.sibling == nil do
      local parent = node.parent
      if parent == nil or parent.tag == "host" or parent.tag == "root" then
        return nil
      end
      node = parent
    end
    node = node.sibling
    -- descend through non-host fibers to the first host node
    while node.tag ~= "host" and node.tag ~= "text" do
      if node.placement then
        break -- not committed yet; try the next sibling
      end
      if node.child == nil then
        break
      end
      node = node.child
    end
    if node.tag == "host" or node.tag == "text" then
      if not node.placement then
        return node.stateNode
      end
    end
  end
end

--- For a host/text fiber inside a deleted subtree: return the stateNode it
--- should be removed from, or nil if it is nested under another host node
--- that is itself being removed.
local function hostParentOutside(fiber: R.Fiber, deletedRoot: R.Fiber): any?
  local p = fiber.parent
  while p ~= nil and p ~= deletedRoot do
    if p.tag == "host" or p.tag == "root" then
      return nil -- a host ancestor inside the deleted subtree is also being removed
    end
    p = p.parent
  end
  if p == nil then return nil end
  if deletedRoot.tag == "host" or deletedRoot.tag == "text" then
    return nil -- the deleted root itself is a host node; its subtree goes with it
  end
  p = deletedRoot.parent
  while p ~= nil do
    if p.tag == "host" or p.tag == "root" then
      return p.stateNode
    end
    p = p.parent
  end
  return nil
end

--- Unmount a deleted subtree: run effect cleanups (children before
--- parents), unsubscribe stores, detach refs, and remove host nodes
--- exactly once.
local function unmountSubtree(deletedRoot: R.Fiber)
  local stack: Array<R.Fiber> = {}
  local node = deletedRoot
  while true do
    if node ~= nil then
      stack[#stack + 1] = node
      node = node.child
    else
      if #stack == 0 then break end
      local fiber = stack[#stack]
      ---@cast fiber Fiber
      stack[#stack] = nil
      local tag = fiber.tag

      if tag == "function" and fiber.hooks ~= nil then
        for i = 1, #fiber.hooks do
          local hook = fiber.hooks[i]
          if hook ~= nil then
            if hook.destroy ~= nil then
              hook.destroy()
              hook.destroy = nil
            end
            if hook.unsubscribe ~= nil then
              hook.unsubscribe()
              hook.unsubscribe = nil
            end
          end
        end
      end

      if (tag == "host" or tag == "text") and fiber.stateNode ~= nil then
        detachRef(fiber)
        local parentStateNode
        if fiber == deletedRoot then
          parentStateNode = nearestHostAncestor(fiber)
        else
          parentStateNode = hostParentOutside(fiber, deletedRoot)
        end
        if parentStateNode ~= nil then
          host.removeChild(parentStateNode, fiber.stateNode)
        end
      end

      node = fiber.sibling
    end
  end
end

local function commitDeletions(rootFiber: R.Fiber)
  local stack = { rootFiber }
  while #stack > 0 do
    local fiber = stack[#stack]
    stack[#stack] = nil
    local deletions = fiber.deletions
    if deletions ~= nil then
      for i = 1, #deletions do
        unmountSubtree(deletions[i])
      end
      fiber.deletions = nil
    end
    local child = fiber.child
    while child ~= nil do
      stack[#stack + 1] = child
      child = child.sibling
    end
  end
end

local function commitPlacements(rootFiber: R.Fiber)
  local stack = { rootFiber }
  while #stack > 0 do
    local fiber = stack[#stack]
    stack[#stack] = nil

    if (fiber.tag == "host" or fiber.tag == "text") and fiber.placement then
      local parent = nearestHostAncestor(fiber)
      local before = getHostSibling(fiber)
      if before ~= nil then
        host.insertBefore(parent, fiber.stateNode, before)
      else
        host.appendChild(parent, fiber.stateNode)
      end
      attachRef(fiber)
    end

    -- push children in reverse so they are visited in order
    local children, child = {}, fiber.child
    while child ~= nil do
      children[#children + 1] = child
      child = child.sibling
    end
    for i = #children, 1, -1 do
      stack[#stack + 1] = children[i]
    end
  end
end

--- Run passive effects (useEffect), children before parents.
local function commitPassiveEffects(rootFiber: R.Fiber)
  local stack = {} ---@type Fiber[]
  local node = rootFiber ---@type Fiber?
  while true do
    if node ~= nil then
      stack[#stack + 1] = node
      node = node.child
    else
      if #stack == 0 then break end
      local fiber = stack[#stack]
      ---@cast fiber Fiber
      stack[#stack] = nil
      if fiber.tag == "function" and fiber.hooks ~= nil then
        for i = 1, #fiber.hooks do
          local hook = fiber.hooks[i]
          if hook ~= nil and hook.effectDirty then
            hook.effectDirty = false
            if hook.destroy ~= nil then
              hook.destroy()
              hook.destroy = nil
            end
            if hook.effectFn ~= nil then
              local cleanup = hook.effectFn()
              if type(cleanup) == "function" then
                hook.destroy = cleanup
              end
            end
          end
        end
      end
      node = fiber.sibling
    end
  end
end

local function commitRoot(wip: R.Fiber)
  commitDeletions(wip)
  commitPlacements(wip)
  commitPassiveEffects(wip)
end

--------------------------------------------------------------------------
-- Rendering / scheduling
--------------------------------------------------------------------------

local function performRender(root: R.Root)
  local current = root.current
  local wip = createHostRootFiber(root.container)
  wip.alternate = current
  current.alternate = wip
  wip.pendingElement = root.pendingElement

  root.rendering = true
  wipFiber = wip
  contextStack = {}

  local ok, err = xpcall(function()
    workLoop()
    commitRoot(wip)
  end, function(e)
    local dbg = rawget(_G, "debug")
    local msg
    if type(e) == "string" then
      msg = e
    elseif e == nil then
      msg = "<nil error>"
    else
      msg = tostring(e)
    end
    -- Managed exceptions already carry their full traceback; only wrap raw
    -- Lua error objects with the current stack trace.
    if dbg ~= nil and dbg.traceback ~= nil and msg:find("stack traceback", 1, true) == nil then
      return dbg.traceback(msg, 2)
    end
    return msg
  end)

  wipFiber = nil
  root.rendering = false

  if not ok then
    -- Keep the previous committed tree and surface the error to the caller.
    error(err, 0)
  end

  root.current = wip
end

scheduleUpdate = function(fiber: R.Fiber)
  local root = nil
  local p = fiber
  while p ~= nil do
    if p.tag == "root" then
      root = roots[p.stateNode]
      break
    end
    p = p.parent
  end
  if root == nil then
    return -- component was unmounted; ignore
  end
  root.dirty = true
  flushRoot(root)
end

flushRoot = function(root: R.Root)
  if root.rendering then
    return -- a pass is already in flight; its loop will pick up `dirty`
  end
  local passes = 0
  while root.dirty do
    passes = passes + 1
    if passes > MAX_RENDERS then
      warn("Aborting update: too many re-renders (possible infinite render loop).")
      root.dirty = false
      return
    end
    root.dirty = false
    performRender(root)
  end
end

--- Render a VNode into a host container. Subsequent calls with the same
--- container update the existing tree in place.
function core.render(element: R.VNode, container: any)
  if host == nil then
    error("React: setHostConfig() must be called before render()", 2)
  end
  local root = roots[container]
  if root == nil then
    root = {
      container = container,
      current = createHostRootFiber(container),
      pendingElement = nil,
      dirty = false,
      rendering = false,
    }
    roots[container] = root
  end
  root.pendingElement = element
  root.dirty = true
  flushRoot(root)
end

--- Remove the rendered tree from a container and run all cleanups.
function core.unmount(container: any)
  local root = roots[container]
  if root == nil then
    return
  end
  roots[container] = nil
  local child = root.current and root.current.child
  if child ~= nil then
    unmountSubtree(child)
  end
  root.current = nil
end

return core
