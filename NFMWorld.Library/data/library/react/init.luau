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

local vdom = require('./react/vdom')
local core = require('./react/core')

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

React.View = 'view'
React.Text = 'text'
React.Image = 'image'
React.TextInput = 'textinput'

return React
