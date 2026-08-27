-- Settings view (Sx port of preact settings.luau). Rendered by the main menu and the
-- pause menu. Fine-grained: `config`/`options`/`tab`/`capturingAction`/modals are
-- signals; tab content and controls read them reactively through accessors, so editing a
-- slider only re-sets that one control's props/text, never the whole tree.

local Sx = require("../../library/sx/init")
local styled = Sx.styled
local x = Sx.x

-- ── Helpers ──────────────────────────────────────────────────────

local function clamp01(v)
	if v < 0 then return 0 end
	if v > 1 then return 1 end
	return v
end

-- value-or-accessor: returns the current value whether `v` is plain or a getter.
local function readProp(v)
	if type(v) == "function" then
		return v()
	end
	return v
end

local function keyCodeToName(code)
	if code == 0 then return "None" end
	local names = {
		[8] = "Backspace", [9] = "Tab", [13] = "Enter",
		[16] = "Shift", [17] = "Ctrl", [18] = "Alt",
		[27] = "Escape", [32] = "Space",
		[33] = "PageUp", [34] = "PageDown", [35] = "End", [36] = "Home",
		[37] = "Left", [38] = "Up", [39] = "Right", [40] = "Down",
		[44] = "PrintScreen", [45] = "Insert", [46] = "Delete",
		[91] = "LWin", [92] = "RWin",
		[112] = "F1", [113] = "F2", [114] = "F3", [115] = "F4",
		[116] = "F5", [117] = "F6", [118] = "F7", [119] = "F8",
		[120] = "F9", [121] = "F10", [122] = "F11", [123] = "F12",
		[144] = "NumLock", [145] = "ScrollLock",
		[160] = "LShift", [161] = "RShift",
		[162] = "LCtrl", [163] = "RCtrl",
		[164] = "LAlt", [165] = "RAlt",
		[186] = ";", [187] = "=", [188] = ",", [189] = "-", [190] = ".", [191] = "/",
		[192] = "`",
		[219] = "[", [220] = "\\", [221] = "]", [222] = "'",
	}
	local name = names[code]
	if name ~= nil then return name end
	if code >= 65 and code <= 90 then return string.char(code) end
	if code >= 48 and code <= 57 then return string.char(code) end
	if code >= 96 and code <= 105 then return ("NumPad%d"):format(code - 96) end
	if code == 106 then return "NumPad*" end
	if code == 107 then return "NumPad+" end
	if code == 109 then return "NumPad-" end
	if code == 110 then return "NumPad." end
	if code == 111 then return "NumPad/" end
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

local Root = styled("view") {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "column",
	backgroundColor = "rgba(0,0,0,0.65)",
}

local Header = styled("text") {
	fontSize = 28,
	fontStyle = "bold",
	textAlign = "center",
	marginTop = 20,
}

local TabBar = styled("view") {
	display = "flex",
	flexDirection = "row",
	justifyContent = "center",
	gap = 4,
	paddingTop = 16,
	paddingLeft = 40,
	paddingRight = 40,
}

local Content = styled("view") {
	flex = 1,
	overflow = "scroll",
	paddingTop = 20,
	paddingLeft = 40,
	paddingRight = 40,
	paddingBottom = 10,
}

local BottomBar = styled("view") {
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

local SectionLabel = styled("text") {
	fontSize = 11,
	fontStyle = "bold",
	color = "rgba(255,255,255,0.35)",
	marginBottom = 10,
}

local Row = styled("view") {
	display = "flex",
	flexDirection = "row",
	alignItems = "center",
	justifyContent = "space-between",
	gap = 16,
	paddingTop = 6,
	paddingBottom = 6,
}

local Label = styled("text") {
	fontSize = 14,
	color = "rgba(255,255,255,0.8)",
	minWidth = 140,
}

local SliderValue = styled("text") {
	fontSize = 12,
	color = "rgba(255,255,255,0.5)",
	marginLeft = 8,
	minWidth = 50,
}

local Divider = styled("view") {
	height = 1,
	backgroundColor = "rgba(255,255,255,0.06)",
	marginTop = 12,
	marginBottom = 12,
}

local ResetLink = styled("text") {
	color = "rgba(255,255,255,0.35)",
	fontSize = 12,
	paddingTop = 8,
	paddingBottom = 8,
	hover = { color = "rgba(255,152,0,0.7)" },
}

local KeyBindingRow = styled("view") {
	display = "flex",
	flexDirection = "row",
	alignItems = "center",
	justifyContent = "space-between",
	gap = 12,
	paddingTop = 4,
	paddingBottom = 4,
}

local KeyBindingLabel = styled("text") {
	fontSize = 13,
	color = "rgba(255,255,255,0.75)",
	minWidth = 160,
}

-- ── Interactive controls (approximations of HTML controls) ───────
-- Reactive props (`active`/`capturing`/`checked`/`value`) may be accessors.

local function TabBtn(props)
	return x("view") {
		style = Sx.createMemo(function()
			local active = readProp(props.active)
			return {
				paddingTop = 8,
				paddingBottom = 8,
				paddingLeft = 20,
				paddingRight = 20,
				backgroundColor = active and "rgba(79,195,247,0.18)" or "transparent",
				borderWidth = 1,
				borderColor = active and "rgba(79,195,247,0.35)" or "transparent",
				borderRadius = 6,
			}
		end),
		onmousedown = function() props.onPress() end,
		x("text") {
			style = Sx.createMemo(function()
				local active = readProp(props.active)
				return {
					fontSize = 13,
					fontStyle = "bold",
					color = active and "#fff" or "rgba(255,255,255,0.45)",
				}
			end),
			props.label,
		},
	}
end

local function Btn(props)
	local primary = props.variant == "primary"
	local bg = primary and "rgba(79,195,247,0.22)" or "rgba(255,255,255,0.06)"
	local border = primary and "rgba(79,195,247,0.35)" or "rgba(255,255,255,0.12)"
	return x("view") {
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
		x("text") { style = { fontSize = 13, fontStyle = "bold", color = "#fff" }, props.label },
	}
end

local function KeyBindingBtn(props)
	return x("view") {
		style = Sx.createMemo(function()
			local capturing = readProp(props.capturing)
			return {
				paddingTop = 5,
				paddingBottom = 5,
				paddingLeft = 18,
				paddingRight = 18,
				minWidth = 140,
				alignItems = "center",
				backgroundColor = capturing and "rgba(255,152,0,0.25)" or "rgba(255,255,255,0.06)",
				borderWidth = 1,
				borderColor = capturing and "rgba(255,152,0,0.5)" or "rgba(255,255,255,0.12)",
				borderRadius = 5,
			}
		end),
		onmousedown = function() props.onPress() end,
		x("text") {
			style = Sx.createMemo(function()
				local capturing = readProp(props.capturing)
				return { fontSize = 13, fontStyle = "bold", color = capturing and "#ffb300" or "#fff" }
			end),
			function()
				return readProp(props.label)
			end,
		},
	}
end

-- Click-to-cycle select (approx of <select>): click advances to next option.
local function Select(props)
	return x("view") {
		style = Sx.createMemo(function()
			local options = props.options or {}
			local value = readProp(props.value) or 0
			local label = ""
			if #options > 0 then
				label = options[value + 1] or options[1] or ""
			end
			return {
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
			}
		end),
		onmousedown = function()
			local options = props.options or {}
			if #options == 0 then return end
			props.onSelect(((readProp(props.value) or 0) + 1) % #options)
		end,
		x("text") {
			style = Sx.createMemo(function()
				local options = props.options or {}
				local value = readProp(props.value) or 0
				local label = ""
				if #options > 0 then
					label = options[value + 1] or options[1] or ""
				end
				return { fontSize = 13, color = "#fff" }
			end),
			function()
				local options = props.options or {}
				local value = readProp(props.value) or 0
				if #options > 0 then
					return options[value + 1] or options[1] or ""
				end
				return ""
			end,
		},
	}
end

local function Checkbox(props)
	return x("view") {
		style = Sx.createMemo(function()
			local checked = readProp(props.checked)
			return {
				width = 16,
				height = 16,
				backgroundColor = checked and "#4fc3f7" or "rgba(255,255,255,0.08)",
				borderWidth = 1,
				borderColor = checked and "#4fc3f7" or "rgba(255,255,255,0.25)",
				borderRadius = 3,
			}
		end),
		onmousedown = function() props.onToggle() end,
	}
end

local TRACK_W = 160

-- Click/drag slider: maps pointer X on a 160px track to [min, max] at `step`.
local function Slider(props)
	local min = props.min or 0
	local max = props.max or 1
	local step = props.step or 1

	local function setFromX(px)
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

	return x("view") {
		style = {
			width = TRACK_W,
			height = 20,
			display = "flex",
			flexDirection = "row",
			alignItems = "center",
		},
		onmousedown = function(event) setFromX(event.relativePosition.x) end,
		onmousedrag = function(event) setFromX(event.relativePosition.x) end,
		x("view") {
			style = {
				width = "100%",
				height = 4,
				backgroundColor = "rgba(255,255,255,0.15)",
				borderRadius = 2,
				overflow = "hidden",
			},
			x("view") {
				style = Sx.createMemo(function()
					local value = readProp(props.value) or 0
					local frac = clamp01((value - min) / (max - min))
					return {
						width = ("%d%%"):format(frac * 100),
						height = "100%",
						backgroundColor = "#4fc3f7",
					}
				end),
			},
		},
	}
end

-- ── Tabs ─────────────────────────────────────────────────────────
-- Each tab receives the `config` GETTER (signal) so reads are reactive.

local function VideoTab(props)
	local config = props.config
	local options = props.options
	local set = props.set

	return x(Sx.Fragment) {
		x(SectionLabel) { "Display" },
		x(Row) {
			x(Label) { "Renderer" },
			x(Select) {
				options = options().renderers or {},
				value = function() return config().selectedRenderer end,
				onSelect = function(v) set("selectedRenderer", v) end,
			},
		},
		x(Row) {
			x(Label) { "Resolution" },
			x(Select) {
				options = options().resolutions or {},
				value = function() return config().selectedResolution end,
				onSelect = function(v) set("selectedResolution", v) end,
			},
		},
		x(Row) {
			x(Label) { "Display Mode" },
			x(Select) {
				options = options().displayModes or {},
				value = function() return config().selectedDisplayMode end,
				onSelect = function(v) set("selectedDisplayMode", v) end,
			},
		},
		x(Row) {
			x(Label) { "VSync" },
			x(Checkbox) { checked = function() return config().vsync end, onToggle = function() set("vsync", not config().vsync) end },
		},
		x(Row) {
			x(Label) { "FPS Limit" },
			x(Slider) {
				min = 0, max = 240, step = 1,
				value = function() return config().fpsLimit end,
				onValue = function(v) set("fpsLimit", v) end,
			},
			x(SliderValue) {
				function()
					local v = config().fpsLimit
					return v == 0 and "Unlimited" or ("%d FPS"):format(v)
				end,
			},
		},

		x(Divider) {},
		x(SectionLabel) { "Quality" },
		x(Row) {
			x(Label) { "Antialiasing" },
			x(Select) {
				options = options().antialiasModes or {},
				value = function() return config().antialias end,
				onSelect = function(v) set("antialias", v) end,
			},
		},
		x(Row) {
			x(Label) { "Shadow Distance" },
			x(Select) {
				options = options().shadowCascadeLevels or {},
				value = function() return config().shadowCascadeLevel end,
				onSelect = function(v) set("shadowCascadeLevel", v) end,
			},
		},
		x(Row) {
			x(Label) { "Shadow Resolution" },
			x(Select) {
				options = options().shadowResolutions or {},
				value = function() return config().shadowResolution end,
				onSelect = function(v) set("shadowResolution", v) end,
			},
		},
		x(Row) {
			x(Label) { "Render Distance" },
			x(Select) {
				options = options().renderDistanceNames or {},
				value = function() return config().renderDistance end,
				onSelect = function(v) set("renderDistance", v) end,
			},
		},

		x(Divider) {},
		x(SectionLabel) { "Advanced" },
		x(Row) {
			x(Label) { "Low Latency" },
			x(Checkbox) { checked = function() return config().lowLatency end, onToggle = function() set("lowLatency", not config().lowLatency) end },
		},
		x(Row) {
			x(Label) { "Outline Width" },
			x(Slider) {
				min = 0.5, max = 4, step = 0.1,
				value = function() return config().lineWidth end,
				onValue = function(v) set("lineWidth", v) end,
			},
			x(SliderValue) { function() return ("%.1f"):format(config().lineWidth) end },
		},
		x(Row) {
			x(Label) { "Distant Outlines" },
			x(Select) {
				options = DISTANT_OUTLINE_BEHAVIORS,
				value = function() return config().distantOutlineBehavior end,
				onSelect = function(v) set("distantOutlineBehavior", v) end,
			},
		},
	}
end

local function AudioTab(props)
	local config = props.config
	local set = props.set

	return x(Sx.Fragment) {
		x(SectionLabel) { "Volume" },
		x(Row) {
			x(Label) { "Mute All" },
			x(Checkbox) { checked = function() return config().muteAll end, onToggle = function() set("muteAll", not config().muteAll) end },
		},
		x(Row) {
			x(Label) { "Master Volume" },
			x(Slider) {
				min = 0, max = 1, step = 0.01,
				value = function() return config().masterVolume end,
				onValue = function(v) set("masterVolume", v) end,
			},
			x(SliderValue) { function() return ("%d%%"):format(math.floor(config().masterVolume * 100 + 0.5)) end },
		},
		x(Row) {
			x(Label) { "Music Volume" },
			x(Slider) {
				min = 0, max = 1, step = 0.01,
				value = function() return config().musicVolume end,
				onValue = function(v) set("musicVolume", v) end,
			},
			x(SliderValue) { function() return ("%d%%"):format(math.floor(config().musicVolume * 100 + 0.5)) end },
		},
		x(Row) {
			x(Label) { "Effects Volume" },
			x(Slider) {
				min = 0, max = 1, step = 0.01,
				value = function() return config().effectsVolume end,
				onValue = function(v) set("effectsVolume", v) end,
			},
			x(SliderValue) { function() return ("%d%%"):format(math.floor(config().effectsVolume * 100 + 0.5)) end },
		},

		x(Divider) {},
		x(Row) {
			x(Label) { "Remastered Music" },
			x(Checkbox) { checked = function() return config().remasteredMusic end, onToggle = function() set("remasteredMusic", not config().remasteredMusic) end },
		},
	}
end

local function GameTab(props)
	local config = props.config
	local set = props.set

	return x(Sx.Fragment) {
		x(SectionLabel) { "Camera" },
		x(Row) {
			x(Label) { "Field of View" },
			x(Slider) {
				min = 58.7, max = 120, step = 0.5,
				value = function() return config().fov end,
				onValue = function(v) set("fov", v) end,
			},
			x(SliderValue) { function() return ("%.1f°"):format(config().fov) end },
		},
		x(Row) {
			x(Label) { "Smooth FOV" },
			x(Checkbox) { checked = function() return config().smoothFov end, onToggle = function() set("smoothFov", not config().smoothFov) end },
		},
		x(Row) {
			x(Label) { "Follow Y Offset" },
			x(Slider) {
				min = -160, max = 500, step = 1,
				value = function() return config().followY end,
				onValue = function(v) set("followY", v) end,
			},
			x(SliderValue) { function() return ("%d"):format(config().followY) end },
		},
		x(Row) {
			x(Label) { "Follow Z Offset" },
			x(Slider) {
				min = -500, max = 500, step = 1,
				value = function() return config().followZ end,
				onValue = function(v) set("followZ", v) end,
			},
			x(SliderValue) { function() return ("%d"):format(config().followZ) end },
		},

		x(Divider) {},
		x(ResetLink) {
			onmousedown = function() props.onReset() end,
			"Reset Camera Defaults",
		},
	}
end

local function KeyboardTab(props)
	local config = props.config

	return x(Sx.Fragment) {
		x(SectionLabel) { "Key Bindings" },

		x(ResetLink) {
			onmousedown = function() props.onReset() end,
			"Reset All to Defaults",
		},

		x(Divider) {},

		x(Sx.For) {
			each = function()
				local c = config()
				return c ~= nil and c.keyBindings or {}
			end,
			function(item, index)
				local b = item()
				return x(KeyBindingRow) {
					x(KeyBindingLabel) { b.displayName ~= "" and b.displayName or b.action },
					x(KeyBindingBtn) {
						capturing = function() return props.capturingAction() == b.action end,
						label = function()
							local cap = props.capturingAction() == b.action
							return cap and "Press any key..." or keyCodeToName(b.keyCode)
						end,
						onPress = function()
							props.startCapture(b.action)
						end,
					},
				}
			end,
		},

		x(Sx.Show) {
			when = function() return props.capturingAction() ~= nil end,
			x("text") {
				style = { fontSize = 12, color = "rgba(255,152,0,0.6)", marginTop = 10, textAlign = "center" },
				"Press any key to bind, or ESC to cancel...",
			},
		},
	}
end

-- ── Settings component ───────────────────────────────────────────

local function Settings(props)
	local onClose = props.onClose

	local tab, setTab = Sx.createSignal("video")
	local config, setConfig = Sx.createSignal(nil)
	local options, setOptions = Sx.createSignal(nil)
	local capturingAction, setCapturingAction = Sx.createSignal(nil)
	local confirmModal, setConfirmModal = Sx.createSignal(nil)
	local restartModal, setRestartModal = Sx.createSignal(false)
	local pendingClose = { current = false }
	local rootRef = {}

	local function copySnapshot(c)
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

	local function updateConfig(updater)
		local prev = config()
		if prev == nil then return end
		setConfig(updater(prev))
	end

	local function applySetting(key, value)
		UiLib.call("applySetting", { key = key, value = value })
	end

	local function set(key, value)
		updateConfig(function(c)
			local nextC = copySnapshot(c)
			nextC[key] = value
			return nextC
		end)
		applySetting(key, value)
	end

	Sx.createEffect(function()
		local u1 = UiLib.onEvent("settings:config", function(data)
			if data == nil then
				setConfig(nil)
			else
				setConfig(copySnapshot(data))
			end
		end)

		local u2 = UiLib.onEvent("settings:options", function(data)
			setOptions(data)
		end)

		local u3 = UiLib.onEvent("settings:keyCaptured", function(data)
			setCapturingAction(nil)
			if not data.cancelled and data.action ~= nil then
				local prev = config()
				if prev ~= nil then
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
					local nextC = copySnapshot(prev)
					nextC.keyBindings = kb
					setConfig(nextC)
				end
			end
		end)

		local u4 = UiLib.onEvent("settings:requireRestart", function()
			setRestartModal(true)
		end)

		local u5 = UiLib.onEvent("settings:saved", function()
			if pendingClose.current then
				pendingClose.current = false
				if onClose ~= nil then onClose() end
			end
		end)

		UiLib.call("getConfig", {})

		Sx.onCleanup(function()
			u1()
			u2()
			u3()
			u4()
			u5()
		end)
	end)

	Sx.createEffect(function()
		if rootRef.current ~= nil then rootRef.current:focus() end
	end)

	local function handleRootKey(event)
		if event.keyCode == Key.escape and onClose ~= nil then
			onClose()
		end
	end

	local function handleSave()
		UiLib.call("saveConfig", {})
	end

	local function handleClose()
		UiLib.call("close", {})
		if onClose ~= nil then onClose() end
	end

	local function handleOk()
		UiLib.call("saveConfig", {})
		pendingClose.current = true
	end

	local function handleResetDefaults(section, title, message)
		setConfirmModal {
			title = title,
			message = message,
			onConfirm = function()
				UiLib.call("resetDefaults", { section = section })
				setConfirmModal(nil)
			end,
		}
	end

	local function startCapture(action)
		setCapturingAction(action)
		UiLib.call("startCapture", { action = action })
	end

	return x(Root) {
		ref = rootRef,
		tabindex = 0,
		onkeydown = handleRootKey,

		x(Sx.Switch) {
			x(Sx.Match) {
				when = function() return config() == nil or options() == nil end,
				x(Header) { "Settings" },
				x(Content) {
					x("text") {
						style = { textAlign = "center", color = "rgba(255,255,255,0.3)", marginTop = 80 },
						"Loading settings...",
					},
				},
			},
			x(Sx.Match) {
				when = function() return true end,
				x(Header) { "Settings" },
				x(TabBar) {
					x(TabBtn) { label = "Keyboard", active = function() return tab() == "keyboard" end, onPress = function() setTab("keyboard") end },
					x(TabBtn) { label = "Video", active = function() return tab() == "video" end, onPress = function() setTab("video") end },
					x(TabBtn) { label = "Audio", active = function() return tab() == "audio" end, onPress = function() setTab("audio") end },
					x(TabBtn) { label = "Game", active = function() return tab() == "game" end, onPress = function() setTab("game") end },
				},
				x(Content) {
					x(Sx.Show) {
						when = function() return tab() == "video" end,
						x(VideoTab) { config = config, options = options, set = set },
					},
					x(Sx.Show) {
						when = function() return tab() == "audio" end,
						x(AudioTab) { config = config, set = set },
					},
					x(Sx.Show) {
						when = function() return tab() == "game" end,
						x(GameTab) {
							config = config,
							set = set,
							onReset = function()
								handleResetDefaults("camera", "Reset Camera", "Are you sure you want to reset camera settings to default?")
							end,
						},
					},
					x(Sx.Show) {
						when = function() return tab() == "keyboard" end,
						x(KeyboardTab) {
							config = config,
							capturingAction = capturingAction,
							startCapture = startCapture,
							onReset = function()
								handleResetDefaults("keyboard", "Reset Key Binds", "Are you sure you want to reset key binds to default?")
							end,
						},
					},
				},
				x(BottomBar) {
					x(Btn) { variant = "primary", label = "OK", onPress = handleOk },
					x(Btn) { label = "Cancel", onPress = handleClose },
					x(Btn) { label = "Apply", onPress = handleSave },
				},

				x(Sx.Show) {
					when = function() return confirmModal() ~= nil and not restartModal() end,
					x("view") {
						style = {
							position = "absolute", top = 0, left = 0, right = 0, bottom = 0,
							display = "flex", alignItems = "center", justifyContent = "center",
							backgroundColor = "rgba(0,0,0,0.5)",
						},
						onmousedown = function() setConfirmModal(nil) end,
						x("view") {
							style = {
								backgroundColor = "#1a1a2e",
								borderWidth = 1, borderColor = "rgba(255,255,255,0.12)",
								borderRadius = 10,
								paddingTop = 24, paddingBottom = 24, paddingLeft = 28, paddingRight = 28,
								minWidth = 320,
								alignItems = "center",
							},
							onmousedown = function() end,
							x("text") { style = { fontSize = 16, fontStyle = "bold", marginBottom = 8 }, function() return confirmModal().title end },
							x("text") { style = { fontSize = 13, color = "rgba(255,255,255,0.5)", marginBottom = 20 }, function() return confirmModal().message end },
							x("view") {
								style = { display = "flex", flexDirection = "row", justifyContent = "center", gap = 10 },
								x(Btn) { variant = "primary", label = "Yes", onPress = function() return confirmModal().onConfirm() end },
								x(Btn) { label = "No", onPress = function() setConfirmModal(nil) end },
							},
						},
					},
				},

				x(Sx.Show) {
					when = function() return restartModal() end,
					x("view") {
						style = {
							position = "absolute", top = 0, left = 0, right = 0, bottom = 0,
							display = "flex", alignItems = "center", justifyContent = "center",
							backgroundColor = "rgba(0,0,0,0.5)",
						},
						x("view") {
							style = {
								backgroundColor = "#1a1a2e",
								borderWidth = 1, borderColor = "rgba(255,255,255,0.12)",
								borderRadius = 10,
								paddingTop = 24, paddingBottom = 24, paddingLeft = 28, paddingRight = 28,
								minWidth = 320,
								alignItems = "center",
							},
							x("text") { style = { fontSize = 16, fontStyle = "bold", marginBottom = 8 }, "Restart Required" },
							x("text") {
								style = { fontSize = 13, color = "rgba(255,255,255,0.5)", marginBottom = 20, textAlign = "center" },
								"The renderer has been changed. A restart is required for this change to take effect.",
							},
							x("view") {
								style = { display = "flex", flexDirection = "row", justifyContent = "center", gap = 10 },
								x(Btn) { variant = "primary", label = "Restart Now", onPress = function() UiLib.call("restartNow", {}) end },
								x(Btn) {
									label = "Later",
									onPress = function()
										setRestartModal(false)
										if pendingClose.current then
											pendingClose.current = false
											if onClose ~= nil then onClose() end
										end
									end,
								},
							},
						},
					},
				},
			},
		},
	}
end

return Settings
