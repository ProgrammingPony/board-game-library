"""
Generates the Board Game Library application icon.

Design: a checkerboard sits in the bottom-right; a computer mouse is layered on
top of it in the top-left. The whole mark rides on a bright rounded-square
background so it stays clearly legible against the dark Windows taskbar when
night mode is enabled (a transparent / dark-only mark would disappear there).

Output: a single high-res PNG that Unity uses as the Standalone "Default Icon"
(Unity downsamples it for the .exe and taskbar). Drawn at 4x supersampling and
downscaled for clean anti-aliasing.
"""

import os

from PIL import Image, ImageDraw

S = 4                      # supersample factor
SIZE = 1024                # final icon size
W = SIZE * S

# --- palette (chosen for contrast on BOTH dark and light taskbars) ---
BG_TOP      = (28, 196, 178)   # bright teal
BG_BOT      = (16, 138, 138)   # deeper teal
RIM         = (245, 245, 245)  # light rim so the tile edge reads on dark mode
BOARD_LIGHT = (244, 228, 193)  # cream
BOARD_DARK  = (179, 58, 58)    # checkers red
BOARD_FRAME = (74, 38, 16)     # dark wood frame
MOUSE_BODY  = (250, 250, 250)  # white
MOUSE_LINE  = (38, 38, 38)     # outline
MOUSE_SHADE = (210, 214, 218)  # subtle body shading
WHEEL       = (255, 194, 60)   # amber accent
SHADOW      = (0, 0, 0, 90)    # soft drop shadow under the mouse


def rounded_rect_mask(size, radius):
    m = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(m)
    d.rounded_rectangle([0, 0, size - 1, size - 1], radius=radius, fill=255)
    return m


def vertical_gradient(size, top, bot):
    base = Image.new("RGB", (1, size))
    for y in range(size):
        t = y / (size - 1)
        base.putpixel((0, y), tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)))
    return base.resize((size, size))


def main():
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))

    # --- background rounded tile with rim ---
    margin = int(W * 0.045)
    tile = W - 2 * margin
    radius = int(tile * 0.22)

    grad = vertical_gradient(tile, BG_TOP, BG_BOT).convert("RGBA")
    mask = rounded_rect_mask(tile, radius)
    grad.putalpha(mask)
    img.paste(grad, (margin, margin), grad)

    # light rim stroke around the tile
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(
        [margin, margin, margin + tile - 1, margin + tile - 1],
        radius=radius, outline=RIM, width=int(W * 0.012),
    )

    # --- checkerboard, bottom-right ---
    # board occupies the lower-right portion of the tile
    b_size = int(W * 0.52)
    bx = int(W * 0.42)
    by = int(W * 0.42)
    frame = int(b_size * 0.06)
    d.rounded_rectangle(
        [bx, by, bx + b_size, by + b_size],
        radius=int(b_size * 0.05), fill=BOARD_FRAME,
    )

    inner_x = bx + frame
    inner_y = by + frame
    inner = b_size - 2 * frame
    n = 4
    cell = inner / n
    for r in range(n):
        for c in range(n):
            color = BOARD_LIGHT if (r + c) % 2 == 0 else BOARD_DARK
            x0 = inner_x + c * cell
            y0 = inner_y + r * cell
            d.rectangle([x0, y0, x0 + cell, y0 + cell], fill=color)

    # a couple of checker pieces for readability of the "checkers" idea
    def piece(cx, cy, fill, edge):
        rad = cell * 0.34
        d.ellipse([cx - rad, cy - rad * 0.95, cx + rad, cy + rad * 1.1], fill=edge)
        d.ellipse([cx - rad, cy - rad * 1.15, cx + rad, cy + rad * 0.9], fill=fill)
    # red piece on a light square, cream piece on a dark square
    piece(inner_x + cell * 0.5, inner_y + cell * 3.5, (40, 40, 40), (10, 10, 10))
    piece(inner_x + cell * 3.5, inner_y + cell * 0.5, BOARD_LIGHT, (150, 140, 110))

    # --- computer mouse, top-left, layered ABOVE the board ---
    # soft drop shadow first
    shadow_layer = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    ds = ImageDraw.Draw(shadow_layer)
    mx = int(W * 0.155)
    my = int(W * 0.145)
    mw = int(W * 0.34)
    mh = int(W * 0.46)
    off = int(W * 0.018)
    ds.rounded_rectangle(
        [mx + off, my + off, mx + mw + off, my + mh + off],
        radius=int(mw * 0.5), fill=SHADOW,
    )
    shadow_layer = shadow_layer.filter_blur() if hasattr(shadow_layer, "filter_blur") else shadow_layer
    from PIL import ImageFilter
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(int(W * 0.012)))
    img = Image.alpha_composite(img, shadow_layer)
    d = ImageDraw.Draw(img)

    lw = int(W * 0.016)
    # mouse body (rounded, slightly egg-shaped: narrower at top)
    d.rounded_rectangle(
        [mx, my, mx + mw, my + mh],
        radius=int(mw * 0.5), fill=MOUSE_BODY, outline=MOUSE_LINE, width=lw,
    )
    # subtle right-side shading
    shade = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    dsh = ImageDraw.Draw(shade)
    dsh.rounded_rectangle(
        [mx + mw * 0.52, my + mh * 0.18, mx + mw, my + mh],
        radius=int(mw * 0.45), fill=MOUSE_SHADE + (120,),
    )
    smask = Image.new("L", (W, W), 0)
    dm = ImageDraw.Draw(smask)
    dm.rounded_rectangle([mx, my, mx + mw, my + mh], radius=int(mw * 0.5), fill=255)
    img.paste(shade, (0, 0), Image.composite(shade, Image.new("RGBA", (W, W), (0, 0, 0, 0)), smask))
    d = ImageDraw.Draw(img)
    # re-stroke outline on top of shading
    d.rounded_rectangle(
        [mx, my, mx + mw, my + mh],
        radius=int(mw * 0.5), outline=MOUSE_LINE, width=lw,
    )

    # button split line (left/right buttons)
    split_y = my + mh * 0.42
    d.line([mx + mw * 0.5, my + lw, mx + mw * 0.5, split_y], fill=MOUSE_LINE, width=lw)
    d.line([mx + lw, split_y, mx + mw - lw, split_y], fill=MOUSE_LINE, width=lw)

    # scroll wheel (amber accent)
    ww = mw * 0.12
    wh = mh * 0.13
    wcx = mx + mw * 0.5
    wcy = my + mh * 0.2
    d.rounded_rectangle(
        [wcx - ww, wcy - wh, wcx + ww, wcy + wh],
        radius=int(ww * 0.8), fill=WHEEL, outline=MOUSE_LINE, width=int(lw * 0.8),
    )

    # downsample and write into the Unity project (path resolved relative to
    # this script: tools/icons/ -> repo root -> BoardGameLibrary/Assets/Icons)
    out = img.resize((SIZE, SIZE), Image.LANCZOS)
    repo_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    path = os.path.join(repo_root, "BoardGameLibrary", "Assets", "Icons", "AppIcon.png")
    out.save(path)
    print("wrote", path)


if __name__ == "__main__":
    main()
