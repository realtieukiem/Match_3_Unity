using System.Collections.Generic;

public static class SelectedCardLoadout
{
    private static readonly List<CardDefinition> cards = new List<CardDefinition>();

    public static IReadOnlyList<CardDefinition> Cards
    {
        get { return cards; }
    }

    public static int MaxSelectedCards { get; private set; } = 3;

    public static void Set(IEnumerable<CardDefinition> selectedCards, int maxSelectedCards)
    {
        cards.Clear();
        MaxSelectedCards = maxSelectedCards > 0 ? maxSelectedCards : 1;

        if (selectedCards == null)
            return;

        foreach (CardDefinition card in selectedCards)
        {
            if (card == null || cards.Contains(card))
                continue;

            if (cards.Count >= MaxSelectedCards)
                break;

            cards.Add(card);
        }
    }

    public static void Clear()
    {
        cards.Clear();
        MaxSelectedCards = 3;
    }
}
