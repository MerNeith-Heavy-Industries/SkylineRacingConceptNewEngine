--!strict
local React = require("../../library/react")
local dom = require("../../library/uilib-dom")
local useState = React.useState
local useEffect = React.useEffect
local useCallback = React.useCallback
local useRef = React.useRef
local useSyncExternalStore = React.useSyncExternalStore
local useContext = React.useContext
local h = React.h
local View = React.View
local Text = React.Text
local Image = React.Image
local TextInput = React.TextInput

local function MainMenu()
	return h(View, {
		style = {
			flexDirection = "row",
			alignItems = "stretch",
			justifyContent = "center",
			width = 100,
			height = 100,
		},
		children = {},
	})
end

dom.render(h(MainMenu, {}))
