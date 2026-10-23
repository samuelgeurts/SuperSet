# Familiemodus: ontwerpnotitie

Samuel Geurts (SNG-Solutions)

Status: gepland (2026-10-22), nog niet gebouwd. Epic #27 met de fasen als tickets #48-#51 in Backlog.
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

## Besluiten (2026-10-22)
- **Dienst:** Firebase, project `superset-75145`, database Firestore. Het Firebase Unity-SDK ondersteunt WebGL niet, dus het spel praat via REST (`UnityWebRequest`), met anonieme login (Firebase Auth REST).
- **Openbaar of privé:** beginnen met één openbaar bord (`boards/main`). Alles hangt onder een bord-ID, zodat eigen borden per gezin later alleen een extra ID zijn (fase 4).
- **Samen of tegen elkaar:** iedereen kan zien hoeveel sets elke speler heeft gevonden (scorelijst per speler), plus de kleur per vinder op het bord.
- **Identiteit:** zelfgekozen bijnaam met anonieme login.

## Bewust kiezen (open)
- **Strikte controle op de server (fase 3, vervolg):** zie "Controle op de server" hieronder. De gratis regels controleren alleen de vorm; echt nagaan dat vier kaarten een set vormen kan alleen met een Cloud Function (Blaze, betaalkaart).
- **Wat als het bord klaar of vast is:** een nieuw bord starten ("seizoen 2") en het oude bewaren als trofee (fase 4).

## Verhouding tot de rest
De familiemodus is de **kleinste test voor de backend** die Adventure ook nodig heeft: één bord, geen kaart, geen moeilijkheidsladder. Daarmee test je spelers zonder account, het logboek met zetten, de controle op de server en de seed op een schaal die je kunt overzien.

Voorgestelde volgorde: eerst de familiemodus als eerste backend, daarna Adventure fase 2 erbovenop. Live multiplayer (#23) kan on hold blijven; een paar onderdelen (bord uit seed en deck, gedeelde score) zijn te hergebruiken.

## Fasen
- [ ] **1. Bord uit seed en logboek, lokaal (#48):** het bord opbouwen uit een seed en een lijst gevonden sets; kleur per vinder en sets per speler. Geen database.
- [ ] **2. Database (#49):** bord, spelers (bijnaam, anoniem) en logboek opslaan in Firestore; het bord laden en vernieuwen.
- [ ] **3. Controle op de server (#50):** claims valideren, dubbele claims afhandelen.
- [ ] **4. Gezinnen (#51):** een eigen bord per code of link; seizoenen.

## Firebase instellen (fase 2)
Project `superset-75145`. Het spel praat via REST met Firebase Auth en Firestore (het Unity-SDK werkt niet in WebGL), met anonieme login. De `apiKey` staat in `FirebaseConfig.ApiKey`; zonder sleutel werkt het familiebord alleen offline op het toestel zelf (met twee nepspelers als demo).

Eenmalig in de [Firebase-console](https://console.firebase.google.com/project/superset-75145):
1. **Authentication** > Sign-in method > **Anonymous** inschakelen.
2. **Firestore Database** aanmaken (productiemodus, regio in Europa).
3. Tabblad **Rules**: de inhoud van [firebase/firestore.rules](../firebase/firestore.rules) plakken en publiceren. Iedereen met een anonieme login mag lezen en nieuwe sets schrijven, maar een gevonden set kan daarna niet meer worden gewijzigd of verwijderd. Fase 3 voegt de controle toe dat een claim echt een set is.
4. **Projectinstellingen** > Je apps > webapp (`</>`) toevoegen en de `apiKey` in `Assets/scripts/FirebaseConfig.cs` zetten (`ApiKey`). De sleutel is geen geheim. Heeft de sleutel een beperking op websites, laat dan ook de itch.io-domeinen toe.

Datamodel:
- `boards/main`: het bord (`seed`). De eerste speler maakt het aan.
- `boards/main/presence/{uid}`: wie online is (`name`, `lastSeen`), zie hieronder.
- `boards/main/claims/000123`: het logboek, één document per gevonden set (`n`, `player`, `slots` als "7,11,21,31", `uid`). Het nummer in de naam is een slot: een speler kan alleen het eerstvolgende nummer schrijven. Heeft iemand anders dat al gedaan, dan geeft Firestore "bestaat al" (HTTP 409), en moet de speler eerst het bord verversen. Zo wint de eerste claim.
- Het spel vraagt elke 30 seconden alleen de nieuwe claims op (`n` groter dan de laatste die het kent) en laat die van anderen één voor één zien.

Spelers worden herkend aan hun `uid`; de naam is een label dat je kunt wijzigen (Change name in het modusmenu) en dat bij de volgende set van die speler zichtbaar wordt voor de anderen. Twee spelers met dezelfde naam blijven dus gescheiden.

Bekende beperkingen: het naamvak werkt op een telefoon in de browser mogelijk niet goed (IMGUI-toetsenbord), en een vol bord start nog geen nieuw seizoen (#51).

Testen zonder echte Firebase: de Firebase-emulator (`firebase emulators:start --only auth,firestore --project superset-75145`) en in de editor de omgevingsvariabele `SUPERSET_FIREBASE_EMULATOR=127.0.0.1`. Daarmee zijn de aanmelding, het aanmaken van het bord, een claim van een tweede speler, het tonen daarvan, een eigen claim en een dubbele claim (409) getest, ook met de regels uit `firebase/firestore.rules`.

## Controle op de server (fase 3, #50)
Gekozen: de gratis Firestore-regels ([firebase/firestore.rules](../firebase/firestore.rules), Spark-plan, geen betaalkaart). Ze weigeren een claim die niet klopt:
- de naam van het document is het nummer met 6 cijfers en het vorige nummer bestaat (er zijn geen gaten, en wie het eerst schrijft wint);
- de vier slots zijn verschillende getallen van 0 tot 35, oplopend;
- de claim heeft het uid van wie schrijft en een naam van 1 tot 14 tekens;
- een geschreven claim kan niet meer worden gewijzigd of verwijderd.

**Wat de regels niet kunnen:** controleren dat de vier kaarten echt een set vormen. De regels zien de kaarten op tafel niet: het bord wordt uit de seed en het logboek opgebouwd, met een willekeurige (maar vaste) reeks wissels, en dat kan een regel niet narekenen. Het spel van elke speler doet die controle wel: bij het afspelen van het logboek wordt een claim die geen set is, of kaarten gebruikt die al weg zijn, overgeslagen. Het bord en de scores van eerlijke spelers blijven dus kloppen; een vervalste claim kost alleen een nummer.

**Beperking:** iedereen met de sleutel kan anoniem een account maken, dus een kwaadwillende kan het dagelijkse gratis aantal schrijfacties opmaken door veel claims te sturen. Daartegen helpt alleen Firebase App Check (nog niet ingericht) of een server die claims controleert. Een Cloud Function (Blaze) zou ook de setcontrole zelf kunnen doen; dat is de stap als het bord echt druk wordt.

Publiceren van de regels (nodig na elke wijziging): `firebase deploy --only firestore:rules` in de map van het project (`firebase.json` en `.firebaserc` staan erin).

## Wie is online (#58)
- Elke minuut schrijft het spel (alleen als het venster actief is) het document `boards/main/presence/{uid}` met de naam en de tijd van de server. Online is wie in de laatste 150 seconden van zich liet horen. De lijst wordt daarna opgehaald met één vraag op `lastSeen`.
- Spelers zonder set maar wel online staan ook in de lijst. Een groen bolletje bij de online spelers, en in de strook "N online   Total N".
- Een speler schrijft alleen zijn eigen document, met de tijd van de server (de regels dwingen dat af).
- **Kosten op het gratis plan:** 20.000 schrijfacties en 50.000 leesacties per dag voor het hele project. Het spel vraagt nu elke 30 seconden om nieuwe sets (één leesactie per vraag, dus ongeveer 2.900 per dag per speler die de hele dag open heeft) en elke minuut om de online lijst (een leesactie per online speler). Dat past voor een kleine groep.

## Weggewisselde kaarten (#59)
Ligt er na het uitdelen geen set op tafel, dan wisselt het spel kaarten van tafel voor de volgende van de stapel tot er een set ligt. De weggewisselde kaarten gaan nu **onderop de trekstapel** en gaan dus niet meer verloren (het zijn er ongeveer 107 op de 300 nieuwe tafels van het familiebord-formaat). Er is een grens van 300 wissels; lukt het daarmee niet, dan is het bord af. De wissels volgen dezelfde vaste reeks, dus het opnieuw opbouwen uit het logboek blijft kloppen. Een oudere versie van de app bouwt een bord waar gewisseld moest worden anders op; speel dus met één versie per bord.
