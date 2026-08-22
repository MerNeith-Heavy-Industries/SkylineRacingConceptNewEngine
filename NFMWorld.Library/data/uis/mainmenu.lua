local React = require('react')
local useState = React.useState
local useEffect = React.useEffect
local useCallback = React.useCallback
local useRef = React.useRef
local useSyncExternalStore = React.useSyncExternalStore
local useContext = React.useContext
local h = React.h

function MainMenu()
  local account, setAccount = useState(nil);
  local pageStack, setPageStack = useState<MenuPage[]>([]);
  local currentView, setCurrentView = useState<"menu" | "settings">("menu");

  useEffect(() => {
    return onNfmwEvent<AccountData | null>("main-menu:account", setAccount, AccountData.deserialize.bind(AccountData));
  }, []);

  const goBack = useCallback(() => {
    setPageStack((s) => s.slice(0, -1));
  }, []);

  const pushPage = useCallback((page: MenuPage) => {
    setPageStack((s) => [...s, page]);
  }, []);

  // Build page factories
  const buildSpMenu = useCallback((): MenuPage => ({
    title: "SINGLEPLAYER",
    items: [
      { label: "NFM1", description: "Play the original NFM1 singleplayer campaign.", action: () => callNfmw("navigate", { page: "playNfm1" }) },
      { label: "NFM2", description: "Play the original NFM2 singleplayer campaign.", action: () => callNfmw("navigate", { page: "playNfm2" }) },
      { label: "COMMUNITY", description: "Play custom experiences crafted by the community.", action: () => callNfmw("navigate", { page: "playCommunity" }) },
      { label: "FREE PLAY", description: "Play freely without any restrictions.", action: () => callNfmw("navigate", { page: "play" }) },
      { label: "BACK", description: "Return to the previous menu.", action: goBack },
    ],
  }), [goBack]);

  const buildMpMenu = useCallback((): MenuPage => ({
    title: "MULTIPLAYER",
    items: [
      { label: "COMPETITIVE", description: "Compete against other players via matchmaking.", action: () => callNfmw("navigate", { page: "multiplayer" }) },
      { label: "CASUAL", description: "Play with people in a free relaxed environment.", action: () => callNfmw("navigate", { page: "casual" }) },
      { label: "BACK", description: "Return to the previous menu.", action: goBack },
    ],
  }), [goBack]);

  const buildWorkshopMenu = useCallback((): MenuPage => ({
    title: "WORKSHOP",
    items: [
      { label: "MODEL EDITOR", description: "View and edit custom models.", action: () => callNfmw("navigate", { page: "modelEditor" }) },
      { label: "STAGE EDITOR", description: "Design your own stages.", action: () => callNfmw("navigate", { page: "stageEditor" }) },
      { label: "CAMPAIGN EDITOR", description: "Craft custom experiences.", action: () => callNfmw("navigate", { page: "campaignEditor" }) },
      { label: "BACK", description: "Return to the previous menu.", action: goBack },
    ],
  }), [goBack]);

  const buildTrainingMenu = useCallback((): MenuPage => ({
    title: "TRAINING",
    items: [
      { label: "TIME TRIALS", description: "Flex your fastest time against other people.", action: () => callNfmw("navigate", { page: "timeTrials" }) },
      { label: "CHALLENGES", description: "Complete challenges to sharpen your mechanical skills.", action: () => callNfmw("navigate", { page: "challenges" }) },
      { label: "GAME INSTRUCTIONS", description: "Read about the rules and controls of the game.", action: () => callNfmw("navigate", { page: "gameInstructions" }) },
      { label: "BACK", description: "Return to the previous menu.", action: goBack },
    ],
  }), [goBack]);

  const buildPlayMenu = useCallback((): MenuPage => ({
    title: "PLAY",
    items: [
      { label: "SINGLEPLAYER", description: "Play the original single player experiences.", action: () => pushPage(buildSpMenu()) },
      { label: "MULTIPLAYER", description: "Play online with other players.", action: () => pushPage(buildMpMenu()) },
      { label: "TRAINING", description: "Train your skills and learn the game mechanics.", action: () => pushPage(buildTrainingMenu()) },
      { label: "BACK", description: "Return to the previous menu.", action: goBack },
    ],
  }), [goBack, buildSpMenu, buildMpMenu, buildTrainingMenu, pushPage]);

  // Main menu page
  const mainPage: MenuPage = {
    title: "NFM WORLD",
    items: [
      { label: "PLAY", description: "Play public, private matches online or play singleplayer.", action: () => pushPage(buildPlayMenu()) },
      { label: "GARAGE", description: "Customize and inspect your vehicles in the garage.", action: () => callNfmw("navigate", { page: "garage" }) },
      { label: "WORKSHOP", description: "Build your own models and stages.", action: () => pushPage(buildWorkshopMenu()) },
      { label: "SETTINGS", description: "Adjust game settings.", action: () => setCurrentView("settings") },
      { label: "CREDITS", description: "View game credits.", action: () => callNfmw("navigate", { page: "credits" }) },
      { label: "QUIT", description: "Exit the game.", action: () => callNfmw("navigate", { page: "quit" }) },
    ],
  };

  const currentPage = pageStack.length > 0 ? pageStack[pageStack.length - 1] : mainPage;
  const showBack = pageStack.length > 0;

  // ── Embedded Settings view ────────────────────────────────────
  if (currentView === "settings") {
    return <Settings onClose={() => setCurrentView("menu")} />;
  }

  return (
    <Root>
      <PageTitle>{currentPage.title}</PageTitle>
      <Subtitle>
        {pageStack.length === 0
          ? (account?.isLoggedIn ? `Welcome, ${account.name}` : "Racing Simulator")
          : ""}
      </Subtitle>
      <Items>
        {currentPage.items.map((item) => (
          <ItemBtn key={item.label} onClick={item.action}>
            <ItemLabel>{item.label}</ItemLabel>
            <ItemDesc>{item.description}</ItemDesc>
          </ItemBtn>
        ))}
        {showBack && <BackBtn onClick={goBack}>← BACK</BackBtn>}
      </Items>
      {pageStack.length === 0 && <Footer>NFM World — CEF + Preact UI</Footer>}
    </Root>
  );
}
