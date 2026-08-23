--!strict

-- Pause menu overlay ported from nfm-world/data/html/src/pages/PauseMenu.tsx.
-- Controlled by a `visible` prop from RaceHud (driven by the "race:paused" event).

local React = require("../../library/react")
local styled = require("../../library/styled")
local Settings = require("./settings")

local useState = React.useState
local useEffect = React.useEffect
local x = React.x
local View = React.View
local Text = React.Text

-- ── Styled components ────────────────────────────────────────────

local Overlay = styled(View) {
	position = "absolute",
	top = 0,
	left = 0,
	right = 0,
	bottom = 0,
	display = "flex",
	alignItems = "center",
	justifyContent = "center",
	backgroundColor = "rgba(0,0,0,0.65)",
}

local MenuCard = styled(View) {
	width = 380,
	paddingTop = 36,
	paddingBottom = 36,
	paddingLeft = 40,
	paddingRight = 40,
	backgroundColor = "rgba(20,20,40,0.85)",
	borderWidth = 1,
	borderColor = "rgba(255,255,255,0.1)",
	borderRadius = 16,
	display = "flex",
	flexDirection = "column",
	alignItems = "center",
	gap = 6,
}

local Title = styled(Text) {
	fontSize = 22,
	fontStyle = "bold",
	marginBottom = 4,
}

local Subtitle = styled(Text) {
	fontSize = 12,
	color = "rgba(255,255,255,0.35)",
	marginBottom = 20,
}

local RaceInfo = styled(View) {
	width = "100%",
	display = "flex",
	flexDirection = "row",
	justifyContent = "center",
	gap = 32,
	marginBottom = 20,
	paddingTop = 10,
	paddingBottom = 10,
	borderTopWidth = 1,
	borderBottomWidth = 1,
	borderColor = "rgba(255,255,255,0.06)",
}

local InfoItem = styled(View) {
	display = "flex",
	flexDirection = "column",
	alignItems = "center",
	gap = 2,
}

local InfoLabel = styled(Text) {
	fontSize = 10,
	color = "rgba(255,255,255,0.35)",
}

local InfoValue = styled(Text) {
	fontSize = 18,
	fontStyle = "bold",
}

local ConfirmBox = styled(View) {
	backgroundColor = "#1a1a2e",
	borderWidth = 1,
	borderColor = "rgba(255,255,255,0.12)",
	borderRadius = 10,
	paddingTop = 24,
	paddingBottom = 24,
	paddingLeft = 28,
	paddingRight = 28,
	minWidth = 320,
	alignItems = "center",
}

local ConfirmTitle = styled(Text) {
	fontSize = 16,
	fontStyle = "bold",
	marginBottom = 8,
}

local ConfirmMsg = styled(Text) {
	fontSize = 13,
	color = "rgba(255,255,255,0.45)",
	marginBottom = 20,
	textAlign = "center",
}

local ConfirmBtns = styled(View) {
	display = "flex",
	flexDirection = "row",
	justifyContent = "center",
	gap = 10,
}

-- ── Button helpers ───────────────────────────────────────────────

local function menuBtnStyle(variant: string?): { [string]: any }
	local primary = variant == "primary"
	local danger = variant == "danger"
	if danger then
		return {
			width = "100%",
			paddingTop = 14, paddingBottom = 14, paddingLeft = 24, paddingRight = 24,
			backgroundColor = "rgba(255,82,82,0.12)",
			borderWidth = 1, borderColor = "rgba(255,82,82,0.25)",
			borderRadius = 8,
			alignItems = "center",
		}
	elseif primary then
		return {
			width = "100%",
			paddingTop = 14, paddingBottom = 14, paddingLeft = 24, paddingRight = 24,
			backgroundColor = "rgba(79,195,247,0.15)",
			borderWidth = 1, borderColor = "rgba(255,255,255,0.1)",
			borderRadius = 8,
			alignItems = "center",
		}
	end
	return {
		width = "100%",
		paddingTop = 14, paddingBottom = 14, paddingLeft = 24, paddingRight = 24,
		backgroundColor = "rgba(255,255,255,0.05)",
		borderWidth = 1, borderColor = "rgba(255,255,255,0.1)",
		borderRadius = 8,
		alignItems = "center",
	}
end

local function MenuBtn(props: { variant: "danger" | "primary" | nil, label: string, onPress: () -> () }): React.VNode
	return x(View) {
		style = menuBtnStyle(props.variant),
		onmousedown = function() props.onPress() end,
		x(Text) { style = { fontSize = 15, fontStyle = "bold", color = "#fff" }, props.label },
	}
end

local function ConfirmBtn(props: { variant: "danger" | "primary" | nil, label: string, onPress: () -> () }): React.VNode
	local danger = props.variant == "danger"
	return x(View) {
		style = {
			paddingTop = 10, paddingBottom = 10, paddingLeft = 28, paddingRight = 28,
			backgroundColor = danger and "rgba(255,82,82,0.18)" or "rgba(255,255,255,0.06)",
			borderWidth = 1, borderColor = danger and "rgba(255,82,82,0.3)" or "rgba(255,255,255,0.12)",
			borderRadius = 6,
		},
		onmousedown = function() props.onPress() end,
		x(Text) { style = { fontSize = 13, fontStyle = "bold", color = "#fff" }, props.label },
	}
end

-- ── Component ────────────────────────────────────────────────────

local function PauseMenu(props: any): React.VNode | nil
	local visible = props.visible

	local view, setView = useState<<string>>("menu")
	local pauseState, setPauseState = useState<<any>>(nil)

	useEffect(function()
		return UiLib.onEvent("race:pauseState", function(data: any)
			setPauseState(data)
		end)
	end, {})

	useEffect(function()
		if not visible then setView("menu") end
	end, { visible })

	useEffect(function()
		if not visible then return end
		if view == "settings" then
			UiLib.call("settingsOpened", {})
		else
			UiLib.call("settingsClosed", {})
		end
	end, { view, visible })

	if not visible then return nil end

	if view == "settings" then
		return x(View) {
			style = { position = "absolute", top = 0, left = 0, right = 0, bottom = 0 },
			x(Settings) { onClose = function() setView("menu") end },
		}
	end

	if view == "confirmQuit" then
		return x(Overlay) {
			x(View) {
				style = {
					position = "absolute", top = 0, left = 0, right = 0, bottom = 0,
					display = "flex", alignItems = "center", justifyContent = "center",
					backgroundColor = "rgba(0,0,0,0.7)",
				},
				onmousedown = function() setView("menu") end,
				x(ConfirmBox) {
					onmousedown = function() end,
					x(ConfirmTitle) { "Quit to Main Menu?" },
					x(ConfirmMsg) { "All race progress will be lost." },
					x(ConfirmBtns) {
						x(ConfirmBtn) { label = "Cancel", onPress = function() setView("menu") end },
						x(ConfirmBtn) { variant = "danger", label = "Quit", onPress = function() UiLib.call("quit", {}) end },
					},
				},
			},
		}
	end

	if view == "confirmRestart" then
		return x(Overlay) {
			x(View) {
				style = {
					position = "absolute", top = 0, left = 0, right = 0, bottom = 0,
					display = "flex", alignItems = "center", justifyContent = "center",
					backgroundColor = "rgba(0,0,0,0.7)",
				},
				onmousedown = function() setView("menu") end,
				x(ConfirmBox) {
					onmousedown = function() end,
					x(ConfirmTitle) { "Restart Race?" },
					x(ConfirmMsg) { "Your current lap time and position will be reset." },
					x(ConfirmBtns) {
						x(ConfirmBtn) { label = "Cancel", onPress = function() setView("menu") end },
						x(ConfirmBtn) { variant = "danger", label = "Restart", onPress = function() UiLib.call("restart", {}) end },
					},
				},
			},
		}
	end

	local stageName = pauseState ~= nil and pauseState.stageName or "Race"

	return x(Overlay) {
		x(MenuCard) {
			x(Title) { "Paused" },
			x(Subtitle) { stageName },

			pauseState ~= nil and x(RaceInfo) {
				x(InfoItem) {
					x(InfoLabel) { "Lap" },
					x(InfoValue) { ("%d/%d"):format(pauseState.lap, pauseState.totalLaps) },
				},
				x(InfoItem) {
					x(InfoLabel) { "Position" },
					x(InfoValue) { ("%d/%d"):format(pauseState.position, pauseState.totalRacers) },
				},
			},

			x(MenuBtn) { variant = "primary", label = "Resume", onPress = function() UiLib.call("resume", {}) end },
			x(MenuBtn) { label = "Restart", onPress = function() setView("confirmRestart") end },
			x(MenuBtn) { label = "Settings", onPress = function() setView("settings") end },
			x(MenuBtn) { variant = "danger", label = "Quit to Main Menu", onPress = function() setView("confirmQuit") end },
		},
	}
end

return PauseMenu
