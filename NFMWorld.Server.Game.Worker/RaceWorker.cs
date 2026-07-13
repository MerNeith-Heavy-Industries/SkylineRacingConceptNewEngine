using MemoryPack;
using NFMWorld.Server.SharedMemory;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Multiplayer;
using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Headless race simulation loop for the Worker process.
/// Uses <see cref="RaceGamemode"/> for countdown, checkpoint tracking, and
/// finish detection. Client inputs are fully trusted (v1) — each tick applies
/// the received <see cref="CarFrame"/> directly to the car via ApplyToCar,
/// then GameTick advances AI and race logic.
/// </summary>
public class RaceWorker
{
    private readonly RaceGamemode _gamemode;
    private readonly Dictionary<byte, int> _playerIndexToCarIndex = new();
    private readonly Dictionary<int, byte> _carIndexToPlayerIndex = new();
    private readonly Dictionary<byte, PlayerState> _accumulatedInputs = new();
    private readonly TaskCompletionSource<byte[]> _raceFinishedTcs = new();
    private byte[]? _finalPositions;
    private bool _canTick;

    /// <summary>Fires when the race finishes. Result is the position standings byte array.</summary>
    public Task<byte[]> RaceFinishedTask => _raceFinishedTcs.Task;

    public RaceWorker(
        string stageName,
        GameModes gamemodeType,
        IDictionary<byte, PlayerInfo> players)
    {
        var simData = BackendGamemodeData.Create(stageName);

        var playerParams = new List<PlayerParameters>();
        foreach (var (idx, info) in players.OrderBy(kv => kv.Key))
        {
            playerParams.Add(new PlayerParameters
            {
                PlayerName = info.Name,
                CarName = info.Vehicle,
                Color = info.Color,
                IsBot = false,
                IsClientPlayer = false
            });
            _playerIndexToCarIndex[idx] = playerParams.Count - 1;
            _carIndexToPlayerIndex[playerParams.Count - 1] = idx;
        }

        var parameters = new BaseGamemodeParameters { Players = playerParams };
        _gamemode = new RaceGamemode(parameters, simData);
        _gamemode.RaceFinished += OnRaceFinished;

        Console.WriteLine(
            $"[Worker] Stage={stageName}, Gamemode={gamemodeType}, " +
            $"Players={players.Count}");
    }

    /// <summary>Starts the race (calls Enter → Reset → countdown).</summary>
    public void Start()
    {
        _gamemode.Enter();
        _canTick = true;
        Console.WriteLine("[Worker] Race started, countdown in progress...");
    }

    /// <summary>Accumulates a player input from the Controller. Thread-safe.</summary>
    public void AccumulateInput(byte playerIndex, PlayerState state)
    {
        lock (_accumulatedInputs)
        {
            _accumulatedInputs[playerIndex] = state;
        }
    }

    /// <summary>Snaps accumulated inputs and clears the buffer.</summary>
    public PlayerInputBatch SnapInputs(uint tickNumber)
    {
        lock (_accumulatedInputs)
        {
            var batch = new PlayerInputBatch
            {
                TickNumber = tickNumber,
                ServerTime = DateTimeOffset.UtcNow,
                PlayerStates = new Dictionary<byte, PlayerState>(_accumulatedInputs)
            };
            _accumulatedInputs.Clear();
            return batch;
        }
    }

    /// <summary>
    /// Processes one tick of inputs and returns the resulting game state.
    /// Applies each client's CarFrame directly (full trust), then ticks the gamemode.
    /// </summary>
    public GameStateSnapshot Tick(PlayerInputBatch batch)
    {
        if (!_canTick)
            throw new InvalidOperationException("Call Start() before Tick()");

        // Apply client inputs to cars (full trust v1)
        var cars = _gamemode.carsInRace;
        foreach (var (playerIndex, playerState) in batch.PlayerStates)
        {
            if (_playerIndexToCarIndex.TryGetValue(playerIndex, out var carIndex)
                && carIndex < cars.Count)
            {
                playerState.CarFrame.ApplyToCar(cars[carIndex]);
            }
        }

        // Tick gamemode — drives AI, handles collisions, checkpoints, race logic
        _gamemode.GameTick();

        // Build state snapshot
        var states = new Dictionary<byte, PlayerState>();
        foreach (var (playerIndex, carIndex) in _playerIndexToCarIndex)
        {
            if (carIndex < cars.Count)
            {
                states[playerIndex] = PlayerState.CreateFrom(batch.TickNumber, cars[carIndex]);
            }
        }

        var isFinished = _raceFinishedTcs.Task.IsCompleted;

        return new GameStateSnapshot
        {
            TickNumber = batch.TickNumber,
            ServerTime = DateTimeOffset.UtcNow,
            PlayerStates = states,
            IsRaceFinished = isFinished
        };
    }

    /// <summary>Stops the simulation.</summary>
    public void Stop()
    {
        _canTick = false;
        _gamemode.Exit();
    }

    private void OnRaceFinished(object? sender, byte[] positions)
    {
        Console.WriteLine($"[Worker] Race finished! Winner: car index {positions[0]}");
        _canTick = false;
        _finalPositions = positions;
        _raceFinishedTcs.TrySetResult(positions);
    }

    /// <summary>
    /// Builds final player results from the gamemode's position standings.
    /// </summary>
    public Dictionary<byte, PlayerResult> GetFinalResults()
    {
        var results = new Dictionary<byte, PlayerResult>();
        if (_finalPositions is null) return results;

        for (byte pos = 0; pos < _finalPositions.Length; pos++)
        {
            var carIndex = _finalPositions[pos];
            if (_carIndexToPlayerIndex.TryGetValue(carIndex, out var playerIndex))
            {
                results[playerIndex] = new PlayerResult
                {
                    FinishPosition = (byte)(pos + 1),
                    Finished = true
                };
            }
        }

        // Mark any players not in positions as DNF
        foreach (var (playerIndex, carIndex) in _playerIndexToCarIndex)
        {
            results.TryAdd(playerIndex, new PlayerResult
            {
                FinishPosition = 0,
                Finished = false
            });
        }

        return results;
    }
}
