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

---@class TextStyles : Styles
---@field color? string
---@field stroke? string
---@field fontFamily? string
---@field fontSize? string
---@field fontStyle? string
---@field verticalAlign? "top"|"bottom"|"middle"
---@field horizontalAlign? "left"|"right"|"center"

---@class Styles
---@field direction? "ltr"|"rtl"|"inherit"
---@field flexDirection? "row"|"row-reverse"|"column"|"column-reverse"
---@field justifyContent? "flex-start"|"center"|"flex-end"|"space-between"|"space-around"|"space-evenly"
---@field alignItems? "flex-start"|"center"|"flex-end"|"stretch"|"baseline"|"space-between"|"space-around"|"space-evenly"
---@field alignSelf? "auto"|"flex-start"|"center"|"flex-end"|"stretch"|"baseline"
---@field alignContent? "flex-start"|"center"|"flex-end"|"stretch"|"baseline"
---@field position? "static"|"relative"|"absolute"
---@field flexWrap? "nowrap"|"wrap"|"wrap-reverse"
---@field overflow? "visible"|"hidden"|"scroll"
---@field display? "flex"|"none"|"contents"
---@field boxSizing? "border-box"|"content-box"
---@field visibility? "visible"|"hidden"|"collapse"
---@field flex? number|nil
---@field flexGrow? number|nil
---@field flexShrink? number|nil
---@field flexBasis? string|nil
---@field left? string|"undefined"|"auto" accepts % and px
---@field top? string|"undefined"|"auto" accepts % and px
---@field right? string|"undefined"|"auto" accepts % and px
---@field bottom? string|"undefined"|"auto" accepts % and px
---@field marginTop? string|number|"undefined"|"auto" accepts % and px
---@field marginBottom? string|number|"undefined"|"auto" accepts % and px
---@field marginLeft? string|number|"undefined"|"auto" accepts % and px
---@field marginRight? string|number|"undefined"|"auto" accepts % and px
---@field margin? string|number|"undefined"|"auto" accepts % and px
---@field paddingTop? string|number|"undefined" accepts % and px
---@field paddingBottom? string|number|"undefined" accepts % and px
---@field paddingLeft? string|number|"undefined" accepts % and px
---@field paddingRight? string|number|"undefined" accepts % and px
---@field padding? string|number|"undefined" accepts % and px
---@field borderTopWidth? string|number|"undefined"|"none" accepts px
---@field borderBottomWidth? string|number|"undefined"|"none" accepts px
---@field borderLeftWidth? string|number|"undefined"|"none" accepts px
---@field borderRightWidth? string|number|"undefined"|"none" accepts px
---@field borderWidth? string|number|"undefined"|"none" accepts px
---@field borderTopLeftRadius? string|number accepts px
---@field borderTopRightRadius? string|number accepts px
---@field borderBottomLeftRadius? string|number accepts px
---@field borderBottomRightRadius? string|number accepts px
---@field borderRadius? string|number accepts px

---@class ViewProps
---@field style? Styles
---@field onanimationframebegan? fun(): nil
---@field active? boolean
---@field focused? boolean
---@field taborder? integer
---@field onmousedown? fun(event: MouseEvent): nil
---@field onmouseup? fun(event: MouseEvent): nil
---@field onmousedrag? fun(event: MouseDragEvent): nil
---@field onmousescroll? fun(event: MouseWheelEvent): nil
---@field onmousemove? fun(event: MouseMoveEvent): nil
---@field onmouseenter? fun(event: MouseMoveEvent): nil
---@field onmouseleave? fun(event: MouseMoveEvent): nil
---@field onkeytype? fun(event: KeyboardTypingEvent): nil
---@field onkeypress? fun(event: KeyboardEvent): nil
---@field onkeyup? fun(event: KeyboardEvent): nil

---@class ImageProps : ViewProps
---@field src? string
---@field scale? number

---@class TextProps : ViewProps
---@field style? TextStyles

---@class TextInputStyles : TextStyles
---@field cursorColor? string
---@field selectionColor? string
---@field placeholderColor? string

---@class TextInputProps : ViewProps
---@field style? TextInputStyles
---@field value? string
---@field placeholder? string
---@field onsubmit? fun(value: string): nil
---@field onchange? fun(value: string): nil

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
---@overload fun(vtype: "view", props: ViewProps, ...: Child|Children): VNode
---@overload fun(vtype: "text", props: TextProps, ...: Child|Children): VNode
---@overload fun(vtype: "image", props: ImageProps, ...: Child|Children): VNode
---@overload fun(vtype: "textinput", props: TextInputProps, ...: Child|Children): VNode
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
