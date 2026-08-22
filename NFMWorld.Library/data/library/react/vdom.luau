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

local R = require('./types')

local vdom = {}

--- Marker type used for text VNodes.
vdom.TEXT = {}

--- Marker type used for fragments (`React.Fragment`). Renders nothing;
--- children render in place.
vdom.FRAGMENT = {}

--- Internal marker type wrapping function children (Context.Consumer).
vdom.FUNCTION = {}

--- Metatable marking tables that were produced by `h` as VNodes.
local VNODE_MT = { __reactVNode = true }

--- Wrap a freshly built VNode so `h` can recognize it later.
local function markVNode(node: R.VNode): R.VNode
  setmetatable(node, VNODE_MT)
  return (node :: any) :: R.VNode
end

local function isVNode(value: any): boolean
  return type(value) == "table" and getmetatable(value) == VNODE_MT
end

--- Flatten a child (or a nested array of children) into `out` as VNodes.
--- Strings and numbers become text VNodes; nil / true / false are skipped.
local function flattenInto(out: Array<R.VNode>, child: R.VNode|string|number|boolean|nil|Array<R.VNode>)
  if child == nil or child == false or child == true then
    return
  elseif type(child) == "string" or type(child) == "number" then
    out[#out + 1] = markVNode({ type = vdom.TEXT, props = { text = tostring(child) } })
  elseif type(child) == "function" then
    -- function-as-child (used by Context.Consumer); wrapped so child lists
    -- only ever contain VNodes
    out[#out + 1] = markVNode({ type = vdom.FUNCTION, props = { fn = child } })
  elseif type(child) == "table" then
    if (child :: R.VNode).type ~= nil then
      out[#out + 1] = child :: R.VNode -- already a VNode
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
function vdom.normalize(result: R.VNode|string|number|boolean|nil|Array<R.VNode>): Array<R.VNode>
  local out = {}
  flattenInto(out, result)
  return out
end

type Styles = {
  direction: "ltr" | "rtl" | "inherit" | nil,
  flexDirection: "row" | "row-reverse" | "column" | "column-reverse" | nil,
  justifyContent: "flex-start" | "center" | "flex-end" | "space-between" | "space-around" | "space-evenly" | nil,
  alignItems: "flex-start" | "center" | "flex-end" | "stretch" | "baseline" | "space-between" | "space-around" | "space-evenly" | nil,
  alignSelf: "auto" | "flex-start" | "center" | "flex-end" | "stretch" | "baseline" | nil,
  alignContent: "flex-start" | "center" | "flex-end" | "stretch" | "baseline" | nil,
  position: "static" | "relative" | "absolute" | nil,
  flexWrap: "nowrap" | "wrap" | "wrap-reverse" | nil,
  overflow: "visible" | "hidden" | "scroll" | nil,
  display: "flex" | "none" | "contents" | nil,
  boxSizing: "border-box" | "content-box" | nil,
  visibility: "visible" | "hidden" | "collapse" | nil,
  flex: number | nil,
  flexGrow: number | nil,
  flexShrink: number | nil,
  flexBasis: string | nil,
  --- accepts % and px
  left: string | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  top: string | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  right: string | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  bottom: string | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  marginTop: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  marginBottom: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  marginLeft: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  marginRight: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  margin: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  paddingTop: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  paddingBottom: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  paddingLeft: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  paddingRight: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts % and px
  padding: string | number | "undefined" | "auto" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderTopWidth: string | number | "undefined" | "none" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderBottomWidth: string | number | "undefined" | "none" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderLeftWidth: string | number | "undefined" | "none" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderRightWidth: string | number | "undefined" | "none" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderWidth: string | number | "undefined" | "none" | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderTopLeftRadius: string | number | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderTopRightRadius: string | number | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderBottomLeftRadius: string | number | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderBottomRightRadius: string | number | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
  --- accepts px
  borderRadius: string | number | "left" | "right" | "center" | "justify" | "safe" | "unsafe" | nil,
}

type TextStyles = Styles & {
  color: string?,
  stroke: string?,
  fontFamily: string?,
  fontSize: string?,
  fontStyle: string?,
  verticalAlign: "top" | "bottom" | "middle",
  horizontalAlign: "left" | "right" | "center",
}

type ViewProps = {
  style: Styles?,
  onanimationframebegan: (() -> nil)?,
  active: boolean?,
  focused: boolean?,
  taborder: integer?,
  onmousedown: ((event: MouseEvent) -> nil)?,
  onmouseup: ((event: MouseEvent) -> nil)?,
  onmousedrag: ((event: MouseDragEvent) -> nil)?,
  onmousescroll: ((event: MouseWheelEvent) -> nil)?,
  onmousemove: ((event: MouseMoveEvent) -> nil)?,
  onmouseenter: ((event: MouseMoveEvent) -> nil)?,
  onmouseleave: ((event: MouseMoveEvent) -> nil)?,
  onkeytype: ((event: KeyboardTypingEvent) -> nil)?,
  onkeypress: ((event: KeyboardEvent) -> nil)?,
  onkeyup: ((event: KeyboardEvent) -> nil)?,
  children: R.Child?,
}

type ImageProps = ViewProps & {
  src: string?,
  scale: number?,
}

type TextProps = ViewProps & {
  style: TextStyles?,
}

type TextInputStyles = TextStyles & {
  cursorColor: string?,
  selectionColor: string?,
  placeholderColor: string?,
}

type TextInputProps = ViewProps & {
  style: TextInputStyles?,
  value: string?,
  placeholder: string?,
  onsubmit: ((value: string) -> nil)?,
  onchange: ((value: string) -> nil)?,
}

export type React_StatelessFunctionalComponent<Props> = (
	props: Props,
	context: any
) -> R.VNode

type h
  = (<P>((vtype: React_StatelessFunctionalComponent<P>, props: P, ...R.Child) -> R.VNode) -> R.VNode)
  & ((vtype: "view", props: ViewProps, ...R.Child) -> R.VNode)
  & ((vtype: "text", props: TextProps, ...R.Child) -> R.VNode)
  & ((vtype: "image", props: ImageProps, ...R.Child) -> R.VNode)
  & ((vtype: "textinput", props: TextInputProps, ...R.Child) -> R.VNode)

--- Create a virtual element.
---
---     h("view", { id = "x" }, "hello")
---     h("view", { id = "x" }, h("text", nil, "hi"))
---     h("view", nil, { "a", "b" })            -- array child is flattened
---
--- The second argument is optional. If it is a child (string, number, VNode
--- or array of children) it is shifted into the children list.
vdom.h = (function(vtype, props, ...)
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
end) :: h

return vdom
