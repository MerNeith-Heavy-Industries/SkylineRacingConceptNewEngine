--!strict
-- Unified Button for the Sx UI.
--
-- Every button shares one implementation and visual language, including a fine-grained
-- hover highlight (only the hovered button re-renders). `variant` picks the color
-- language (primary / danger / warning / ghost / default); `active` adds an accent
-- "selected" state (tabs, toggles); `size` / `fullWidth` / `style` / `textStyle` adapt it
-- to the surrounding layout.

local Sx = require("../sx/index")
local theme = require("./theme")
local x = Sx.x

export type ButtonProps = {
	label: Sx.Signalish<string>,
	onPress: () -> (),
	variant: "primary" | "danger" | "warning" | "ghost"?,
	active: Sx.Signalish<boolean?>?,
	size: "sm" | "md" | "lg"?,
	fullWidth: boolean?,
	disabled: boolean?,
	style: Sx.Styles?,
	textStyle: Sx.TextStyles?,
}

local function paddingFor(size: string): (number, number, number, number)
	if size == "lg" then return 14, 14, 24, 24 end
	if size == "sm" then return 4, 4, 10, 10 end
	return 8, 8, 20, 20 -- md
end

local function Button(props: ButtonProps)
	local hovered, setHovered = Sx.createSignal(false)
	local disabled = props.disabled == true
	local size = props.size or "md"
	local padTop, padBottom, padLeft, padRight = paddingFor(size)

	return x(Sx.View) {
		style = Sx.createMemo(function(): Sx.Styles
			local active = props.active ~= nil and Sx.read(props.active)
			local hov = hovered() and not disabled
			local variant = props.variant

			local bg: string = theme.surface
			local border: string = theme.surfaceBorder

			if variant == "primary" then
				bg = hov and theme.accentSoftStrong or theme.surface
				border = theme.accentBorder
			elseif variant == "danger" then
				bg = hov and theme.dangerSoftStrong or theme.dangerSoft
				border = theme.dangerBorder
			elseif variant == "warning" then
				bg = hov and theme.warningSoftStrong or theme.warningSoft
				border = theme.warningBorder
			elseif active then
				-- Tabs / toggles: an active ghost or default button shows the accent state.
				bg = hov and theme.accentSoftStrong or theme.accentSoft
				border = theme.accentBorder
			elseif variant == "ghost" then
				bg = hov and theme.surfaceHover or "transparent"
				border = "transparent"
			else
				bg = hov and theme.surfaceHover or theme.surface
				border = theme.surfaceBorder
			end

			local style: Sx.Styles = {
				display = "flex",
				flexDirection = "row",
				alignItems = "center",
				justifyContent = "center",
				gap = 6,
				paddingTop = padTop,
				paddingBottom = padBottom,
				paddingLeft = padLeft,
				paddingRight = padRight,
				backgroundColor = bg,
				borderWidth = 1,
				borderColor = border,
				borderRadius = 6,
				width = props.fullWidth and "100%" or nil,
			}

			if props.style ~= nil then
				for k, v in pairs(props.style :: any) do
					(style :: any)[k] = v
				end
			end

			return style :: Sx.Styles
		end),
		onmousedown = function()
			if disabled then return end
			props.onPress()
		end,
		onmouseenter = function()
			setHovered(true)
		end,
		onmouseleave = function()
			setHovered(false)
		end,
		x(Sx.Text) {
			style = Sx.createMemo(function(): Sx.TextStyles
				local active = props.active ~= nil and Sx.read(props.active)
				local variant = props.variant
				local color: string = theme.textPrimary
				if disabled then
					color = theme.textFaint
				elseif variant == "warning" and active then
					color = theme.warning
				elseif variant == "ghost" and not active then
					color = theme.textSecondary
				end

				local ts: { [string]: any } = { fontSize = 13, fontStyle = "bold", color = color }
				if props.textStyle ~= nil then
					for k, v in pairs(props.textStyle :: any) do
						ts[k] = v
					end
				end
				return ts :: Sx.TextStyles
			end),
			function() return Sx.read(props.label) end,
		},
	}
end

return Button
