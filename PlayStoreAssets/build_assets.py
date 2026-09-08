from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parent
SOURCE = ROOT / "source"
FINAL = ROOT / "final"

CREAM = "#FBF7F0"
SURFACE = "#FFFDFC"
CORAL = "#EB5B43"
MUSTARD = "#F4B63F"
SAGE = "#59B7A4"
INK = "#242322"
MUTED = "#6D6964"
BORDER = "#E9E0D6"

FONT_REGULAR = PROJECT / "Palabravo/Resources/Fonts/OpenSans-Regular.ttf"
FONT_SEMIBOLD = PROJECT / "Palabravo/Resources/Fonts/OpenSans-Semibold.ttf"
FONT_SERIF = PROJECT / "Palabravo/Resources/Fonts/Lora-Variable.ttf"
MASCOT = PROJECT / "Palabravo/Resources/Images/palabravo_mascot.png"
APP_ICON_SOURCE = (
    PROJECT
    / "Palabravo/obj/Debug/net10.0-ios/iossimulator-arm64/resizetizer/r/Assets.xcassets"
    / "appicon.appiconset/appiconItunesArtwork.png"
)


def font(path: Path, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(path), size=size)


def cover(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    return ImageOps.fit(image.convert("RGB"), size, method=Image.Resampling.LANCZOS)


def contain(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    copy = image.copy()
    copy.thumbnail(size, Image.Resampling.LANCZOS)
    return copy


def rounded_image(image: Image.Image, radius: int) -> Image.Image:
    result = image.convert("RGBA")
    mask = Image.new("L", result.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, *result.size), radius=radius, fill=255)
    result.putalpha(mask)
    return result


def paste_with_shadow(
    canvas: Image.Image,
    image: Image.Image,
    position: tuple[int, int],
    radius: int,
    blur: int = 28,
) -> None:
    rounded = rounded_image(image, radius)
    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    shadow_mask = Image.new("L", rounded.size, 0)
    ImageDraw.Draw(shadow_mask).rounded_rectangle(
        (0, 0, *rounded.size), radius=radius, fill=110
    )
    shadow_shape = Image.new("RGBA", rounded.size, (36, 35, 34, 0))
    shadow_shape.putalpha(shadow_mask)
    shadow.alpha_composite(shadow_shape, (position[0], position[1] + 16))
    shadow = shadow.filter(ImageFilter.GaussianBlur(blur))
    canvas.alpha_composite(shadow)
    canvas.alpha_composite(rounded, position)


def draw_tile_row(draw: ImageDraw.ImageDraw, x: int, y: int) -> None:
    for index, color in enumerate((CORAL, MUSTARD, SAGE, "#6674D9")):
        left = x + index * 54
        draw.rounded_rectangle((left, y, left + 38, y + 38), radius=10, fill=color)


def current_game_screenshot() -> Image.Image:
    """Bring the supplied test capture in line with the current three-attempt UI."""
    image = Image.open(SOURCE / "game-long-word.png").convert("RGB")
    draw = ImageDraw.Draw(image)

    # Current code allows three errors and keeps long words on one line.
    draw.rectangle((48, 246, 672, 296), fill=SURFACE)
    for index in range(3):
        center_x = 77 + index * 54
        box = (center_x - 12, 257, center_x + 12, 281)
        if index < 2:
            draw.ellipse(box, fill=CORAL)
        else:
            draw.ellipse(box, fill=SURFACE, outline=CORAL, width=2)
    error_font = font(FONT_SEMIBOLD, 31)
    draw.text((599, 251), "1 / 3", font=error_font, fill="#3E9B61", anchor="ma")

    tile = (368, 937, 520, 1051)
    draw.rounded_rectangle(tile, radius=18, fill=CORAL)
    tile_font = font(FONT_SEMIBOLD, 15)
    draw.text(
        ((tile[0] + tile[2]) // 2, (tile[1] + tile[3]) // 2),
        "VIOLONCHELO",
        font=tile_font,
        fill="white",
        anchor="mm",
    )
    return image


def build_icon() -> None:
    source = Image.open(APP_ICON_SOURCE).convert("RGB")
    icon = source.resize((512, 512), Image.Resampling.LANCZOS)
    draw = ImageDraw.Draw(icon)
    smile = []
    for step in range(25):
        t = step / 24
        x = 248 * (1 - t) ** 2 + 268 * 2 * (1 - t) * t + 288 * t**2
        y = 408 * (1 - t) ** 2 + 428 * 2 * (1 - t) * t + 408 * t**2
        smile.append((round(x), round(y)))
    draw.line(smile, fill=INK, width=10, joint="curve")
    draw.ellipse((243, 403, 253, 413), fill=INK)
    draw.ellipse((283, 403, 293, 413), fill=INK)
    icon.save(FINAL / "app-icon-512.png", optimize=True)


def build_feature(locale: str, tagline: str) -> None:
    background = cover(
        Image.open(SOURCE / "feature-background-generated.png"), (1024, 500)
    ).convert("RGBA")
    draw = ImageDraw.Draw(background)

    panel = Image.new("RGBA", (590, 310), (255, 253, 252, 220))
    panel = rounded_image(panel, 42)
    background.alpha_composite(panel, (54, 94))

    title_font = font(FONT_SEMIBOLD, 69)
    tagline_font = font(FONT_SERIF, 35)
    draw.text((95, 132), "Palabravo", font=title_font, fill=INK)
    draw.multiline_text(
        (98, 226), tagline,
        font=tagline_font,
        fill=INK,
        spacing=8,
    )
    draw_tile_row(draw, 98, 340)

    mascot = contain(Image.open(MASCOT).convert("RGBA"), (380, 455))
    background.alpha_composite(mascot, (665, 38))
    background.convert("RGB").save(
        FINAL / f"feature-graphic-{locale}.png", optimize=True
    )


def build_phone_screenshot(
    locale: str,
    number: int,
    headline: str,
    crop_box: tuple[int, int, int, int] | None = None,
    supporting_line: str | None = None,
) -> None:
    canvas = Image.new("RGBA", (1080, 1920), CREAM)
    draw = ImageDraw.Draw(canvas)

    draw.ellipse((-220, -300, 460, 360), fill="#FCE4DE")
    draw.ellipse((850, 20, 1210, 380), fill="#FFF0C8")
    draw.rounded_rectangle((68, 48, 270, 108), radius=30, fill=CORAL)
    draw.text((101, 62), "PALABRAVO", font=font(FONT_SEMIBOLD, 27), fill="white")

    headline_font = font(FONT_SERIF, 53 if len(headline) < 29 else 47)
    text_box = draw.multiline_textbbox((0, 0), headline, font=headline_font, spacing=4)
    text_width = text_box[2] - text_box[0]
    draw.multiline_text(
        ((1080 - text_width) // 2, 127),
        headline,
        font=headline_font,
        fill=INK,
        spacing=4,
        align="center",
    )

    source = current_game_screenshot()
    if crop_box:
        source = source.crop(crop_box)
        target_width = 840
        target_height = round(source.height * target_width / source.width)
        screenshot = source.resize(
            (target_width, target_height), Image.Resampling.LANCZOS
        )
        paste_with_shadow(canvas, screenshot, (120, 270), radius=46)
        if supporting_line:
            line_font = font(FONT_SEMIBOLD, 36)
            draw.text(
                (540, 1732), supporting_line,
                font=line_font,
                fill=MUTED,
                anchor="ma",
            )
            draw_tile_row(draw, 432, 1785)
    else:
        screenshot = source.resize((740, 1644), Image.Resampling.LANCZOS)
        paste_with_shadow(canvas, screenshot, (170, 248), radius=46)

    canvas.convert("RGB").save(
        FINAL / f"phone-{number:02d}-{locale}.png", optimize=True
    )


def main() -> None:
    FINAL.mkdir(parents=True, exist_ok=True)
    build_icon()

    build_feature("es-419", "16 palabras.\n4 conexiones.")
    build_feature("en-US", "Practice Spanish.\nFind the connections.")

    build_phone_screenshot(
        "es-419", 1, "16 palabras. 4 conexiones."
    )
    build_phone_screenshot(
        "es-419", 2, "Elige. Conecta. Resuelve.",
        crop_box=(0, 190, 720, 1340),
        supporting_line="30 retos · 6 rangos",
    )
    build_phone_screenshot(
        "en-US", 1, "Practice Spanish through play"
    )
    build_phone_screenshot(
        "en-US", 2, "Build your Spanish vocabulary",
        crop_box=(0, 190, 720, 1340),
        supporting_line="30 puzzles · 6 ranks",
    )


if __name__ == "__main__":
    main()
