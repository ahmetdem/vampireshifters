using System;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Quest 14: Custom serializable struct for leaderboard data.
/// Implements INetworkSerializable for network transmission and IEquatable for change detection.
/// </summary>
public struct LeaderboardEntry : INetworkSerializable, IEquatable<LeaderboardEntry>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public int Coins;
    public int Level;
    public int Deaths;

    public LeaderboardEntry(ulong clientId, string playerName, int coins, int level, int deaths)
    {
        ClientId = clientId;
        PlayerName = new FixedString32Bytes(playerName);
        Coins = coins;
        Level = level;
        Deaths = deaths;
    }

    // INetworkSerializable implementation - required for NetworkList
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Coins);
        serializer.SerializeValue(ref Level);
        serializer.SerializeValue(ref Deaths);
    }

    // IEquatable implementation - required for NetworkList change detection
    public bool Equals(LeaderboardEntry other)
    {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               Coins == other.Coins &&
               Level == other.Level &&
               Deaths == other.Deaths;
    }

    public override bool Equals(object obj)
    {
        return obj is LeaderboardEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, PlayerName, Coins, Level, Deaths);
    }

    public static bool operator ==(LeaderboardEntry left, LeaderboardEntry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LeaderboardEntry left, LeaderboardEntry right)
    {
        return !left.Equals(right);
    }
}
