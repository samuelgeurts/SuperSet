# Animatie en geluid: ontwerpnotitie

Samuel Geurts (SNG-Solutions)

Status: idee, besproken op 2026-10-18; nog geen ticket of code.
Bij een besluit: werk dit document bij en noteer het ook onder het ticket.

## Het idee
Het spel krijgt beweging en geluid in de sfeer van Patience op Windows: kaarten worden van een stapel gedeeld,
een gevonden set wordt even getoond en verdwijnt op een aflegstapel, en elke gebeurtenis heeft een eigen geluidje.

## Besluiten
- **Trekstapel links, aflegstapel rechts**; het bord blijft in het midden.
- **Delen aan het begin:** alle kaarten vliegen van de trekstapel naar hun plek op tafel, ongeveer 1 seconde in totaal,
  met een deelgeluid (zoals Patience).
- **Set gevonden:** pling. De 3 of 4 kaarten worden iets vergroot naast elkaar voor het bord getoond, het bord erachter
  is gedimd en de kaarten houden de bekende omlijning (teal selectierand). Na 2 seconden gaan ze naar de aflegstapel.
- **Nieuwe kaarten:** de 3 of 4 vervangende kaarten worden van de trekstapel gedeeld, met hetzelfde deelgeluid.
- **Foute set:** een andere, lagere toon, plus een korte rode rand of trilling op de gekozen kaarten; daarna wordt de selectie gewist.
- **Tijdens animaties** is invoer geblokkeerd, zodat je geen kaart kiest die net verdwijnt.
- **Einde deck:** de trekstapel verdwijnt en de gaten blijven leeg, zoals nu; de aflegstapel groeit tot het eindscherm.
- **Geluidsbron:** zelf genereren (in code of met een tool); geen licentiezorgen en kleine bestanden voor de WebGL-download.
- **Geluid aan/uit:** luidspreker-icoon in de topbalk, keuze wordt onthouden.

## Advies (nog niet vastgelegd)
- Animaties in code met een kleine tweening-aanpak, in lijn met de kaarten die al in code getekend worden.
- Een aparte, kleine geluidsmanager (afspelen, aan/uit, onthouden), zodat de speellogica er niets van weet.
- Eerst de animatiegebeurtenissen scheiden van de spelregels. `DeckView.cs` is een hotspot; een kleine verdeling
  (bord, indeling, topbalk, animatie) hoort daarom voor of tijdens de eerste story.
- Browsers starten geluid pas na een eerste tik of klik; de eerste deelanimatie in WebGL kan dus stil blijven.
  Bij de story over geluid moet dit expliciet worden bekeken.

## Voorgestelde stories (concept)
1. Stapels en delen aan het begin, met deelgeluid (trekstapel links, aflegstapel rechts).
2. Gevonden set tonen en afleggen: vergroot en gedimd bord, 2 seconden, naar aflegstapel, pling.
3. Vervangende kaarten delen van de trekstapel, met deelgeluid.
4. Foute set: lage toon en rode rand of trilling.
5. Geluid aan/uit in de topbalk, met onthouden keuze.

## Open punten
- Hoe de geluiden precies klinken (toonhoogte, lengte); die kies je pas bij het testen.
- Of de animatiesnelheid later een instelling wordt.
