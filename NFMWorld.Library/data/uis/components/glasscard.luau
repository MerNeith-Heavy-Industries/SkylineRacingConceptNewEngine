--!strict

-- Shared glassmorphism primitives ported from
-- nfm-world/data/html/src/shared/components/GlassCard.tsx
--
-- Note: the Lua `styled` engine does not support `backdrop-filter`,
-- `box-shadow`, `transition`, `transform`, or `animation`, so those
-- are dropped; only the supported style properties are used.

local React = require("../../library/react")

local x = React.x
local View = React.View
local Text = React.Text

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

-- ── GlassCard ────────────────────────────────────────────────────
-- Card with a 3px colored top accent bar.

local function GlassCard(props: { color: string?, style: React.Styles?, children: React.Children } & React.ViewProps): React.VNode
	local color = props.color or "#4fc3f7"

	local style: React.Styles = {
		backgroundColor = "rgba(255,255,255,0.05)",
		borderRadius = 12,
		borderWidth = 1,
		borderColor = "rgba(255,255,255,0.1)",
		paddingTop = 16,
		paddingBottom = 16,
		paddingLeft = 20,
		paddingRight = 20,
		position = "relative",
		overflow = "hidden",
	}

	if props.style ~= nil then
		for k, v in pairs((props.style :: any)) do
			(style :: any)[k] = v
		end
	end

	local bar = x(View) {
		style = {
			position = "absolute",
			top = 0,
			left = 0,
			right = 0,
			height = 3,
			backgroundColor = color,
			borderTopLeftRadius = 12,
			borderTopRightRadius = 12,
		},
	}

	-- Forward remaining props (onmousedown, key, ...) to the wrapper View.
	local wrapperProps: { [string]: any } = {}
	for k, v in pairs(props :: any) do
		if k ~= "color" and k ~= "style" and k ~= "children" and k ~= "key" then
			wrapperProps[k] = v
		end
	end
	wrapperProps.style = style

	return React.createElement(View, wrapperProps, bar, props.children)
end

-- ── StatBar ──────────────────────────────────────────────────────
-- Labeled progress bar (value is 0..1).

local function StatBar(props: { value: number?, color: string?, height: number?, label: string? }): React.VNode
	local value = clamp01(props.value or 0)
	local color = props.color or "#4fc3f7"
	local height = props.height or 8
	local label = props.label or ""

	local pct = math.floor(value * 100 + 0.5)

	return x(View) {
		style = { marginBottom = 8 },
		x(View) {
			style = {
				display = "flex",
				flexDirection = "row",
				justifyContent = "space-between",
				marginBottom = 4,
			},
			x(Text) {
				style = { fontSize = 12, color = "rgba(255,255,255,0.7)" },
				label,
			},
			x(Text) {
				style = { fontSize = 12, color = "rgba(255,255,255,0.7)" },
				("%d%%"):format(pct),
			},
		},
		x(View) {
			style = {
				width = "100%",
				height = height,
				backgroundColor = "rgba(255,255,255,0.1)",
				borderRadius = height / 2,
				overflow = "hidden",
			},
			x(View) {
				style = {
					width = ("%d%%"):format(pct),
					height = "100%",
					backgroundColor = color,
					borderRadius = height / 2,
				},
			},
		},
	}
end

-- ── CenterText ───────────────────────────────────────────────────
-- Large center-screen text overlay. Splits on "\n".

local function splitLines(s: string): { string }
	local t = {}
	local start = 1
	while true do
		local i = string.find(s, "\n", start, true)
		if i == nil then
			t[#t + 1] = s:sub(start)
			break
		end
		t[#t + 1] = s:sub(start, i - 1)
		start = i + 1
	end
	return t
end

local function CenterText(props: { text: string?, size: number?, color: string? }): React.VNode
	local text = props.text or ""
	local size = props.size or 64
	local color = props.color or "rgba(255,255,255,0.9)"
	local lines = splitLines(text)

	return x(View) {
		style = {
			position = "absolute",
			top = 0,
			left = 0,
			right = 0,
			bottom = 0,
			display = "flex",
			flexDirection = "column",
			alignItems = "center",
			justifyContent = "center",
		},
		map(lines, function(line: string): React.VNode
			return x(Text) {
				style = { fontSize = size, color = color, fontStyle = "bold", textAlign = "center" },
				line,
			}
		end),
	}
end

return {
	GlassCard = GlassCard,
	StatBar = StatBar,
	CenterText = CenterText,
}
