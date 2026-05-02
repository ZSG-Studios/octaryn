# Third-Party Notices

This documentation site vendors a small PrismJS subset for local syntax highlighting.

## PrismJS

- Package: `prismjs`
- Version: `1.30.0`
- Repository: <https://github.com/PrismJS/prism>
- License: MIT
- Vendored files:
  - `docs/assets/vendor/prism/prism-core.min.js`
  - `docs/assets/vendor/prism/prism-clike.min.js`
  - `docs/assets/vendor/prism/prism-csharp.min.js`
  - `docs/assets/vendor/prism/LICENSE`

## Texture Pack Sources

No third-party texture pack image assets are currently vendored in this repository.

The basegame atlas builder can import Minecraft-style resource packs. Generated atlas assets must keep release metadata for the exact pack source, pack version or commit, SHA-256, license, required credit, and required license file.

### Classic Faithful 32x Jappa

- Status: default import source for `octaryn-basegame/Tools/build_atlas_from_pack.py` when `--pack` is omitted.
- Repository: <https://github.com/ClassicFaithful/Classic-32x-Jappa-Java>
- Website: <https://faithfulpack.net/>
- Credits: <https://github.com/ClassicFaithful/Classic-32x-Jappa-Java/blob/main/credits.txt>
- License: Faithful License Version 3, <https://faithfulpack.net/license>
- Release rule: generated assets containing this work must include clear credit, link back to Faithful, avoid monetization of content containing the work, avoid implying official Faithful status, and include the unmodified Faithful license file with any distributed content that contains the work.
