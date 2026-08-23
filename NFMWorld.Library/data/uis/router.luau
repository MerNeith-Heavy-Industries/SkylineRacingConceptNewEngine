--!strict
local styled = require("../library/styled")
local React = require("../library/react")
local dom = require("../library/uilib-dom")
local useState = React.useState
local useEffect = React.useEffect
local useCallback = React.useCallback
local useRef = React.useRef
local useSyncExternalStore = React.useSyncExternalStore
local useContext = React.useContext
local x = React.x
local View = React.View
local Text = React.Text
local Image = React.Image
local TextInput = React.TextInput

local MainMenu = require("./routes/mainmenu")
local Garage = require("./routes/garage")
local RaceHud = require("./routes/racehud")
local TestPage = require("./routes/test")

-- ── Simple hash-based router ─────────────────────────────────────
-- Reads window.location.hash to determine which page component to
-- render. Hash changes (e.g. #/garage) are handled without page
-- reload, keeping the CEF render process alive.

local routes: { [string]: React.StatelessFunctionalComponent<any> } = {
	["main-menu"] = MainMenu,
	["garage"] = Garage,
	["race"] = RaceHud,
	["test"] = TestPage,
}

local defaultRoute = "main-menu"

function Router()
	local hash, setHash = useState(defaultRoute)

	useEffect(function()
		-- Navigation must flush synchronously: the phase's component has to
		-- mount (and subscribe to its `phase:*` events) BEFORE the C# phase
		-- pushes its initial state (e.g. GaragePhase.Enter → PushCollections).
		-- Otherwise the push arrives before the subscription and is lost.
		return UiLib.onEvent("nfmw:navigate", function(hash: string)
			React.flushSync(function() setHash(hash) end)
		end)
	end, {})

	local Page = routes[hash] or routes[defaultRoute]
	return x(Page) {}
end

function App()
	return x(Router) {}
end

dom.render(x(App) {})
