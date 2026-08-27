-- In-race HUD overlay (Sx port of preact racehud.luau), ported from
-- nfm-world/data/html/src/pages/RaceHud.tsx. Fine-grained: each telemetry value is a
-- signal; a per-frame `race:hudState` push updates only the affected leaf text/bar via
-- commitTextUpdate/setProperty.

local Sx = require("../../library/sx/init")
local styled = Sx.styled
local x = Sx.x
local glasscard = require("../components/glasscard")
local GlassCard = glasscard.GlassCard
local StatBar = glasscard.StatBar
local CenterText = glasscard.CenterText
local PauseMenu = require("../components/pausemenu")

local Root = styled("view") {
	width = "100%",
	height = "100%",
	position = "relative",
}

local CornerLabel = styled("text") {
	fontSize = 11,
	color = "rgba(255,255,255,0.5)",
	marginBottom = 4,
}

local function PowerDamageBars()
	local power, setPower = Sx.createSignal(1)
	local damage, setDamage = Sx.createSignal(0)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("race:hudState", function(data)
			if data ~= nil then
				setPower(data.power)
				setDamage(data.damage)
			end
		end)
		Sx.onCleanup(unsub)
	end)

	return x("view") {
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
			x(StatBar) { label = "", value = function() return power() / 0.98 end, color = "#ff5252", height = 10 },
		},
		x(GlassCard) { color = "#4fc3f7",
			x(CornerLabel) { "Damage" },
			x(StatBar) { label = "", value = function() return damage() end, color = "#4fc3f7", height = 10 },
		},
	}
end

local function Splits()
	local lapDiffMs, setLapDiffMs = Sx.createSignal(0)
	local lastLapDiffMs, setLastLapDiffMs = Sx.createSignal(0)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("race:hudState", function(data)
			if data ~= nil then
				setLapDiffMs(data.lapDiffMs or 0)
				setLastLapDiffMs(data.lastLapDiffMs or 0)
			end
		end)
		Sx.onCleanup(unsub)
	end)

	return x("view") {
		style = {
			position = "absolute",
			bottom = 24,
			left = 0,
			right = 0,
			display = "flex",
			flexDirection = "column",
			alignItems = "flex-end",
		},
		x("text") {
			style = { fontSize = 36, fontStyle = "bold" },
			function() return ("Lap Diff: %d"):format(lapDiffMs()) end,
		},
		x("text") {
			style = { fontSize = 14, color = "rgba(255,255,255,0.5)" },
			function() return ("Last: %d"):format(lastLapDiffMs()) end,
		},
	}
end

local function Speed()
	local speed, setSpeed = Sx.createSignal(0)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("race:hudState", function(data)
			if data ~= nil then
				setSpeed(data.speed)
			end
		end)
		Sx.onCleanup(unsub)
	end)

	local speedKmh = Sx.createMemo(function()
		return math.floor(speed() * 1.4 * 21.0 * 60.0 * 60.0 / 100000.0 + 0.5)
	end)

	return x("view") {
		style = {
			position = "absolute",
			bottom = 24,
			left = 0,
			right = 0,
			display = "flex",
			flexDirection = "column",
			alignItems = "center",
		},
		x("text") { style = { fontSize = 56, fontStyle = "bold" }, function() return ("%d"):format(speedKmh()) end },
		x("text") { style = { fontSize = 14, color = "rgba(255,255,255,0.5)" }, "KM/H" },
	}
end

local function RaceHud()
	local position, setPosition = Sx.createSignal(1)
	local totalRacers, setTotalRacers = Sx.createSignal(1)
	local countdownTimer, setCountdownTimer = Sx.createSignal(0)
	local stateText, setStateText = Sx.createSignal("")
	local stateTextEndsAt, setStateTextEndsAt = Sx.createSignal(nil)
	local isPaused, setIsPaused = Sx.createSignal(false)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("race:hudState", function(data)
			if data ~= nil then
				setPosition(data.position)
				setTotalRacers(data.totalRacers)
				setCountdownTimer(data.countdownTimer)
				setStateText(data.stateText)
				setStateTextEndsAt(data.stateTextEndsAt)
			end
		end)
		Sx.onCleanup(unsub)
	end)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("race:paused", function(data)
			setIsPaused(data)
		end)
		Sx.onCleanup(unsub)
	end)

	local actualStateText = Sx.createMemo(function()
		local stEndsAt = stateTextEndsAt()
		local stText = stateText()
		if stEndsAt ~= nil and os.time() >= stEndsAt then
			stText = nil
		end
		local countdown = countdownTimer()
		if countdown > 0 then
			return ("Starting in %d"):format(countdown)
		end
		return stText or ""
	end)

	return x(Root) {
		x(Sx.Show) {
			when = function() return actualStateText() ~= "" end,
			x(CenterText) { text = function() return actualStateText() end, size = 24 },
		},

		x(PowerDamageBars) {},

		x(Splits) {},

		x(Speed) {},

		x("view") {
			style = {
				position = "absolute",
				bottom = 24,
				right = 24,
				display = "flex",
				flexDirection = "column",
				alignItems = "flex-end",
			},
			x("text") { style = { fontSize = 36, fontStyle = "bold" }, function() return ("%d/%d"):format(position(), totalRacers()) end },
			x("text") { style = { fontSize = 12, color = "rgba(255,255,255,0.5)" }, "POSITION" },
		},

		x(Sx.Show) {
			when = isPaused,
			x(PauseMenu) {},
		},
	}
end

return RaceHud
