--!strict

-- Garage view ported from nfm-world/data/html/src/pages/Garage.tsx.

local React = require("../../library/react")
local styled = require("../../library/styled")
local glasscard = require("../components/glasscard")
local GlassCard = glasscard.GlassCard
local StatBar = glasscard.StatBar

local useState = React.useState
local useEffect = React.useEffect
local useRef = React.useRef
local useMemo = React.useMemo
local useCallback = React.useCallback
local x = React.x
local View = React.View
local Text = React.Text
local TextInput = React.TextInput

local Root = styled(View) {
	width = "100%",
	height = "100%",
	display = "flex",
	flexDirection = "row",
}

local Sidebar = styled(View) {
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

local Header = styled(Text) {
	fontSize = 28,
	fontStyle = "bold",
}

local CollectionList = styled(View) {
	flex = 1,
	display = "flex",
	flexDirection = "column",
	overflow = "scroll",
	gap = 8,
}

local StatsPanel = styled(View) {
	flexShrink = 0,
	display = "flex",
	alignItems = "center",
	justifyContent = "center",
}

local StatsCard = styled(View) {
	width = 280,
	padding = 24,
	flexDirection = "column",
}

local CarName = styled(Text) {
	fontSize = 18,
	fontStyle = "bold",
	color = "#4fc3f7",
	marginBottom = 16,
}

local Placeholder = styled(Text) {
	fontSize = 18,
	color = "rgba(255,255,255,0.3)",
}

local CollectionHeader = styled(Text) {
	fontSize = 12,
	marginBottom = 6,
	paddingTop = 4,
	paddingBottom = 4,
	paddingLeft = 8,
	paddingRight = 8,
	borderRadius = 4,
}

local CarCardName = styled(Text) {
	fontSize = 14,
	fontStyle = "bold",
	marginBottom = 4,
}

local CarCardCollection = styled(Text) {
	fontSize = 11,
	color = "rgba(255,255,255,0.4)",
}

local NoResults = styled(Text) {
	fontSize = 14,
	color = "rgba(255,255,255,0.3)",
	textAlign = "center",
	padding = 24,
}

local BackBtn = styled(View) {
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

local function map<T, V>(tbl: { T }, f: (T, number) -> V): { V }
	local t = {}
	for i = 1, #tbl do
		t[i] = f(tbl[i], i)
	end
	return t
end

-- Module-level constant so the collection wrapper View's style table has a
-- stable identity across renders (diffProps uses reference equality).
local collectionViewStyle = { flexDirection = "column", flexShrink = 0 }

-- Card component. Wrapped in React.memo so that on a car click (which
-- rebuilds the list with fresh VNodes) only the cards whose `selected` prop
-- actually changed re-render — the other ~150 cards bail out at the memo
-- boundary. useCallback/useMemo keep the handler + style stable so diffProps
-- skips re-setting them too.
local CarCard = React.memo(function(props: {
	name: string,
	collectionName: string,
	fileName: string,
	selected: boolean,
	onSelect: ((string, string) -> ()),
}): React.VNode
	local style = useMemo<<React.Style>>(function()
		return { marginBottom = 8, opacity = props.selected and 1 or 0.7 }
	end, { props.selected })
	local onMousedown = useCallback(function()
		props.onSelect(props.collectionName, props.fileName)
	end, { props.onSelect, props.collectionName, props.fileName })
	return x(GlassCard) {
		name = "glasscard " .. props.name,
		color = props.selected and "#4fc3f7" or "rgba(255,255,255,0.15)",
		style = style,
		onmousedown = onMousedown,
		x(CarCardName) { name = "carcardname " .. props.name, props.name },
		x(CarCardCollection) { name = "carcardcollection " .. props.collectionName, props.collectionName },
	}
end)

local CollectionSection = React.memo(function(props: {
	name: string,
	active: boolean,
	onSelect: ((string) -> ()),
}): React.VNode
	local style = useMemo(function()
		return {
			marginBottom = 6,
			paddingTop = 4,
			paddingBottom = 4,
			paddingLeft = 8,
			paddingRight = 8,
			borderRadius = 4,
			borderLeftWidth = 2,
			borderColor = props.active and "#4fc3f7" or "transparent",
			backgroundColor = props.active and "rgba(79,195,247,0.08)" or "transparent",
		}
	end, { props.active })
	local textStyle = useMemo(function()
		return { fontSize = 12, color = props.active and "#4fc3f7" or "rgba(255,255,255,0.4)" }
	end, { props.active })
	local onMousedown = useCallback(function()
		props.onSelect(props.name)
	end, { props.onSelect, props.name })
	return x(View) {
		name = "inner view " .. props.name,
		style = style,
		onmousedown = onMousedown,
		x(Text) {
			style = textStyle,
			props.name,
		},
	}
end)

local function Garage(): React.VNode
	local currentCar, setCurrentCar = useState<<CarStatsData | nil>>(nil)
	local collections, setCollections = useState<<CarCollectionsData | nil>>(nil)
	local currentCollection, setCurrentCollection = useState<<CarCollectionData | nil>>(nil)
	local searchQuery, setSearchQuery = useState<<string>>("")
	local rootRef = useRef<<Component | nil>>(nil)

	useEffect(function()
		local u1 = UiLib.onEvent("garage:currentCar", function(data: any)
			setCurrentCar(data)
		end)
		local u2 = UiLib.onEvent("garage:collections", function(data: any)
			setCollections(data)
		end)
		local u3 = UiLib.onEvent("garage:currentCollection", function(data: any)
			setCurrentCollection(data)
		end)
		return function()
			u1()
			u2()
			u3()
		end
	end, {})

	useEffect(function()
		if rootRef.current ~= nil then
			rootRef.current:focus()
		end
	end, {})

	local handleSelectCar = useCallback(function(collection: string, fileName: string)
		UiLib.call("selectCar", { collection = collection, fileName = fileName })
	end, {})

	local handleSelectCollection = useCallback(function(collection: string)
		UiLib.call("selectCollection", { collection = collection })
	end, {})

	local function handleGlobalKey(event: any)
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
	local query = searchQuery:lower():match("^%s*(.-)%s*$")

	local filteredCollections = useMemo(function()
		local result: any = {}
		if collections ~= nil then
			if query == "" then
				for i = 1, #collections.collections do
					result[#result + 1] = collections.collections[i]
				end
			else
				for i = 1, #collections.collections do
					local col = collections.collections[i]
					local matchedCars = {}
					for j = 1, #col.cars do
						local car = col.cars[j]
						if car.name:lower():find(query :: string, 1, true) ~= nil then
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
	end, { collections, searchQuery })

	-- Memoize the collection-list subtree so unrelated re-renders (e.g.
	-- hovering the back button) don't rebuild every car-card VNode.
	local listChildren = useMemo(function()
		return map(filteredCollections, function(col: any): React.VNode
			local active = currentCollection ~= nil and currentCollection.id == col.id
			return x(View) {
				name = "view " .. col.name,
				key = col.name,
				style = collectionViewStyle,
				x(CollectionSection) {
					name = col.name,
					active = active,
					onSelect = handleSelectCollection,
				},
				map(col.cars, function(car: any): React.VNode
					return x(CarCard) {
						key = car.fileName,
						name = car.name,
						collectionName = col.name,
						fileName = car.fileName,
						selected = currentCar ~= nil and currentCar.fileName == car.fileName,
						onSelect = handleSelectCar,
					}
				end),
			}
		end)
	end, { filteredCollections, currentCar, currentCollection })

	local statsNode = useMemo(function()
		return currentCar ~= nil and x(StatsCard) {
			x(CarName) { currentCar.name },
			x(StatBar) { label = "Top Speed", value = currentCar.topSpeed, color = "#ff6e40" },
			x(StatBar) { label = "Acceleration", value = currentCar.acceleration, color = "#ffd740" },
			x(StatBar) { label = "Handling", value = currentCar.handling, color = "#69f0ae" },
			x(StatBar) { label = "Power Save", value = currentCar.powerSave, color = "#40c4ff" },
			x(StatBar) { label = "Strength", value = currentCar.strength, color = "#ff4081" },
			x(StatBar) { label = "Max Health", value = currentCar.maxHealth, color = "#e040fb" },
			x(StatBar) { label = "Stunting", value = currentCar.stunting, color = "#ff6e40" },
			x(StatBar) { label = "Hypergliding", value = currentCar.hypergliding, color = "#7c4dff" },
			x(StatBar) { label = "AB'ing", value = currentCar.abing, color = "#448aff" },
		} or x(Placeholder) { "Select a car to view stats" }
	end, { currentCar })

	return x(Root) {
		ref = rootRef,
		tabindex = 0,
		onkeydown = handleGlobalKey,

		x(Sidebar) {
			x(Header) { "Garage" },

			x(StatsPanel) {
				statsNode,
			},

			x(TextInput) {
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
				onchange = function(value: string)
					setSearchQuery(value)
				end,
				onsubmit = function(value: string)
					if #filteredCollections > 0 then
						local firstCol = filteredCollections[1]
						if firstCol.cars ~= nil and #firstCol.cars > 0 then
							handleSelectCar(firstCol.name, firstCol.cars[1].fileName)
							setSearchQuery("")
							if rootRef.current ~= nil then
								rootRef.current:focus()
							end
						end
					end
				end,
				onkeydown = function(event: any)
					if event.keyCode == Key.escape then
						setSearchQuery("")
						if rootRef.current ~= nil then
							rootRef.current:focus()
						end
					end
				end,
			},

			x(CollectionList) {
				listChildren,

				query ~= "" and #filteredCollections == 0 and x(NoResults) {
					('No cars match "%s"'):format(searchQuery),
				},
			},

			x(BackBtn) {
				onmousedown = function()
					UiLib.call("back", {})
				end,
				x(Text) {
					style = { fontSize = 14, fontStyle = "bold", color = "rgba(255,255,255,0.6)" },
					"← Back to Menu",
				},
			},
		},
	}
end

return Garage
