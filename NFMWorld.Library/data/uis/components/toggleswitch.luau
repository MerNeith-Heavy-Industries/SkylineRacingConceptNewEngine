--!strict
-- ToggleSwitch — reusable fine-grained reactive switch for Sx.
--
-- Controlled component: `checked` (signal/accessor) + `onToggle`. A pill track with a
-- sliding knob, visually distinct from a Checkbox. Optional reactive `label`.

local Sx = require("../../library/sx/index")
local x = Sx.x

export type ToggleSwitchProps = {
	checked: Sx.Signalish<boolean>,
	onToggle: () -> (),
	label: Sx.Signalish<string>?,
	disabled: boolean?,
	size: number?,
}

local function ToggleSwitch(props: ToggleSwitchProps)
	local size = props.size or 20
	local width = math.floor(size * 2.2 + 0.5)
	local knob = math.max(4, size - 4)
	local pad = 2
	local disabled = props.disabled == true

	return x(Sx.View) {
		style = {
			display = "flex",
			flexDirection = "row",
			alignItems = "center",
			gap = 8,
		},
		onmousedown = function()
			if disabled then return end
			props.onToggle()
		end,
		x(Sx.View) {
			style = Sx.createMemo(function(): Sx.Styles
				local checked = Sx.read(props.checked)
				return {
					width = width,
					height = size,
					position = "relative",
					backgroundColor = checked and "#4fc3f7" or "rgba(255,255,255,0.2)",
					borderRadius = size / 2,
				}
			end),
			x(Sx.View) {
				style = Sx.createMemo(function(): Sx.Styles
					local checked = Sx.read(props.checked)
					return {
						position = "absolute",
						top = pad,
						left = checked and (width - knob - pad) or pad,
						width = knob,
						height = knob,
						backgroundColor = "#ffffff",
						borderRadius = knob / 2,
					}
				end),
			},
		},
		x(Sx.Text) {
			style = Sx.createMemo(function(): Sx.TextStyles
				local checked = Sx.read(props.checked)
				return {
					fontSize = 13,
					color = disabled
						and "rgba(255,255,255,0.3)"
						or (checked and "#fff" or "rgba(255,255,255,0.7)"),
				}
			end),
			function()
				local label = props.label
				if label == nil then return "" end
				return Sx.read(label)
			end,
		},
	}
end

return ToggleSwitch
