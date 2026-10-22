# Superset (Unity)

Dit is een Unity-project (Unity 6000.6.5f1). Deze regels gaan voor op de algemene
werkafspraken in `~/.claude/CLAUDE.md`.

- **Labels:** taal-label is altijd `Unity` (geen `PowerShell`, `C#` of `XAML`), plus het
  soort-label (`bug`, `feature`, `breaking`).
- **Uitvoerder:** de specialist-agents (`powershell-`, `csharp-`, `xaml-specialist`) gelden
  hier niet. Tickets in "Ready" voer ik zelf uit, in een worktree buiten de repo.
- **Testen:** er zijn geen automatische tests en `dotnet build` werkt niet. Controle gaat via
  Unity: compilatiefouten staan in `Logs/Editor.log` (zoek op `error CS`). Gedrag in Play-modus
  test de gebruiker in de editor; ticket dan naar "Review" en niet zelf op Done zetten.
- **Unity open:** wijzig geen bestanden in een worktree die op dat moment in Unity open staat,
  en start Unity niet headless op een project dat al open is.
- **`.meta`-bestanden:** altijd samen met het bijbehorende bestand committen. Hernoem of
  verplaats assets nooit zonder hun `.meta`, anders breken de verwijzingen in scènes en prefabs.
- **Scènes en prefabs** zijn YAML. Pas ze niet met de hand aan tenzij het ticket dat vraagt;
  liever via code of door de gebruiker in de editor.
- **Hoofdscène:** `Assets/Scenes/Card Deck Test.unity`.
- **GitHub-project:** https://github.com/users/samuelgeurts/projects/7 (repo `samuelgeurts/SuperSet`).
- **Wachten op builds:** een Unity-build duurt 10 tot 15 minuten. Start hem met
  `run_in_background` en wacht op de melding in plaats van te pollen met `sleep`-lussen
  (elke aanroep kost context). Eén korte tussenstand bij de start is genoeg.
- **APK met vaste downloadlink:** upload bij elke GitHub-release de APK twee keer: als
  `SuperSet-<versie>.apk` en als `SuperSet.apk` (vaste naam). De link
  `https://github.com/samuelgeurts/SuperSet/releases/latest/download/SuperSet.apk` geeft dan altijd
  de nieuwste.
