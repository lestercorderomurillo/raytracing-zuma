# Original generated artwork

These four images were newly generated for this project with the built-in `image_gen` tool. All requests used text descriptions, with no input images, existing game sprites, ripped artwork, or original-game screenshots. Their art direction is an invented archaeological fantasy inspired by Maya architectural forms, jade, bronze, stepped fretwork, and limestone; it is not a historical reconstruction.

| Source asset | Use |
| --- | --- |
| `frog-guardian.png` | Transparent turquoise jade and bronze frog guardian; rotates toward the aim point. |
| `marbles-roll.png` | Four colors × eight rolling frames. Each color has a distinct engraved motif. |
| `temple-courtyard.png` | Painted stone floor, serpent reliefs, stepped borders, and jungle plants. |
| `temple-props.png` | Pedestal, entrance, exit, and decorative medallion in a 2 × 2 atlas. |

The complete generation prompts are preserved in [prompts.json](prompts.json). The source PNGs are kept unedited, including their alpha channels and generation metadata. MGCB converts them into runtime textures with premultiplied alpha and mipmaps for clean scaling at game size.

[atlas.json](atlas.json) records individual source rectangles. Marble bounds were measured from the opaque connected components rather than assuming the generator produced pixel-exact cells. All 32 frames are normalized to the same displayed diameter, without slicing or resaving the source artwork. The temple props use the sheet's four quadrants.

Marble animation is driven by traveled distance, with one revolution per marble circumference. It freezes with gameplay, follows the path tangent, and runs backward when the chain retracts. Flying projectiles use their distance from the launcher. The loaded and queued previews use the same generated sprites as the chain.

Lighting still uses Radiant's moving emissive circles and light-blocking geometry. The generated art supplies the visible surfaces; collision radii and matching rules remain independent of image dimensions.
