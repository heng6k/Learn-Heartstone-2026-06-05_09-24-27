import unittest

from serve_webgl import IMMUTABLE_CACHE, REVALIDATE_CACHE, cache_control_for_path


class CacheControlTests(unittest.TestCase):
    def test_only_versioned_assets_are_immutable(self) -> None:
        self.assertEqual(IMMUTABLE_CACHE, cache_control_for_path("/Build/game.wasm.br"))
        self.assertEqual(IMMUTABLE_CACHE, cache_control_for_path("/Build/game.loader.js"))
        self.assertEqual(IMMUTABLE_CACHE, cache_control_for_path("/content/minions.v20260727.json"))
        self.assertEqual(REVALIDATE_CACHE, cache_control_for_path("/"))
        self.assertEqual(REVALIDATE_CACHE, cache_control_for_path("/index.html"))
        self.assertEqual(REVALIDATE_CACHE, cache_control_for_path("/TemplateData/style.css"))
        self.assertEqual(REVALIDATE_CACHE, cache_control_for_path("/release-meta.json?x=1"))
        self.assertEqual(REVALIDATE_CACHE, cache_control_for_path("/content/content-manifest.json"))


if __name__ == "__main__":
    unittest.main()
