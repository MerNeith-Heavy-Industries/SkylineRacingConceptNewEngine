import { useState, useEffect } from "preact/hooks";
import { styled } from "goober";
import { callNfmw, onNfmwEvent } from "../shared/bridge";
import { AccountData } from "../shared/memorypack/AccountData";

const Root = styled("div")`
  width: 100%; height: 100%;
  display: flex; flex-direction: column;
  align-items: center; justify-content: center;
  animation: nfmw-fadeIn 0.3s ease-out;
`;

const Title = styled("div")`
  font-size: 48px; font-weight: 800; letter-spacing: 4px;
  text-transform: uppercase; margin-bottom: 8px;
  text-shadow: 0 2px 16px rgba(79,195,247,0.4);
`;

const Subtitle = styled("div")`
  font-size: 14px; color: rgba(255,255,255,0.5);
  margin-bottom: 40px; letter-spacing: 2px;
`;

const Buttons = styled("div")`
  display: flex; flex-direction: column; gap: 10px; min-width: 260px;
`;

const Btn = styled<{ accent?: boolean }>("button")`
  padding: 14px 32px; font-size: 16px; font-weight: 600; color: #fff;
  background: ${(p) => p.accent ? "rgba(79,195,247,0.2)" : "rgba(255,255,255,0.08)"};
  border: 1px solid ${(p) => p.accent ? "rgba(79,195,247,0.4)" : "rgba(255,255,255,0.12)"};
  border-radius: 8px; cursor: pointer; transition: all 0.15s ease;
  text-align: center; letter-spacing: 1px;
  &:hover {
    background: rgba(79,195,247,0.15);
    border-color: rgba(79,195,247,0.3);
    transform: translateY(-1px);
  }
  &:active { transform: translateY(0); }
`;

const Footer = styled("div")`
  position: absolute; bottom: 20px; font-size: 11px;
  color: rgba(255,255,255,0.3); letter-spacing: 1px;
`;

const menuItems = [
  { action: "play", label: "PLAY", accent: true },
  { action: "garage", label: "GARAGE" },
  { action: "settings", label: "SETTINGS" },
  { action: "credits", label: "CREDITS" },
  { action: "quit", label: "QUIT" },
];

export function MainMenu() {
  const [account, setAccount] = useState<AccountData | null>(null);

  useEffect(() => {
    return onNfmwEvent<AccountData | null>("main-menu:account", setAccount, AccountData.deserialize.bind(AccountData));
  }, []);

  return (
    <Root>
      <Title>NFM World</Title>
      <Subtitle>
        {account?.isLoggedIn ? `Welcome, ${account.name}` : "Racing Simulator"}
      </Subtitle>
      <Buttons>
        {menuItems.map((item) => (
          <Btn key={item.action} accent={item.accent} onClick={() => callNfmw("navigate", { page: item.action })}>
            {item.label}
          </Btn>
        ))}
      </Buttons>
      <Footer>NFM World — CEF + Preact UI</Footer>
    </Root>
  );
}
