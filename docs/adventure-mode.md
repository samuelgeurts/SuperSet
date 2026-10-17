# Adventure mode: ontwerpnotitie

Samuel Geurts (SNG-Solutions)

Status: idee, nog niet gepland of gebouwd. Het bijbehorende ticket is [#26](https://github.com/samuelgeurts/SuperSet/issues/26).
Dit document bevat het idee met advies, zodat het later in losse tickets per fase gesplitst kan worden.
Bij een besluit: werk dit document bij en noteer het ook onder #26.

## Het idee
Een Adventure mode: elk spel is een vakje op een groot, in theorie oneindig, schaakbordachtig platform. Je begint in het midden met de makkelijkste graad en puzzelt verder. Elk vakje houdt statistieken bij: hoe vaak het gespeeld is en wie het als eerste oploste. Daar hoort een database achter.

## Advies: seed per vakje (geen echte willekeur)
- Het bord van een vakje volgt uit `wereld-seed + (x, y)`: iedereen speelt dezelfde puzzel. Alleen dan kloppen "eerste oplosser" en het aantal keer gespeeld.
- Er hoeven geen borden opgeslagen te worden; het vakje is een formule. In de database komen alleen statistieken, en pas als iemand een vakje aanraakt.
- Een nieuwe wereld (seizoen) is gewoon een andere seed.
- Echte willekeur kan als aparte modus, bijvoorbeeld een dagelijkse puzzel.
- Technisch: `Random.Range` bij het schudden en bij het verwisselen van kaarten vervangen door een generator met seed. Bij de multiplayer-branch (#23) is dit al gedaan voor het bord (`System.Random` met seed).

## Wat is "opgelost"?
Nu eindigt een spel als het deck op is. Opties: het deck leeg, een doel ("vind N sets", N groeit met de afstand) of een tijd/zetlimiet. Advies: een doel per vakje.

## Moeilijkheid laten doorlopen
Er zijn nu 6 niveaus (3, 4 of 5 eigenschappen, sets van 3 of 4 kaarten). Voor een oneindige wereld zijn er meer knoppen nodig: minder kaarten op tafel, meer sets die gevonden moeten worden, een tijdslimiet, speciale vakjes (bijvoorbeeld half verborgen kaarten). De afstand tot het midden bepaalt de moeilijkheid.

## Database
Asynchroon: de speler speelt alleen en stuurt daarna het resultaat op. Alleen gewone HTTPS-aanvragen; geen WebSocket nodig. Werkt vanuit browser (itch.io) en Android. Mogelijke diensten: Supabase (Postgres) of Firebase.

Per vakje: coördinaten, aantal keer gespeeld, aantal keer opgelost, eerste oplosser (naam of anonieme id), tijdstip, beste tijd. Spelers: anonieme apparaat-id met een zelfgekozen bijnaam; inloggen kan later.

## Aandachtspunten
- **Valsspelen:** een speler kan zomaar melden dat hij iets oploste. Omdat het bord deterministisch is, kan de server de zetten controleren. In de eerste versie mag dit achterwege blijven; plan het wel in, zeker als "eerste" iets betekent.
- **Namen:** vrije bijnamen worden misbruikt; begin met gegenereerde namen of een filter.
- **Kaart tonen:** per stukje rond de speler; een warmtekaart van veelgespeelde vakjes is een mooie uitbreiding.
- **Vooruitgang:** alleen vakjes naast een opgelost vakje spelen (schaakbordgevoel) of overal heen springen.

## Voorstel in fasen
- [ ] **1. Lokaal:** kaart, seed, moeilijkheid per afstand en vooruitgang op het toestel. Geen database. Test of het leuk is.
- [ ] **2. Statistieken:** database met anonieme spelers, aantal keer gespeeld en eerste oplosser.
- [ ] **3. Controle en sociaal:** zetten controleren, bijnamen, ranglijst.
- [ ] **4. Uitbreiden:** seizoenen, speciale vakjes, eventueel live multiplayer (zie #23).

## Beslispunten (nog open)
- Wat betekent "opgelost": het deck leeg, een doel of tijd?
- Overal heen springen, of alleen naar naastgelegen vakjes?
- Eén gedeelde wereld voor iedereen, of een eigen wereld per speler of groep?
- Hoe belangrijk is "de eerste zijn"? Dat bepaalt hoe streng de controle moet zijn.

Gerelateerd: #23 (multiplayer, on hold). De Play Store is een apart onderwerp en nog niet gepland.

