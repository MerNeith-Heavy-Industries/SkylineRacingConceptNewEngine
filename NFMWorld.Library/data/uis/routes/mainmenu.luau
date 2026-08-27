-- Sx port of the main menu. Fine-grained: components run once; the page stack and
-- account are signals; the item list is a keyed For; reactive text children update via
-- single commitTextUpdate calls. No vdom diffing, no per-frame re-render.
--
-- NOTE: the Settings component is not ported to Sx yet — its embedded view is kept
-- commented out below.

local Sx = require("../../library/sx/init")
local styled = Sx.styled
local x = Sx.x

-- local Settings = require("../components/settings") -- TODO: port to Sx

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

local Root = styled("view") {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "column",
	alignItems = "center",
	justifyContent = "center",
}

local PageTitle = styled("text") {
	fontSize = 48,
	fontStyle = "bold",
	fontFamily = "Adventure",
	marginBottom = 8,
}

local Subtitle = styled("text") {
	fontSize = 14,
	color = "rgba(255,255,255,0.5)",
	marginBottom = 40,
}

local Items = styled("view") {
	display = "flex",
	flexDirection = "column",
	gap = 6,
	minWidth = 320,
	alignItems = "stretch",
}

local ItemBtn = styled("view") {
	display = "flex",
	flexDirection = "column",
	alignItems = "flex-start",
	padding = 14,
	backgroundColor = "rgba(60,60,60,0.12)",
	borderWidth = "1px",
	borderColor = "rgba(255,255,255,0.1)",
	borderRadius = 8,
	hover = {
		backgroundColor = "rgba(79,195,247,0.24)",
		borderColor = "rgba(79,195,247,0.25)",
	},
}

local ItemLabel = styled("text") {
	color = "#fff",
	textAlign = "left",
	fontSize = 16,
	fontStyle = "bold",
}

local ItemDesc = styled("text") {
	fontSize = 12,
	color = "rgba(255,255,255,0.4)",
	marginTop = 2,
}

local BackBtn = styled("text") {
	marginTop = 12,
	padding = 10,
	fontSize = 14,
	fontStyle = "bold",
	color = "rgba(255,255,255,0.5)",
	backgroundColor = "rgba(255,255,255,0.04)",
	borderWidth = "1px",
	borderColor = "rgba(255,255,255,0.08)",
	borderRadius = 6,
	hover = { color = "#fff", backgroundColor = "rgba(255,255,255,0.08)" },
}

local Footer = styled("text") {
	position = "absolute",
	bottom = 20,
	fontSize = 11,
	color = "rgba(255,255,255,0.3)",
}

-- ── Component ────────────────────────────────────────────────────

local MainMenu = function()
	local account, setAccount = Sx.createSignal(nil)
	local pageStack, setPageStack = Sx.createSignal({})
	local currentView, setCurrentView = Sx.createSignal("menu")

	Sx.createEffect(function()
		local unsub = UiLib.onEvent("main-menu:account", function(value)
			setAccount(value)
		end)
		Sx.onCleanup(unsub)
	end)

	local goBack = function()
		local s = pageStack()
		local newS = {}
		for i = 1, #s - 1 do
			newS[i] = s[i]
		end
		setPageStack(newS)
	end

	local pushPage = function(page: MenuPage)
		local s = pageStack()
		local newS = {}
		for i = 1, #s do
			newS[i] = s[i]
		end
		newS[#newS + 1] = page
		setPageStack(newS)
	end

	-- Build page factories
	local buildSpMenu = function(): MenuPage
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
	end

	local buildMpMenu = function(): MenuPage
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
	end

	local buildWorkshopMenu = function(): MenuPage
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
	end

	local buildTrainingMenu = function(): MenuPage
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
	end

	local buildPlayMenu = function(): MenuPage
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
	end

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
				-- TODO: port Settings to Sx. Was: action = function() setCurrentView("settings") end
				action = function() end,
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

	local currentPage = Sx.createMemo(function()
		local stack = pageStack()
		return #stack > 0 and stack[#stack] or mainPage
	end)

	return (
		x(Root) {
			-- Switch for conditional views
			x(Sx.Switch) {
				-- Settings view (Settings not ported to Sx yet — commented out)
				-- x(Sx.Match) {
				-- 	when = function() return currentView() == "settings" end,
				-- 	x(Settings) {
				-- 		onClose = function() setCurrentView("menu") end
				-- 	},
				-- },
				-- Menu view (default)
				x(Sx.Match) {
					when = function() return currentView() == "menu" end,

					x(PageTitle) {
						name = "PageTitle",
						function()
							return currentPage().title
						end,
					},

					-- Welcome subtitle, shown only on the root page when logged in
					x(Sx.Show) {
						when = function()
							local stack = pageStack()
							local acc = account()
							return #stack == 0 and acc ~= nil and acc.isLoggedIn
						end,
						x(Subtitle) {
							name = "Subtitle",
							function()
								return ("Welcome, %s"):format(account().name)
							end,
						},
					},

					-- Menu items (keyed For)
					x(Items) {
						name = "Items",
						x(Sx.For) {
							each = function()
								return currentPage().items
							end,
							function(item, index)
								return x(ItemBtn) {
									name = "ItemBtn",
									onmousedown = function(event)
										item().action()
									end,
									x(ItemLabel) {
										name = "ItemLabel",
										function()
											return item().label
										end,
									},
									x(ItemDesc) {
										name = "ItemDesc",
										function()
											return item().description
										end,
									},
								}
							end,
						},
					},

					-- Footer on the root page only
					x(Sx.Show) {
						when = function() return #pageStack() == 0 end,
						x(Footer) { name = "Footer", "NFM World — Lua + React UI" },
					},
				},
			},
		}
	)
end

return MainMenu
