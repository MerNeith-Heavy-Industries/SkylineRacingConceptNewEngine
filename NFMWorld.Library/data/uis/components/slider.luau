--!strict
-- Slider — reusable fine-grained reactive slider for Sx.
--
-- Controlled component: `value` (signal/accessor) + `onValue`. Maps pointer X across
-- a track of `width` px to [min, max], snapped to `step`. Both click and drag update.
-- Generalizes the hardcoded-track slider from settings.luau (`width` replaces TRACK_W).

local Sx = require("../../library/sx/index")
local x = Sx.x

export type SliderProps = {
	min: number?,
	max: number?,
	step: number?,
	value: Sx.Signalish<number>,
	onValue: (number) -> (),
	width: number?,
	disabled: boolean?,
}

local function clamp01(v: number)
	if v < 0 then return 0 end
	if v > 1 then return 1 end
	return v
end

local function Slider(props: SliderProps)
	local min = props.min or 0
	local max = props.max or 1
	local step = props.step or 1
	local trackW = props.width or 160
	local disabled = props.disabled == true

	local function setFromX(px: number)
		local frac = clamp01(px / trackW)
		local raw = min + frac * (max - min)
		local stepped = raw
		if step > 0 then
			stepped = math.floor((raw - min) / step + 0.5) * step + min
		end
		if stepped < min then stepped = min end
		if stepped > max then stepped = max end
		props.onValue(stepped)
	end

	return x(Sx.View) {
		style = {
			width = trackW,
			height = 20,
			display = "flex",
			flexDirection = "row",
			alignItems = "center",
		},
		onmousedown = function(event)
			if disabled then return end
			setFromX(event.relativePosition.x)
		end,
		onmousedrag = function(event)
			if disabled then return end
			setFromX(event.relativePosition.x)
		end,
		x(Sx.View) {
			style = {
				width = "100%",
				height = 4,
				backgroundColor = "rgba(255,255,255,0.15)",
				borderRadius = 2,
				overflow = "hidden",
			},
			x(Sx.View) {
				style = Sx.createMemo(function(): Sx.Styles
					local value = Sx.read(props.value) or 0
					local frac = clamp01((value - min) / (max - min))
					return {
						width = ("%d%%"):format(math.floor(frac * 100 + 0.5)),
						height = "100%",
						backgroundColor = disabled and "rgba(79,195,247,0.4)" or "#4fc3f7",
					}
				end),
			},
		},
	}
end

return Slider
