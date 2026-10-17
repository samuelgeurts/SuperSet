# Familiemodus: ontwerpnotitie

Samuel Geurts (SNG-Solutions)

Status: idee, nog niet gepland of gebouwd. Het bijbehorende ticket staat in het projectbord (epic, Backlog).
Gerelateerd: [Adventure mode](adventure-mode.md) (#26) en de multiplayer-branch (#23, on hold).
Bij een besluit: werk dit document bij en noteer het ook onder het ticket.

## Het idee
Eén bord, het moeilijkste (5 eigenschappen, sets van 4 kaarten, 36 kaarten op tafel), dat iedereen kan zien en oplossen.
Een soort open multiplayer die niet snel gaat: het bord is zo moeilijk dat je er steeds even naar kijkt, een set vindt en weggaat.

## Waarom dit werkt
- De moeilijkheid maakt er een rustig spel van in plaats van een race.
- Spelers hoeven niet tegelijk online te zijn: asynchroon, dus geen live verbinding nodig, alleen een database en gewone HTTPS-aanvragen.
- Het bord verandert alleen als iemand een set vindt, dus er is bijna geen verkeer.

## Opzet
- **Bord = seed + logboek.** De database bewaart een seed, de volgorde van het deck en een logboek van gevonden sets (wie, welke kaarten, wanneer). Het bord wordt daaruit opnieuw opgebouwd: hetzelfde idee als bij de multiplayer en de seed per vakje bij Adventure.
- **Kleur per vinder.** Een gevonden set krijgt de randkleur van de speler die hem vond (past bij de gekleurde randen die er al zijn).
- **Dubbele claims.** De server controleert een claim tegen het bord zoals het nu is; de eerste wint, de tweede krijgt "al gevonden". Sets met kaarten die al vervangen zijn, zijn niet meer geldig.
- **Controle op de server.** De setlogica (`SetLogic`) moet ook op de server of in een databasefunctie, anders kan het bord vervuild raken.
- Mogelijke diensten: Supabase (Postgres) of Firebase, zoals bij Adventure.

## Bewust kiezen (open)
- **Privé of openbaar:** een eigen bord per gezin via een code of link (seed = familiecode), of één openbaar bord voor iedereen (dan zijn moderatie en namen nodig).
- **Samen of tegen elkaar:** samenwerkend, een scorebord, of beide tonen.
- **Wat als het bord klaar of vast is:** een nieuw bord starten ("seizoen 2") en het oude bewaren als trofee.
- **Identiteit:** een zelfgekozen bijnaam zonder account is voor een gezin genoeg; valsspelen is bij familie nauwelijks een punt.

## Verhouding tot de rest
De familiemodus is de **kleinste test voor de backend** die Adventure ook nodig heeft: één bord, geen kaart, geen moeilijkheidsladder. Daarmee test je spelers zonder account, het logboek met zetten, de controle op de server en de seed op een schaal die je kunt overzien.

Voorgestelde volgorde: eerst de familiemodus als eerste backend, daarna Adventure fase 2 erbovenop. Live multiplayer (#23) kan on hold blijven; een paar onderdelen (bord uit seed en deck, gedeelde score) zijn te hergebruiken.

## Fasen
- [ ] **1. Bord uit seed en logboek, lokaal:** het bord opbouwen uit een seed en een lijst gevonden sets; kleur per vinder. Geen database.
- [ ] **2. Database:** bord, spelers (bijnaam, anoniem) en logboek opslaan; het bord laden en vernieuwen.
- [ ] **3. Controle op de server:** claims valideren, dubbele claims afhandelen.
- [ ] **4. Gezinnen:** een eigen bord per code of link; seizoenen.
