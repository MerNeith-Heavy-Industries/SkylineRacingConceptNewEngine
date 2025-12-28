# Multiplayer Netcode Specification

## Lobby

### Joining
* Player sends C2S_PlayerIdentity - done c/s
* Server periodically sends S2C_LobbyState * done c/s

### Chatting
* Player sends C2S_LobbyChatMessage - done c/s
* Lobby broadcasts S2C_LobbyChatMessage - done c/s

### Creating Games
* Player sends C2S_CreateSession - done c/s
* Server broadcasts S2C_LobbyState - done c/s

### Joining Games
* Player sends C2S_JoinSession - done c/s
* Server broadcasts S2C_LobbyState - done c/s

### Leaving Games
* Player sends C2S_LeaveSession - done c/s
* Server broadcasts S2C_LobbyState - done c/s

### Ready Up
* Player sends C2S_LobbyPlayerReadyState
* Server broadcasts S2C_LobbyState

### Starting Games
* Room creator client sends C2S_LobbyStartRace - done c/s
* Server sends S2C_RaceStarted to joined clients - done c/s


* Server waits 20 seconds for all players to send C2S_RaceLoaded - done c/s
* Server sends S2C_RaceCanStart - done c/s
* If timeout, server sends S2C_RaceFailedToStart - done c/s
* Enter in-game state - done c/s

### Spectating
* Player sends C2S_JoinAsSpectator
* Enter in-game state as spectator (only receives S2C_PlayerState updates)

### Cleaning up finished sessions
* Server periodically removes sessions that have been finished for more than 5 minutes

## In-Game (Netcode v1 non-deterministic)
V1 is a dumb relay without rollback. The client just sends
positional and state updates to the server, which relays
them to all other clients.

* Clients send C2S_PlayerState - done c/s
* Server broadcasts S2C_PlayerState - done c/s

#### Finishing Game
* Client sends C2S_GameFinished
  * First-come first-served full trust basis
* Server broadcasts S2C_GameFinished
* Return to lobby state

#### Disconnecting
* Client sends C2S_SelfDisconnect
* Server broadcasts S2C_PlayerState with disconnect=true

### In-Game (Netcode v2 deterministic with rollback)
V2 is a deterministic lockstep netcode with rollback.

* Clients send C2S_InputFrames - last confirmed frame + inputs for next frames
* Server broadcasts S2C_AuthoritativeFrame - authoritative game state at specific frame + player inputs

* Client predicts current frame based on previous frames + prediction
* Client rollbacks to authoritative frame when received + re-simulates to current frame

## Future Work
* Thread per session on server for scalability
* Only send lobby updates to changed sessions