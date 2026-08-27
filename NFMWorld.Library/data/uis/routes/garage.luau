-- Garage view (Sx port of preact garage.luau), ported from
-- nfm-world/data/html/src/pages/Garage.tsx. Fine-grained: collections/currentCar/
-- currentCollection/searchQuery are signals; the collection list is a keyed For; a car
-- click updates only the cards whose selection actually changed (stable style tables +
-- reference-compare in the renderer skip redundant setProperty).

local Sx = require("../../library/sx/index")
local styled = Sx.styled
local x = Sx.x
local glasscard = require("../components/glasscard")
local GlassCard = glasscard.GlassCard
local StatBar = glasscard.StatBar

-- value-or-accessor helper
local function readProp(v)
	if type(v) == "function" then
		return v()
	end
	return v
end

local Root = styled("view") {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "row",
}

local Sidebar = styled("view") {
	width = 340,
	height = "100%",
	paddingTop = 32,
	paddingBottom = 32,
	paddingLeft = 24,
	paddingRight = 24,
	display = "flex",
	flexDirection = "column",
	gap = 16,
	backgroundColor = "rgba(0,0,0,0.3)",
	borderRightWidth = 1,
	borderColor = "rgba(255,255,255,0.06)",
}

local Header = styled("text") {
	fontSize = 28,
	fontStyle = "bold",
}

local CollectionList = styled("view") {
	flex = 1,
	display = "flex",
	flexDirection = "column",
	overflow = "scroll",
	gap = 8,
}

local StatsPanel = styled("view") {
	flexShrink = 0,
	display = "flex",
	alignItems = "center",
	justifyContent = "center",
}

local StatsCard = styled("view") {
	width = 280,
	padding = 24,
	flexDirection = "column",
}

local CarName = styled("text") {
	fontSize = 18,
	fontStyle = "bold",
	color = "#4fc3f7",
	marginBottom = 16,
}

local Placeholder = styled("text") {
	fontSize = 18,
	color = "rgba(255,255,255,0.3)",
}

local CarCardName = styled("text") {
	fontSize = 14,
	fontStyle = "bold",
	marginBottom = 4,
}

local CarCardCollection = styled("text") {
	fontSize = 11,
	color = "rgba(255,255,255,0.4)",
}

local NoResults = styled("text") {
	fontSize = 14,
	color = "rgba(255,255,255,0.3)",
	textAlign = "center",
	padding = 24,
}

local BackBtn = styled("view") {
	paddingTop = 10,
	paddingBottom = 10,
	paddingLeft = 24,
	paddingRight = 24,
	alignItems = "center",
	backgroundColor = "rgba(255,255,255,0.06)",
	borderWidth = 1,
	borderColor = "rgba(255,255,255,0.1)",
	borderRadius = 6,
	hover = { backgroundColor = "rgba(255,255,255,0.1)" },
}

-- Module-level constant style for the collection wrapper View (stable identity).
local collectionViewStyle = { flexDirection = "column", flexShrink = 0 }

-- Stable card styles: selection change swaps the reference, so unchanged cards skip
-- setProperty (the renderer's reactive-prop effect compares references).
local cardStyleSelected = { marginBottom = 8, opacity = 1 }
local cardStyleUnselected = { marginBottom = 8, opacity = 0.7 }

-- Card component. `selected` is an accessor; only cards whose selection changes update.
local function CarCard(props)
	local style = Sx.createMemo(function()
		if readProp(props.selected) then
			return cardStyleSelected
		end
		return cardStyleUnselected
	end)

	return x(GlassCard) {
		name = "glasscard " .. props.name,
		color = function()
			return readProp(props.selected) and "#4fc3f7" or "rgba(255,255,255,0.15)"
		end,
		style = style,
		onmousedown = function()
			props.onSelect(props.collectionName, props.fileName)
		end,
		x(CarCardName) { name = "carcardname " .. props.name, props.name },
		x(CarCardCollection) { name = "carcardcollection " .. props.collectionName, props.collectionName },
	}
end

local function CollectionSection(props)
	return x("view") {
		name = "inner view " .. props.name,
		style = Sx.createMemo(function()
			local active = readProp(props.active)
			return {
				marginBottom = 6,
				paddingTop = 4,
				paddingBottom = 4,
				paddingLeft = 8,
				paddingRight = 8,
				borderRadius = 4,
				borderLeftWidth = 2,
				borderColor = active and "#4fc3f7" or "transparent",
				backgroundColor = active and "rgba(79,195,247,0.08)" or "transparent",
			}
		end),
		onmousedown = function()
			props.onSelect(props.name)
		end,
		x("text") {
			style = Sx.createMemo(function()
				local active = readProp(props.active)
				return { fontSize = 12, color = active and "#4fc3f7" or "rgba(255,255,255,0.4)" }
			end),
			props.name,
		},
	}
end

local function Garage()
	local currentCar, setCurrentCar = Sx.createSignal(nil)
	local collections, setCollections = Sx.createSignal(nil)
	local currentCollection, setCurrentCollection = Sx.createSignal(nil)
	local searchQuery, setSearchQuery = Sx.createSignal("")
	local rootRef = {}

	Sx.createEffect(function()
		local u1 = UiLib.onEvent("garage:currentCar", function(data)
			setCurrentCar(data)
		end)
		local u2 = UiLib.onEvent("garage:collections", function(data)
			setCollections(data)
		end)
		local u3 = UiLib.onEvent("garage:currentCollection", function(data)
			setCurrentCollection(data)
		end)
		Sx.onCleanup(function()
			u1()
			u2()
			u3()
		end)
	end)

	Sx.createEffect(function()
		if rootRef.current ~= nil then
			rootRef.current:focus()
		end
	end)

	local function handleSelectCar(collection, fileName)
		UiLib.call("selectCar", { collection = collection, fileName = fileName })
	end

	local function handleSelectCollection(collection)
		UiLib.call("selectCollection", { collection = collection })
	end

	local function handleGlobalKey(event)
		if event.keyCode == Key.left then
			UiLib.call("cycleCar", { direction = "left" })
		elseif event.keyCode == Key.right then
			UiLib.call("cycleCar", { direction = "right" })
		elseif event.keyCode == Key.enter then
			UiLib.call("confirm", {})
		elseif event.keyCode == Key.escape then
			UiLib.call("cancel", {})
		end
	end

	-- Client-side search filter
	local query = Sx.createMemo(function()
		return searchQuery():lower():match("^%s*(.-)%s*$") or ""
	end)

	local filteredCollections = Sx.createMemo(function()
		local result = {}
		local cols = collections()
		if cols ~= nil then
			local q = query()
			if q == "" then
				for i = 1, #cols.collections do
					result[#result + 1] = cols.collections[i]
				end
			else
				for i = 1, #cols.collections do
					local col = cols.collections[i]
					local matchedCars = {}
					for j = 1, #col.cars do
						local car = col.cars[j]
						if car.name:lower():find(q, 1, true) ~= nil then
							matchedCars[#matchedCars + 1] = car
						end
					end
					if #matchedCars > 0 then
						result[#result + 1] = {
							id = col.id,
							name = col.name,
							cars = matchedCars,
						}
					end
				end
			end
		end
		return result
	end)

	return x(Root) {
		ref = rootRef,
		tabindex = 0,
		onkeydown = handleGlobalKey,

		x(Sidebar) {
			x(Header) { "Garage" },

			x(StatsPanel) {
				x(Sx.Show) {
					when = function() return currentCar() ~= nil end,
					fallback = x(Placeholder) { "Select a car to view stats" },
					x(StatsCard) {
						x(CarName) { function() return currentCar().name end },
						x(StatBar) { label = "Top Speed", value = function() return currentCar().topSpeed end, color = "#ff6e40" },
						x(StatBar) { label = "Acceleration", value = function() return currentCar().acceleration end, color = "#ffd740" },
						x(StatBar) { label = "Handling", value = function() return currentCar().handling end, color = "#69f0ae" },
						x(StatBar) { label = "Power Save", value = function() return currentCar().powerSave end, color = "#40c4ff" },
						x(StatBar) { label = "Strength", value = function() return currentCar().strength end, color = "#ff4081" },
						x(StatBar) { label = "Max Health", value = function() return currentCar().maxHealth end, color = "#e040fb" },
						x(StatBar) { label = "Stunting", value = function() return currentCar().stunting end, color = "#ff6e40" },
						x(StatBar) { label = "Hypergliding", value = function() return currentCar().hypergliding end, color = "#7c4dff" },
						x(StatBar) { label = "AB'ing", value = function() return currentCar().abing end, color = "#448aff" },
					},
				},
			},

			x("textinput") {
				style = {
					width = "100%",
					paddingTop = 10,
					paddingBottom = 10,
					paddingLeft = 14,
					paddingRight = 14,
					backgroundColor = "rgba(255,255,255,0.08)",
					borderWidth = 1,
					borderColor = "rgba(255,255,255,0.12)",
					borderRadius = 8,
				},
				placeholder = "Search cars...",
				value = searchQuery,
				onchange = function(value)
					setSearchQuery(value)
				end,
				onsubmit = function(value)
					local fc = filteredCollections()
					if #fc > 0 then
						local firstCol = fc[1]
						if firstCol.cars ~= nil and #firstCol.cars > 0 then
							handleSelectCar(firstCol.name, firstCol.cars[1].fileName)
							setSearchQuery("")
							if rootRef.current ~= nil then
								rootRef.current:focus()
							end
						end
					end
				end,
				onkeydown = function(event)
					if event.keyCode == Key.escape then
						setSearchQuery("")
						if rootRef.current ~= nil then
							rootRef.current:focus()
						end
					end
				end,
			},

			x(CollectionList) {
				x(Sx.For) {
					each = filteredCollections,
					function(item, index)
						local col = item()
						return x("view") {
							name = "view " .. col.name,
							style = collectionViewStyle,
							x(CollectionSection) {
								name = col.name,
								active = function()
									local cc = currentCollection()
									return cc ~= nil and cc.id == col.id
								end,
								onSelect = handleSelectCollection,
							},
							x(Sx.For) {
								each = function() return col.cars end,
								function(item2, index2)
									local car = item2()
									return x(CarCard) {
										name = car.name,
										collectionName = col.name,
										fileName = car.fileName,
										selected = function()
											local cc = currentCar()
											return cc ~= nil and cc.fileName == car.fileName
										end,
										onSelect = handleSelectCar,
									}
								end,
							},
						}
					end,
				},

				x(Sx.Show) {
					when = function()
						return query() ~= "" and #filteredCollections() == 0
					end,
					x(NoResults) {
						function()
							return ('No cars match "%s"'):format(searchQuery())
						end,
					},
				},
			},

			x(BackBtn) {
				onmousedown = function()
					UiLib.call("back", {})
				end,
				x("text") {
					style = { fontSize = 14, fontStyle = "bold", color = "rgba(255,255,255,0.6)" },
					"← Back to Menu",
				},
			},
		},
	}
end

return Garage
