--- A tiny React / Preact-style UI library for Lua 5.1.
---
---     local React = require("react")
---     local h = React.h
---
---     React.setHostConfig({ ... })          -- plug in a DOM/RmlUI adapter
---     React.render(h(App), container)       -- mount / update
---     React.unmount(container)              -- tear down
---
--- Function components + hooks only. Rendering is synchronous: state updates
--- flush immediately (no scheduler, no time-slicing). See README.md for the
--- host config contract and the list of supported hooks.

local R = require("./reactlib/types")

local vdom = require("./reactlib/vdom")
local core = require("./reactlib/core")

local React = {}

React.version = "0.1.0"

--- Render-nothing element type; children render in place.
React.Fragment = vdom.FRAGMENT

--- Create a virtual element. See `react/vdom.lua`.
React.h = vdom.h
React.createElement = vdom.h

--- Install the host environment adapter (DOM/RmlUI). Required before render.
React.setHostConfig = core.setHostConfig

--- Mount `element` into `container` (updates in place on later calls).
React.render = core.render

--- Remove the tree rendered into `container`, running effect cleanups.
React.unmount = core.unmount

--- Create a context object: `{ defaultValue, Provider, Consumer }`.
React.createContext = core.createContext

React.useState = core.useState
React.useEffect = core.useEffect
React.useMemo = core.useMemo
React.useCallback = core.useCallback
React.useRef = core.useRef
React.useSyncExternalStore = core.useSyncExternalStore
React.useContext = core.useContext

-- Built-in components

export type Styles = {
	direction: "ltr" | "rtl" | "inherit" | nil,
	flexDirection: "row" | "row-reverse" | "column" | "column-reverse" | nil,
	justifyContent: "flex-start" | "center" | "flex-end" | "space-between" | "space-around" | "space-evenly" | nil,
	alignItems: "flex-start"
		| "center"
		| "flex-end"
		| "stretch"
		| "baseline"
		| "space-between"
		| "space-around"
		| "space-evenly"
		| nil,
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
	left: string | number | nil,
	--- accepts % and px
	top: string | number | nil,
	--- accepts % and px
	right: string | number | nil,
	--- accepts % and px
	bottom: string | number | nil,
	--- accepts % and px
	marginTop: string | number | "undefined" | "none" | "auto" | nil,
	--- accepts % and px
	marginBottom: string | number | "undefined" | "none" | "auto" | nil,
	--- accepts % and px
	marginLeft: string | number | "undefined" | "none" | "auto" | nil,
	--- accepts % and px
	marginRight: string | number | "undefined" | "none" | "auto" | nil,
	--- accepts % and px
	margin: string | number | "undefined" | "none" | "auto" | nil,
	--- accepts % and px
	paddingTop: string | number | "undefined" | "none" | nil,
	--- accepts % and px
	paddingBottom: string | number | "undefined" | "none" | nil,
	--- accepts % and px
	paddingLeft: string | number | "undefined" | "none" | nil,
	--- accepts % and px
	paddingRight: string | number | "undefined" | "none" | nil,
	--- accepts % and px
	padding: string | number | "undefined" | "none" | nil,
	--- accepts px
	borderTopWidth: string | number | "undefined" | "none" | nil,
	--- accepts px
	borderBottomWidth: string | number | "undefined" | "none" | nil,
	--- accepts px
	borderLeftWidth: string | number | "undefined" | "none" | nil,
	--- accepts px
	borderRightWidth: string | number | "undefined" | "none" | nil,
	--- accepts px
	borderWidth: string | number | "undefined" | "none" | nil,
	--- accepts % and px
	columnGap: string | number | "undefined" | nil,
	--- accepts % and px
	rowGap: string | number | "undefined" | nil,
	--- accepts % and px
	gap: string | number | "undefined" | nil,
	--- accepts % and px
	width: string | number | "undefined" | "auto" | "stretch" | "fit-content" | "max-content" | nil,
	--- accepts % and px
	height: string | number | "undefined" | "auto" | "stretch" | "fit-content" | "max-content" | nil,
	--- accepts % and px
	minWidth: string | number | "undefined" | "auto" | "stretch" | "fit-content" | "max-content" | nil,
	--- accepts % and px
	minHeight: string | number | "undefined" | "auto" | "stretch" | "fit-content" | "max-content" | nil,
	--- accepts % and px
	maxWidth: string | number | "undefined" | "auto" | "stretch" | "fit-content" | "max-content" | nil,
	--- accepts % and px
	maxHeight: string | number | "undefined" | "auto" | "stretch" | "fit-content" | "max-content" | nil,
	--- a/b or number
	aspectRatio: string | number | nil,
	borderColor: string | nil,
	backgroundColor: string | nil,
	--- accepts px
	borderTopLeftRadius: string | number | nil,
	--- accepts px
	borderTopRightRadius: string | number | nil,
	--- accepts px
	borderBottomLeftRadius: string | number | nil,
	--- accepts px
	borderBottomRightRadius: string | number | nil,
	--- accepts px
	borderRadius: string | number | nil,
}

export type TextStyles = Styles & {
	color: string?,
	stroke: string?,
	fontFamily: string?,
	fontSize: string | number | nil,
	fontStyle: string?,
	verticalAlign: "top" | "bottom" | "middle" | nil,
	textAlign: "left" | "right" | "center" | nil,
}

export type ViewProps = {
	key: any?,
	style: Styles?,
	onanimationframebegan: (() -> ())?,
	active: boolean?,
	focused: boolean?,
	taborder: integer?,
	onmousedown: ((event: MouseEvent) -> ())?,
	onmouseup: ((event: MouseEvent) -> ())?,
	onmousedrag: ((event: MouseDragEvent) -> ())?,
	onmousescroll: ((event: MouseWheelEvent) -> ())?,
	onmousemove: ((event: MouseMoveEvent) -> ())?,
	onmouseenter: ((event: MouseMoveEvent) -> ())?,
	onmouseleave: ((event: MouseMoveEvent) -> ())?,
	onkeytype: ((event: KeyboardTypingEvent) -> ())?,
	onkeypress: ((event: KeyboardEvent) -> ())?,
	onkeyup: ((event: KeyboardEvent) -> ())?,
	onfocus: (() -> ())?,
	onblur: (() -> ())?,
	children: R.Children?,
}

export type ImageProps = ViewProps & {
	src: string?,
	scale: number?,
}

export type TextProps = ViewProps & {
	style: TextStyles?,
}

export type TextInputStyles = TextStyles & {
	cursorColor: string?,
	selectionColor: string?,
	placeholderColor: string?,
}

export type TextInputProps = ViewProps & {
	style: TextInputStyles?,
	value: string?,
	placeholder: string?,
	onsubmit: ((value: string) -> nil)?,
	onchange: ((value: string) -> nil)?,
}

React.View = function(props: ViewProps, ...: R.Child): R.VNode
	return (vdom.h :: any)("view", props, ...) :: R.VNode
end
React.Text = function(props: TextProps, ...: R.Child): R.VNode
	return (vdom.h :: any)("text", props, ...) :: R.VNode
end
React.Image = function(props: ImageProps, ...: R.Child): R.VNode
	return (vdom.h :: any)("image", props, ...) :: R.VNode
end
React.TextInput = function(props: TextInputProps, ...: R.Child): R.VNode
	return (vdom.h :: any)("textinput", props, ...) :: R.VNode
end

export type StatelessFunctionalComponent<Props> = vdom.React_StatelessFunctionalComponent<Props>
export type VNode = R.VNode
export type ElementType = R.ElementType
export type Child = R.Child
export type Children = R.Children

-- P & { [number]: R.Child } will misbehave in Luau so we need a type function to augment P with a numeric indexer for children.
type function WithChildren(tbl, childType)
	local props = {}

	local function collect(t)
		if t:is("table") then
			for k, v in t:properties() do
				props[k] = v
			end
		elseif t:is("intersection") then
			for _, part in t:components() do
				collect(part)
			end
		else
			-- error("expected a table or intersection of tables")
			return
		end
	end

	collect(tbl)

	return types.newtable(props, { index = types.number, readresult = childType })
end

--- Luau DSL helper function to create a virtual element.
---
--- Use like:
---
---     return x(View) {
---       style = {
---         flexDirection = "row",
---         alignItems = "stretch",
---         justifyContent = "center",
---         width = 100,
---         height = 100,
---       },
---       x(TextInput) {
---         style = {
---           width = 100,
---           height = 20,
---         }
---       }
---     }
function React.x<P>(vtype: StatelessFunctionalComponent<P>): (WithChildren<P, R.Child>) -> R.VNode
	return function(props: WithChildren<P, R.Child>): R.VNode
		local children = {}
		local props1 = (props :: any) :: { [number]: R.Child } -- type fix
		for i = 1, #props1 do
			children[#children + 1] = props1[i] -- add to children
			props1[i] = nil -- remove from props so it doesn't get passed to the component
		end

		return (vdom.h :: any)(vtype, props, unpack(children)) :: R.VNode
	end
end

return React
