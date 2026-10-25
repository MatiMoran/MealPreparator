#!/usr/bin/env python3
"""Download Da Factory's menu catalog and images, then write a YAML snapshot."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlparse
from urllib.request import Request, urlopen


SOURCE_URL = "https://dafactory.com.ar/viandas/"
PROJECT_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUTPUT = PROJECT_ROOT / "data" / "dafactory_platos.yaml"
DEFAULT_UNIFIED_OUTPUT = PROJECT_ROOT / "data" / "dafactory_recetas_unificadas.yaml"
DEFAULT_IMAGES_DIR = PROJECT_ROOT / "data" / "dafactory_images"
DATA_ASSIGNMENT = re.compile(r"\bvar\s+DAFC_DATA\s*=\s*")


def fetch_page(url: str = SOURCE_URL) -> str:
    request = Request(
        url,
        headers={
            "User-Agent": "MealPreparator Da Factory menu scraper/1.0",
            "Accept": "text/html",
        },
    )
    with urlopen(request, timeout=30) as response:
        charset = response.headers.get_content_charset() or "utf-8"
        return response.read().decode(charset, errors="replace")


def extract_catalog(html: str) -> dict:
    assignment = DATA_ASSIGNMENT.search(html)
    if assignment is None:
        raise ValueError("The page does not contain the DAFC_DATA catalog.")

    try:
        catalog, _ = json.JSONDecoder().raw_decode(html, assignment.end())
    except json.JSONDecodeError as error:
        raise ValueError("The DAFC_DATA catalog is not valid JSON.") from error

    menus = catalog.get("tipos") if isinstance(catalog, dict) else None
    if not isinstance(menus, list) or not menus:
        raise ValueError("The DAFC_DATA catalog has no menu types.")
    return catalog


def build_yaml_data(catalog: dict) -> dict:
    menus = []
    for menu in catalog["tipos"]:
        label = menu.get("label")
        dishes_by_category = menu.get("dishes")
        if not isinstance(label, str) or not isinstance(dishes_by_category, dict):
            continue

        categories = {}
        for category, dishes in dishes_by_category.items():
            if not isinstance(category, str) or not isinstance(dishes, list):
                continue
            entries = [
                {
                    "nombre": dish["n"].strip(),
                    "imagen": dish.get("local_image"),
                }
                for dish in dishes
                if isinstance(dish, dict)
                and isinstance(dish.get("n"), str)
                and dish["n"].strip()
            ]
            categories[category] = entries

        menu_data = {"tipo": label}
        description = menu.get("desc")
        if isinstance(description, str) and description.strip():
            menu_data["descripcion"] = description.strip()
        menu_data["categorias"] = categories
        menus.append(menu_data)

    if not menus:
        raise ValueError("No usable menu types were found in the DAFC_DATA catalog.")
    return {"carta": {"menus": menus}}


def image_reference(path: str | None, relative_to: Path) -> str | None:
    if path is None:
        return None
    return Path(os.path.relpath(path, relative_to)).as_posix()


def build_unified_data(catalog: dict, local_images: dict[str, str], output_dir: Path) -> dict:
    categories = {}
    seen_names = set()

    for menu in catalog["tipos"]:
        for category, dishes in menu.get("dishes", {}).items():
            if not isinstance(category, str) or not isinstance(dishes, list):
                continue
            category_dishes = categories.setdefault(category, {})

            for dish in dishes:
                if not isinstance(dish, dict) or not isinstance(dish.get("n"), str):
                    continue
                name = dish["n"].strip()
                if not name:
                    continue

                source_image = dish.get("img", "").strip()
                local_image = image_reference(
                    local_images.get(source_image), output_dir
                )

                if name in seen_names:
                    if name in category_dishes and category_dishes[name] is None and local_image:
                        category_dishes[name] = local_image
                    continue

                seen_names.add(name)
                category_dishes[name] = local_image

    return {"categorias": categories}


def image_urls(catalog: dict) -> list[str]:
    urls = set()
    for menu in catalog["tipos"]:
        for dishes in menu.get("dishes", {}).values():
            for dish in dishes:
                url = dish.get("img", "").strip()
                if url:
                    urls.add(url)
    return sorted(urls)


def image_filename(url: str) -> str:
    parsed = urlparse(url)
    suffix = Path(parsed.path).suffix.lower()
    if suffix not in {".jpg", ".jpeg", ".png", ".webp", ".gif"}:
        suffix = ".img"
    digest = hashlib.sha256(url.encode("utf-8")).hexdigest()[:16]
    return f"{digest}{suffix}"


def download_images(
    urls: list[str], images_dir: Path
) -> tuple[dict[str, str], list[str]]:
    images_dir.mkdir(parents=True, exist_ok=True)
    local_images = {}
    failed_urls = []

    def download_one(url: str) -> tuple[str, str | None, str | None]:
        destination = images_dir / image_filename(url)
        absolute_path = str(destination.resolve())
        if destination.is_file() and destination.stat().st_size > 0:
            return url, absolute_path, None

        try:
            request = Request(
                quote(url, safe=":/?#[]@!$&'()*+,;=%"),
                headers={"User-Agent": "MealPreparator Da Factory menu scraper/1.0"},
            )
            with urlopen(request, timeout=30) as response:
                content_type = response.headers.get_content_type()
                if not content_type.startswith("image/"):
                    raise ValueError(f"unexpected content type: {content_type}")
                image_bytes = response.read()
            if not image_bytes:
                raise ValueError("empty image response")
            destination.write_bytes(image_bytes)
            return url, absolute_path, None
        except (HTTPError, URLError, OSError, ValueError) as error:
            return url, None, str(error)

    with ThreadPoolExecutor(max_workers=6) as executor:
        futures = [executor.submit(download_one, url) for url in urls]
        for future in as_completed(futures):
            url, local_path, error = future.result()
            if error:
                failed_urls.append(url)
                print(f"Warning: could not download {url}: {error}", file=sys.stderr)
            elif local_path:
                local_images[url] = local_path

    return local_images, failed_urls


def yaml_scalar(value: str) -> str:
    return json.dumps(value, ensure_ascii=False)


def render_yaml(data: dict) -> str:
    lines = ["carta:", "  menus:"]
    for menu in data["carta"]["menus"]:
        lines.append(f"    - tipo: {yaml_scalar(menu['tipo'])}")
        if "descripcion" in menu:
            lines.append(f"      descripcion: {yaml_scalar(menu['descripcion'])}")
        lines.append("      categorias:")
        for category, entries in menu["categorias"].items():
            if entries:
                lines.append(f"        {yaml_scalar(category)}:")
                for entry in entries:
                    lines.append(f"          - nombre: {yaml_scalar(entry['nombre'])}")
                    image = entry["imagen"]
                    lines.append(
                        f"            imagen: {yaml_scalar(image) if image else 'null'}"
                    )
            else:
                lines.append(f"        {yaml_scalar(category)}: []")
    return "\n".join(lines) + "\n"


def render_unified_yaml(data: dict) -> str:
    lines = ["categorias:"]
    for category, dishes in data["categorias"].items():
        lines.append(f"  {yaml_scalar(category)}:")
        for name, image in dishes.items():
            image_value = yaml_scalar(image) if image else "null"
            lines.append(f"    {yaml_scalar(name)}: {image_value}")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Scrape Da Factory's menu dish names into a YAML file."
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help=f"YAML output path (default: {DEFAULT_OUTPUT})",
    )
    parser.add_argument(
        "--unified-output",
        type=Path,
        default=DEFAULT_UNIFIED_OUTPUT,
        help=f"Deduplicated YAML output path (default: {DEFAULT_UNIFIED_OUTPUT})",
    )
    parser.add_argument(
        "--images-dir",
        type=Path,
        default=DEFAULT_IMAGES_DIR,
        help=f"Directory for downloaded images (default: {DEFAULT_IMAGES_DIR})",
    )
    args = parser.parse_args()

    try:
        html = fetch_page()
        catalog = extract_catalog(html)
        output = args.output.expanduser().resolve()
        unified_output = args.unified_output.expanduser().resolve()
        images_dir = args.images_dir.expanduser().resolve()
        local_images, failed_urls = download_images(
            image_urls(catalog), images_dir
        )
        for menu in catalog["tipos"]:
            for dishes in menu.get("dishes", {}).values():
                for dish in dishes:
                    image_url = dish.get("img", "").strip()
                    dish["local_image"] = image_reference(
                        local_images.get(image_url), output.parent
                    )
        yaml_data = build_yaml_data(catalog)
        unified_data = build_unified_data(
            catalog, local_images, unified_output.parent
        )
        output.parent.mkdir(parents=True, exist_ok=True)
        unified_output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(render_yaml(yaml_data), encoding="utf-8")
        unified_output.write_text(
            render_unified_yaml(unified_data), encoding="utf-8"
        )
    except (OSError, URLError, ValueError) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1

    menu_count = len(yaml_data["carta"]["menus"])
    dish_count = sum(
        len(entries)
        for menu in yaml_data["carta"]["menus"]
        for entries in menu["categorias"].values()
    )
    print(
        f"Wrote {dish_count} dish entries across {menu_count} menus; "
        f"{len(local_images)} unique images are available locally in {images_dir}."
    )
    if failed_urls:
        print(
            f"{len(failed_urls)} image URLs failed; those entries have imagen: null.",
            file=sys.stderr,
        )
    missing_image_count = dish_count - sum(
        1
        for menu in yaml_data["carta"]["menus"]
        for entries in menu["categorias"].values()
        for entry in entries
        if entry["imagen"]
    )
    if missing_image_count:
        print(
            f"{missing_image_count} dish entries have no local image "
            "(the source has no image URL or the download failed)."
        )
    print(f"YAML written to {output}")
    unique_recipe_count = sum(
        len(dishes) for dishes in unified_data["categorias"].values()
    )
    print(f"Unified YAML written to {unified_output} ({unique_recipe_count} unique recipes)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
