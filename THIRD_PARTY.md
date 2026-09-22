# Third-party tools, models and assets

What was used to make Iron Night, under which license, and what each license asks of us.

| Used for | What | License | Obligation |
|---|---|---|---|
| Engine | Unity 6 (Personal) | Unity Terms of Service | revenue/funding under the Personal tier threshold |
| Vehicle 3D models | Microsoft TRELLIS 2 (image-to-3D, run locally) | MIT | keep the license notice |
| Tank models (see the table below) | Sketchfab artists, CC BY 4.0 | CC BY 4.0 | credit the author, link the model and the license, say if it was changed |
| Image encoder inside TRELLIS 2 | Meta DINOv3 | DINOv3 License | show "Built with DINOv3" in the game credits |
| 3D generation UI | ComfyUI + ComfyUI-Trellis2 nodes | GPL-3.0 / MIT | tools only, nothing shipped in the game |
| Reference images for the models | Generated images (OpenAI image model) | OpenAI Terms of Use | outputs belong to the user; no attribution required |
| Ground textures | Stable Diffusion XL 1.0 + ControlNet Union SDXL (run locally) | CreativeML OpenRAIL++-M / Apache-2.0 | use restrictions of OpenRAIL++ (no illegal or harmful use) |
| Mockup placeholder sprites (not shipped) | Kenney "Top-down Tanks Redux" | CC0 | none |
| Mockup placeholder sprites (not shipped) | UnLucky Studio "Top Down Planes" (OpenGameArt) | CC0 | none |
| Sound effects (guns, hits, explosions, engine, tracks, wind, rain, front line) | Pixabay sound library, listed per file in unity/Assets/_Game/Resources/Audio/SOURCES.md | Pixabay Content License | free for commercial use, no attribution; not to be redistributed as standalone files |
| Sound effects (UI, heavy gun) | Mixkit, listed per file in the same SOURCES.md | Mixkit Sound Effects Free License | free for commercial use, no attribution; not to be redistributed as standalone files |
| Effects, ground details, UI | procedural, written for this project | — | — |

Generated images and models are not copyrightable works by themselves; the code, the game design, the scene compositions
and everything hand-made in this repository are the author's own work.

## Tank models from Sketchfab (CC BY 4.0)
All modified for the game: split into hull and turret, rescaled, textures resized. Sources with the licence texts are in `art/models/sketchfab/<name>/license.txt`.

| In the game | Model | Author | Link |
|---|---|---|---|
| M4 Sherman | Sherman M4A3 Green Set E | mamont nikita | https://sketchfab.com/3d-models/sherman-m4a3-green-set-e-da4aa5094c4a474b896d055ab27f10f5 |
| M24 Chaffee | M24 Chaffee | buffinbag | https://sketchfab.com/3d-models/m24-chaffee-4f5e64590a514da2a6736fcac5c954e9 |
| M26 Pershing | M26 Pershing Eagle 7 | Hxhdjdjdk | https://sketchfab.com/3d-models/m26-pershing-eagle-7-667ebbc7463f4a91b3fca404cdb9bdc3 |
| T-34-85 | T-34 85 Tank | Julian | https://sketchfab.com/3d-models/t-34-85-tank-ca24cc254922473a91ec213aebd83292 |
| KV-1 | Kv-1 (КВ-1) | Artem Goyko | https://sketchfab.com/3d-models/kv-1-1-f23cf9b8822a4ad48f068a2c51c44848 |
| SU-100 | SU-100 | XxRxX | https://sketchfab.com/3d-models/su-100-29fd624d0cfe42779dc4a0a5880ea905 |
| IS-2 | IS-2M - USSR | Mr_Chiko | https://sketchfab.com/3d-models/is-2m-ussr-36f76a2b9c7542e5af95a9fb41691b60 |
| Panzer IV | Panzer IV Medium Tank - Toshueyi | Joanthan To | https://sketchfab.com/3d-models/panzer-iv-medium-tank-toshueyi-14c74d148326448c8edb5fee81be3894 |

Licence: https://creativecommons.org/licenses/by/4.0/

## Fonts (SIL Open Font License 1.1)
| Font | Author | Use |
|---|---|---|
| Bebas Neue | Ryoichi Tsunekawa (Dharma Type) | the game's name and headings |
| Barlow | Jeremy Tribby | all other text |

The licence texts are in `unity/Assets/_Game/Resources/Fonts/OFL-*.txt`. The fonts are used unmodified.

## Tank photographs in the depot (Wikimedia Commons, public domain / CC0)
Wartime photographs shown on the tank tiles in the depot's garage list. All public domain (US Army Signal Corps, RIA Novosti archive, Archives de Normandie) or CC0, cropped.

| Tank | File on Commons | Author / source |
|---|---|---|
| M4 Sherman | SC 191908-S - A General Sherman tank ... near Tribehou, France, 25 July 1944 | US Army Signal Corps |
| M4A3E8 Easy Eight | M4A3E8.JPG | Yellowute (public domain) |
| M18 Hellcat | M18 tank destroyer fires its 90mm gun ... Brest, France, September 1944 | US Army Signal Corps |
| M24 Chaffee | Side view of the new M-24 tank ... | US Army Signal Corps |
| M26 Pershing | M26 Pershing at Remagen.png | US Army Signal Corps |
| M10 Wolverine | SC 199025 - An M10 tank destroyer ... Berle, Luxembourg | US Army Signal Corps |
| Sherman Firefly | Sherman Firefly in Field.jpg | Conseil Régional de Basse-Normandie / Archives Nationales du Canada |
| T-34-85 | Колонна советских танков Т-34-85 в городе Швехат в пригороде Вены | Yevgeny Khaldei, RIA Novosti (public domain) |
| KV-1 | Soviet tank KV-1 model 1939.jpg | public domain |
| SU-100 | СУ-101 и СУ-100 танкодром Уралмашзавода апрель 1945 | Техотдел УЗТМ (CC0) |
| IS-2 | САУ ИСУ-122 и танк ИС-2 на одной из дорог Трансильвании | Israel Ozersky, RIA Novosti (public domain) |
