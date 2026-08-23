--!strict

-- In-race HUD overlay ported from nfm-world/data/html/src/pages/RaceHud.tsx.

local React = require("../../library/react")
local styled = require("../../library/styled")
local glasscard = require("../components/glasscard")
local GlassCard = glasscard.GlassCard
local StatBar = glasscard.StatBar
local CenterText = glasscard.CenterText
local PauseMenu = require("../components/pausemenu")

local useState = React.useState
local useEffect = React.useEffect
local x = React.x
local View = React.View
local Text = React.Text

local Root = styled(View) {
	width = "100%",
	height = "100%",
	position = "relative",
}

local CornerLabel = styled(Text) {
	fontSize = 11,
	color = "rgba(255,255,255,0.5)",
	marginBottom = 4,
}

local function formatTime(ms: number): string
	local mins = math.floor(ms / 60000)
	local secs = math.floor(ms / 1000) % 60
	local msecs = ms % 1000
	local minsFmt = ("%02d"):format(mins)
	local secsFmt = ("%02d"):format(secs)
	local msFmt = ("%03d"):format(msecs)
	if mins > 0 then
		return ("%s:%s.%s"):format(minsFmt, secsFmt, msFmt)
	end
	return ("%s.%s"):format(secsFmt, msFmt)
end

local function RaceHud(): React.VNode
	local hud, setHud = useState<<any>>(nil)
	local isPaused, setIsPaused = useState<<boolean>>(false)

	useEffect(function()
		return UiLib.onEvent("race:hudState", function(data: any)
			if data ~= nil then setHud(data) end
		end)
	end, {})

	useEffect(function()
		return UiLib.onEvent("race:paused", function(data: any)
			setIsPaused(data)
		end)
	end, {})

	-- Derived values (computed each render; C# pushes hudState each tick).
	local speed = hud ~= nil and hud.speed or 0
	local power = hud ~= nil and hud.power or 1
	local damage = hud ~= nil and hud.damage or 0
	local lap = hud ~= nil and hud.lap or 1
	local totalLaps = hud ~= nil and hud.totalLaps or 3
	local position = hud ~= nil and hud.position or 1
	local totalRacers = hud ~= nil and hud.totalRacers or 1
	local lapTime = hud ~= nil and hud.lapTime or 0
	local chkDiffMs = hud ~= nil and hud.chkDiffMs or nil
	local lastChkDiffMs = hud ~= nil and hud.lastChkDiffMs or nil
	local lapDiffMs = hud ~= nil and hud.lapDiffMs or nil
	local lastLapDiffMs = hud ~= nil and hud.lastLapDiffMs or nil
	local countdownTimer = hud ~= nil and hud.countdownTimer or 0

	local stateText = hud ~= nil and hud.stateText or nil
	local stateTextEndsAt = hud ~= nil and hud.stateTextEndsAt or nil
	if stateTextEndsAt ~= nil and os.time() >= stateTextEndsAt then
		stateText = nil
	end

	local actualStateText
	if countdownTimer > 0 then
		actualStateText = ("Starting in %d"):format(countdownTimer)
	else
		actualStateText = stateText or ""
	end

	local speedKmh = speed * 1.4 * 21.0 * 60.0 * 60.0 / 100000.0

	return x(Root) {
		actualStateText ~= "" and x(CenterText) { text = actualStateText, size = 24 },

		x(View) {
			style = {
				position = "absolute",
				top = 16,
				right = 16,
				width = 200,
				display = "flex",
				flexDirection = "column",
				gap = 8,
			},
			x(GlassCard) { color = "#ff5252",
				x(CornerLabel) { "Power" },
				x(StatBar) { label = "", value = power / 0.98, color = "#ff5252", height = 10 },
			},
			x(GlassCard) { color = "#4fc3f7",
				x(CornerLabel) { "Damage" },
				x(StatBar) { label = "", value = damage, color = "#4fc3f7", height = 10 },
			},
		},

		x(View) {
			style = { position = "absolute", top = 16, left = 16, width = 180 },
			x(GlassCard) { color = "#69f0ae",
				x(CornerLabel) { ("Lap %d/%d"):format(lap, totalLaps) },
				x(Text) { style = { fontSize = 28, fontStyle = "bold" }, formatTime(lapTime) },
				chkDiffMs ~= nil and lastChkDiffMs ~= nil and x(Text) {
					style = { fontSize = 11, color = "rgba(255,255,255,0.4)", marginTop = 4 },
					("CHK Diff: %s (Last: %s)"):format(formatTime(chkDiffMs), formatTime(lastChkDiffMs)),
				},
				lapDiffMs ~= nil and lastLapDiffMs ~= nil and x(Text) {
					style = { fontSize = 11, color = "rgba(255,255,255,0.4)", marginTop = 4 },
					("Lap Diff: %s (Last: %s)"):format(formatTime(lapDiffMs), formatTime(lastLapDiffMs)),
				},
			},
		},

		x(View) {
			style = {
				position = "absolute",
				bottom = 24,
				left = 0,
				right = 0,
				display = "flex",
				flexDirection = "column",
				alignItems = "center",
			},
			x(Text) { style = { fontSize = 56, fontStyle = "bold" }, ("%d"):format(math.floor(speedKmh + 0.5)) },
			x(Text) { style = { fontSize = 14, color = "rgba(255,255,255,0.5)" }, "KM/H" },
		},

		x(View) {
			style = {
				position = "absolute",
				bottom = 24,
				right = 24,
				display = "flex",
				flexDirection = "column",
				alignItems = "flex-end",
			},
			x(Text) { style = { fontSize = 36, fontStyle = "bold" }, ("%d/%d"):format(position, totalRacers) },
			x(Text) { style = { fontSize = 12, color = "rgba(255,255,255,0.5)" }, "POSITION" },
		},

		x(PauseMenu) { visible = isPaused },
	}
end

return RaceHud
