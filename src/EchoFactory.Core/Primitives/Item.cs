namespace EchoFactory.Core;

/// <summary>A moving numeric payload.</summary>
public readonly struct Item : IEquatable<Item>
{
    public readonly ItemId Id;
    public readonly int Value;

    public Item(ItemId id, int value)
    {
        Id = id;
        Value = value;
    }

    public bool Equals(Item other) => Id.Equals(other.Id) && Value == other.Value;

    public override bool Equals(object? obj) => obj is Item i && Equals(i);

    public override int GetHashCode() => HashCode.Combine(Id, Value);

    public static bool operator ==(Item a, Item b) => a.Equals(b);

    public static bool operator !=(Item a, Item b) => !a.Equals(b);

    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture, $"{Value}#{Id}");
}
