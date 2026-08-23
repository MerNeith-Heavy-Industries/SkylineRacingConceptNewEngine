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

-- ── Simple hash-based router ─────────────────────────────────────
-- Reads window.location.hash to determine which page component to
-- render. Hash changes (e.g. #/garage) are handled without page
-- reload, keeping the CEF render process alive.

local routes: { [string]: React.StatelessFunctionalComponent<any> } = {
	["main-menu"] = MainMenu,
	-- "garage": Garage,
	-- "race": RaceHud,
	-- "test": TestPage,
	-- 'empty': () => <div style={{ width: "100%", height: "100%" }}></div>
}

local defaultRoute = "main-menu"

function Router()
	local hash, setHash = useState(defaultRoute)

	useEffect(function()
		return UiLib.onEvent("nfmw:navigate", setHash)
	end, {})

	local Page = routes[hash] or routes[defaultRoute]
	return x(Page) {}
end

function App()
	return x(Router) {}
end

dom.render(x(App) {})
