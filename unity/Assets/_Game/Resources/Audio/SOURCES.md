# Sound sources

The real recordings (engines, tracks, turret, guns, the fighter) come from the Sonniss GDC Game Audio Bundles 2018 and
2019 (https://sonniss.com/gameaudiogdc/): royalty-free, commercial use in games, no attribution required, not to be
used for AI/ML training; prepared by `art/pipeline/sonniss-prep.py`.

Library recordings, prepared by `art/pipeline/sfx-prep.py` (mono 44.1 kHz, trimmed, normalised, loops cross-faded).
Pixabay files are under the Pixabay Content License (free for commercial use, no attribution required; the
`freesound_community` ones were CC0 on Freesound before Pixabay mirrored them). Mixkit files are under the Mixkit
Sound Effects Free License (free for commercial use). Neither may be redistributed as standalone sound files.

| Clip | Source | Author |
|---|---|---|
| shot | Sonniss GDC 2019, "Battlefield Howitzers": M101 105 mm, distant, "Pound, Thick" | Airborne Sound |
| shotHeavy | Sonniss GDC 2019, "Battlefield Howitzers": M101 105 mm, medium distant, "Firm, Heavy" | Airborne Sound |
| shotFar | Sonniss GDC 2019, "Battlefield Howitzers": M101 105 mm, distant, "Explode, Crack" | Airborne Sound |
| flak | Pixabay 85147 "antiair" | freesound_community |
| reload | Pixabay 47828 "Tank Reload" | freesound_community |
| mg | Pixabay 43670 "Machine Gun Burst" | freesound_community |
| faust | Pixabay 307512 "Rocket Launcher" | 49053354 |
| hit | Pixabay 454390 "Hammer Steel Impact" | Universfield |
| ricochet | Pixabay 101553 "Ricochet 2" | freesound_community |
| ricochet2 | Pixabay 41134 "Whizzby" | freesound_community |
| explosion | Pixabay 369789 "Big Explosion sfx" | kave_msri |
| artillery | Pixabay 100420 "Large Explosion" | freesound_community |
| whistle | Pixabay 104653 "Incoming mortar 1" | freesound_community |
| engine | Sonniss GDC 2018, "Sherman M4A3 Medium Tank" t3, on board, medium drive | Pole Position Production |
| engine_idle | Sonniss GDC 2018, "Chaffee M24 Light Tank" t6, on board, idle in neutral | Pole Position Production |
| engine_enemy | Sonniss GDC 2019, "Panzer IV Ausf. G" t10, outside, idle | Pole Position Production |
| tracks | Sonniss GDC 2018, "Sherman M4A3 Medium Tank" t4, microphone by the right track | Pole Position Production |
| turret | Sonniss GDC 2018, "Chaffee M24 Light Tank" t7, hydraulic traverse | Pole Position Production |
| fighter | Sonniss GDC 2018, "North American P-51D Mustang" t2, full power, made into a pass | Pole Position Production |
| wind | Pixabay 17044 "Outdoors_Night_Windy_01" | freesound_community |
| rain | Pixabay 350531 "Heavy Rain on Metal Roof" | eryliaa |
| front | Pixabay 242655 "SFX - Distant War Zone Bombardment" | fronbondi_skegs |
| click | Mixkit 1117 "Classic click" | Mixkit |
| pickup | Mixkit 2544 "Metal button radio ping" | Mixkit |
| levelUp | Mixkit 265 "Quick positive video game notification interface" | Mixkit |

## Music
Four themes from Pixabay (Pixabay Content License: free for commercial use, no attribution required, not to be
redistributed as standalone files), kept as the downloaded MP3s; Unity imports them compressed in memory as Vorbis, so
two decks can play one theme at once for its seamless loop (`Sfx.Theme`).

| Clip | Where | Source | Author |
|---|---|---|---|
| music_menu | the hangar | Pixabay 219094 "Battlefield Borders (Ambience)", 2:00 | AberrantRealities |
| music_battle | under the fight | Pixabay 444879 "Echoes of the Battlefield - Cinematic Percussion Ambience", 3:39 | DesiFreeMusic |
| music_boss | while the Tiger Ace or the King Tiger lives | Pixabay 493408 "Cinematic Drums War", 1:50 | Alec_Koff |
| music_dawn | once, at the win | Pixabay 293125 "Majestic Brass Fanfare", 0:51 | Luis_Humanoide |

Until 2026-09-28 the menu played Sousa's "The U.S. Field Artillery" by the United States Marine Band (public domain,
`menu_theme.wav`, in the git history).
