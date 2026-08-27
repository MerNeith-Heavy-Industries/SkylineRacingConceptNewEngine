-- Pause menu overlay (Sx port of preact pausemenu.luau).
-- Controlled by a `visible` prop from RaceHud (driven by the "race:paused" event).

local Sx = require("../../library/sx/init")
local styled = Sx.styled
local x = Sx.x
local Settings = require("./settings")

-- ── Styled components ────────────────────────────────────────────

local Overlay = styled("view") {
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

local MenuCard = styled("view") {
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

local Title = styled("text") {
	fontSize = 22,
	fontStyle = "bold",
	marginBottom = 4,
}

local Subtitle = styled("text") {
	fontSize = 12,
	color = "rgba(255,255,255,0.35)",
	marginBottom = 20,
}

local RaceInfo = styled("view") {
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

local InfoItem = styled("view") {
	display = "flex",
	flexDirection = "column",
	alignItems = "center",
	gap = 2,
}

local InfoLabel = styled("text") {
	fontSize = 10,
	color = "rgba(255,255,255,0.35)",
}

local InfoValue = styled("text") {
	fontSize = 18,
	fontStyle = "bold",
}

local ConfirmBox = styled("view") {
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

local ConfirmTitle = styled("text") {
	fontSize = 16,
	fontStyle = "bold",
	marginBottom = 8,
}

local ConfirmMsg = styled("text") {
	fontSize = 13,
	color = "rgba(255,255,255,0.45)",
	marginBottom = 20,
	textAlign = "center",
}

local ConfirmBtns = styled("view") {
	display = "flex",
	flexDirection = "row",
	justifyContent = "center",
	gap = 10,
}

-- ── Button helpers ───────────────────────────────────────────────

local function menuBtnStyle(variant)
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

local function MenuBtn(props)
	return x("view") {
		style = menuBtnStyle(props.variant),
		onmousedown = function() props.onPress() end,
		x("text") { style = { fontSize = 15, fontStyle = "bold", color = "#fff" }, props.label },
	}
end

local function ConfirmBtn(props)
	local danger = props.variant == "danger"
	return x("view") {
		style = {
			paddingTop = 10, paddingBottom = 10, paddingLeft = 28, paddingRight = 28,
			backgroundColor = danger and "rgba(255,82,82,0.18)" or "rgba(255,255,255,0.06)",
			borderWidth = 1, borderColor = danger and "rgba(255,82,82,0.3)" or "rgba(255,255,255,0.12)",
			borderRadius = 6,
		},
		onmousedown = function() props.onPress() end,
		x("text") { style = { fontSize = 13, fontStyle = "bold", color = "#fff" }, props.label },
	}
end

-- ── Component ────────────────────────────────────────────────────

local function PauseMenu()
	local view, setView = Sx.createSignal("menu")
	local pauseState, setPauseState = Sx.createSignal(nil)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("race:pauseState", function(data)
			setPauseState(data)
		end)
		Sx.onCleanup(unsub)
	end)

	Sx.createEffect(function()
		if view() == "settings" then
			UiLib.call("settingsOpened", {})
		else
			UiLib.call("settingsClosed", {})
		end
	end)

	return (
		x(Sx.Switch) {
			-- Settings
			x(Sx.Match) {
				when = function() return view() == "settings" end,
				x("view") {
					style = { position = "absolute", top = 0, left = 0, right = 0, bottom = 0 },
					x(Settings) { onClose = function() setView("menu") end },
				},
			},
			-- Confirm quit
			x(Sx.Match) {
				when = function() return view() == "confirmQuit" end,
				x(Overlay) {
					x("view") {
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
				},
			},
			-- Confirm restart
			x(Sx.Match) {
				when = function() return view() == "confirmRestart" end,
				x(Overlay) {
					x("view") {
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
				},
			},
			-- Main menu
			x(Sx.Match) {
				when = function() return true end,
				x(Overlay) {
					x(MenuCard) {
						x(Title) { "Paused" },
						x(Subtitle) { function()
							local ps = pauseState()
							return ps ~= nil and ps.stageName or "Race"
						end },

						x(Sx.Show) {
							when = function() return pauseState() ~= nil end,
							x(RaceInfo) {
								x(InfoItem) {
									x(InfoLabel) { "Lap" },
									x(InfoValue) { function()
										local ps = pauseState()
										return ("%d/%d"):format(ps.lap, ps.totalLaps)
									end },
								},
								x(InfoItem) {
									x(InfoLabel) { "Position" },
									x(InfoValue) { function()
										local ps = pauseState()
										return ("%d/%d"):format(ps.position, ps.totalRacers)
									end },
								},
							},
						},

						x(MenuBtn) { variant = "primary", label = "Resume", onPress = function() UiLib.call("resume", {}) end },
						x(MenuBtn) { label = "Restart", onPress = function() setView("confirmRestart") end },
						x(MenuBtn) { label = "Settings", onPress = function() setView("settings") end },
						x(MenuBtn) { variant = "danger", label = "Quit to Main Menu", onPress = function() setView("confirmQuit") end },
					},
				},
			},
		}
	)
end

return PauseMenu
