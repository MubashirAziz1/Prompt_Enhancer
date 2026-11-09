# Blue liquid floating button

Replace the rectangular floating button with a glossy blue liquid sphere inspired
by `reference/3D-blob button.jpeg`. Keep opening the prompt editor, dragging,
keyboard activation, and the Hide/Quit context menu.

## Design

Cobalt #1457D9, deep blue #082B83, cyan #71CFFF, ice #D9F5FF, white #F8FDFF.
The existing Segoe UI Variable editor typography remains; the sphere has no text.
Its accessible name and tooltip explain its action.

Layout: a centered organic sphere in a 96 by 96 logical-pixel native window.
The native window region follows the silhouette so desktop corners remain clear.

```text
     .------.
   /  soft   \
  | highlight |   click: open editor
  |   fold    |   drag: move
   \________/    right-click: Hide / Quit
```

Spend the visual emphasis on volume: a softly folded surface, blue depth,
studio reflections, and a fine illuminated rim. Pointer movement changes the
surface and reflections; pressing compresses it with a short spring return.
No perpetual idle animation. Respect Windows animation settings and high contrast.

Review: remove the original text stack and avoid adding an unrelated icon,
ambient particles, or a surrounding card. Those would obscure the reference's
uninterrupted liquid surface.
