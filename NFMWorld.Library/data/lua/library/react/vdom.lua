--- Virtual DOM: element creation (`h`) and child normalization.
---
--- A VNode is a plain table produced by `h`:
---
---     { type = <tag | component | Fragment | TEXT>, props = { children = {...}, ... }, key = ... }
---
--- Text children (strings / numbers) are normalized into text VNodes:
---
---     { type = TEXT, props = { text = "..." } }
---
--- VNodes created by `h` carry a metatable so they can be told apart from
--- arbitrary user tables (e.g. `{ type = "text" }` used as props for
--- `<input type="text">`).

local vdom = {}

--- Marker type used for text VNodes.
---@type table
vdom.TEXT = {}

--- Marker type used for fragments (`React.Fragment`). Renders nothing;
--- children render in place.
---@type table
vdom.FRAGMENT = {}

--- Internal marker type wrapping function children (Context.Consumer).
---@type table
vdom.FUNCTION = {}

---@class VNodeProps
---@field children? VNode[]     -- normalized child list (absent on text VNodes)
---@field ref any?              -- ref object (`{ current = ... }`) or function
---@field key any?              -- reconciliation key
---@field fn? function          -- function payload of a vdom.FUNCTION VNode
---@field [string] any

---@class VNode
---@field type string|function|table  -- tag string, component function, or marker table
---@field props VNodeProps
---@field key any?

---@alias Component fun(props: VNodeProps): (VNode|string|number|boolean|nil|VNode[])
---@alias ElementType string|Component|table
---@alias Child VNode|string|number|boolean|nil|fun(value: any): any -- functions are allowed as Context.Consumer children
---@alias Children Child|Child[]

--- Metatable marking tables that were produced by `h` as VNodes.
---@type table
local VNODE_MT = { __reactVNode = true }

--- Wrap a freshly built VNode so `h` can recognize it later.
---@param node VNode
---@return VNode
local function markVNode(node)
  setmetatable(node, VNODE_MT)
  return node
end

---@param value any
---@return boolean
local function isVNode(value)
  return type(value) == "table" and getmetatable(value) == VNODE_MT
end

--- Flatten a child (or a nested array of children) into `out` as VNodes.
--- Strings and numbers become text VNodes; nil / true / false are skipped.
---@param out VNode[]
---@param child any
local function flattenInto(out, child)
  if child == nil or child == false or child == true then
    return
  elseif type(child) == "string" or type(child) == "number" then
    out[#out + 1] = markVNode({ type = vdom.TEXT, props = { text = tostring(child) } })
  elseif type(child) == "function" then
    -- function-as-child (used by Context.Consumer); wrapped so child lists
    -- only ever contain VNodes
    out[#out + 1] = markVNode({ type = vdom.FUNCTION, props = { fn = child } })
  elseif type(child) == "table" then
    if child.type ~= nil then
      out[#out + 1] = child -- already a VNode
    else
      for i = 1, #child do -- nested array of children
        flattenInto(out, child[i])
      end
    end
  else
    error("React.h: invalid child of type '" .. type(child) .. "'", 3)
  end
end

--- Normalize a component's return value into a flat list of VNodes.
---@param result VNode|string|number|boolean|nil|VNode[]
---@return VNode[]
function vdom.normalize(result)
  local out = {}
  flattenInto(out, result)
  return out
end

--- Create a virtual element.
---
---     h("div", { id = "x" }, "hello")
---     h("div", { id = "x" }, h("span", nil, "hi"))
---     h("div", nil, { "a", "b" })            -- array child is flattened
---
--- The second argument is optional. If it is a child (string, number, VNode
--- or array of children) it is shifted into the children list.
---@param vtype ElementType
---@param props VNodeProps|Child|Children?
---@param ... Child|Children
---@return VNode
function vdom.h(vtype, props, ...)
  local children = { ... }
  local count = select("#", ...)

  if props ~= nil then
    local pt = type(props)
    if pt ~= "table" then
      -- string / number / boolean child
      table.insert(children, 1, props)
      count = count + 1
      props = nil
    elseif isVNode(props) then
      table.insert(children, 1, props)
      count = count + 1
      props = nil
    elseif props.type == nil and #props > 0 then
      -- array of children passed as the second argument
      table.insert(children, 1, props)
      count = count + 1
      props = nil
    end
    -- otherwise: a plain props table ({}, { id = "x" }, { type = "text" }, ...)
  end

  local flat = {}
  for i = 1, count do
    flattenInto(flat, children[i])
  end

  local p = props or {}
  ---@cast p VNodeProps
  if count > 0 then
    p.children = flat
  elseif p.children ~= nil then
    -- children passed through props: normalize them in place
    local normalized = {}
    flattenInto(normalized, p.children)
    p.children = normalized
  else
    p.children = flat
  end

  return markVNode({ type = vtype, props = p, key = p.key })
end

return vdom
