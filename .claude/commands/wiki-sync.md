Sync game data from Beastborne to the Discord bot's wiki JSON files.

The Discord bot lives at: C:\Users\jscho\Documents\public-square-bot

1. Read `Code/Core/MonsterManager.cs` to extract all monster species data (look for species registration calls with Name, Element, Rarity, BaseStats, Description, EvolvesFrom, EvolvesTo, EvolutionLevel, BeastiaryNumber, BaseCatchRate, PossibleTraits, LearnableMoves)
2. Read `Code/Core/MonsterManager.cs` for move definitions (Name, Element, Category, Power, Accuracy, PP, Priority, Effect, Description)
3. Read `Code/Core/ItemManager.cs` for item definitions (Name, Category, Rarity, Effect, Description, BuyPrice, SellPrice)
4. Read `Code/Core/MonsterManager.cs` for trait definitions (Name, Rarity, Description)

Export to these JSON files in the bot project:
- `C:\Users\jscho\Documents\public-square-bot\src\data\monsters.json`
- `C:\Users\jscho\Documents\public-square-bot\src\data\moves.json`
- `C:\Users\jscho\Documents\public-square-bot\src\data\items.json`
- `C:\Users\jscho\Documents\public-square-bot\src\data\traits.json`

Use the existing JSON files as format reference. Keep the same key structure. Show a summary of what changed (new entries, removed entries, updated fields).
