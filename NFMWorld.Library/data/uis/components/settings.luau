--!strict

-- Settings view ported from nfm-world/data/html/src/pages/Settings.tsx.
-- Reusable component: rendered by the main menu and the pause menu.

local React = require("../../library/react")
local styled = require("../../library/styled")

local useState = React.useState
local useEffect = React.useEffect
local useCallback = React.useCallback
local useRef = React.useRef
local x = React.x
local View = React.View
local Text = React.Text

-- ── Helpers ──────────────────────────────────────────────────────

local function clamp01(v: number): number
	if v < 0 then return 0 end
	if v > 1 then return 1 end
	return v
end

local function map<T, V>(tbl: { T }, f: (T, number) -> V): { V }
	local t = {}
	for i = 1, #tbl do
		t[i] = f(tbl[i], i)
	end
	return t
end

local function keyCodeToName(code: number): string
	if code == 0 then return "None" end
	local names: { [number]: string } = {
		[0x08] = "Backspace", [0x09] = "Tab", [0x0d] = "Enter",
		[0x10] = "Shift", [0x11] = "Ctrl", [0x12] = "Alt",
		[0x1b] = "Escape", [0x20] = "Space",
		[0x21] = "PageUp", [0x22] = "PageDown", [0x23] = "End", [0x24] = "Home",
		[0x25] = "Left", [0x26] = "Up", [0x27] = "Right", [0x28] = "Down",
		[0x2c] = "PrintScreen", [0x2d] = "Insert", [0x2e] = "Delete",
		[0x5b] = "LWin", [0x5c] = "RWin",
		[0x70] = "F1", [0x71] = "F2", [0x72] = "F3", [0x73] = "F4",
		[0x74] = "F5", [0x75] = "F6", [0x76] = "F7", [0x77] = "F8",
		[0x78] = "F9", [0x79] = "F10", [0x7a] = "F11", [0x7b] = "F12",
		[0x90] = "NumLock", [0x91] = "ScrollLock",
		[0xa0] = "LShift", [0xa1] = "RShift",
		[0xa2] = "LCtrl", [0xa3] = "RCtrl",
		[0xa4] = "LAlt", [0xa5] = "RAlt",
		[0xba] = ";", [0xbb] = "=", [0xbc] = ",", [0xbd] = "-", [0xbe] = ".", [0xbf] = "/",
		[0xc0] = "`",
		[0xdb] = "[", [0xdc] = "\\", [0xdd] = "]", [0xde] = "'",
	}
	local name = names[code]
	if name ~= nil then return name end
	if code >= 0x41 and code <= 0x5a then return string.char(code) end
	if code >= 0x30 and code <= 0x39 then return string.char(code) end
	if code >= 0x60 and code <= 0x69 then return ("NumPad%d"):format(code - 0x60) end
	if code == 0x6a then return "NumPad*" end
	if code == 0x6b then return "NumPad+" end
	if code == 0x6d then return "NumPad-" end
	if code == 0x6e then return "NumPad." end
	if code == 0x6f then return "NumPad/" end
	return ("Key(0x%x)"):format(code)
end

local DISTANT_OUTLINE_BEHAVIORS = {
	"Distance Falloff (With Cutoff)",
	"Distance Falloff",
	"Classic Cutoff (NFM)",
	"Always Render",
	"Hide Outlines",
}

-- ── Styled primitives ────────────────────────────────────────────

local Root = styled(View) {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "column",
	backgroundColor = "rgba(0,0,0,0.65)",
}

local Header = styled(Text) {
	fontSize = 28,
	fontStyle = "bold",
	textAlign = "center",
	marginTop = 20,
}

local TabBar = styled(View) {
	display = "flex",
	flexDirection = "row",
	justifyContent = "center",
	gap = 4,
	paddingTop = 16,
	paddingLeft = 40,
	paddingRight = 40,
}

local Content = styled(View) {
	flex = 1,
	overflow = "scroll",
	paddingTop = 20,
	paddingLeft = 40,
	paddingRight = 40,
	paddingBottom = 10,
}

local BottomBar = styled(View) {
	display = "flex",
	flexDirection = "row",
	alignItems = "center",
	justifyContent = "center",
	gap = 12,
	paddingTop = 16,
	paddingBottom = 16,
	paddingLeft = 40,
	paddingRight = 40,
	borderTopWidth = 1,
	borderColor = "rgba(255,255,255,0.08)",
}

local SectionLabel = styled(Text) {
	fontSize = 11,
	fontStyle = "bold",
	color = "rgba(255,255,255,0.35)",
	marginBottom = 10,
}

local Row = styled(View) {
	display = "flex",
	flexDirection = "row",
	alignItems = "center",
	justifyContent = "space-between",
	gap = 16,
	paddingTop = 6,
	paddingBottom = 6,
}

local Label = styled(Text) {
	fontSize = 14,
	color = "rgba(255,255,255,0.8)",
	minWidth = 140,
}

local SliderValue = styled(Text) {
	fontSize = 12,
	color = "rgba(255,255,255,0.5)",
	marginLeft = 8,
	minWidth = 50,
}

local Divider = styled(View) {
	height = 1,
	backgroundColor = "rgba(255,255,255,0.06)",
	marginTop = 12,
	marginBottom = 12,
}

local ResetLink = styled(Text) {
	color = "rgba(255,255,255,0.35)",
	fontSize = 12,
	paddingTop = 8,
	paddingBottom = 8,
	hover = { color = "rgba(255,152,0,0.7)" },
}

local KeyBindingRow = styled(View) {
	display = "flex",
	flexDirection = "row",
	alignItems = "center",
	justifyContent = "space-between",
	gap = 12,
	paddingTop = 4,
	paddingBottom = 4,
}

local KeyBindingLabel = styled(Text) {
	fontSize = 13,
	color = "rgba(255,255,255,0.75)",
	minWidth = 160,
}

-- ── Interactive controls (approximations of HTML controls) ───────

local function TabBtn(props: { active: boolean?, label: string, onPress: () -> () }): React.VNode
	local active = props.active
	return x(View) {
		style = {
			paddingTop = 8,
			paddingBottom = 8,
			paddingLeft = 20,
			paddingRight = 20,
			backgroundColor = active and "rgba(79,195,247,0.18)" or "transparent",
			borderWidth = 1,
			borderColor = active and "rgba(79,195,247,0.35)" or "transparent",
			borderRadius = 6,
		},
		onmousedown = function() props.onPress() end,
		x(Text) {
			style = {
				fontSize = 13,
				fontStyle = "bold",
				color = active and "#fff" or "rgba(255,255,255,0.45)",
			},
			props.label,
		},
	}
end

local function Btn(props: { variant: "primary" | nil, label: string, onPress: () -> () }): React.VNode
	local primary = props.variant == "primary"
	local bg = primary and "rgba(79,195,247,0.22)" or "rgba(255,255,255,0.06)"
	local border = primary and "rgba(79,195,247,0.35)" or "rgba(255,255,255,0.12)"
	return x(View) {
		style = {
			paddingTop = 10,
			paddingBottom = 10,
			paddingLeft = 28,
			paddingRight = 28,
			backgroundColor = bg,
			borderWidth = 1,
			borderColor = border,
			borderRadius = 6,
		},
		onmousedown = function() props.onPress() end,
		x(Text) { style = { fontSize = 13, fontStyle = "bold", color = "#fff" }, props.label },
	}
end

local function KeyBindingBtn(props: { capturing: boolean?, label: string, onPress: () -> () }): React.VNode
	local capturing = props.capturing
	local bg = capturing and "rgba(255,152,0,0.25)" or "rgba(255,255,255,0.06)"
	local border = capturing and "rgba(255,152,0,0.5)" or "rgba(255,255,255,0.12)"
	return x(View) {
		style = {
			paddingTop = 5,
			paddingBottom = 5,
			paddingLeft = 18,
			paddingRight = 18,
			minWidth = 140,
			alignItems = "center",
			backgroundColor = bg,
			borderWidth = 1,
			borderColor = border,
			borderRadius = 5,
		},
		onmousedown = function() props.onPress() end,
		x(Text) { style = { fontSize = 13, fontStyle = "bold", color = "#fff" }, props.label },
	}
end

-- Click-to-cycle select (approx of <select>): click advances to next option.
local function Select(props: { options: { [number]: string }?, value: number?, onSelect: ((value: number) -> ()) }): React.VNode
	local options = props.options or {}
	local value = props.value or 0
	local label = ""
	if #options > 0 then
		label = options[value + 1] or options[1] or ""
	end
	return x(View) {
		style = {
			paddingTop = 6,
			paddingBottom = 6,
			paddingLeft = 10,
			paddingRight = 10,
			minWidth = 160,
			alignItems = "center",
			backgroundColor = "rgba(255,255,255,0.06)",
			borderWidth = 1,
			borderColor = "rgba(255,255,255,0.15)",
			borderRadius = 5,
		},
		onmousedown = function()
			if #options == 0 then return end
			props.onSelect((value + 1) % #options)
		end,
		x(Text) { style = { fontSize = 13, color = "#fff" }, label },
	}
end

local function Checkbox(props: { checked: boolean?, onToggle: () -> () }): React.VNode
	local checked = props.checked
	return x(View) {
		style = {
			width = 16,
			height = 16,
			backgroundColor = checked and "#4fc3f7" or "rgba(255,255,255,0.08)",
			borderWidth = 1,
			borderColor = checked and "#4fc3f7" or "rgba(255,255,255,0.25)",
			borderRadius = 3,
		},
		onmousedown = function() props.onToggle() end,
	}
end

local TRACK_W = 160

-- Click/drag slider: maps pointer X on a 160px track to [min, max] at `step`.
local function Slider(props: { min: number?, max: number?, step: number?, value: number?, onValue: ((value: number) -> ()) }): React.VNode
	local min = props.min or 0
	local max = props.max or 1
	local step = props.step or 1
	local value = props.value or 0

	local function setFromX(px: number)
		local frac = clamp01(px / TRACK_W)
		local raw = min + frac * (max - min)
		local stepped = raw
		if step > 0 then
			stepped = math.floor((raw - min) / step + 0.5) * step + min
		end
		if stepped < min then stepped = min end
		if stepped > max then stepped = max end
		props.onValue(stepped)
	end

	local frac = clamp01((value - min) / (max - min))

	return x(View) {
		style = {
			width = TRACK_W,
			height = 20,
			display = "flex",
			flexDirection = "row",
			alignItems = "center",
		},
		onmousedown = function(event) setFromX(event.relativePosition.x) end,
		onmousedrag = function(event) setFromX(event.relativePosition.x) end,
		x(View) {
			style = {
				width = "100%",
				height = 4,
				backgroundColor = "rgba(255,255,255,0.15)",
				borderRadius = 2,
				overflow = "hidden",
			},
			x(View) {
				style = {
					width = ("%d%%"):format(frac * 100),
					height = "100%",
					backgroundColor = "#4fc3f7",
				},
			},
		},
	}
end

-- ── Tabs ─────────────────────────────────────────────────────────

local function VideoTab(props: { config: SettingsSnapshot, options: AvailableOptions, set: ((key: string, value: any) -> ()) }): React.VNode
	local config = props.config
	local options = props.options
	local set = props.set

	return x(React.Fragment) {
		x(SectionLabel) { "Display" },
		x(Row) {
			x(Label) { "Renderer" },
			x(Select) {
				options = options.renderers or {},
				value = config.selectedRenderer,
				onSelect = function(v) set("selectedRenderer", v) end,
			},
		},
		x(Row) {
			x(Label) { "Resolution" },
			x(Select) {
				options = options.resolutions or {},
				value = config.selectedResolution,
				onSelect = function(v) set("selectedResolution", v) end,
			},
		},
		x(Row) {
			x(Label) { "Display Mode" },
			x(Select) {
				options = options.displayModes or {},
				value = config.selectedDisplayMode,
				onSelect = function(v) set("selectedDisplayMode", v) end,
			},
		},
		x(Row) {
			x(Label) { "VSync" },
			x(Checkbox) { checked = config.vsync, onToggle = function() set("vsync", not config.vsync) end },
		},
		x(Row) {
			x(Label) { "FPS Limit" },
			x(Slider) {
				min = 0, max = 240, step = 1, value = config.fpsLimit,
				onValue = function(v) set("fpsLimit", v) end,
			},
			x(SliderValue) { config.fpsLimit == 0 and "Unlimited" or ("%d FPS"):format(config.fpsLimit) },
		},

		x(Divider) {},
		x(SectionLabel) { "Quality" },
		x(Row) {
			x(Label) { "Antialiasing" },
			x(Select) {
				options = options.antialiasModes or {},
				value = config.antialias,
				onSelect = function(v) set("antialias", v) end,
			},
		},
		x(Row) {
			x(Label) { "Shadow Distance" },
			x(Select) {
				options = options.shadowCascadeLevels or {},
				value = config.shadowCascadeLevel,
				onSelect = function(v) set("shadowCascadeLevel", v) end,
			},
		},
		x(Row) {
			x(Label) { "Shadow Resolution" },
			x(Select) {
				options = options.shadowResolutions or {},
				value = config.shadowResolution,
				onSelect = function(v) set("shadowResolution", v) end,
			},
		},
		x(Row) {
			x(Label) { "Render Distance" },
			x(Select) {
				options = options.renderDistanceNames or {},
				value = config.renderDistance,
				onSelect = function(v) set("renderDistance", v) end,
			},
		},

		x(Divider) {},
		x(SectionLabel) { "Advanced" },
		x(Row) {
			x(Label) { "Low Latency" },
			x(Checkbox) { checked = config.lowLatency, onToggle = function() set("lowLatency", not config.lowLatency) end },
		},
		x(Row) {
			x(Label) { "Outline Width" },
			x(Slider) {
				min = 0.5, max = 4, step = 0.1, value = config.lineWidth,
				onValue = function(v) set("lineWidth", v) end,
			},
			x(SliderValue) { ("%.1f"):format(config.lineWidth) },
		},
		x(Row) {
			x(Label) { "Distant Outlines" },
			x(Select) {
				options = DISTANT_OUTLINE_BEHAVIORS,
				value = config.distantOutlineBehavior,
				onSelect = function(v) set("distantOutlineBehavior", v) end,
			},
		},
	}
end

local function AudioTab(props: { config: SettingsSnapshot, set: ((key: string, value: any) -> ()) }): React.VNode
	local config = props.config
	local set = props.set

	return x(React.Fragment) {
		x(SectionLabel) { "Volume" },
		x(Row) {
			x(Label) { "Mute All" },
			x(Checkbox) { checked = config.muteAll, onToggle = function() set("muteAll", not config.muteAll) end },
		},
		x(Row) {
			x(Label) { "Master Volume" },
			x(Slider) {
				min = 0, max = 1, step = 0.01, value = config.masterVolume,
				onValue = function(v) set("masterVolume", v) end,
			},
			x(SliderValue) { ("%d%%"):format(math.floor(config.masterVolume * 100 + 0.5)) },
		},
		x(Row) {
			x(Label) { "Music Volume" },
			x(Slider) {
				min = 0, max = 1, step = 0.01, value = config.musicVolume,
				onValue = function(v) set("musicVolume", v) end,
			},
			x(SliderValue) { ("%d%%"):format(math.floor(config.musicVolume * 100 + 0.5)) },
		},
		x(Row) {
			x(Label) { "Effects Volume" },
			x(Slider) {
				min = 0, max = 1, step = 0.01, value = config.effectsVolume,
				onValue = function(v) set("effectsVolume", v) end,
			},
			x(SliderValue) { ("%d%%"):format(math.floor(config.effectsVolume * 100 + 0.5)) },
		},

		x(Divider) {},
		x(Row) {
			x(Label) { "Remastered Music" },
			x(Checkbox) { checked = config.remasteredMusic, onToggle = function() set("remasteredMusic", not config.remasteredMusic) end },
		},
	}
end

local function GameTab(props: { config: SettingsSnapshot, set: ((key: string, value: any) -> ()), onReset: () -> () }): React.VNode
	local config = props.config
	local set = props.set

	return x(React.Fragment) {
		x(SectionLabel) { "Camera" },
		x(Row) {
			x(Label) { "Field of View" },
			x(Slider) {
				min = 58.7, max = 120, step = 0.5, value = config.fov,
				onValue = function(v) set("fov", v) end,
			},
			x(SliderValue) { ("%.1f°"):format(config.fov) },
		},
		x(Row) {
			x(Label) { "Smooth FOV" },
			x(Checkbox) { checked = config.smoothFov, onToggle = function() set("smoothFov", not config.smoothFov) end },
		},
		x(Row) {
			x(Label) { "Follow Y Offset" },
			x(Slider) {
				min = -160, max = 500, step = 1, value = config.followY,
				onValue = function(v) set("followY", v) end,
			},
			x(SliderValue) { ("%d"):format(config.followY) },
		},
		x(Row) {
			x(Label) { "Follow Z Offset" },
			x(Slider) {
				min = -500, max = 500, step = 1, value = config.followZ,
				onValue = function(v) set("followZ", v) end,
			},
			x(SliderValue) { ("%d"):format(config.followZ) },
		},

		x(Divider) {},
		x(ResetLink) {
			onmousedown = function() props.onReset() end,
			"Reset Camera Defaults",
		},
	}
end

local function KeyboardTab(props: { config: SettingsSnapshot, capturingAction: string?, startCapture: (action: string) -> (), onReset: () -> () }): React.VNode
	local config = props.config
	local capturingAction = props.capturingAction

	return x(React.Fragment) {
		x(SectionLabel) { "Key Bindings" },

		x(ResetLink) {
			onmousedown = function() props.onReset() end,
			"Reset All to Defaults",
		},

		x(Divider) {},

		map(config.keyBindings, function(b: any): React.VNode
			local capturing = capturingAction == b.action
			return x(KeyBindingRow) {
				key = b.action,
				x(KeyBindingLabel) { b.displayName ~= "" and b.displayName or b.action },
				x(KeyBindingBtn) {
					capturing = capturing,
					label = capturing and "Press any key..." or keyCodeToName(b.keyCode),
					onPress = function()
						props.startCapture(b.action)
					end,
				},
			}
		end),

		capturingAction ~= nil and x(Text) {
			style = { fontSize = 12, color = "rgba(255,152,0,0.6)", marginTop = 10, textAlign = "center" },
			"Press any key to bind, or ESC to cancel...",
		},
	}
end

-- ── Settings component ───────────────────────────────────────────

local function Settings(props: { onClose: () -> () }): React.VNode
	local onClose = props.onClose

	local tab, setTab = useState<<string>>("video")
	local config, setConfig = useState<<SettingsSnapshot | nil>>(nil)
	local options, setOptions = useState<<AvailableOptions | nil>>(nil)
	local capturingAction, setCapturingAction = useState<<string | nil>>(nil)
	local confirmModal, setConfirmModal = useState<<{
        title: string,
        message: string,
        onConfirm: () -> (),
    } | nil>>(nil)
	local restartModal, setRestartModal = useState<<boolean>>(false)
	local pendingCloseRef = useRef(false)
	local rootRef = useRef<<any>>(nil)

	local function copySnapshot(c: SettingsSnapshot): SettingsSnapshot
		local t = SettingsSnapshot.new()
		t.selectedRenderer = c.selectedRenderer
		t.selectedResolution = c.selectedResolution
		t.selectedDisplayMode = c.selectedDisplayMode
		t.vsync = c.vsync
		t.fpsLimit = c.fpsLimit
		t.antialias = c.antialias
		t.shadowCascadeLevel = c.shadowCascadeLevel
		t.shadowResolution = c.shadowResolution
		t.renderDistance = c.renderDistance
		t.lowLatency = c.lowLatency
		t.lineWidth = c.lineWidth
		t.masterVolume = c.masterVolume
		t.musicVolume = c.musicVolume
		t.effectsVolume = c.effectsVolume
		t.muteAll = c.muteAll
		t.remasteredMusic = c.remasteredMusic
		t.fov = c.fov
		t.followY = c.followY
		t.followZ = c.followZ
		t.smoothFov = c.smoothFov
		t.distantOutlineBehavior = c.distantOutlineBehavior
		local kb = {}
		if c.keyBindings ~= nil then
			for i = 1, #c.keyBindings do
				local b = c.keyBindings[i]
                kb[i] = KeyBindingData.new()
                kb[i].action = b.action
                kb[i].displayName = b.displayName
                kb[i].keyCode = b.keyCode
			end
		end
		t.keyBindings = kb
		return t
	end

	local updateConfig = useCallback(function(updater: (SettingsSnapshot) -> SettingsSnapshot)
		setConfig(function(prev: SettingsSnapshot | nil)
			if prev == nil then return prev end
			return updater(prev)
		end)
	end, {})

	local applySetting = useCallback(function(key: string, value: any)
		UiLib.call("applySetting", { key = key, value = value })
	end, {})

	local set = useCallback(function<T>(key: string, value: T)
		updateConfig(function(c: SettingsSnapshot)
			local next = copySnapshot(c)
			;(next :: any)[key] = value
			return next
		end)
		applySetting(key, value)
	end, { updateConfig, applySetting })

	useEffect(function()
		local u1 = UiLib.onEvent("settings:config", function(data: SettingsSnapshot)
			if data == nil then
				setConfig(nil)
			else
				setConfig(copySnapshot(data))
			end
		end)

		local u2 = UiLib.onEvent("settings:options", function(data: AvailableOptions)
			setOptions(data)
		end)

		local u3 = UiLib.onEvent("settings:keyCaptured", function(data: CapturedKey)
			setCapturingAction(nil)

			if not data.cancelled and data.action ~= nil then
				setConfig(function(prev: SettingsSnapshot | nil)
					if prev == nil then return prev end

					local kb = {}
					for i = 1, #prev.keyBindings do
						local b = prev.keyBindings[i]
						local newKeyCode = b.keyCode
						if b.keyCode == data.keyCode and b.action ~= data.action then
							newKeyCode = 0
						elseif b.action == data.action then
							newKeyCode = data.keyCode
						end
                        kb[i] = KeyBindingData.new()
                        kb[i].action = b.action
                        kb[i].displayName = b.displayName
                        kb[i].keyCode = newKeyCode
					end

					local next = copySnapshot(prev)
					next.keyBindings = kb
					return next
				end)
			end
		end)

		local u4 = UiLib.onEvent("settings:requireRestart", function()
			setRestartModal(true)
		end)

		local u5 = UiLib.onEvent("settings:saved", function()
			if pendingCloseRef.current then
				pendingCloseRef.current = false
				if onClose ~= nil then onClose() end
			end
		end)

		UiLib.call("getConfig", {})

		return function()
			u1()
			u2()
			u3()
			u4()
			u5()
		end
	end, {})

	useEffect(function()
		if rootRef.current ~= nil then rootRef.current:focus() end
	end, {})

	local function handleRootKey(event: KeyboardEvent)
		if event.keyCode == Key.escape and onClose ~= nil then
			onClose()
		end
	end

	local handleSave = useCallback(function()
		UiLib.call("saveConfig", {})
	end, {})

	local handleClose = useCallback(function()
		UiLib.call("close", {})
		if onClose ~= nil then onClose() end
	end, { onClose })

	local handleOk = useCallback(function()
		UiLib.call("saveConfig", {})
		pendingCloseRef.current = true
	end, {})

	local handleResetDefaults = useCallback(function(section: string, title: string, message: string)
		setConfirmModal {
			title = title,
			message = message,
			onConfirm = function()
				UiLib.call("resetDefaults", { section = section })
				setConfirmModal(nil)
			end,
		}
	end, {})

	local startCapture = useCallback(function(action: string)
		setCapturingAction(action)
		UiLib.call("startCapture", { action = action })
	end, {})

	if config == nil or options == nil then
		return x(Root) {
			ref = rootRef,
			tabindex = 0,
			onkeydown = handleRootKey,
			x(Header) { "Settings" },
			x(Content) {
				x(Text) {
					style = { textAlign = "center", color = "rgba(255,255,255,0.3)", marginTop = 80 },
					"Loading settings...",
				},
			},
		}
	end

	return x(Root) {
		ref = rootRef,
		tabindex = 0,
		onkeydown = handleRootKey,
		x(Header) { "Settings" },

		x(TabBar) {
			x(TabBtn) { label = "Keyboard", active = tab == "keyboard", onPress = function() setTab("keyboard") end },
			x(TabBtn) { label = "Video", active = tab == "video", onPress = function() setTab("video") end },
			x(TabBtn) { label = "Audio", active = tab == "audio", onPress = function() setTab("audio") end },
			x(TabBtn) { label = "Game", active = tab == "game", onPress = function() setTab("game") end },
		},

		x(Content) {
			tab == "video" and x(VideoTab) { config = config, options = options, set = set },
			tab == "audio" and x(AudioTab) { config = config, set = set },
			tab == "game" and x(GameTab) {
				config = config,
				set = set,
				onReset = function()
					handleResetDefaults("camera", "Reset Camera", "Are you sure you want to reset camera settings to default?")
				end,
			},
			tab == "keyboard" and x(KeyboardTab) {
				config = config,
				capturingAction = capturingAction,
				startCapture = startCapture,
				onReset = function()
					handleResetDefaults("keyboard", "Reset Key Binds", "Are you sure you want to reset key binds to default?")
				end,
			},
		},

		x(BottomBar) {
			x(Btn) { variant = "primary", label = "OK", onPress = handleOk },
			x(Btn) { label = "Cancel", onPress = handleClose },
			x(Btn) { label = "Apply", onPress = handleSave },
		},

		confirmModal ~= nil and not restartModal and x(View) {
			style = {
				position = "absolute", top = 0, left = 0, right = 0, bottom = 0,
				display = "flex", alignItems = "center", justifyContent = "center",
				backgroundColor = "rgba(0,0,0,0.5)",
			},
			onmousedown = function() setConfirmModal(nil) end,
			x(View) {
				style = {
					backgroundColor = "#1a1a2e",
					borderWidth = 1, borderColor = "rgba(255,255,255,0.12)",
					borderRadius = 10,
					paddingTop = 24, paddingBottom = 24, paddingLeft = 28, paddingRight = 28,
					minWidth = 320,
					alignItems = "center",
				},
				onmousedown = function() end,
				x(Text) { style = { fontSize = 16, fontStyle = "bold", marginBottom = 8 }, confirmModal.title },
				x(Text) { style = { fontSize = 13, color = "rgba(255,255,255,0.5)", marginBottom = 20 }, confirmModal.message },
				x(View) {
					style = { display = "flex", flexDirection = "row", justifyContent = "center", gap = 10 },
					x(Btn) { variant = "primary", label = "Yes", onPress = confirmModal.onConfirm },
					x(Btn) { label = "No", onPress = function() setConfirmModal(nil) end },
				},
			},
		},

		restartModal and x(View) {
			style = {
				position = "absolute", top = 0, left = 0, right = 0, bottom = 0,
				display = "flex", alignItems = "center", justifyContent = "center",
				backgroundColor = "rgba(0,0,0,0.5)",
			},
			x(View) {
				style = {
					backgroundColor = "#1a1a2e",
					borderWidth = 1, borderColor = "rgba(255,255,255,0.12)",
					borderRadius = 10,
					paddingTop = 24, paddingBottom = 24, paddingLeft = 28, paddingRight = 28,
					minWidth = 320,
					alignItems = "center",
				},
				x(Text) { style = { fontSize = 16, fontStyle = "bold", marginBottom = 8 }, "Restart Required" },
				x(Text) {
					style = { fontSize = 13, color = "rgba(255,255,255,0.5)", marginBottom = 20, textAlign = "center" },
					"The renderer has been changed. A restart is required for this change to take effect.",
				},
				x(View) {
					style = { display = "flex", flexDirection = "row", justifyContent = "center", gap = 10 },
					x(Btn) { variant = "primary", label = "Restart Now", onPress = function() UiLib.call("restartNow", {}) end },
					x(Btn) {
						label = "Later",
						onPress = function()
							setRestartModal(false)
							if pendingCloseRef.current then
								pendingCloseRef.current = false
								if onClose ~= nil then onClose() end
							end
						end,
					},
				},
			},
		},
	}
end

return Settings
