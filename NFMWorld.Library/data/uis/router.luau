-- Sx hash-based router. Subscribes to C# navigation (nfmw:navigate) via a signal and
-- renders the matching page through Sx.Switch. Only the main-menu route is ported to Sx
-- yet; the other routes are commented out.

local Sx = require("../library/sx/init")
local x = Sx.x

local MainMenu = require("./routes/mainmenu")
local Garage = require("./routes/garage")
local RaceHud = require("./routes/racehud")
local TestPage = require("./routes/test")

local defaultRoute = "main-menu"

local function Router()
	local route, setRoute = Sx.createSignal(defaultRoute)

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("nfmw:navigate", function(hash)
			setRoute(hash)
		end)
		Sx.onCleanup(unsub)
	end)

	return (
		x(Sx.Switch) {
			x(Sx.Match) {
				when = function() return route() == "main-menu" end,
				x(MainMenu) {},
			},
			x(Sx.Match) {
				when = function() return route() == "garage" end,
				x(Garage) {},
			},
			x(Sx.Match) {
				when = function() return route() == "race" end,
				x(RaceHud) {},
			},
			x(Sx.Match) {
				when = function() return route() == "test" end,
				x(TestPage) {},
			},
			-- Fallback: any unhandled route shows the default page (main menu)
			x(Sx.Match) {
				when = function() return true end,
				x(MainMenu) {},
			},
		}
	)
end

Sx.render(x(Router) {})
