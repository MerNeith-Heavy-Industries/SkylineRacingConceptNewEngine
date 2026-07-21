import { useState, useEffect } from "preact/hooks";
import { styled } from "goober";
import { callNfmw, onNfmwEvent } from "@shared/bridge";
import { CounterData } from '@shared/memorypack/CounterData';

const Root = styled("div")`
  width: 100%; height: 100%; display: flex; flex-direction: column;
  align-items: center; justify-content: center; gap: 16px;
  animation: nfmw-fadeIn 0.3s ease-out;
`;

const Label = styled("div")`
  font-size: 14px; color: rgba(255,255,255,0.5);
  letter-spacing: 2px; text-transform: uppercase;
`;

const Counter = styled("div")`
  font-size: 64px; font-weight: 800; color: #4fc3f7;
  text-shadow: 0 2px 16px rgba(79,195,247,0.3);
`;

const IncBtn = styled("button")`
  padding: 10px 24px; font-size: 14px; font-weight: 600;
  color: #fff; background: rgba(79,195,247,0.15);
  border: 1px solid rgba(79,195,247,0.3); border-radius: 6px;
  cursor: pointer; transition: all 0.15s ease;
  &:hover { background: rgba(79,195,247,0.25); }
`;

const BackBtn = styled("button")`
  padding: 10px 24px; font-size: 14px; font-weight: 600;
  color: #fff; background: rgba(255,255,255,0.08);
  border: 1px solid rgba(255,255,255,0.12); border-radius: 6px;
  cursor: pointer; transition: all 0.15s ease;
  &:hover { background: rgba(255,255,255,0.15); }
`;

export function TestPage() {
  const [counter, setCounter] = useState(0);

  useEffect(() => {
    return onNfmwEvent<CounterData | null>("test:counter", (data) => setCounter(data?.value ?? 0), CounterData.deserialize.bind(CounterData));
  }, []);

  return (
    <Root>
      <Label>CEF + Preact Test</Label>
      <Counter>{counter}</Counter>
      <IncBtn onClick={() => callNfmw("increment")}>Increment (JS → C#)</IncBtn>
      <BackBtn onClick={() => callNfmw("back")}>← Back to Menu</BackBtn>
    </Root>
  );
}
