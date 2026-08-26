--!strict
local styled = require("../../library/styled")
local React = require("../../library/react")
local dom = require("../../library/uilib-dom")
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

local Settings = require("../components/settings")

-- ── Types ────────────────────────────────────────────────────────

type MenuItem = {
	label: string,
	description: string,
	action: () -> (),
}

type MenuPage = {
	title: string,
	items: { MenuItem },
}

-- ── Styled components ────────────────────────────────────────────

local Root = styled(View) {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "column",
	alignItems = "center",
	justifyContent = "center",
}

local PageTitle = styled(Text) {
	fontSize = 48,
	fontStyle = "bold",
	fontFamily = "Adventure",
	-- letterSpacing = 4,
	-- textTransform = "uppercase",
	marginBottom = 8,
}

local Subtitle = styled(Text) {
	fontSize = 14,
	color = "rgba(255,255,255,0.5)",
	marginBottom = 40,
	-- letterSpacing = 2,
}

local Items = styled(View) {
	display = "flex",
	flexDirection = "column",
	gap = 6,
	minWidth = 320,
	alignItems = "stretch",
}

local ItemBtn = styled(View) {
	display = "flex",
	flexDirection = "column",
	alignItems = "flex-start",
	padding = 14,
	backgroundColor = "rgba(60,60,60,0.12)",
	borderWidth = "1px",
	borderColor = "rgba(255,255,255,0.1)",
	borderRadius = 8,
	-- cursor = 'pointer',
	-- transition = 'all 0.15s ease',
	-- width = "100%",
	hover = {
		backgroundColor = "rgba(79,195,247,0.24)",
		borderColor = "rgba(79,195,247,0.25)",
		-- transform = 'translateY(-1px)',
	},
	-- active = { transform = 'translateY(0)' },
}

local ItemLabel = styled(Text) {
	color = "#fff",
	textAlign = "left",
	fontSize = 16,
	fontStyle = "bold",
	-- letterSpacing = 1,
}

local ItemDesc = styled(Text) {
	fontSize = 12,
	color = "rgba(255,255,255,0.4)",
	marginTop = 2,
	-- letterSpacing = 0.5,
}

local BackBtn = styled(Text) {
	marginTop = 12,
	padding = 10,
	fontSize = 14,
	fontStyle = "bold",
	color = "rgba(255,255,255,0.5)",
	backgroundColor = "rgba(255,255,255,0.04)",
	borderWidth = "1px",
	borderColor = "rgba(255,255,255,0.08)",
	borderRadius = 6,
	-- cursor = 'pointer',
	-- transition = 'all 0.15s ease',
	hover = { color = "#fff", backgroundColor = "rgba(255,255,255,0.08)" },
}

local Footer = styled(Text) {
	position = "absolute",
	bottom = 20,
	fontSize = 11,
	color = "rgba(255,255,255,0.3)",
	-- letterSpacing = 1,
}

-- ── Component ────────────────────────────────────────────────────

local function slice<T>(tbl: { T }, first: number?, last: number?, step: number?): { T }
	local sliced = {}

	for i = first or 1, last or #tbl, step or 1 do
		sliced[#sliced + 1] = tbl[i]
	end

	return sliced
end

local function map<T, V>(tbl: { T }, f: (T) -> V): { V }
	local t = {}
	for k, v in pairs(tbl) do
		t[k] = f(v)
	end
	return t
end

local function append<T>(a: { T }, b: T): { T }
	local result = {}
	for i = 1, #a do
		result[i] = a[i]
	end
	result[#result + 1] = b
	return result
end

local MainMenu = React.memo(function(): React.VNode
	local account, setAccount = useState<<AccountData | nil>>(nil)
	local pageStack, setPageStack = useState<<{ MenuPage }>> {}
	local currentView, setCurrentView = useState<<"menu" | "settings">>("menu")

	useEffect(function()
		return UiLib.onEvent<<AccountData | nil>>("main-menu:account", setAccount)
	end, {})

	local goBack = useCallback(function()
		setPageStack(function(s)
			return slice(s, 1, #s - 1)
		end)
	end, {})

	local pushPage = useCallback(function(page: MenuPage)
		setPageStack(function(s)
			-- table.unpack returns nil on an empty table, so use append
			return append(s, page)
		end)
	end, {})

	-- Build page factories
	local buildSpMenu = useCallback(function(): MenuPage
		return {
			title = "SINGLEPLAYER",
			items = {
				{
					label = "NFM1",
					description = "Play the original NFM1 singleplayer campaign.",
					action = function()
						UiLib.call("navigate", { page = "playNfm1" })
					end,
				},
				{
					label = "NFM2",
					description = "Play the original NFM2 singleplayer campaign.",
					action = function()
						UiLib.call("navigate", { page = "playNfm2" })
					end,
				},
				{
					label = "COMMUNITY",
					description = "Play custom experiences crafted by the community.",
					action = function()
						UiLib.call("navigate", { page = "playCommunity" })
					end,
				},
				{
					label = "FREE PLAY",
					description = "Play freely without any restrictions.",
					action = function()
						UiLib.call("navigate", { page = "play" })
					end,
				},
				{
					label = "BACK",
					description = "Return to the previous menu.",
					action = goBack,
				},
			},
		}
	end, { goBack })

	local buildMpMenu = useCallback(function(): MenuPage
		return {
			title = "MULTIPLAYER",
			items = {
				{
					label = "COMPETITIVE",
					description = "Compete against other players via matchmaking.",
					action = function()
						UiLib.call("navigate", { page = "multiplayer" })
					end,
				},
				{
					label = "CASUAL",
					description = "Play with people in a free relaxed environment.",
					action = function()
						UiLib.call("navigate", { page = "casual" })
					end,
				},
				{
					label = "BACK",
					description = "Return to the previous menu.",
					action = goBack,
				},
			},
		}
	end, { goBack })

	local buildWorkshopMenu = useCallback(function(): MenuPage
		return {
			title = "WORKSHOP",
			items = {
				{
					label = "MODEL EDITOR",
					description = "View and edit custom models.",
					action = function()
						UiLib.call("navigate", { page = "modelEditor" })
					end,
				},
				{
					label = "STAGE EDITOR",
					description = "Design your own stages.",
					action = function()
						UiLib.call("navigate", { page = "stageEditor" })
					end,
				},
				{
					label = "CAMPAIGN EDITOR",
					description = "Craft custom experiences.",
					action = function()
						UiLib.call("navigate", { page = "campaignEditor" })
					end,
				},
				{
					label = "BACK",
					description = "Return to the previous menu.",
					action = goBack,
				},
			},
		}
	end, { goBack })

	local buildTrainingMenu = useCallback(function(): MenuPage
		return {
			title = "TRAINING",
			items = {
				{
					label = "TIME TRIALS",
					description = "Flex your fastest time against other people.",
					action = function()
						UiLib.call("navigate", { page = "timeTrials" })
					end,
				},
				{
					label = "CHALLENGES",
					description = "Complete challenges to sharpen your mechanical skills.",
					action = function()
						UiLib.call("navigate", { page = "challenges" })
					end,
				},
				{
					label = "GAME INSTRUCTIONS",
					description = "Read about the rules and controls of the game.",
					action = function()
						UiLib.call("navigate", { page = "gameInstructions" })
					end,
				},
				{
					label = "BACK",
					description = "Return to the previous menu.",
					action = goBack,
				},
			},
		}
	end, { goBack })

	local buildPlayMenu = useCallback(function(): MenuPage
		return {
			title = "PLAY",
			items = {
				{
					label = "SINGLEPLAYER",
					description = "Play the original single player experiences.",
					action = function()
						pushPage(buildSpMenu())
					end,
				},
				{
					label = "MULTIPLAYER",
					description = "Play online with other players.",
					action = function()
						pushPage(buildMpMenu())
					end,
				},
				{
					label = "TRAINING",
					description = "Train your skills and learn the game mechanics.",
					action = function()
						pushPage(buildTrainingMenu())
					end,
				},
				{
					label = "BACK",
					description = "Return to the previous menu.",
					action = goBack,
				},
			},
		}
	end, { goBack, buildSpMenu, buildMpMenu, buildTrainingMenu, pushPage })

	-- Main menu page
	local mainPage: MenuPage = {
		title = "NFM WORLD",
		items = {
			{
				label = "PLAY",
				description = "Play public, private matches online or play singleplayer.",
				action = function()
					pushPage(buildPlayMenu())
				end,
			},
			{
				label = "GARAGE",
				description = "Customize and inspect your vehicles in the garage.",
				action = function()
					UiLib.call("navigate", { page = "garage" })
				end,
			},
			{
				label = "WORKSHOP",
				description = "Build your own models and stages.",
				action = function()
					pushPage(buildWorkshopMenu())
				end,
			},
			{
				label = "SETTINGS",
				description = "Adjust game settings.",
				action = function()
					setCurrentView("settings")
				end,
			},
			{
				label = "CREDITS",
				description = "View game credits.",
				action = function()
					UiLib.call("navigate", { page = "credits" })
				end,
			},
			{
				label = "QUIT",
				description = "Exit the game.",
				action = function()
					UiLib.call("navigate", { page = "quit" })
				end,
			},
		},
	}

	local currentPage = #pageStack > 0 and pageStack[#pageStack] or mainPage
	local showBack = #pageStack > 0

	-- ── Embedded Settings view ────────────────────────────────────
	if currentView == "settings" then
		return x(Settings) { onClose = function() setCurrentView("menu") end }
	end

	return (
		x(Root) {
			x(PageTitle) { name = "PageTitle", currentPage.title },
			x(Subtitle) {
				name = "Subtitle",
				#pageStack == 0 and (account and account.isLoggedIn and ("Welcome, %s"):format(account.name) or "")
					or "",
			},
			x(Items) {
				name = "Items",
				map(currentPage.items, function(item)
					return x(ItemBtn) {
						name = "ItemBtn",
						key = item.label,
						onmousedown = function(event)
							item.action()
						end,
						x(ItemLabel) { name = "ItemLabel", item.label },
						x(ItemDesc) { name = "ItemDesc", item.description },
					}
				end),
				showBack and x(BackBtn) {
					name = "BackBtn",
					onmousedown = function(event)
						goBack()
					end,
					"← BACK",
				},
			},
			#pageStack == 0 and x(Footer) { name = "Footer", "NFM World — Lua + React UI" },
		}
	)
end)

return MainMenu
