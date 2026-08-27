-- Test page (Sx port of preact test.luau), ported from
-- nfm-world/data/html/src/pages/Test.tsx.

local Sx = require("../../library/sx/index")
local styled = Sx.styled
local x = Sx.x

local Root = styled("view") {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "column",
	alignItems = "center",
	justifyContent = "center",
	gap = 16,
}

local Label = styled("text") {
	fontSize = 14,
	color = "rgba(255,255,255,0.5)",
}

local Counter = styled("text") {
	fontSize = 64,
	fontStyle = "bold",
	color = "#4fc3f7",
}

local function TestPage()
	local counter, setCounter = Sx.createSignal(0)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("test:counter", function(data)
			if data ~= nil then
				setCounter(data.value or 0)
			else
				setCounter(0)
			end
		end)
		Sx.onCleanup(unsub)
	end)

	return x(Root) {
		x(Label) { "CEF + Preact Test" },
		x(Counter) { function() return ("%d"):format(counter()) end },
		x("view") {
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
			x("text") { style = { fontSize = 14, fontStyle = "bold", color = "#fff" }, "Increment (JS → C#)" },
		},
		x("view") {
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
			x("text") { style = { fontSize = 14, fontStyle = "bold", color = "#fff" }, "← Back to Menu" },
		},
	}
end

return TestPage
