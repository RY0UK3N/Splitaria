from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
BRAND = ROOT / "assets" / "brand"
SOURCE = BRAND / "splitaria-mark-minimal-master.png"


def square_canvas(source: Image.Image) -> Image.Image:
    rgba = source.convert("RGBA")
    alpha = rgba.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise RuntimeError("The brand source has no visible pixels.")
    cropped = rgba.crop(bounds)
    side = max(cropped.size)
    padding = max(24, round(side * 0.07))
    canvas = Image.new("RGBA", (side + padding * 2, side + padding * 2), (0, 0, 0, 0))
    canvas.alpha_composite(cropped, ((canvas.width - cropped.width) // 2, (canvas.height - cropped.height) // 2))
    return canvas


def main() -> None:
    BRAND.mkdir(parents=True, exist_ok=True)
    with Image.open(SOURCE) as image:
        master = square_canvas(image)

    generated: dict[int, Image.Image] = {}
    for size in (16, 24, 32, 48, 64, 128, 256, 512):
        generated[size] = master.resize((size, size), Image.Resampling.LANCZOS)
        generated[size].save(BRAND / f"splitaria-mark-{size}.png", optimize=True)

    generated[256].save(
        BRAND / "Splitaria.ico",
        format="ICO",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )

    print(f"Generated brand assets in {BRAND}")
    print(f"Source mode: {master.mode}; transparent alpha: {master.getchannel('A').getextrema()[0] == 0}")


if __name__ == "__main__":
    main()
