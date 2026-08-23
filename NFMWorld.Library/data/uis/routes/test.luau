--!strict

-- Test page ported from nfm-world/data/html/src/pages/Test.tsx.

local React = require("../../library/react")
local styled = require("../../library/styled")

local useState = React.useState
local useEffect = React.useEffect
local x = React.x
local View = React.View
local Text = React.Text

local Root = styled(View) {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "column",
	alignItems = "center",
	justifyContent = "center",
	gap = 16,
}

local Label = styled(Text) {
	fontSize = 14,
	color = "rgba(255,255,255,0.5)",
}

local Counter = styled(Text) {
	fontSize = 64,
	fontStyle = "bold",
	color = "#4fc3f7",
}

local function TestPage(): React.VNode
	local counter, setCounter = useState<<integer>>(0)

	useEffect(function()
		return UiLib.onEvent("test:counter", function(data: any)
			if data ~= nil then
				setCounter(data.value or 0)
			else
				setCounter(0)
			end
		end)
	end, {})

	return x(Root) {
		x(Label) { "CEF + Preact Test" },
		x(Counter) { ("%d"):format(counter) },
		x(View) {
			style = {
				paddingTop = 10,
				paddingBottom = 10,
				paddingLeft = 24,
				paddingRight = 24,
				backgroundColor = "rgba(79,195,247,0.15)",
				borderWidth = 1,
				borderColor = "rgba(79,195,247,0.3)",
				borderRadius = 6,
			},
			onmousedown = function() UiLib.call("increment", {}) end,
			x(Text) { style = { fontSize = 14, fontStyle = "bold", color = "#fff" }, "Increment (JS → C#)" },
		},
		x(View) {
			style = {
				paddingTop = 10,
				paddingBottom = 10,
				paddingLeft = 24,
				paddingRight = 24,
				backgroundColor = "rgba(255,255,255,0.08)",
				borderWidth = 1,
				borderColor = "rgba(255,255,255,0.12)",
				borderRadius = 6,
			},
			onmousedown = function() UiLib.call("back", {}) end,
			x(Text) { style = { fontSize = 14, fontStyle = "bold", color = "#fff" }, "← Back to Menu" },
		},
	}
end

return TestPage
